import { afterEach, describe, expect, it, vi } from 'vitest'
import { useLearningDataInvalidation } from '~/composables/useLearningDataInvalidation'
import { queryKeys } from './query-keys'

describe('learning data invalidation', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('refreshes Today and navigation after a learning-state mutation', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().refreshTodayAndNavigation()

    expect(refresh).toHaveBeenCalledWith(queryKeys.today)
    expect(refresh).toHaveBeenCalledWith(queryKeys.navigationIndicators)
  })

  it('refreshes only the affected list and Today after Knowledge capture', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().afterCapture('KnowledgeNode')

    expect(refresh.mock.calls.map(([key]) => key)).toEqual([
      queryKeys.knowledgeList, queryKeys.knowledgeTopics, queryKeys.today,
    ])
  })

  it('does not refresh navigation after recommendation lifecycle changes', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().refreshToday()

    expect(refresh).toHaveBeenCalledOnce()
    expect(refresh).toHaveBeenCalledWith(queryKeys.today)
  })
})
