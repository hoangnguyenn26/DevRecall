import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AppSidebar from '~/components/navigation/AppSidebar.vue'
import { appNavigation } from './app-navigation'
import { isNavigationItemActive } from './navigation.utils'

let currentPath = '/app'

const globalStubs = {
  NavigationAppLogo: { template: '<a href="/app" aria-label="DevRecall Today">DevRecall</a>' },
  NavigationAppNavigationBadge: true,
  NuxtLink: { props: ['to'], template: '<a :href="to"><slot /></a>' },
  UButton: { template: '<button><slot /></button>' },
  UIcon: { template: '<span aria-hidden="true" />' },
  UTooltip: { template: '<div><slot /></div>' },
}

describe('application navigation', () => {
  beforeEach(() => vi.stubGlobal('useRoute', () => ({ path: currentPath })))

  it('uses exact matching for Today', () => {
    const today = appNavigation[0]!.items[0]!
    expect(isNavigationItemActive(today, '/app')).toBe(true)
    expect(isNavigationItemActive(today, '/app/knowledge')).toBe(false)
  })

  it('highlights a parent item on a nested route', () => {
    const plans = appNavigation.flatMap(section => section.items).find(item => item.label === 'Study Plans')!
    expect(isNavigationItemActive(plans, '/app/study-plans/123')).toBe(true)
  })

  it('keeps accessible names when the sidebar is collapsed', () => {
    currentPath = '/app/knowledge'
    const wrapper = mount(AppSidebar, { props: { collapsed: true }, global: { stubs: globalStubs } })
    expect(wrapper.get('nav').attributes('aria-label')).toBe('Application navigation')
    expect(wrapper.get('a[aria-label="Knowledge"]').attributes('aria-current')).toBe('page')
    expect(wrapper.get('a[aria-label="Learn"]').attributes('href')).toBe('/app/learn')
  })

  it('renders section labels in the expanded sidebar', () => {
    currentPath = '/app'
    const wrapper = mount(AppSidebar, { props: { collapsed: false }, global: { stubs: globalStubs } })
    expect(wrapper.text()).toContain('Practice')
    expect(wrapper.text()).toContain('Study Plans')
  })
})
