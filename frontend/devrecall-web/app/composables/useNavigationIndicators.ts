import { queryKeys } from '~/query/query-keys'

interface NavigationIndicators {
  reviewsDue: number
  hasActiveStudyPlan: boolean
  criticalWeakTopics: number
  hasIncompleteOnboarding: boolean
}

export function useNavigationIndicators() {
  const api = useApi()
  const { data, refresh } = useAsyncData<NavigationIndicators>(queryKeys.navigationIndicators, () => api.get('/navigation-indicators'), {
    default: () => ({ reviewsDue: 0, hasActiveStudyPlan: false, criticalWeakTopics: 0, hasIncompleteOnboarding: false }),
    dedupe: 'defer',
  })
  return { indicators: data, refresh }
}
