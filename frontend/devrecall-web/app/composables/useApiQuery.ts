import { computed, readonly, ref } from 'vue'
import type { NormalizedApiError } from '~/utils/normalize-api-error'
import { normalizeApiError } from '~/utils/normalize-api-error'

interface ApiQueryOptions<T> {
  immediate?: boolean
  initialData?: T
  onError?: (error: NormalizedApiError) => void
}

export function useApiQuery<T>(key: string, fetcher: () => Promise<T>, options: ApiQueryOptions<T> = {}) {
  const normalizedError = ref<NormalizedApiError | null>(null)
  const { data, status, refresh, clear } = useAsyncData<T>(key, async () => {
    normalizedError.value = null
    try {
      return await fetcher()
    } catch (error) {
      const normalized = normalizeApiError(error)
      normalizedError.value = normalized
      options.onError?.(normalized)
      throw error
    }
  }, {
    immediate: options.immediate ?? true,
    default: options.initialData === undefined ? undefined : () => options.initialData as T,
  })

  const hasData = computed(() => data.value !== null && data.value !== undefined)
  const isPending = computed(() => status.value === 'pending' && !hasData.value)
  const refreshing = computed(() => status.value === 'pending' && hasData.value)
  return { data, status, error: readonly(normalizedError), isPending, refreshing, hasData, refresh, clear }
}
