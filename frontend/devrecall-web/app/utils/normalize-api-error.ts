import { ApiError } from '~/types/api'
import type { ApiProblemDetails } from '~/types/problem-details'

export interface NormalizedApiError {
  status: number
  code: string
  title: string
  detail?: string
  fieldErrors: Record<string, string[]>
  traceId?: string
}

export function normalizeApiError(error: unknown): NormalizedApiError {
  const fallback: NormalizedApiError = {
    status: 500,
    code: 'UNEXPECTED_ERROR',
    title: 'Something went wrong.',
    fieldErrors: {},
  }

  const data = error instanceof ApiError
    ? error.problem
    : typeof error === 'object' && error !== null && 'data' in error
      ? (error as { data?: ApiProblemDetails }).data
      : undefined

  if (!data) return fallback

  return {
    status: data.status ?? 500,
    code: data.code ?? 'UNEXPECTED_ERROR',
    title: data.title ?? 'Something went wrong.',
    detail: data.detail,
    fieldErrors: data.errors ?? {},
    traceId: data.traceId,
  }
}
