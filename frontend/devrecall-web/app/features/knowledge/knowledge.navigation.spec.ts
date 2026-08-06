import { describe, expect, it } from 'vitest'
import { buildKnowledgeFilterKey, moveKnowledgeActiveId, resolveKnowledgeActiveId } from './knowledge.navigation'
import { isEditableTarget } from '~/utils/keyboard'

const items = [{ id: 'a' }, { id: 'b' }, { id: 'c' }]

describe('knowledge keyboard navigation', () => {
  it('starts at the selected note and moves within list boundaries', () => {
    expect(resolveKnowledgeActiveId(items, 'b')).toBe('b')
    expect(moveKnowledgeActiveId(items, 'b', 'next')).toBe('c')
    expect(moveKnowledgeActiveId(items, 'c', 'next')).toBe('c')
    expect(moveKnowledgeActiveId(items, 'a', 'previous')).toBe('a')
  })

  it('supports first and last navigation', () => {
    expect(moveKnowledgeActiveId(items, 'b', 'first')).toBe('a')
    expect(moveKnowledgeActiveId(items, 'b', 'last')).toBe('c')
  })

  it('uses a stable filter key independent of tag order', () => {
    const base = { topicId: 'topic', uncategorized: false, query: ' query ', sort: 'updated', page: 1 }
    expect(buildKnowledgeFilterKey({ ...base, tagIds: ['b', 'a'] })).toBe(buildKnowledgeFilterKey({ ...base, tagIds: ['a', 'b'] }))
    expect(buildKnowledgeFilterKey({ ...base, tagIds: ['a'] })).not.toBe(buildKnowledgeFilterKey({ ...base, tagIds: ['a'], page: 2 }))
  })

  it('detects editing targets through nested elements', () => {
    const textarea = document.createElement('textarea'); const child = document.createElement('span')
    const editor = document.createElement('div'); editor.setAttribute('contenteditable', 'true'); editor.append(child)
    expect(isEditableTarget(textarea)).toBe(true)
    expect(isEditableTarget(child)).toBe(true)
    expect(isEditableTarget(document.createElement('button'))).toBe(false)
  })
})
