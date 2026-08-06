import { describe, expect, it } from 'vitest'
import { getGlobalSearchMetadata, groupGlobalSearchResults } from './global-search.meta'
import type { GlobalSearchResult } from './global-search.types'

function result(resourceType: string, resourceId: string): GlobalSearchResult {
  return { resourceType, resourceId, title: resourceId, targetPath: `/app/${resourceId}`, rank: 1, updatedAtUtc: '2026-08-06T00:00:00Z', highlights: [] }
}

describe('global search presentation', () => {
  it('groups the bounded resource types without changing result order', () => {
    const groups = groupGlobalSearchResults([result('Knowledge', 'one'), result('InterviewQuestion', 'two'), result('Knowledge', 'three')])
    expect(groups.map(group => group.label)).toEqual(['Knowledge', 'Interview questions'])
    expect(groups[0]?.results.map(item => item.resourceId)).toEqual(['one', 'three'])
  })

  it('marks unknown backend resource types as non-navigable presentation groups', () => {
    expect(getGlobalSearchMetadata('FutureResource')).toMatchObject({ known: false, label: 'Other results' })
  })
})
