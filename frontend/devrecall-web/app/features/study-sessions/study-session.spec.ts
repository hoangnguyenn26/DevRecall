import { describe, expect, it } from 'vitest'
import { studySessionItemTarget } from './study-session.actions'

const item = {
  id: '11111111-1111-4111-8111-111111111111',
  resourceType: 'InterviewQuestion',
  resourceId: '22222222-2222-4222-8222-222222222222',
  resourceTitle: 'DI',
  isResourceAvailable: true,
  plannedDurationMinutes: 15,
  position: 1,
  status: 'Pending',
}
describe('Study Session item actions', () => {
  it('builds trusted internal practice context', () =>
    expect(studySessionItemTarget('33333333-3333-4333-8333-333333333333', item)).toBe(
      '/app/interview/22222222-2222-4222-8222-222222222222/practice?studySession=33333333-3333-4333-8333-333333333333&studyItem=11111111-1111-4111-8111-111111111111',
    ))
  it('does not expose an action for unavailable items', () =>
    expect(studySessionItemTarget('session', { ...item, isResourceAvailable: false })).toBeNull())
})
