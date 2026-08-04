import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { mount } from '@vue/test-utils'
import { reactive, ref } from 'vue'
import { describe, expect, it, vi } from 'vitest'
import AppContainer from '~/components/core/AppContainer.vue'
import PageHeader from '~/components/core/PageHeader.vue'
import ThemeToggle from '~/components/core/ThemeToggle.vue'
import AppEmptyState from '~/components/feedback/AppEmptyState.vue'
import { useApi } from '~/composables/useApi'
import { ApiError } from '~/types/api'
import { normalizeApiError } from '~/utils/normalize-api-error'
import { studyPlanStatusMeta } from '~/utils/status-meta'

describe('DevRecall UI foundation', () => {
  it('maps every supported container size', async () => {
    const expected = { reading: 'max-w-[46rem]', standard: 'max-w-6xl', wide: 'max-w-[90rem]', full: 'max-w-none' }
    for (const [size, className] of Object.entries(expected)) {
      const wrapper = mount(AppContainer, { props: { size: size as keyof typeof expected } })
      expect(wrapper.classes()).toContain(className)
    }
  })

  it('renders the page heading and its action', async () => {
    const wrapper = mount(PageHeader, {
      props: { title: 'A deliberately long page title', description: 'Context', eyebrow: 'Internal' },
      slots: { actions: '<button>Primary action</button>' },
    })
    expect(wrapper.get('h1').text()).toBe('A deliberately long page title')
    expect(wrapper.get('button').text()).toBe('Primary action')
  })

  it('exposes a useful empty-state action', async () => {
    const wrapper = mount(AppEmptyState, {
      props: { title: 'No knowledge yet', description: 'Capture the first concept.' },
      slots: { actions: '<button>Create knowledge</button>' },
      global: { stubs: { UIcon: true } },
    })
    expect(wrapper.text()).toContain('Capture the first concept.')
    expect(wrapper.get('button').text()).toBe('Create knowledge')
  })

  it('maps lifecycle status and normalizes stable API error codes', () => {
    expect(studyPlanStatusMeta.Ready).toMatchObject({ label: 'Ready', tone: 'info' })
    const normalized = normalizeApiError(new ApiError({ status: 409, title: 'Conflict', code: 'STUDY_PLAN_VERSION_CONFLICT' }))
    expect(normalized).toMatchObject({ status: 409, code: 'STUDY_PLAN_VERSION_CONFLICT' })
  })

  it('gives the theme toggle an accessible label', async () => {
    const colorMode = reactive({ value: 'dark', preference: 'system' })
    document.documentElement.classList.add('dark')
    vi.stubGlobal('useColorMode', () => colorMode)
    const wrapper = mount(ThemeToggle, {
      global: { stubs: { UButton: { template: '<button v-bind="$attrs"><slot /></button>' } } },
    })
    expect(wrapper.get('button').attributes('aria-label')).toBe('Switch to light mode')
    await wrapper.get('button').trigger('click')
    expect(colorMode.preference).toBe('light')
    document.documentElement.classList.remove('dark')
    vi.unstubAllGlobals()
  })

  it('sends cookies and provides a reduced-motion fallback', async () => {
    vi.stubGlobal('useRuntimeConfig', () => ({ public: { apiBaseUrl: 'http://localhost/api/v1' } }))
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }))
    await useApi().get('/health-check')
    expect(fetchMock).toHaveBeenCalledWith(expect.any(URL), expect.objectContaining({ credentials: 'include' }))
    fetchMock.mockRestore()
    vi.unstubAllGlobals()

    const cssPath = resolve(process.cwd(), 'app/assets/css/main.css')
    expect(readFileSync(cssPath, 'utf8')).toContain('@media (prefers-reduced-motion: reduce)')
  })

  it('redirects anonymous users but allows an authenticated app route', async () => {
    vi.stubGlobal('defineNuxtRouteMiddleware', (handler: unknown) => handler)
    const navigateTo = vi.fn<(input: unknown) => void>()
    const auth = { restoreSession: vi.fn<() => Promise<void>>(async () => undefined), isAuthenticated: ref(false) }
    vi.stubGlobal('navigateTo', navigateTo)
    vi.stubGlobal('useAuth', () => auth)
    const { default: guard } = await import('~/middleware/auth')

    await guard({ fullPath: '/app' } as never, {} as never)
    expect(navigateTo).toHaveBeenCalledWith({ path: '/login', query: { redirect: '/app' } })

    navigateTo.mockClear()
    auth.isAuthenticated.value = true
    await guard({ fullPath: '/app' } as never, {} as never)
    expect(navigateTo).not.toHaveBeenCalled()
    vi.unstubAllGlobals()
  })
})
