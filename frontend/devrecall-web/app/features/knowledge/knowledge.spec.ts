import { describe, expect, it } from 'vitest'
import { formatRelativeKnowledgeDate, knowledgeSortOptions } from './knowledge.meta'
import { buildRelatedReason, createKnowledgeEditState, isKnowledgeEditDirty } from './knowledge.edit'

describe('knowledge workspace metadata', () => {
  it('exposes only stable semantic sort values', () => {
    expect(knowledgeSortOptions.map(option => option.value)).toEqual(['updated', 'created', 'title'])
  })

  it('formats recent activity without leaking raw timestamps', () => {
    const now = Date.UTC(2026, 7, 5, 12)
    expect(formatRelativeKnowledgeDate(new Date(now).toISOString(), now)).toBe('Updated today')
    expect(formatRelativeKnowledgeDate(new Date(now - 86_400_000).toISOString(), now)).toBe('yesterday')
  })

  it('creates an isolated edit state and ignores tag order when detecting changes', () => {
    const detail = { id: '1', title: 'Title', summary: '', tags: [{ id: 'a', name: 'A' }, { id: 'b', name: 'B' }],
      tagCount: 2, createdAtUtc: '', updatedAtUtc: '', version: 3, content: 'Body', relatedItems: [] }
    const state = createKnowledgeEditState(detail)
    state.title = 'Changed'
    expect(detail.title).toBe('Title')
    expect(isKnowledgeEditDirty({ ...state, title: ' Title ', tagIds: ['b', 'a'] }, createKnowledgeEditState(detail))).toBe(false)
  })

  it('explains related knowledge without exposing the score', () => {
    expect(buildRelatedReason({ sameTopic: true, sharedTagCount: 2 })).toBe('Same topic · 2 shared tags')
    expect(buildRelatedReason({ sameTopic: false, sharedTagCount: 0 })).toBe('Recently updated')
  })
})
