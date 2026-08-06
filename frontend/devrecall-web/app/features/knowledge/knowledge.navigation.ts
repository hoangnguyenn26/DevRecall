export interface KnowledgeNavigationItem { id: string }

export function resolveKnowledgeActiveId(items: KnowledgeNavigationItem[], selectedId?: string, activeId?: string): string | undefined {
  if (activeId && items.some(item => item.id === activeId)) return activeId
  if (selectedId && items.some(item => item.id === selectedId)) return selectedId
  return items[0]?.id
}

export function moveKnowledgeActiveId(items: KnowledgeNavigationItem[], activeId: string | undefined, direction: 'next' | 'previous' | 'first' | 'last'): string | undefined {
  if (!items.length) return undefined
  if (direction === 'first') return items[0]?.id
  if (direction === 'last') return items.at(-1)?.id
  const index = Math.max(0, items.findIndex(item => item.id === activeId))
  return items[direction === 'next' ? Math.min(index + 1, items.length - 1) : Math.max(index - 1, 0)]?.id
}

export function buildKnowledgeFilterKey(input: { topicId?: string; uncategorized: boolean; query: string; tagIds: string[]; sort: string; page: number }): string {
  return JSON.stringify({ topicId: input.topicId ?? null, uncategorized: input.uncategorized, query: input.query.trim(), tagIds: [...input.tagIds].sort(), sort: input.sort, page: input.page })
}
