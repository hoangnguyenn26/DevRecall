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
  activity: { date: string; studyMinutes: number; practiceCount: number }[]
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
