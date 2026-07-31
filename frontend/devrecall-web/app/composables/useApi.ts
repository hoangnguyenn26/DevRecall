import { ApiError, type ProblemDetails, type Query } from '~/types/api'

type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  query?: Query
  signal?: AbortSignal
}

let csrfToken: string | undefined
let csrfHeaderName = 'X-CSRF-TOKEN'

function appendQuery(url: URL, query?: Query) {
  if (!query) return
  for (const [key, raw] of Object.entries(query)) {
    const values = Array.isArray(raw) ? raw : [raw]
    for (const value of values) {
      if (value !== null && value !== undefined && value !== '') url.searchParams.append(key, String(value))
    }
  }
}

export function useApi() {
  const config = useRuntimeConfig()

  async function request<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
    const base = config.public.apiBaseUrl.replace(/\/$/, '')
    const url = new URL(
      `${base}/${path.replace(/^\//, '')}`,
      import.meta.client ? window.location.origin : 'http://localhost')
    appendQuery(url, options.query)
    const method = options.method ?? 'GET'
    if (!['GET', 'HEAD', 'OPTIONS'].includes(method) && !csrfToken) {
      const csrfResponse = await fetch(`${base}/auth/csrf-token`, {
        credentials: 'include',
        headers: { Accept: 'application/json' },
      })
      if (!csrfResponse.ok) throw new Error('Unable to establish request security.')
      const tokens = await csrfResponse.json() as { requestToken: string; headerName: string }
      csrfToken = tokens.requestToken
      csrfHeaderName = tokens.headerName
    }
    const headers: Record<string, string> = {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      'X-Correlation-ID': crypto.randomUUID(),
    }
    if (csrfToken && !['GET', 'HEAD', 'OPTIONS'].includes(method)) headers[csrfHeaderName] = csrfToken
    const response = await fetch(url, {
      method,
      credentials: 'include',
      signal: options.signal,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })

    if (!response.ok) {
      const problem = await response.json().catch(() => ({
        title: 'Request failed',
        status: response.status,
      })) as ProblemDetails
      if (problem.code === 'ANTIFORGERY_TOKEN_INVALID') csrfToken = undefined
      throw new ApiError({ ...problem, status: response.status })
    }
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  return {
    get: <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>(path, { query, signal }),
    post: <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>(path, { method: 'POST', body, signal }),
    put: <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>(path, { method: 'PUT', body, signal }),
    delete: <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>(path, { method: 'DELETE', query, signal }),
  }
}
