import type { PagedResponse } from '~/types/api'
import type { StudyPlanDetail, StudyPlanEditState, StudyPlanListItem } from './study-plan.types'

export function useStudyPlanApi() {
  const api = useApi()
  return {
    list: (status = '', pageSize = 50) =>
      api.get<PagedResponse<StudyPlanListItem>>('/study-plans', { status, page: 1, pageSize }),
    detail: (id: string) => api.get<StudyPlanDetail>(`/study-plans/${id}`),
    addLearningContent: (id: string, slug: string, expectedVersion: number, submissionId: string) =>
      api.post<{ studyPlanId: string; itemId: string; planTitle: string; added: boolean; version: number }>(
        `/study-plans/${id}/learning-content/${encodeURIComponent(slug)}`,
        { expectedVersion, submissionId }),
    learningContentOptions: (slug: string) => api.get<Array<StudyPlanListItem & { alreadyContains: boolean }>>(
      `/study-plans/learning-content/${encodeURIComponent(slug)}/options`),
    saveDraft: (id: string, state: StudyPlanEditState) =>
      api.put(`/study-plans/${id}/draft`, {
        title: state.title,
        expectedVersion: state.expectedVersion,
        items: state.items.map((item) => ({
          itemId: item.itemId.startsWith('new:') ? null : item.itemId,
          resourceType: item.resourceType,
          resourceId: item.resourceId,
          plannedDurationMinutes: item.plannedDurationMinutes,
        })),
      }),
    markReady: (id: string, expectedVersion: number) =>
      api.post(`/study-plans/${id}/ready`, { expectedVersion }),
    start: (id: string, expectedVersion: number) =>
      api.post<{ studySessionId: string }>(`/study-plans/${id}/convert`, { expectedVersion }),
  }
}
