import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'

import type { SystemInfo } from '@/api/systemApi'

import HomeView from '../HomeView.vue'

vi.mock('@/api/systemApi', () => ({
  getSystemInfo: vi.fn<() => Promise<SystemInfo>>().mockResolvedValue({
    applicationName: 'DevRecall',
    version: '0.1.0',
    environment: 'Test',
    currentTimeUtc: '2026-07-23T00:00:00+00:00',
  }),
}))

describe('HomeView', () => {
  it('renders system information returned by the API module', async () => {
    const wrapper = mount(HomeView)

    expect(wrapper.text()).toContain('Connecting to backend...')

    await flushPromises()

    expect(wrapper.text()).toContain('DevRecall')
    expect(wrapper.text()).toContain('0.1.0')
    expect(wrapper.text()).toContain('Test')
    expect(wrapper.text()).toContain('2026-07-23T00:00:00+00:00')
  })
})
