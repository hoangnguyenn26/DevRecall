import { describe, expect, it, vi } from 'vitest'
import type { InterviewPracticeResult } from './interview-practice.types'
import { useInterviewPractice, type CompleteInterviewPractice } from './useInterviewPractice'

const payload = { questionId: 'question', question: 'Explain IQueryable.', category: 'LINQ', difficulty: 'Medium',
  referenceAnswer: { answerId: 'answer', content: 'Reference content', version: 2 },
  followUps: [{ followUpId: 'follow-up', question: 'When is it executed?' }], questionVersion: 1 }
const result: InterviewPracticeResult = { attemptId: 'attempt', questionId: 'question', selfRating: 'Good', followUpsAnswered: 0, followUpsSkipped: 1, durationSeconds: 90, completedAtUtc: '2026-08-09T00:01:30Z' }

describe('interview practice', () => {
  it('keeps the reference hidden in the answering phase and preserves the answer for comparison', () => {
    const session = useInterviewPractice(vi.fn<CompleteInterviewPractice>(), () => 'submission')
    session.start(payload); session.answer.value = 'My answer'
    expect(session.phase.value).toBe('answering')
    expect(session.compare()).toBe(true)
    expect(session.answer.value).toBe('My answer')
    expect(session.phase.value).toBe('comparing')
  })

  it('requires a rating and can skip an optional follow-up', async () => {
    const complete = vi.fn<CompleteInterviewPractice>().mockResolvedValue(result)
    const session = useInterviewPractice(complete, () => 'submission')
    session.start(payload); session.answer.value = 'My answer'; session.compare()
    expect(await session.continueAfterRating()).toBe(false)
    session.rate('Good'); expect(await session.continueAfterRating()).toBe(true)
    expect(session.phase.value).toBe('follow-up')
    expect(await session.nextFollowUp(true)).toBe(true)
    expect(session.phase.value).toBe('completed')
    expect(session.dirty.value).toBe(false)
  })

  it('blocks duplicate submit and reuses the submission ID after failure', async () => {
    const complete = vi.fn<CompleteInterviewPractice>().mockRejectedValueOnce(new Error('offline')).mockResolvedValue(result)
    const session = useInterviewPractice(complete, () => 'stable-id')
    session.start({ ...payload, followUps: [] }); session.answer.value = 'My answer'; session.compare(); session.rate('Good')
    expect(await session.continueAfterRating()).toBe(false)
    expect(await session.submit()).toBe(true)
    expect(complete.mock.calls.map(call => call[0].submissionId)).toEqual(['stable-id', 'stable-id'])
  })
})
