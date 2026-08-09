import { describe, expect, it, vi } from 'vitest'
import { runBestEffortRefresh } from './best-effort-refresh'

describe('runBestEffortRefresh', () => {
  it('reports a completed refresh', async () => {
    const refresh = vi.fn<() => Promise<void>>().mockResolvedValue(undefined)

    await expect(runBestEffortRefresh(refresh)).resolves.toBe(true)
    expect(refresh).toHaveBeenCalledOnce()
  })

  it('does not turn a refresh failure into a mutation failure', async () => {
    const refresh = vi.fn<() => Promise<void>>().mockRejectedValue(new Error('offline'))

    await expect(runBestEffortRefresh(refresh)).resolves.toBe(false)
    expect(refresh).toHaveBeenCalledOnce()
  })
})
