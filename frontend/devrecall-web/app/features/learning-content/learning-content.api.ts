import type { LearningContentDetail, LearningContentFilters, LearningContentPage } from './learning-content.types'

export function useLearningContentApi() {
  const api = useApi()
  return {
    list: (filters: LearningContentFilters, signal?: AbortSignal) => api.get<LearningContentPage>(
      '/learning-content', { technology: filters.technology, difficulty: filters.difficulty,
        page: filters.page, pageSize: 12 }, signal),
    detail: (slug: string, signal?: AbortSignal) => api.get<LearningContentDetail>(
      `/learning-content/${encodeURIComponent(slug)}`, undefined, signal),
  }
}
