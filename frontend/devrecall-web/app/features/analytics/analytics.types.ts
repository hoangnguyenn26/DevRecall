export type AnalyticsRange = '7d' | '30d' | '90d'
export interface Comparison {
  current: number
  previous: number
  difference: number
}
export interface AnalyticsOverview {
  range: AnalyticsRange
  period: { startUtc: string; endUtc: string }
  studyMinutes: Comparison
  activeDays: Comparison
  studySessions: Comparison
  practiceActivities: Comparison
  reviewCount: number
  interviewAttemptCount: number
  dsaAttemptCount: number
  learningContentCompletedCount: number
  activity: { date: string; studyMinutes: number; practiceCount: number; lessonsCompleted: number }[]
  recentActivity: {
    type: 'LearningContentCompleted'
    title: string
    occurredAtUtc: string
    sourceSlug?: string
    isSourceAvailable: boolean
  }[]
}
export interface Distribution {
  first: number
  second: number
  third: number
  fourth: number
  total: number
}
export interface LearningPerformance {
  range: AnalyticsRange
  period: { startUtc: string; endUtc: string }
  reviewCurrent: Distribution
  reviewPrevious: Distribution
  interviewCurrent: Distribution
  interviewPrevious: Distribution
  dsaCurrent: Distribution
  dsaPrevious: Distribution
}
export interface LearningInsight {
  type: string
  tone: 'Attention' | 'Progress' | 'Neutral'
  priority: string
  title: string
  summary: string
  signals: { type: string; label: string; value: string }[]
  action?: {
    type: string
    label: string
    targetType: string
    targetId?: string
    isAvailable: boolean
  }
}
export interface LearningInsights {
  generatedAtUtc: string
  range: AnalyticsRange
  items: LearningInsight[]
}
