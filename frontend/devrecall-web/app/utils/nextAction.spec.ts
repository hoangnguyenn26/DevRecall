import { describe, expect, it } from 'vitest'
import type { DueReviewItem, Recommendation, StudyPlanListItem, StudySessionListItem } from '~/types/domain'
import { resolveNextAction } from './nextAction'

describe('resolveNextAction', () => {
  it('prioritizes an active study session', () => {
    const sessions = [{ id: 'session-1', title: 'Focus', status: 'InProgress' }] as StudySessionListItem[]
    const plans = [{ studyPlanId: 'plan-1', status: 'Ready' }] as StudyPlanListItem[]
    expect(resolveNextAction(sessions, plans, [], []).to).toBe('/app/study-sessions/session-1')
  })

  it('prioritizes ready plans before drafts and reviews', () => {
    const plans = [
      { studyPlanId: 'draft', status: 'Draft', title: 'Draft' },
      { studyPlanId: 'ready', status: 'Ready', itemCount: 2, totalPlannedDurationMinutes: 45 },
    ] as unknown as StudyPlanListItem[]
    const reviews = [{ reviewItemId: 'review' }] as DueReviewItem[]
    expect(resolveNextAction([], plans, reviews, []).to).toBe('/app/study-plans/ready')
  })

  it('falls back to a recommendation when nothing is underway or due', () => {
    const recommendations = [{ resourceType: 'Dsa', resourceId: 'problem-1', resourceTitle: 'Graphs' }] as Recommendation[]
    expect(resolveNextAction([], [], [], recommendations).to).toBe('/app/dsa/problem-1')
  })

  it('offers plan generation when there is no learning signal', () => {
    expect(resolveNextAction([], [], [], []).to).toBe('/app/study-plans')
  })
})
