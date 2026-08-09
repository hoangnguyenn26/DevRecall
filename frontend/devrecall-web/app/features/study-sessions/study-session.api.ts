import type { StudySessionDetail } from './study-session.types'

export function useStudySessionApi() {
  const api = useApi()
  return {
    detail: (id: string) => api.get<StudySessionDetail>(`/study-sessions/${id}`),
    completeItem: (
      sessionId: string,
      itemId: string,
      expectedVersion: number,
      submissionId: string,
      evidenceId?: string,
    ) =>
      api.post(`/study-sessions/${sessionId}/items/${itemId}/complete`, {
        notes: null,
        expectedVersion,
        submissionId,
        evidenceId,
      }),
    skipItem: (sessionId: string, itemId: string, expectedVersion: number, submissionId: string) =>
      api.post(`/study-sessions/${sessionId}/items/${itemId}/skip`, {
        notes: null,
        expectedVersion,
        submissionId,
      }),
  }
}
