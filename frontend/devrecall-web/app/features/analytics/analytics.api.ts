import type { AnalyticsRange, AnalyticsOverview, LearningPerformance } from './analytics.types'
export function useAnalyticsApi() {
  const api = useApi()
  return {
    overview: (range: AnalyticsRange) =>
      api.get<AnalyticsOverview>('/analytics/overview', { range }),
    performance: (range: AnalyticsRange) =>
      api.get<LearningPerformance>('/analytics/performance', { range }),
  }
}
