export const recommendationPriorityMeta = {
  Critical: 'Act on this first',
  High: 'High priority',
  Medium: 'Recommended',
  Low: 'When time allows',
} as const
export function recommendationReason(reason: {
  type: string
  count?: number
  level?: string
}): string {
  if (reason.type === 'WeakTopicSeverity')
    return `The related topic currently has ${reason.level ?? 'current'} weakness signals`
  if (reason.type === 'ContributingSignals')
    return `${reason.count ?? 0} learning signals contributed to this recommendation`
  return reason.type.replace(/([A-Z])/g, ' $1').trim()
}
export function recommendationAction(type: string, resourceType: string, id: string) {
  if (type === 'PracticeInterview')
    return { label: 'Start Interview practice', to: `/app/interview/${id}/practice` }
  if (type === 'RetryDsaProblem')
    return { label: 'Practice DSA problem', to: `/app/dsa/${id}/practice` }
  if (resourceType === 'KnowledgeNode')
    return { label: 'Open knowledge', to: `/app/knowledge/${id}` }
  return { label: 'Open resource', to: undefined }
}
