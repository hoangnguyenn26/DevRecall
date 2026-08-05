import type { PagedResponse } from '~/types/api'

export interface KnowledgeTag { id: string; name: string }
export interface KnowledgeListItem {
  id: string; title: string; summary: string; topicId?: string; topicName?: string
  tags: KnowledgeTag[]; tagCount: number; createdAtUtc: string; updatedAtUtc: string; version: number
}
export type KnowledgeListPage = PagedResponse<KnowledgeListItem>
export interface KnowledgeDetail extends KnowledgeListItem {
  content: string; description?: string; sourceUrl?: string; relatedItems: RelatedKnowledge[]
}
export interface RelatedKnowledge { id: string; title: string; topicName?: string; sharedTagCount: number; sameTopic: boolean; updatedAtUtc: string }
export interface KnowledgeTagOption extends KnowledgeTag { normalizedName: string; knowledgeCount: number }
export interface KnowledgeEditState { title: string; content: string; topicId: string | null; tagIds: string[]; expectedVersion: number }
export type KnowledgeDetailMode = 'read' | 'edit'
export interface KnowledgeTopic {
  id: string; parentId?: string; name: string; directKnowledgeCount: number
  descendantKnowledgeCount: number; totalKnowledgeCount: number; childCount: number
  children: KnowledgeTopic[]
}
export interface KnowledgeTopicTree {
  items: KnowledgeTopic[]; totalKnowledgeCount: number; uncategorizedCount: number
}
export type KnowledgeSort = 'updated' | 'created' | 'title'
