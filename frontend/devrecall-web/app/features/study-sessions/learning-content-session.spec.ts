import { describe, expect, it } from 'vitest'
import { studySessionItemTarget } from './study-session.actions'

describe('Learning Content Study Session navigation', () => {
  it('opens external content inside the app without completing it and blocks archived sources', () => {
    const item = {
      id: 'task-1', resourceType: 'LearningContent', resourceId: 'resource-1',
      contentType: 'ExternalResource' as const, sourceName: 'Microsoft Learn',
      resourceKey: 'ef-core-handling-concurrency-conflicts', resourceTitle: 'Concurrency',
      isResourceAvailable: true, plannedDurationMinutes: 20, position: 1, status: 'Pending',
    }
    expect(studySessionItemTarget('session-1', item)).toBe('/app/learn/ef-core-handling-concurrency-conflicts?studySession=session-1&studyItem=task-1')
    expect(item.status).toBe('Pending')
    expect(studySessionItemTarget('session-1', { ...item, isResourceAvailable: false })).toBeNull()
  })
  it('opens a lesson with trusted session navigation context', () => {
    expect(studySessionItemTarget('session-1', {
      id: 'item-1', resourceType: 'LearningContent', resourceId: 'lesson-id',
      resourceKey: 'aspnet-core-lifetimes', resourceTitle: 'ASP.NET Core lifetimes',
      isResourceAvailable: true, plannedDurationMinutes: 15, position: 1, status: 'Pending',
    })).toBe('/app/learn/aspnet-core-lifetimes?studySession=session-1&studyItem=item-1')
  })
})
