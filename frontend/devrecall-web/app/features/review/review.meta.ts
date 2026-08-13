import type { ReviewRating } from './review.types'

export interface ReviewRatingMeta { label: string; description: string; shortcut: string; color: 'error' | 'warning' | 'primary' | 'success' }

export const reviewRatings: ReviewRating[] = ['Again', 'Hard', 'Good', 'Easy']
export const reviewRatingMeta: Record<ReviewRating, ReviewRatingMeta> = {
  Again: { label: 'Again', description: 'I could not recall it', shortcut: '1', color: 'error' },
  Hard: { label: 'Hard', description: 'I recalled it with difficulty', shortcut: '2', color: 'warning' },
  Good: { label: 'Good', description: 'I recalled it correctly', shortcut: '3', color: 'primary' },
  Easy: { label: 'Easy', description: 'I recalled it immediately', shortcut: '4', color: 'success' },
}

export const reviewResourceLabels: Record<string, string> = {
  KnowledgeNode: 'Knowledge', InterviewQuestion: 'Interview', DsaProblem: 'DSA',
  LearningContent: 'Lesson concept',
}
