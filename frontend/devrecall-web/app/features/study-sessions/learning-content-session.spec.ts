import { describe, expect, it } from 'vitest'
import { studySessionItemTarget } from './study-session.actions'

describe('Learning Content Study Session navigation', () => {
  it('opens a lesson with trusted session navigation context', () => {
    expect(studySessionItemTarget('session-1', {
      id: 'item-1', resourceType: 'LearningContent', resourceId: 'lesson-id',
      resourceKey: 'aspnet-core-lifetimes', resourceTitle: 'ASP.NET Core lifetimes',
      isResourceAvailable: true, plannedDurationMinutes: 15, position: 1, status: 'Pending',
    })).toBe('/app/learn/aspnet-core-lifetimes?studySession=session-1&studyItem=item-1')
  })
})
