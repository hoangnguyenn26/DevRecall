import { queryKeys } from '~/query/query-keys'

export function useLearningDataInvalidation() {
  const refreshToday = (): Promise<void> => refreshNuxtData(queryKeys.today)
  const refreshTodayAndNavigation = (): Promise<void> =>
    Promise.all([refreshNuxtData(queryKeys.today), refreshNuxtData(queryKeys.navigationIndicators)]).then(() => undefined)

  async function refreshLearningEntryPoints(): Promise<void> {
    await refreshTodayAndNavigation()
  }

  async function afterCapture(type: 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem'): Promise<void> {
    const moduleKey = type === 'KnowledgeNode'
      ? queryKeys.knowledgeList
      : type === 'InterviewQuestion' ? queryKeys.interviewList : queryKeys.dsaList
    await Promise.all([refreshNuxtData(moduleKey), refreshNuxtData(queryKeys.today)])
  }

  return { refreshToday, refreshTodayAndNavigation, refreshLearningEntryPoints, afterCapture }
}
