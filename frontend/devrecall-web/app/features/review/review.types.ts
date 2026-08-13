export type ReviewRating = 'Again' | 'Hard' | 'Good' | 'Easy'
export type ReviewCardPhase = 'prompt' | 'answer' | 'submitting' | 'feedback' | 'completed'

export interface ReviewSource {
  type: 'LearningContent'
  title: string
  slug: string
  isAvailable: boolean
}

export interface DueReviewItem {
  reviewItemId: string
  resourceType: string
  resourceId: string
  resourceTitle: string
  resourcePreview?: string
  dueAtUtc: string
  lastReviewedAtUtc?: string
  intervalDays: number
  reviewCount: number
  overdueMinutes: number
  source?: ReviewSource
}

export interface ReviewResult {
  reviewItemId: string
  reviewHistoryId: string
  evaluation: ReviewRating
  previousIntervalDays: number
  nextIntervalDays: number
  reviewedAtUtc: string
  nextDueAtUtc: string
  reviewCount: number
}

export interface RateReviewRequest {
  evaluation: ReviewRating
  expectedReviewCount: number
  submissionId: string
}
