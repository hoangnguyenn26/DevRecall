import { describe, expect, it } from 'vitest'
import { comparisonText, factualTrend, parseAnalyticsRange } from './analytics.meta'
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
})
