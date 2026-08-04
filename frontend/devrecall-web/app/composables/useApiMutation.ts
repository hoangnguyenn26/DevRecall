import { readonly, ref } from 'vue'
import type { NormalizedApiError } from '~/utils/normalize-api-error'
import { normalizeApiError } from '~/utils/normalize-api-error'

interface ApiMutationOptions<TResponse> {
  successTitle?: string
  successDescription?: string
  showSuccessToast?: boolean
  onSuccess?: (response: TResponse) => void | Promise<void>
  onError?: (error: NormalizedApiError) => void
}

export function useApiMutation<TRequest, TResponse>(
  mutation: (request: TRequest) => Promise<TResponse>,
  options: ApiMutationOptions<TResponse> = {},
) {
  const pending = ref(false)
  const error = ref<NormalizedApiError | null>(null)
  const toast = useToast()

  async function execute(request: TRequest): Promise<TResponse | null> {
    if (pending.value) return null
    pending.value = true
    error.value = null
    try {
      const response = await mutation(request)
      if (options.showSuccessToast ?? Boolean(options.successTitle)) {
        toast.add({
          title: options.successTitle ?? 'Changes saved',
          description: options.successDescription,
          color: 'success',
          duration: 3500,
        })
      }
      await options.onSuccess?.(response)
      return response
    } catch (caught) {
      const normalized = normalizeApiError(caught)
      error.value = normalized
      options.onError?.(normalized)
      throw caught
    } finally {
      pending.value = false
    }
  }

  function clearError(): void {
    error.value = null
  }

  return { pending: readonly(pending), error: readonly(error), execute, clearError }
}
