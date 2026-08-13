import { describe, expect, it } from 'vitest'
import { comparisonText, factualTrend, insightAction, learningActivityTarget, parseAnalyticsRange } from './analytics.meta'
describe('analytics presentation', () => {
  it('canonicalizes unsupported ranges', () => {
    expect(parseAnalyticsRange(undefined)).toBe('7d')
    expect(parseAnalyticsRange('abc')).toBe('7d')
    expect(parseAnalyticsRange('30d')).toBe('30d')
  })
  it('does not calculate infinite percentages', () => {
    expect(comparisonText({ current: 4, previous: 0, difference: 4 }, 'activities')).toBe(
      'No activity in the previous period',
    )
  })
  it('avoids exaggerated low-sample trends', () => {
    const low = { first: 0, second: 0, third: 0, fourth: 1, total: 1 }
    expect(factualTrend(low, low, ['third', 'fourth'])).toBe(
      'Not enough activity for a useful comparison.',
    )
  })
  it('only maps known insight targets to trusted routes', () => {
    expect(
      insightAction({
        type: 'PracticeInterview',
        targetType: 'InterviewQuestion',
        targetId: 'question-id',
        isAvailable: true,
      }),
    ).toBe('/app/interview/question-id/practice')
    expect(
      insightAction({
        type: 'Unknown',
        targetType: 'ExternalUrl',
        targetId: 'https://example.com',
        isAvailable: true,
      }),
    ).toBeUndefined()
    expect(
      insightAction({
        type: 'PracticeInterview',
        targetType: 'InterviewQuestion',
        targetId: 'question-id',
        isAvailable: false,
      }),
    ).toBeUndefined()
  })
  it('only links available lesson completion activity', () => {
    const activity = { type: 'LearningContentCompleted' as const, title: 'EF Core',
      occurredAtUtc: '2026-08-13T12:00:00Z', sourceSlug: 'ef-core', isSourceAvailable: true }
    expect(learningActivityTarget(activity)).toBe('/app/learn/ef-core')
    expect(learningActivityTarget({ ...activity, sourceSlug: undefined, isSourceAvailable: false }))
      .toBeUndefined()
  })
})
