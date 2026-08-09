import { afterEach, describe, expect, it, vi } from 'vitest'
import { useDraftClipboard } from './useDraftClipboard'

describe('practice draft clipboard', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('copies the authored draft unchanged', async () => {
    const writeText = vi.fn<(content: string) => Promise<void>>().mockResolvedValue(undefined); const add = vi.fn<(message: unknown) => void>()
    vi.stubGlobal('navigator', { clipboard: { writeText } }); vi.stubGlobal('useToast', () => ({ add }))
    expect(await useDraftClipboard().copyDraft('return 1;', 'Solution')).toBe(true)
    expect(writeText).toHaveBeenCalledWith('return 1;')
  })

  it('reports clipboard failure without throwing', async () => {
    const add = vi.fn<(message: unknown) => void>(); vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn<(content: string) => Promise<void>>().mockRejectedValue(new Error('denied')) } }); vi.stubGlobal('useToast', () => ({ add }))
    expect(await useDraftClipboard().copyDraft('private answer', 'Answer')).toBe(false)
    expect(add).toHaveBeenCalledWith(expect.objectContaining({ color: 'warning' }))
  })
})
