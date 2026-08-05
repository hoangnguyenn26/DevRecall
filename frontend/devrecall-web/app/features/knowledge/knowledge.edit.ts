import type { KnowledgeDetail, KnowledgeEditState } from './knowledge.types'

export function createKnowledgeEditState(detail: KnowledgeDetail): KnowledgeEditState {
  return { title: detail.title, content: detail.content, topicId: detail.topicId ?? null,
    tagIds: detail.tags.map(tag => tag.id), expectedVersion: detail.version }
}

export function normalizeKnowledgeEditState(state: KnowledgeEditState) {
  return { title: state.title.trim(), content: state.content.trim(), topicId: state.topicId,
    tagIds: [...new Set(state.tagIds)].sort() }
}

export function isKnowledgeEditDirty(current: KnowledgeEditState, initial: KnowledgeEditState): boolean {
  return JSON.stringify(normalizeKnowledgeEditState(current)) !== JSON.stringify(normalizeKnowledgeEditState(initial))
}

export function buildRelatedReason(item: { sameTopic: boolean; sharedTagCount: number }): string {
  const reasons = [item.sameTopic ? 'Same topic' : '', item.sharedTagCount ? `${item.sharedTagCount} shared ${item.sharedTagCount === 1 ? 'tag' : 'tags'}` : ''].filter(Boolean)
  return reasons.join(' · ') || 'Recently updated'
}
