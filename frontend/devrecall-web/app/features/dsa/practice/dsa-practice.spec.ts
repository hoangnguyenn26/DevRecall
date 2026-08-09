import { describe, expect, it, vi } from 'vitest'
import { useDsaPractice, type CompleteDsaPractice } from './useDsaPractice'

const result = { id: 'attempt', dsaProblemId: 'problem', attemptNumber: 1, result: 'Solved' as const,
  durationMinutes: 18, attemptedAtUtc: '2026-08-09T00:18:00Z', createdAtUtc: '2026-08-09T00:18:00Z' }

describe('DSA practice', () => {
  it('marks solution work dirty and opens reflection before submission', () => {
    const complete = vi.fn<CompleteDsaPractice>()
    const session = useDsaPractice(complete, () => 'submission')
    session.solution.value = 'return values;'
    expect(session.dirty.value).toBe(true)
    expect(session.finish()).toBe(true)
    expect(session.phase.value).toBe('reflection')
    expect(complete).not.toHaveBeenCalled()
  })

  it('requires an outcome and reuses the submission ID when retrying', async () => {
    const complete = vi.fn<CompleteDsaPractice>().mockRejectedValueOnce(new Error('offline')).mockResolvedValue(result)
    const session = useDsaPractice(complete, () => 'stable-id')
    session.finish(); expect(await session.submit()).toBe(false)
    session.outcome.value = 'Solved'
    expect(await session.submit()).toBe(false)
    expect(await session.submit()).toBe(true)
    expect(complete.mock.calls.map(call => call[0].submissionId)).toEqual(['stable-id', 'stable-id'])
    expect(session.dirty.value).toBe(false)
  })

  it('practice again resets private draft and creates a fresh submission', () => {
    const ids = ['first', 'second']
    const session = useDsaPractice(vi.fn<CompleteDsaPractice>(), () => ids.shift()!)
    session.solution.value = 'private code'; session.outcome.value = 'Failed'; session.reset()
    expect(session.solution.value).toBe('')
    expect(session.outcome.value).toBeUndefined()
    expect(session.submissionId.value).toBe('second')
  })
})
