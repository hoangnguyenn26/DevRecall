import type { TodayDashboard } from './today.types'

export function useTodayApi() {
  const api = useApi()
  return {
    getDashboard: (): Promise<TodayDashboard> =>
      api.get<TodayDashboard>('/today'),
  }
}
