import type { PagedResponse } from '~/types/api'
import type { DueReviewItem, RateReviewRequest, ReviewResult } from './review.types'

export function useReviewApi() {
  const api = useApi()
  return {
    getDue: (take = 20) => api.get<PagedResponse<DueReviewItem>>('/review-items/due', { page: 1, pageSize: Math.min(50, take) }),
    rate: (reviewItemId: string, request: RateReviewRequest) => api.post<ReviewResult>(`/review-items/${reviewItemId}/evaluate`, request),
  }
}
