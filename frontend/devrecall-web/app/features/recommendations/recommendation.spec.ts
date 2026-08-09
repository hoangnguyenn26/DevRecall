import { describe, expect, it } from 'vitest'
import { recommendationAction, recommendationReason } from './recommendation.meta'

describe('recommendation presentation', () => {
  it('explains the stored weakness snapshot without exposing ranking scores', () => {
    expect(recommendationReason({ type: 'WeakTopicSeverity', level: 'Critical' })).toBe(
      'The related topic currently has Critical weakness signals',
    )
    expect(recommendationReason({ type: 'ContributingSignals', count: 4 })).toBe(
      '4 learning signals contributed to this recommendation',
    )
  })

  it('only creates trusted application targets', () => {
    expect(recommendationAction('PracticeInterview', 'InterviewQuestion', 'question-id').to).toBe(
      '/app/interview/question-id/practice',
    )
    expect(recommendationAction('RetryDsaProblem', 'DsaProblem', 'problem-id').to).toBe(
      '/app/dsa/problem-id/practice',
    )
    expect(recommendationAction('Unknown', 'Unknown', 'resource-id').to).toBeUndefined()
  })
})
