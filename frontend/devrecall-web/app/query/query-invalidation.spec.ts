import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useLearningDataInvalidation } from '~/composables/useLearningDataInvalidation'
import { queryKeys } from './query-keys'

describe('learning data invalidation', () => {
  beforeEach(() => {
    vi.stubGlobal('useState', () => ({ value: queryKeys.knowledgeList('active') }))
    vi.stubGlobal('clearNuxtData', vi.fn())
  })
  afterEach(() => vi.unstubAllGlobals())

  it('refreshes Today and navigation after a learning-state mutation', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().refreshTodayAndNavigation()

    expect(refresh).toHaveBeenCalledWith(queryKeys.today)
    expect(refresh).toHaveBeenCalledWith(queryKeys.navigationIndicators)
  })

  it('refreshes only the affected Knowledge projections after capture', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)
    await useLearningDataInvalidation().afterCapture('KnowledgeNode')

    expect(refresh.mock.calls.map(([key]) => key)).toEqual([
      queryKeys.knowledgeList('active'), queryKeys.knowledgeTopics, queryKeys.knowledgeTags(),
    ])
  })

  it('does not refresh navigation after recommendation lifecycle changes', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().refreshToday()

    expect(refresh).toHaveBeenCalledOnce()
    expect(refresh).toHaveBeenCalledWith(queryKeys.today)
  })

  it('refreshes Review entry points after a successful rating', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    vi.stubGlobal('refreshNuxtData', refresh)

    await useLearningDataInvalidation().afterReviewEvaluation()

    expect(refresh.mock.calls.map(([key]) => key)).toEqual([
      queryKeys.reviewDue, queryKeys.navigationIndicators,
    ])
    expect(clearNuxtData).toHaveBeenCalledWith(queryKeys.today)
  })

  it('refreshes only Interview history and marks Today stale after practice', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => undefined)
    const clear = vi.fn<(key: string) => void>()
    vi.stubGlobal('refreshNuxtData', refresh); vi.stubGlobal('clearNuxtData', clear)
    await useLearningDataInvalidation().afterInterviewPractice('question')
    expect(refresh).toHaveBeenCalledWith(queryKeys.interviewAttempts('question'))
    expect(clear).toHaveBeenCalledWith(queryKeys.today)
    expect(refresh).toHaveBeenCalledOnce()
  })

  it('uses resource-specific practice query keys', () => {
    expect(queryKeys.interviewAttempts('a')).not.toBe(queryKeys.interviewAttempts('b'))
    expect(queryKeys.dsaAttempt('a')).not.toBe(queryKeys.dsaAttempt('b'))
  })

  it('marks Today stale without fetching it, including when another refresh fails', async () => {
    const refresh = vi.fn<(key: string) => Promise<void>>(async () => { throw new Error('offline') })
    vi.stubGlobal('refreshNuxtData', refresh)
    const invalidation = useLearningDataInvalidation()
    await invalidation.invalidateToday()
    expect(refresh).not.toHaveBeenCalled()
    await expect(invalidation.afterReviewEvaluation()).rejects.toThrow('offline')
    expect(clearNuxtData).toHaveBeenCalledWith(queryKeys.today)
    expect(refresh).not.toHaveBeenCalledWith(queryKeys.today)
  })
})
