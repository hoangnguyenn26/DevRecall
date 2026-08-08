import { describe, expect, it, vi } from 'vitest'
import type { DueReviewItem, ReviewResult } from './review.types'
import { useReviewSession, type ReviewRateAction } from './useReviewSession'

const item = (id: string): DueReviewItem => ({ reviewItemId: id, resourceType: 'KnowledgeNode', resourceId: id, resourceTitle: `Question ${id}`, resourcePreview: `Answer ${id}`, dueAtUtc: '2026-08-09T00:00:00Z', intervalDays: 0, reviewCount: 0, overdueMinutes: 1 })
const result = (id: string, evaluation: ReviewResult['evaluation']): ReviewResult => ({ reviewItemId: id, reviewHistoryId: `history-${id}`, evaluation, previousIntervalDays: 0, nextIntervalDays: 2, reviewedAtUtc: '2026-08-09T00:00:00Z', nextDueAtUtc: '2026-08-11T00:00:00Z', reviewCount: 1 })

describe('review practice session', () => {
  it('starts with the prompt and hides rating behavior until reveal', async () => {
    const rate = vi.fn<ReviewRateAction>()
    const session = useReviewSession(rate, () => 'submission-1')
    session.start([item('one')], 1)
    expect(session.phase.value).toBe('prompt')
    expect(await session.rate('Good')).toBe(false)
    expect(rate).not.toHaveBeenCalled()
    session.reveal()
    expect(session.phase.value).toBe('answer')
  })

  it('increments progress only after a successful rating', async () => {
    const pending = Promise.withResolvers<ReviewResult>()
    const session = useReviewSession(() => pending.promise, () => 'submission-1')
    session.start([item('one')], 1)
    session.reveal()
    const action = session.rate('Good')
    expect(session.completedCount.value).toBe(0)
    pending.resolve(result('one', 'Good'))
    expect(await action).toBe(true)
    expect(session.completedCount.value).toBe(1)
    expect(session.phase.value).toBe('feedback')
  })

  it('keeps the current item and reuses SubmissionId after failure', async () => {
    const ids: string[] = []
    const rate = vi.fn<ReviewRateAction>(async (_item, _rating, submissionId) => { ids.push(submissionId); if (ids.length === 1) throw new Error('offline'); return result('one', 'Hard') })
    const session = useReviewSession(rate, () => 'stable-submission')
    session.start([item('one')], 1)
    session.reveal()
    expect(await session.rate('Hard')).toBe(false)
    expect(session.currentItem.value?.reviewItemId).toBe('one')
    expect(await session.rate('Hard')).toBe(true)
    expect(ids).toEqual(['stable-submission', 'stable-submission'])
  })

  it('moves explicitly and summarizes all ratings', async () => {
    const session = useReviewSession(async (current, rating) => result(current.reviewItemId, rating), () => crypto.randomUUID())
    session.start([item('one'), item('two')], 3)
    session.reveal(); await session.rate('Again'); session.next()
    expect(session.currentItem.value?.reviewItemId).toBe('two')
    session.reveal(); await session.rate('Easy'); session.next()
    expect(session.phase.value).toBe('completed')
    expect(session.results.value.map(entry => entry.evaluation)).toEqual(['Again', 'Easy'])
    expect(session.remainingDueCount.value).toBe(1)
  })
})
