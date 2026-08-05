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
    const keys = type === 'KnowledgeNode'
      ? [moduleKey, queryKeys.knowledgeTopics, queryKeys.today]
      : [moduleKey, queryKeys.today]
    await Promise.all(keys.map(key => refreshNuxtData(key)))
  }

  return { refreshToday, refreshTodayAndNavigation, refreshLearningEntryPoints, afterCapture }
}
