import type { NormalizedApiError } from '~/utils/normalize-api-error'

export type AsyncStatus = 'idle' | 'pending' | 'success' | 'error'

export interface AsyncState<T> {
  status: AsyncStatus
  data: T | null
  error: NormalizedApiError | null
  refreshing: boolean
}

export function createAsyncState<T>(): AsyncState<T> {
  return { status: 'idle', data: null, error: null, refreshing: false }
}
