import type { AnalyticsRange, Comparison, Distribution } from './analytics.types'
export const analyticsRanges: AnalyticsRange[] = ['7d', '30d', '90d']
export function parseAnalyticsRange(value: unknown): AnalyticsRange {
  return typeof value === 'string' && analyticsRanges.includes(value as AnalyticsRange)
    ? (value as AnalyticsRange)
    : '7d'
}
export function comparisonText(value: Comparison, noun: string): string {
  if (value.previous === 0)
    return value.current === 0
      ? 'No activity in either period'
      : 'No activity in the previous period'
  if (value.difference === 0) return 'Same as the previous period'
  return `${Math.abs(value.difference)} ${noun} ${value.difference > 0 ? 'more' : 'fewer'} than the previous period`
}
export function factualTrend(
  current: Distribution,
  previous: Distribution,
  positiveIndexes: [keyof Distribution, keyof Distribution],
): string {
  if (current.total < 3 || previous.total < 3) return 'Not enough activity for a useful comparison.'
  const currentPositive = current[positiveIndexes[0]] + current[positiveIndexes[1]]
  const previousPositive = previous[positiveIndexes[0]] + previous[positiveIndexes[1]]
  if (currentPositive === previousPositive)
    return 'The selected outcomes occurred equally often in both periods.'
  return `${currentPositive > previousPositive ? 'More' : 'Fewer'} selected outcomes were recorded than in the previous period.`
}
export const analyticsQueryKeys = {
  overview: (range: AnalyticsRange) => `analytics:overview:${range}`,
  performance: (range: AnalyticsRange) => `analytics:performance:${range}`,
}
