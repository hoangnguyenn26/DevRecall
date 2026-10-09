import { mount } from '@vue/test-utils'
import { computed, ref } from 'vue'
import { describe, expect, it, vi } from 'vitest'
import DiscoverCard from './DiscoverCard.vue'
import { discoverReasons, invalidateDiscover, type DiscoverLesson, type DiscoverResource } from './discover'
import { useLearningContentProgressSync } from '~/composables/useLearningContentProgressSync'
import { useLearningProfileApi } from '../learning-profile/learning-profile.api'
import { lessonReturnTo, lessonReturnLabel } from '../learning-content/lesson-return'
import DiscoverPage from '~/pages/app/discover.vue'

const item: DiscoverLesson = {
  slug: 'service-lifetimes', title: 'Service Lifetimes', summary: 'Choose appropriate lifetimes.',
  difficulty: 'Intermediate', estimatedMinutes: 15, technologies: [], topics: [],
  reasons: [{ type: 'GoalMatch', goal: { value: 'PrepareForInterviews', label: 'Prepare for technical interviews' } }],
}
describe('Discover foundation', () => {
  function mountPage(result: { profileConfigured: boolean; recommended?: DiscoverLesson[]; trustedResources?: DiscoverResource[]; basedOnGoals: DiscoverLesson[]; basedOnWeakTopics: DiscoverLesson[] } | null, pending = false, error: object | null = null, refreshError: object | null = null) {
    const refresh = vi.fn<() => void>()
    vi.stubGlobal('definePageMeta', vi.fn<() => void>())
    vi.stubGlobal('useSeoMeta', vi.fn<() => void>())
    vi.stubGlobal('computed', computed)
    vi.stubGlobal('useApi', () => ({ get: vi.fn<() => Promise<unknown>>() }))
    vi.stubGlobal('useApiQuery', () => ({ data: ref(result), isPending: ref(pending), error: ref(error), refreshError: ref(refreshError), refresh }))
    return { refresh, wrapper: mount(DiscoverPage, { global: { stubs: {
      CorePageHeader: { template: '<header><slot /></header>' }, CoreEmptyState: { props: ['title', 'description'], template: '<div>{{ title }} {{ description }}<slot /></div>' },
      CoreErrorState: { emits: ['retry'], template: '<button @click="$emit(\'retry\')">Retry</button>' },
      UButton: { props: ['to'], template: '<a :href="to"><slot /></a>' },
      UCard: { template: '<div><slot /></div>' }, USkeleton: true,
      DiscoverCard: { props: ['item'], template: '<article>{{ item.title }}</article>' },
    } } }) }
  }
  it('offers optional setup and keeps the catalog accessible without a profile', () => {
    const { wrapper } = mountPage({ profileConfigured: false, basedOnGoals: [], basedOnWeakTopics: [] })
    expect(wrapper.text()).toContain('Set up learning profile')
    expect(wrapper.find('a[href="/app/learn"]').exists()).toBe(true)
    expect(wrapper.text()).not.toContain('Strengthen weak areas')
    expect(wrapper.text()).not.toContain('Based on your goals')
  })
  it('does not make completion claims for an empty goal match', () => {
    const { wrapper } = mountPage({ profileConfigured: true, basedOnGoals: [], basedOnWeakTopics: [] })
    expect(wrapper.text()).toContain('No matching learning suggestions right now')
    expect(wrapper.text()).toContain('full learning catalog')
    expect(wrapper.text()).not.toContain('Update learning profile')
    expect(wrapper.find('a[href="/app/learn"]').exists()).toBe(true)
    expect(wrapper.text()).not.toContain('completed all')
  })
  it('hides duplicated secondary sections and empty resources', () => {
    const { wrapper } = mountPage({ profileConfigured: true, basedOnGoals: [item], basedOnWeakTopics: [] })
    expect(wrapper.text()).not.toContain('Based on your goals')
    expect(wrapper.text()).not.toContain('Strengthen weak areas')
    expect(wrapper.text()).not.toContain('Trusted resources for your topics')
  })
  it('keeps resources available without lesson suggestions and uses navigation only', () => {
    const resource: DiscoverResource = { ...item, slug: 'concurrency-docs', resourceKind: 'Documentation', sourceName: 'Microsoft Learn',
      technologies: [{ value: 'EfCore', label: 'EF Core' }], topics: [{ slug: 'concurrency', name: 'Concurrency' }] }
    const page = mountPage({ profileConfigured: true, recommended: [], basedOnGoals: [], basedOnWeakTopics: [], trustedResources: [resource] }).wrapper
    expect(page.text()).toContain('Trusted resources for your topics')
    expect(page.text()).not.toContain('No matching learning suggestions')
    const card = mount(DiscoverCard, { props: { item: resource, resource: true }, global: { stubs: {
      UBadge: { template: '<span><slot /></span>' }, UButton: { props: ['to'], template: '<a :data-to="JSON.stringify(to)"><slot /></a>' },
    } } })
    expect(card.text()).toContain('Documentation · Microsoft Learn')
    expect(card.text()).toContain('~15 min')
    expect(card.text()).toContain('Why this resource')
    expect(card.text()).toContain('Open resource')
    expect(card.text()).not.toContain('Not started')
    expect(card.find('.tags').exists()).toBe(false) // Context remains on detail, not a metadata wall on the card.
    expect(card.find('a').attributes('data-to')).toContain('/app/learn/concurrency-docs')
    expect(card.find('a').attributes('data-to')).toContain('/app/discover')
    expect(discoverReasons({ ...resource, reasons: [{ type: 'TimeFit', availableMinutes: 15 }, { type: 'WeakTopicMatch', label: 'Concurrency' }] }, true))
      .toEqual(['Related to a weak topic: Concurrency'])
  })
  it('renders backend-ranked recommendations in order without inventing scores', () => {
    const { wrapper } = mountPage({ profileConfigured: false,
      recommended: [{ ...item, title: 'First-ranked' }, { ...item, slug: 'second', title: 'Second-ranked' }],
      basedOnGoals: [], basedOnWeakTopics: [] })
    expect(wrapper.text()).toContain('Recommended for you')
    expect(wrapper.findAll('article').map(card => card.text())).toEqual(['First-ranked', 'Second-ranked'])
    expect(wrapper.text()).not.toContain('% match')
    expect(wrapper.text().indexOf('Recommended for you')).toBeLessThan(wrapper.text().indexOf('Personalize your learning'))
    expect(wrapper.find('a[href="/app/settings/learning-profile"]').exists()).toBe(true)
  })
  it('provides loading skeletons and retry without hiding Learn', async () => {
    const loading = mountPage(null, true).wrapper
    expect(loading.find('[aria-busy="true"]').exists()).toBe(true)
    const { wrapper, refresh } = mountPage(null, false, { title: 'Unavailable', status: 503 })
    await wrapper.find('button').trigger('click')
    expect(refresh).toHaveBeenCalledOnce()
    expect(wrapper.find('a[href="/app/learn"]').exists()).toBe(true)
    const stale = mountPage({ profileConfigured: true, recommended: [item], basedOnGoals: [], basedOnWeakTopics: [] }, false, null, { status: 503 }).wrapper
    expect(stale.text()).toContain('Retry')
    expect(stale.text()).not.toContain(item.title)
  })
  it('allows Discover recovery but rejects external or unrelated return destinations', () => {
    expect(lessonReturnTo('/app/discover')).toBe('/app/discover')
    expect(lessonReturnLabel('/app/discover')).toBe('Back to Discover')
    expect(lessonReturnTo('//evil.example')).toBe('/app/learn')
    expect(lessonReturnTo('/app/discover-evil')).toBe('/app/learn')
  })
  it('shows a friendly reason and navigates without starting a lesson', () => {
    const wrapper = mount(DiscoverCard, { props: { item }, global: { stubs: {
      UBadge: { template: '<span><slot /></span>' },
      UButton: { props: ['to'], template: '<a :data-to="JSON.stringify(to)"><slot /></a>' },
    } } })
    expect(wrapper.text()).toContain('Why this lesson')
    expect(wrapper.text()).toContain('Matches your goal: Prepare for technical interviews')
    expect(wrapper.text()).toContain('Open lesson')
    expect(wrapper.find('a').attributes('data-to')).toContain('/app/discover')
    expect(wrapper.find('a').attributes('data-to')).toContain('/app/learn/service-lifetimes')
  })
  it('limits explanation to two goals', () => {
    const reasons = ['One', 'Two', 'Three'].map(label => ({ type: 'GoalMatch' as const, goal: { value: label, label } }))
    expect(discoverReasons({ ...item, reasons })).toEqual(['Matches your goal: One', 'Matches your goal: Two'])
  })
  it('formats truthful technology and self-reported fit reasons', () => {
    expect(discoverReasons({ ...item, reasons: [
      { type: 'PrimaryTechnologyMatch', value: 'AspNetCore', label: 'ASP.NET Core' }, { type: 'TimeFit', availableMinutes: 30 },
      { type: 'DifficultyFit' },
    ] })).toEqual(['Matches your focus: ASP.NET Core', 'Fits your 30 min/day preference'])
    expect(discoverReasons({ ...item, reasons: [{ type: 'SecondaryTechnologyMatch', label: 'EF Core' }, { type: 'DifficultyFit' }] }))
      .toEqual(['Related to: EF Core', 'Difficulty matches your learning profile'])
  })
  it('clears only Discover data without a background refresh', () => {
    const clear = vi.fn<(key: string | ((key: string) => boolean)) => void>()
    vi.stubGlobal('clearNuxtData', clear)
    invalidateDiscover()
    const predicate = clear.mock.calls[0]![0] as (key: string) => boolean
    expect(predicate('discover:current')).toBe(true)
    expect(predicate('today-dashboard')).toBe(false)
  })
  it.each(['start', 'complete'] as const)('invalidates Discover after %s', (action) => {
    const clear = vi.fn<(key: string | ((key: string) => boolean)) => void>()
    vi.stubGlobal('clearNuxtData', clear)
    vi.stubGlobal('useLearningDataInvalidation', () => ({ afterLessonCompleted: vi.fn<() => void>() }))
    useLearningContentProgressSync().afterProgressChanged(action)
    expect(clear.mock.calls.some(([value]) => typeof value === 'function' && value('discover:current'))).toBe(true)
  })
  it('invalidates only after a successful profile save', async () => {
    const clear = vi.fn<(key: string | ((key: string) => boolean)) => void>()
    const put = vi.fn<() => Promise<unknown>>().mockResolvedValue({ isConfigured: true })
    vi.stubGlobal('clearNuxtData', clear)
    vi.stubGlobal('useApi', () => ({ put }))
    const api = useLearningProfileApi()
    await api.put({ targetRole: 'BackendDeveloper', experienceLevel: 'Junior', availableMinutesPerDay: 15, technologies: [], goals: [], expectedVersion: null })
    expect(clear).toHaveBeenCalledTimes(2)
    expect(clear).toHaveBeenCalledWith('today-dashboard')
    expect(clear.mock.calls.some(([key]) => typeof key === 'function' && key('discover:current'))).toBe(true)
    clear.mockClear()
    put.mockRejectedValueOnce(new Error('Conflict'))
    await expect(api.put({ targetRole: '', experienceLevel: '', availableMinutesPerDay: 15, technologies: [], goals: [], expectedVersion: null })).rejects.toThrow('Conflict')
    expect(clear).not.toHaveBeenCalled()
  })
})
