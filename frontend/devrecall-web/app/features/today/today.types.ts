export type TodayActionType =
  | 'ContinueStudySession'
  | 'StartStudyPlan'
  | 'ContinueStudyPlan'
  | 'StartReview'
  | 'GenerateStudyPlan'
  | 'OpenRecommendation'
  | 'CreateKnowledge'

export interface TodayDashboard {
  generatedAtUtc: string
  user: TodayUserSummary
  nextAction: TodayNextAction
  metrics: TodayMetrics
  studyPlan: TodayStudyPlan | null
  recommendations: TodayRecommendation[]
  weakTopics: TodayWeakTopic[]
  weeklyActivity: TodayActivityPoint[]
}
export interface TodayUserSummary { userId: string; displayName: string }
export interface TodayNextAction {
  type: TodayActionType
  title: string
  description: string
  actionLabel: string
  targetPath: string
  icon: string
  context: TodayNextActionContext | null
}
export interface TodayNextActionContext {
  resourceId: string | null
  resourceType: string | null
  resourceTitle: string | null
  plannedDurationMinutes: number | null
  remainingCount: number | null
  priority: string | null
}
export interface TodayMetrics {
  reviewsDue: number
  studyMinutesThisWeek: number
  activeDaysThisWeek: number
  weeklyTargetDays: number
  weeklyProgressPercent: number
}
export interface TodayStudyPlan {
  studyPlanId: string
  title: string
  status: string
  itemCount: number
  totalPlannedDurationMinutes: number
  version: number
  items: TodayStudyPlanItem[]
}
export interface TodayStudyPlanItem {
  itemId: string
  resourceType: string
  resourceId: string
  resourceTitle: string
  isResourceAvailable: boolean
  plannedDurationMinutes: number
  position: number
}
export interface TodayRecommendation {
  recommendationId: string
  resourceType: string
  resourceId: string
  type: string
  priority: string
  priorityScore: number
  resourceTitle: string
}
export interface TodayWeakTopic {
  weakTopicProfileId: string
  resourceType: string
  resourceId: string
  level: string
  score: number
  resourceTitle: string
  isResourceAvailable: boolean
}
export interface TodayActivityPoint {
  date: string
  studyMinutes: number
  activityCount: number
}
