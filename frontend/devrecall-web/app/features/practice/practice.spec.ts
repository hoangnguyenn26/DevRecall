import { describe, expect, it, vi } from 'vitest'
import { createPracticeProgress, usePracticeSession } from './usePracticeSession'
import { matchesPracticeShortcut } from './usePracticeShortcuts'

describe('practice foundation', () => {
  it('counts only submitted items in progress', () => {
    expect(createPracticeProgress(2, 12)).toEqual({ current: 3, total: 12, completed: 2, percent: 16.7 })
  })

  it('keeps the active item after a failed submission', async () => {
    const session = usePracticeSession('active')
    expect(await session.submit(vi.fn().mockRejectedValue(new Error('offline')))).toBe(false)
    expect(session.phase.value).toBe('active')
    expect(session.error.value).toBeInstanceOf(Error)
  })

  it('blocks duplicate submissions while busy', async () => {
    const session = usePracticeSession('submitting')
    expect(await session.submit(vi.fn())).toBe(false)
  })

  it('matches focus shortcuts without stealing modified keys', () => {
    expect(matchesPracticeShortcut(new KeyboardEvent('keydown', { key: '1' }), ['1'])).toBe(true)
    expect(matchesPracticeShortcut(new KeyboardEvent('keydown', { key: '1', ctrlKey: true }), ['1'])).toBe(false)
    expect(matchesPracticeShortcut(new KeyboardEvent('keydown', { key: 'Enter', ctrlKey: true }), ['Ctrl', 'Enter'])).toBe(true)
  })
})
