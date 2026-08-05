import type { CompleteOnboardingRequest, OnboardingStatus } from './onboarding.types'

export function useOnboardingApi() {
  const api = useApi()
  return {
    get: (): Promise<OnboardingStatus> => api.get('/onboarding'),
    complete: (request: CompleteOnboardingRequest): Promise<OnboardingStatus> => api.post('/onboarding/complete', request),
    skip: (): Promise<OnboardingStatus> => api.post('/onboarding/skip'),
  }
}
