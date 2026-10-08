import { queryKeys } from '~/query/query-keys'

export function useLearningDataInvalidation() {
  const insightRanges = ['7d', '30d', '90d'] as const
  const clearAnalyticsAndInsights = (): void => {
    for (const range of insightRanges) {
      clearNuxtData(queryKeys.analyticsOverview(range))
      clearNuxtData(queryKeys.analyticsPerformance(range))
      clearNuxtData(queryKeys.learningInsights(range))
    }
  }
  const activeKnowledgeListKey = useState('knowledge:active-list-key', () =>
    queryKeys.knowledgeList('{}'),
  )
  const refreshToday = (): Promise<void> => refreshNuxtData(queryKeys.today)
  const invalidateToday = async (): Promise<void> => { clearNuxtData(queryKeys.today) }
  const refreshTodayAndNavigation = (): Promise<void> =>
    Promise.all([
      refreshNuxtData(queryKeys.today),
      refreshNuxtData(queryKeys.navigationIndicators),
    ]).then(() => undefined)
  const invalidateTodayAndRefreshNavigation = (): Promise<void> => {
    clearNuxtData(queryKeys.today)
    return refreshNuxtData(queryKeys.navigationIndicators)
  }
  const afterReviewEvaluation = (): Promise<void> => {
    clearNuxtData(queryKeys.today)
    clearAnalyticsAndInsights()
    return Promise.all([
      refreshNuxtData(queryKeys.reviewDue),
      refreshNuxtData(queryKeys.navigationIndicators),
    ]).then(() => undefined)
  }
  const afterReviewItemsAdded = (): Promise<void> => {
    clearNuxtData(queryKeys.today)
    return Promise.all([
      refreshNuxtData(queryKeys.reviewDue),
      refreshNuxtData(queryKeys.navigationIndicators),
    ]).then(() => undefined)
  }
  const afterInterviewPractice = (questionId: string): Promise<void> =>
    Promise.all([
      refreshNuxtData(queryKeys.interviewAttempts(questionId)),
      clearNuxtData(queryKeys.today),
    ]).then(() => {
      clearAnalyticsAndInsights()
    })
  const afterDsaPractice = (problemId: string): Promise<void> =>
    Promise.all([
      refreshNuxtData(queryKeys.dsaAttempts(problemId)),
      refreshNuxtData(queryKeys.dsaProblem(problemId)),
      clearNuxtData(queryKeys.today),
    ]).then(() => {
      clearAnalyticsAndInsights()
    })
  const afterStudySessionChanged = async (): Promise<void> => {
    clearNuxtData(queryKeys.today)
    clearAnalyticsAndInsights()
  }
  const afterLessonCompleted = (): void => clearAnalyticsAndInsights()
  const afterRecommendationsChanged = async (): Promise<void> => {
    await invalidateTodayAndRefreshNavigation()
    for (const range of insightRanges) clearNuxtData(queryKeys.learningInsights(range))
  }
  const afterRecommendationDismissed = afterRecommendationsChanged

  async function refreshLearningEntryPoints(): Promise<void> {
    await refreshTodayAndNavigation()
  }

  async function afterCapture(
    type: 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem',
  ): Promise<void> {
    const moduleKey =
      type === 'KnowledgeNode'
        ? activeKnowledgeListKey.value
        : type === 'InterviewQuestion'
          ? queryKeys.interviewList
          : queryKeys.dsaList
    const keys =
      type === 'KnowledgeNode'
        ? [moduleKey, queryKeys.knowledgeTopics, queryKeys.knowledgeTags()]
        : [moduleKey]
    await Promise.all(keys.map((key) => refreshNuxtData(key)))
  }

  return {
    refreshToday,
    invalidateToday,
    refreshTodayAndNavigation,
    invalidateTodayAndRefreshNavigation,
    afterReviewEvaluation,
    afterReviewItemsAdded,
    afterInterviewPractice,
    afterDsaPractice,
    afterStudySessionChanged,
    afterLessonCompleted,
    afterRecommendationDismissed,
    afterRecommendationsChanged,
    refreshLearningEntryPoints,
    afterCapture,
  }
}
