import { queryKeys } from '~/query/query-keys'

export function useLearningDataInvalidation() {
  const activeKnowledgeListKey = useState('knowledge:active-list-key', () => queryKeys.knowledgeList('{}'))
  const refreshToday = (): Promise<void> => refreshNuxtData(queryKeys.today)
  const refreshTodayAndNavigation = (): Promise<void> =>
    Promise.all([refreshNuxtData(queryKeys.today), refreshNuxtData(queryKeys.navigationIndicators)]).then(() => undefined)
  const afterReviewEvaluation = (): Promise<void> =>
    Promise.all([refreshNuxtData(queryKeys.reviewDue), refreshNuxtData(queryKeys.today), refreshNuxtData(queryKeys.navigationIndicators)]).then(() => undefined)

  async function refreshLearningEntryPoints(): Promise<void> {
    await refreshTodayAndNavigation()
  }

  async function afterCapture(type: 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem'): Promise<void> {
    const moduleKey = type === 'KnowledgeNode'
      ? activeKnowledgeListKey.value
      : type === 'InterviewQuestion' ? queryKeys.interviewList : queryKeys.dsaList
    const keys = type === 'KnowledgeNode'
      ? [moduleKey, queryKeys.knowledgeTopics, queryKeys.knowledgeTags(), queryKeys.today]
      : [moduleKey, queryKeys.today]
    await Promise.all(keys.map(key => refreshNuxtData(key)))
  }

  return { refreshToday, refreshTodayAndNavigation, afterReviewEvaluation, refreshLearningEntryPoints, afterCapture }
}
