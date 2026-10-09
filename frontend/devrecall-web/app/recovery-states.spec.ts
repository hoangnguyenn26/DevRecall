import { flushPromises, mount } from '@vue/test-utils'
import { computed, ref, watch } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import TodayPage from '~/pages/app/index.vue'
import ProfilePage from '~/pages/app/settings/learning-profile.vue'
import { queryKeys } from '~/query/query-keys'

const getProfile = vi.hoisted(() => vi.fn())
vi.mock('~/features/today/today.api', () => ({ useTodayApi: () => ({ getDashboard: vi.fn() }) }))
vi.mock('~/features/learning-profile/learning-profile.api', () => ({
  useLearningProfileApi: () => ({ get: getProfile, getOptions: vi.fn(), put: vi.fn() }),
}))
const profile = { isConfigured: true, targetRole: { value: 'BackendDeveloper', label: 'Backend' },
  experienceLevel: { value: 'Junior', label: 'Junior' }, availableMinutesPerDay: 30,
  technologies: [{ value: 'EfCore', label: 'EF Core', isPrimary: true }],
  goals: [{ value: 'ImproveBackendFundamentals', label: 'Backend fundamentals' }], version: 2, updatedAtUtc: null }
const stubs = {
  CoreAppContainer: { template: '<div><slot /></div>' },
  FeedbackPageState: { props: ['error'], template: '<div><p v-if="error">{{ error.title }}</p><slot v-else /></div>' },
  TodayDashboard: { template: '<p>Old primary action</p>' },
  TodayLearningProfileSetupCard: true,
  CorePageHeader: true, CoreLoadingState: true, CoreErrorState: true,
  LearningProfileLearningProfileSummary: true, LearningProfileLearningTechnologySelector: true,
  UButton: { props: ['to'], template: '<a v-if="to" :href="to"><slot /></a><button v-else><slot /></button>' },
  UAlert: { props: ['title', 'description'], template: '<div role="alert">{{ title }} {{ description }}</div>' },
}

describe('Learning recovery states', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.stubGlobal('computed', computed)
    vi.stubGlobal('ref', ref)
    vi.stubGlobal('watch', watch)
    vi.stubGlobal('definePageMeta', vi.fn())
    vi.stubGlobal('useSeoMeta', vi.fn())
    vi.stubGlobal('useUnsavedChangesGuard', vi.fn())
  })

  it('hides a cached Today action when refresh fails, then restores it after recovery', async () => {
    const failure = ref<{ status: number; title: string } | null>({ status: 503, title: 'Unavailable' })
    vi.stubGlobal('useApiQuery', (key: string) => ({ data: ref(key === queryKeys.today ? { nextAction: {} } : profile),
      error: ref(null), refreshError: key === queryKeys.today ? failure : ref(null), isPending: ref(false), refreshing: ref(false), refresh: vi.fn() }))
    const wrapper = mount(TodayPage, { global: { stubs } })
    expect(wrapper.text()).toContain("We couldn't load today's next action.")
    expect(wrapper.text()).not.toContain('Old primary action')
    expect(wrapper.get('a[href="/app/learn"]').text()).toBe('Explore Learn')
    failure.value = null
    await flushPromises()
    expect(wrapper.text()).toContain('Old primary action')
  })

  it('keeps edited Profile draft after failed conflict reload and replaces it only on success', async () => {
    const current = ref({ ...profile })
    const conflict = ref<{ status: number; title: string; detail: string } | null>({ status: 409, title: 'Conflict', detail: 'Changed elsewhere' })
    const clearError = vi.fn(() => { conflict.value = null })
    vi.stubGlobal('useApiMutation', () => ({ pending: ref(false), error: conflict, execute: vi.fn(), clearError }))
    vi.stubGlobal('useApiQuery', (key: string) => ({ data: key === 'learning-profile' ? current : ref({
      targetRoles: [profile.targetRole], experienceLevels: [profile.experienceLevel], technologyGroups: [],
      goals: profile.goals, studyTimeOptions: [30, 45, 60],
    }), isPending: ref(false), error: ref(null), refresh: vi.fn() }))
    getProfile.mockRejectedValueOnce(new Error('Offline')).mockResolvedValueOnce({ ...profile, availableMinutesPerDay: 45, version: 3 })
    const wrapper = mount(ProfilePage, { global: { stubs } })
    await wrapper.get('input[type="radio"][value="60"]').setValue(true)
    const reload = () => wrapper.findAll('button').find(button => button.text() === 'Reload latest')!
    await reload().trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain("Couldn't reload the latest profile")
    expect((wrapper.get('input[value="60"]').element as HTMLInputElement).checked).toBe(true)
    expect(current.value.version).toBe(2)
    expect(clearError).not.toHaveBeenCalled()
    await reload().trigger('click')
    await flushPromises()
    expect((wrapper.get('input[value="45"]').element as HTMLInputElement).checked).toBe(true)
    expect(current.value.version).toBe(3)
    expect(clearError).toHaveBeenCalledOnce()
  })
})
