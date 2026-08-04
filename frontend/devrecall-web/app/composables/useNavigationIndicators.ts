interface NavigationIndicators {
  reviewsDue: number
  activeStudyPlan: boolean
  criticalWeakTopics: number
}

export function useNavigationIndicators() {
  const indicators = useState<NavigationIndicators>('navigation-indicators', () => ({
    reviewsDue: 0,
    activeStudyPlan: false,
    criticalWeakTopics: 0,
  }))

  return { indicators: readonly(indicators) }
}
