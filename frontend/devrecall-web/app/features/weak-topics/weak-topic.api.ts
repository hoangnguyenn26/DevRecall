import type { PagedResponse } from '~/types/api'
import type { WeakTopicDetail, WeakTopicSummary } from './weak-topic.types'
export function useWeakTopicApi() {
  const api = useApi()
  return {
    list: (level = '') =>
      api.get<PagedResponse<WeakTopicSummary>>('/weak-topics', { level, page: 1, pageSize: 20 }),
    detail: (id: string) => api.get<WeakTopicDetail>(`/weak-topics/${id}`),
    recalculateAll: () => api.post('/weak-topics/recalculate-all'),
  }
}
