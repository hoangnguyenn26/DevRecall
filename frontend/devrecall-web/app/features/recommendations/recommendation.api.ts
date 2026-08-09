import type { PagedResponse } from '~/types/api'
import type { RecommendationDetail, RecommendationSummary } from './recommendation.types'
export function useRecommendationApi() {
  const api = useApi()
  return {
    list: (status = 'Active', priority = '') =>
      api.get<PagedResponse<RecommendationSummary>>('/recommendations', {
        status,
        priority,
        page: 1,
        pageSize: 20,
      }),
    detail: (id: string) => api.get<RecommendationDetail>(`/recommendations/${id}`),
    dismiss: (id: string, expectedVersion: number) =>
      api.post(`/recommendations/${id}/dismiss`, { expectedVersion }),
    generate: () => api.post('/recommendations/generate', { maximumCandidates: 100 }),
  }
}
