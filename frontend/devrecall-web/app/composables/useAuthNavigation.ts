import type { OnboardingStatus } from '~/features/onboarding/onboarding.types'

export function useAuthNavigation() {
  const api = useApi()

  async function resolveAuthenticatedDestination(returnTo?: unknown): Promise<string> {
    const onboarding = await api.get<OnboardingStatus>('/onboarding')
    return onboarding.hasCompleted ? resolveSafeRedirect(returnTo) : '/app/onboarding'
  }

  return { resolveAuthenticatedDestination }
}
