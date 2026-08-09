import type { DsaPractice, DsaPracticeResult } from './dsa-practice.types'
import type { CompleteDsaPractice } from './useDsaPractice'

export function useDsaPracticeApi() {
  const api = useApi()
  return {
    get: (problemId: string) => api.get<DsaPractice>(`/dsa-problems/${problemId}/practice`),
    complete: (problemId: string): CompleteDsaPractice => request => api.post<DsaPracticeResult>(`/dsa-problems/${problemId}/attempts`, request),
  }
}
