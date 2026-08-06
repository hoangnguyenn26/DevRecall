import { describe, expect, it } from 'vitest'
import { canonicalizeKnowledgeLocation, isCanonicalKnowledgeLocation } from './knowledge.route'

const topic = 'A6A59486-6256-4E6C-AB4E-D73E5631789A'
const tagA = '0bbf6457-fc6d-4422-843e-268b39035251'
const tagB = 'b88b5437-cb59-4207-b62d-97ccd20ed59c'

describe('knowledge route canonicalization', () => {
  it('removes default and invalid values without retaining unknown keys', () => {
    expect(canonicalizeKnowledgeLocation('/app/knowledge', { page: '1', sort: 'unknown', query: '  ', extra: 'value' }))
      .toEqual({ path: '/app/knowledge', query: {} })
  })

  it('normalizes mutually exclusive topic filters', () => {
    expect(canonicalizeKnowledgeLocation('/app/knowledge', { topicId: topic, topicScope: 'uncategorized' }).query)
      .toEqual({ topicId: topic.toLowerCase() })
  })

  it('deduplicates, validates, and sorts tag ids', () => {
    expect(canonicalizeKnowledgeLocation('/app/knowledge', { tagIds: `${tagB},invalid,${tagA},${tagB}` }).query.tagIds)
      .toBe(`${tagA},${tagB}`)
  })

  it('falls back safely for invalid detail ids and pages', () => {
    expect(canonicalizeKnowledgeLocation('/app/knowledge/not-a-guid', { page: '-2' }))
      .toEqual({ path: '/app/knowledge', query: {} })
  })

  it('recognizes an already canonical location', () => {
    const canonical = canonicalizeKnowledgeLocation('/app/knowledge', { query: 'dependency', sort: 'title', page: '2' })
    expect(isCanonicalKnowledgeLocation('/app/knowledge', canonical.query, canonical)).toBe(true)
  })
})
