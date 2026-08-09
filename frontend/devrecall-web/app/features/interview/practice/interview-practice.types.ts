export type InterviewSelfRating = 'NeedsWork' | 'Fair' | 'Good' | 'Strong'
export type InterviewPracticePhase = 'answering' | 'comparing' | 'follow-up' | 'submitting' | 'completed'
export interface InterviewPracticeFollowUp { followUpId: string; question: string; referenceAnswer?: string }
export interface InterviewPractice { questionId: string; question: string; category?: string; difficulty?: string; referenceAnswer?: { answerId: string; content: string; version: number }; followUps: InterviewPracticeFollowUp[]; questionVersion: number }
export interface InterviewPracticeResult { attemptId: string; questionId: string; selfRating: InterviewSelfRating; followUpsAnswered: number; followUpsSkipped: number; durationSeconds: number; completedAtUtc: string }
