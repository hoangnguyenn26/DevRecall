import type { DueReviewItem, Recommendation, StudyPlanListItem, StudySessionListItem } from '~/types/domain'
import { formatMinutes, resourceRoute } from './format'

export interface NextAction {
  label: string
  detail: string
  to: string
  icon: string
}

export function resolveNextAction(
  sessions: StudySessionListItem[], plans: StudyPlanListItem[],
  reviews: DueReviewItem[], recommendations: Recommendation[]): NextAction {
  const active = sessions.find(item => ['InProgress', 'Active'].includes(item.status))
  if (active) return { label: 'Continue study session', detail: active.title, to: `/app/study-sessions/${active.id}`, icon: 'i-lucide-play' }
  const ready = plans.find(item => item.status === 'Ready')
  if (ready) return { label: 'Open ready study plan', detail: `${ready.itemCount} items · ${formatMinutes(ready.totalPlannedDurationMinutes)}`, to: `/app/study-plans/${ready.studyPlanId}`, icon: 'i-lucide-list-checks' }
  const draft = plans.find(item => item.status === 'Draft')
  if (draft) return { label: 'Continue draft study plan', detail: draft.title, to: `/app/study-plans/${draft.studyPlanId}`, icon: 'i-lucide-pencil-line' }
  if (reviews[0]) return { label: 'Start due reviews', detail: `${reviews.length} ready to practice`, to: '/app/review/session', icon: 'i-lucide-refresh-cw' }
  const recommendation = recommendations[0]
  if (recommendation) return { label: 'Act on a recommendation', detail: recommendation.resourceTitle, to: resourceRoute(recommendation.resourceType, recommendation.resourceId), icon: 'i-lucide-lightbulb' }
  return { label: 'Generate a study plan', detail: 'Turn your active recommendations into a focused session.', to: '/app/study-plans', icon: 'i-lucide-sparkles' }
}
