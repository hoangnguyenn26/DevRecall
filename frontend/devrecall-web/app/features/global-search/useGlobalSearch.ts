import type { GlobalSearchResponse } from './global-search.types'

const debounceMs = 250

export function useGlobalSearch() {
  const api = useApi()
  const response = ref<GlobalSearchResponse | null>(null)
  const pending = ref(false)
  const error = ref<unknown>()
  let timer: ReturnType<typeof setTimeout> | undefined
  let controller: AbortController | undefined
  let sequence = 0

  function clear(): void {
    clearTimeout(timer)
    controller?.abort()
    sequence++
    response.value = null
    pending.value = false
    error.value = undefined
  }

  function search(value: string): void {
    clearTimeout(timer)
    controller?.abort()
    const query = value.trim()
    if (query.length < 2) {
      clear()
      return
    }
    response.value = null
    const currentSequence = ++sequence
    timer = setTimeout(async () => {
      controller = new AbortController()
      pending.value = true
      error.value = undefined
      try {
        const result = await api.get<GlobalSearchResponse>('/search', { q: query, takePerType: 5 }, controller.signal)
        if (currentSequence === sequence) response.value = result
      } catch (cause) {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        if (currentSequence === sequence) error.value = cause
      } finally {
        if (currentSequence === sequence) pending.value = false
      }
    }, debounceMs)
  }

  onScopeDispose(clear)
  return { response: readonly(response), pending: readonly(pending), error: readonly(error), search, clear }
}
