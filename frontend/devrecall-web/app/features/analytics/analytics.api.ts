import type {
  AnalyticsRange,
  AnalyticsOverview,
  LearningInsights,
  LearningPerformance,
} from './analytics.types'
export function useAnalyticsApi() {
  const api = useApi()
  return {
    overview: (range: AnalyticsRange) =>
      api.get<AnalyticsOverview>('/analytics/overview', { range }),
    performance: (range: AnalyticsRange) =>
      api.get<LearningPerformance>('/analytics/performance', { range }),
    insights: (range: AnalyticsRange) =>
      api.get<LearningInsights>('/analytics/insights', { range, take: 10 }),
  }
}
