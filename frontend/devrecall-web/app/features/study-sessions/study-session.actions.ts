import type { StudySessionItem } from './study-session.types'

export function studySessionItemTarget(sessionId: string, item: StudySessionItem): string | null {
  if (!item.isResourceAvailable) return null
  const query = `?studySession=${encodeURIComponent(sessionId)}&studyItem=${encodeURIComponent(item.id)}`
  if (item.resourceType === 'KnowledgeNode') return `/app/knowledge/${item.resourceId}${query}`
  if (item.resourceType === 'InterviewQuestion')
    return `/app/interview/${item.resourceId}/practice${query}`
  if (item.resourceType === 'DsaProblem') return `/app/dsa/${item.resourceId}/practice${query}`
  if (item.resourceType === 'LearningContent' && item.resourceKey)
    return `/app/learn/${item.resourceKey}${query}`
  return null
}
