import { queryKeys } from '~/query/query-keys'

export function useLearningDataInvalidation() {
  async function refreshLearningEntryPoints(): Promise<void> {
    await Promise.all([refreshNuxtData(queryKeys.today), refreshNuxtData(queryKeys.navigationIndicators)])
  }

  async function afterCapture(type: 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem'): Promise<void> {
    const moduleKey = type === 'KnowledgeNode'
      ? queryKeys.knowledgeList
      : type === 'InterviewQuestion' ? queryKeys.interviewList : queryKeys.dsaList
    await Promise.all([refreshNuxtData(moduleKey), refreshNuxtData(queryKeys.today)])
  }

  return { refreshLearningEntryPoints, afterCapture }
}
