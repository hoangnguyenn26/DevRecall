import type { ContinueLearningContentItem, LearningContentDetail, LearningContentFilters, LearningContentHistoryPage, LearningContentPage, LearningContentProgress, LearningContentReviewBatch, SaveLessonToKnowledgeInput, SavedLessonKnowledge } from './learning-content.types'

export function useLearningContentApi() {
  const api = useApi()
  return {
    list: (filters: LearningContentFilters, signal?: AbortSignal) => api.get<LearningContentPage>(
      '/learning-content', { technology: filters.technology, difficulty: filters.difficulty,
        page: filters.page, pageSize: 12 }, signal),
    continueLearning: (signal?: AbortSignal) => api.get<ContinueLearningContentItem[]>(
      '/learning-content/continue', undefined, signal),
    history: (page: number, signal?: AbortSignal) => api.get<LearningContentHistoryPage>(
      '/learning-content/history', { page, pageSize: 20 }, signal),
    detail: (slug: string, signal?: AbortSignal) => api.get<LearningContentDetail>(
      `/learning-content/${encodeURIComponent(slug)}`, undefined, signal),
    start: (slug: string) => api.post<LearningContentProgress>(
      `/learning-content/${encodeURIComponent(slug)}/progress/start`),
    complete: (slug: string, expectedVersion: number | null) => api.post<LearningContentProgress>(
      `/learning-content/${encodeURIComponent(slug)}/progress/complete`, { expectedVersion }),
    saveToKnowledge: (slug: string, input: SaveLessonToKnowledgeInput) => api.post<SavedLessonKnowledge>(
      `/knowledge/from-learning-content/${encodeURIComponent(slug)}`, input),
    addToReview: (slug: string, candidateKeys: string[], submissionId: string) =>
      api.post<LearningContentReviewBatch>(`/review/from-learning-content/${encodeURIComponent(slug)}`,
        { candidateKeys, submissionId }),
  }
}
