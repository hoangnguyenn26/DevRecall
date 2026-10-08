import type { TodayActionType } from './today.types'

const todayActionIconMeta: Record<string, string> = {
  timer: 'i-lucide-timer',
  'list-checks': 'i-lucide-list-checks',
  refresh: 'i-lucide-refresh-cw',
  pencil: 'i-lucide-pencil-line',
  sparkles: 'i-lucide-sparkles',
  'list-plus': 'i-lucide-list-plus',
  'book-open': 'i-lucide-book-open',
}

export function getTodayActionIcon(key: string): string {
  return todayActionIconMeta[key] ?? 'i-lucide-circle-arrow-right'
}

export function getTodayResourcePath(type: string, id: string): string {
  if (type === 'KnowledgeNode') return `/app/knowledge?node=${id}`
  if (type === 'InterviewQuestion') return `/app/interview/${id}`
  if (type === 'DsaProblem') return `/app/dsa/${id}`
  return '/app'
}
const todayActionLabels: Record<TodayActionType, string> = {
  StartReview: 'Review',
  ContinueStudySession: 'Continue studying',
  ContinueLearning: 'Continue learning',
  StartStudyPlan: 'Planned study',
  ContinueStudyPlan: 'Study plan',
  OpenRecommendation: 'Practice suggestion',
  GenerateStudyPlan: 'Plan your study',
  LearnRecommendedContent: 'Recommended next',
  BrowseLearning: 'Explore learning',
  CreateKnowledge: 'Knowledge',
}

export function getTodayActionLabel(type: TodayActionType): string {
  return todayActionLabels[type] ?? 'Next action'
}
