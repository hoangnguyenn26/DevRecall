import { describe, expect, it } from 'vitest'
import { dsaAttemptOutcomeLabel, formatPracticeDuration, interviewComparison } from './practice-history'

describe('practice history presentation', () => {
  it('formats bounded attempt duration without analytics', () => {
    expect(formatPracticeDuration(252)).toBe('4m 12s')
  })

  it('describes interview changes using recorded facts', () => {
    expect(interviewComparison(
      { selfRating: 'Good', followUpsAnswered: 2, durationSeconds: 252 },
      { selfRating: 'Fair', followUpsAnswered: 1, durationSeconds: 390 },
    )).toEqual([
      'Self-rating changed from Fair to Good.',
      'You answered 1 more follow-up.',
      'This attempt took 2m 18s less.',
    ])
  })

  it('centralizes professional DSA outcome labels', () => {
    expect(dsaAttemptOutcomeLabel.Failed).toBe('Could not solve')
    expect(dsaAttemptOutcomeLabel.Skipped).toBe('Solved with help')
  })
})
