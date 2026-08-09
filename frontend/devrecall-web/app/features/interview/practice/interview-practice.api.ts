import type { InterviewPractice, InterviewPracticeResult } from './interview-practice.types'
import type { CompleteInterviewPractice } from './useInterviewPractice'

export function useInterviewPracticeApi() {
  const api = useApi()
  return {
    get: (questionId: string) => api.get<InterviewPractice>(`/interview-questions/${questionId}/practice`),
    complete: (questionId: string): CompleteInterviewPractice => request => api.post<InterviewPracticeResult>(`/interview-questions/${questionId}/attempts`, request),
  }
}
