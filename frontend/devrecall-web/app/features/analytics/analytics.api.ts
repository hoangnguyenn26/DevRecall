import type {
  AnalyticsRange,
  AnalyticsOverview,
  LearningInsights,
  LearningPerformance,
} from './analytics.types'
export function useAnalyticsApi() {
  const api = useApi()
  return {
    overview: (range: AnalyticsRange, signal?: AbortSignal) =>
      api.get<AnalyticsOverview>('/analytics/overview', { range }, signal),
    performance: (range: AnalyticsRange, signal?: AbortSignal) =>
      api.get<LearningPerformance>('/analytics/performance', { range }, signal),
    insights: (range: AnalyticsRange, signal?: AbortSignal) =>
      api.get<LearningInsights>('/analytics/insights', { range, take: 10 }, signal),
  }
}
