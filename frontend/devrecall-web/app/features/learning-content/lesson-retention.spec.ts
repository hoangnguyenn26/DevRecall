import { mount } from '@vue/test-utils'
import { nextTick, ref } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import SaveLessonToKnowledge from './components/SaveLessonToKnowledge.vue'
import AddLessonToReview from './components/AddLessonToReview.vue'
import type { LearningContentDetail, LearningContentReviewBatch, LearningContentReviewCandidate,
  SaveLessonToKnowledgeInput, SavedLessonKnowledge } from './learning-content.types'

const apiMocks = vi.hoisted(() => ({
  saveToKnowledge: vi.fn<(slug: string, input: SaveLessonToKnowledgeInput) => Promise<SavedLessonKnowledge>>(),
  addToReview: vi.fn<(slug: string, keys: string[], submissionId: string) => Promise<LearningContentReviewBatch>>(),
}))

vi.mock('./learning-content.api', () => ({
  useLearningContentApi: () => apiMocks,
}))

const stubs = {
  UButton: {
    inheritAttrs: false,
    props: ['disabled', 'type', 'to'],
    emits: ['click'],
    template: '<button :type="type || \'button\'" :disabled="disabled" @click="$emit(\'click\')"><slot /></button>',
  },
  UModal: {
    props: ['open'],
    template: '<section v-if="open" role="dialog"><slot name="body" /></section>',
  },
  UFormField: { template: '<label><span><slot name="label" /></span><slot /></label>' },
  UInput: {
    props: ['modelValue'], emits: ['update:modelValue'],
    template: '<input :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)">',
  },
  UTextarea: {
    props: ['modelValue', 'placeholder'], emits: ['update:modelValue'],
    template: '<textarea :value="modelValue" :placeholder="placeholder" @input="$emit(\'update:modelValue\', $event.target.value)" />',
  },
  USelect: {
    props: ['modelValue'], emits: ['update:modelValue'],
    template: '<select />',
  },
  UAlert: { props: ['title', 'description'], template: '<div role="alert">{{ title }} {{ description }}</div>' },
  UIcon: { template: '<i />' },
}

const lesson: LearningContentDetail = {
  id: 'lesson-id', slug: 'service-lifetimes', title: 'Service Lifetimes',
  summary: 'Choose the right lifetime.', contentType: 'Lesson', difficulty: 'Intermediate',
  estimatedMinutes: 15, technologies: [], topics: [], objectives: [],
  sections: [{ position: 0, type: 'KeyTakeaway', heading: null,
    bodyMarkdown: 'Scoped services are reused within one request.' }],
  reviewCandidates: [], source: { type: 'Internal', name: 'DevRecall', url: null },
  publishedAtUtc: '2026-08-13T00:00:00Z', progressStatus: 'Completed',
  progress: { status: 'Completed', startedAtUtc: '2026-08-13T00:00:00Z',
    completedAtUtc: '2026-08-13T00:10:00Z', version: 2 },
}

describe('Post-lesson retention dialogs', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    let sequence = 0
    vi.stubGlobal('crypto', { randomUUID: () => `submission-${++sequence}` })
    vi.stubGlobal('useApi', () => ({ get: vi.fn<(path: string) => Promise<unknown>>() }))
    vi.stubGlobal('useConfirmDialog', () => ({ open: vi.fn<() => Promise<boolean>>().mockResolvedValue(true) }))
    vi.stubGlobal('useAsyncData', (key: string) => ({
      data: ref(key.includes('topics') ? { items: [] } : []),
      refresh: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
    }))
    vi.stubGlobal('clearNuxtData', vi.fn<(key: unknown) => void>())
    vi.stubGlobal('useLearningDataInvalidation', () => ({
      afterReviewItemsAdded: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
    }))
  })

  it('prefills an editable Knowledge draft, preserves it on retry, and starts a new submission later', async () => {
    apiMocks.saveToKnowledge.mockRejectedValueOnce({
      data: { status: 503, title: 'Unavailable', detail: 'The source changed.' },
    }).mockResolvedValueOnce({ id: 'note-1', title: lesson.title, alreadyExisted: false })
    const wrapper = mount(SaveLessonToKnowledge, { props: { lesson }, global: { stubs } })

    await wrapper.get('button').trigger('click')
    expect(wrapper.text()).toContain(`From lesson: ${lesson.title}`)
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe(lesson.title)
    expect((wrapper.get('textarea').element as HTMLTextAreaElement).value)
      .toContain('Scoped services are reused within one request.')
    expect(wrapper.get('textarea').attributes('placeholder')).toContain('ideas you want to keep')

    await wrapper.get('textarea').setValue('My personal explanation')
    await wrapper.get('form').trigger('submit')
    await nextTick()
    expect(wrapper.text()).toContain("We couldn't save this note")
    expect((wrapper.get('textarea').element as HTMLTextAreaElement).value).toBe('My personal explanation')
    await wrapper.get('form').trigger('submit')
    await nextTick()

    expect(apiMocks.saveToKnowledge).toHaveBeenCalledTimes(2)
    expect(apiMocks.saveToKnowledge.mock.calls.at(0)![1].submissionId).toBe('submission-1')
    expect(apiMocks.saveToKnowledge.mock.calls.at(1)![1].submissionId).toBe('submission-1')
    expect(wrapper.text()).toContain('Saved to Knowledge')

    await wrapper.findAll('button').find(button => button.text() === 'Done')!.trigger('click')
    await wrapper.get('button').trigger('click')
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe(lesson.title)
    await wrapper.get('form').trigger('submit')
    expect(apiMocks.saveToKnowledge.mock.calls.at(2)![1].submissionId).toBe('submission-2')
  })

  it('starts Review empty, previews answers without mutation, and safely retries the same selection', async () => {
    const candidates: LearningContentReviewCandidate[] = [
      { key: 'scoped', prompt: 'How does Scoped behave?', answer: 'One instance per request scope.', isInReview: false },
      { key: 'existing', prompt: 'Why avoid captive dependencies?', answer: 'They outlive their scope.', isInReview: true },
    ]
    apiMocks.addToReview.mockRejectedValueOnce({
      data: { status: 409, title: 'Conflict', detail: 'Refresh the lesson.' },
    }).mockResolvedValueOnce({ createdCount: 1, existingCount: 0,
      items: [{ candidateKey: 'scoped', reviewItemId: 'review-1', wasCreated: true }] })
    const wrapper = mount(AddLessonToReview, { props: { slug: lesson.slug, candidates }, global: { stubs } })

    await wrapper.get('button').trigger('click')
    const checkboxes = wrapper.findAll('input[type="checkbox"]')
    expect(checkboxes).toHaveLength(2)
    expect((checkboxes.at(0)!.element as HTMLInputElement).checked).toBe(false)
    expect((checkboxes.at(1)!.element as HTMLInputElement).disabled).toBe(true)
    expect(wrapper.text()).toContain('Already in Review')

    await wrapper.findAll('button').find(button => button.text() === 'Preview answer')!.trigger('click')
    expect(wrapper.text()).toContain('One instance per request scope.')
    expect(apiMocks.addToReview).not.toHaveBeenCalled()

    await checkboxes.at(0)!.setValue(true)
    expect(wrapper.text()).toContain('Add 1 to Review')
    await wrapper.get('form').trigger('submit')
    await nextTick()
    expect(wrapper.text()).toContain("We couldn't add these concepts to Review")
    expect((wrapper.get('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true)
    await wrapper.get('form').trigger('submit')
    await nextTick()

    expect(apiMocks.addToReview).toHaveBeenCalledTimes(2)
    expect(apiMocks.addToReview.mock.calls[0]).toEqual([lesson.slug, ['scoped'], 'submission-1'])
    expect(apiMocks.addToReview.mock.calls[1]).toEqual([lesson.slug, ['scoped'], 'submission-1'])
    expect(wrapper.emitted('added')).toEqual([[['scoped']]])
    expect(wrapper.text()).toContain('1 concept added to Review')
  })

  it('hides Review when no candidates exist and explains the already-added state without a checkbox', async () => {
    const empty = mount(AddLessonToReview, { props: { slug: lesson.slug, candidates: [] }, global: { stubs } })
    expect(empty.text()).toBe('')

    const existing = mount(AddLessonToReview, { props: { slug: lesson.slug, candidates: [{
      key: 'existing', prompt: 'Existing prompt', answer: 'Existing answer', isInReview: true,
    }] }, global: { stubs } })
    expect(existing.text()).toContain('Key concepts are already in Review.')
    expect(existing.find('input').exists()).toBe(false)
  })
})
