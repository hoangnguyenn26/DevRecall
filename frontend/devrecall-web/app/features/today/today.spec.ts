import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import NextActionCard from '~/components/today/NextActionCard.vue'
import { useTodayApi } from './today.api'
import { formatTodayMinutes, getDayPeriod } from './today.format'
import { getTodayActionIcon } from './today.meta'
import type { TodayNextAction } from './today.types'

const action: TodayNextAction = {
  type: 'CreateKnowledge',
  title: 'Capture your first concept',
  description: 'Start with something you recently learned.',
  actionLabel: 'Create knowledge',
  targetPath: '/app/knowledge?action=create',
  icon: 'book-open',
  context: null,
}
const stubs = {
  UIcon: true,
  UButton: {
    props: ['to'],
    template: '<a :href="to"><slot /></a>',
  },
}

describe('Today dashboard foundation', () => {
  it('formats short and long study durations readably', () => {
    expect(formatTodayMinutes(35)).toBe('35 min')
    expect(formatTodayMinutes(135)).toBe('2h 15m')
    expect(formatTodayMinutes(120)).toBe('2h')
  })

  it('uses local-hour greeting periods', () => {
    expect(getDayPeriod(new Date(2026, 7, 4, 8))).toBe('morning')
    expect(getDayPeriod(new Date(2026, 7, 4, 15))).toBe('afternoon')
    expect(getDayPeriod(new Date(2026, 7, 4, 20))).toBe('evening')
  })

  it('maps known semantic icons', () => {
    expect(getTodayActionIcon('timer')).toBe('i-lucide-timer')
  })

  it('falls back safely for an unknown icon key', () => {
    expect(getTodayActionIcon('external-class')).toBe(
      'i-lucide-circle-arrow-right')
  })

  it('renders exactly one primary action with its trusted target', () => {
    const wrapper = mount(NextActionCard, {
      props: { action },
      global: { stubs },
    })

    expect(wrapper.findAll('a')).toHaveLength(1)
    expect(wrapper.get('a').attributes('href')).toBe(
      '/app/knowledge?action=create')
  })

  it('renders the new-user onboarding copy', () => {
    const wrapper = mount(NextActionCard, {
      props: { action },
      global: { stubs },
    })

    expect(wrapper.text()).toContain('Capture your first concept')
    expect(wrapper.text()).toContain('Start with something you recently learned.')
  })

  it('loads Today from one composition endpoint', async () => {
    const get = vi.fn<(path: string) => Promise<never>>()
    vi.stubGlobal('useApi', () => ({ get }))

    useTodayApi().getDashboard()

    expect(get).toHaveBeenCalledOnce()
    expect(get).toHaveBeenCalledWith('/today')
  })
})
