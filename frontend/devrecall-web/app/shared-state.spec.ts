import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import ConcurrencyConflictAlert from '~/components/feedback/ConcurrencyConflictAlert.vue'
import PageState from '~/components/feedback/PageState.vue'
import { useApiMutation } from '~/composables/useApiMutation'
import { createAsyncState } from '~/types/async-state'

const stubs = {
  FeedbackPageSkeleton: { template: '<div data-state="pending">Loading</div>' },
  FeedbackAppEmptyState: { props: ['title'], template: '<div data-state="empty">{{ title }}<slot /></div>' },
  FeedbackConcurrencyConflictAlert: { template: '<div data-state="conflict">Conflict</div>' },
  FeedbackAppErrorState: {
    props: ['title'],
    emits: ['retry'],
    template: '<div data-state="error">{{ title }}<button @click="$emit(\'retry\')">Retry</button></div>',
  },
  UAlert: { template: '<div><slot /><slot name="actions" /></div>' },
  UButton: { template: '<button><slot /></button>' },
  UIcon: true,
}

describe('shared application states', () => {
  it('creates a stable idle async state', () => {
    expect(createAsyncState<string>()).toEqual({ status: 'idle', data: null, error: null, refreshing: false })
  })

  it('prevents a mutation from executing twice while pending', async () => {
    vi.stubGlobal('useToast', () => ({ add: vi.fn<(input: unknown) => void>() }))
    let resolveMutation!: (value: string) => void
    const mutation = vi.fn<(request: string) => Promise<string>>(() => new Promise<string>((resolve) => {
      resolveMutation = resolve
    }))
    const state = useApiMutation(mutation)

    const first = state.execute('first')
    const duplicate = await state.execute('duplicate')
    expect(duplicate).toBeNull()
    expect(mutation).toHaveBeenCalledOnce()
    resolveMutation('saved')
    await expect(first).resolves.toBe('saved')
  })

  it('renders initial pending state separately from content', () => {
    const wrapper = mount(PageState, { props: { pending: true }, global: { stubs } })
    expect(wrapper.get('[data-state="pending"]').text()).toContain('Loading')
  })

  it('renders a dedicated not-found state', () => {
    const wrapper = mount(PageState, {
      props: { pending: false, error: { status: 404, code: 'NOT_FOUND', title: 'Not found', fieldErrors: {} } },
      global: { stubs },
    })
    expect(wrapper.get('[data-state="empty"]').text()).toContain('Resource not found')
  })

  it('renders an explicit empty state after a successful query', () => {
    const wrapper = mount(PageState, {
      props: { pending: false, empty: true, emptyTitle: 'No reviews due' },
      global: { stubs },
    })
    expect(wrapper.get('[data-state="empty"]').text()).toContain('No reviews due')
  })

  it('exposes manual retry for recoverable query failures', async () => {
    const wrapper = mount(PageState, {
      props: { pending: false, error: { status: 500, code: 'SERVER_ERROR', title: 'Unavailable', fieldErrors: {} } },
      global: { stubs },
    })
    await wrapper.get('[data-state="error"] button').trigger('click')
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })

  it('renders concurrency feedback for a 409 without retrying automatically', () => {
    const wrapper = mount(PageState, {
      props: { pending: false, error: { status: 409, code: 'CONFLICT', title: 'Conflict', fieldErrors: {} } },
      global: { stubs },
    })
    expect(wrapper.get('[data-state="conflict"]').text()).toContain('Conflict')
  })

  it('emits reload only when the user requests the latest version', async () => {
    const wrapper = mount(ConcurrencyConflictAlert, { props: { resourceLabel: 'study plan' }, global: { stubs } })
    const button = wrapper.get('button')
    await button.trigger('click')
    expect(wrapper.emitted('reload')).toHaveLength(1)
  })

  it('keeps server trace references out of ordinary validation states', () => {
    const wrapper = mount(PageState, {
      props: {
        pending: false,
        error: { status: 400, code: 'VALIDATION_ERROR', title: 'Validation failed', traceId: 'hidden-trace', fieldErrors: { email: ['Required'] } },
      },
      global: { stubs },
    })
    expect(wrapper.text()).not.toContain('hidden-trace')
  })
})
