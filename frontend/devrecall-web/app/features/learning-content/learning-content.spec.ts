import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import LearningMarkdown from '~/components/learning-content/LearningMarkdown.vue'
import LearningContentCard from '~/components/learning-content/LearningContentCard.vue'
import { filtersFromQuery, queryFromFilters, withFilter } from './learning-content.filters'
import { learningContentKeys } from './learning-content.query-keys'
import { isSafeMarkdownUrl, parseInlineMarkdown, parseMarkdown } from './learning-markdown'
import { buildLessonKnowledgeDraft } from './lesson-knowledge'
import type { LearningContentDetail } from './learning-content.types'

const card = {
  slug: 'aspnet-core-service-lifetimes', title: 'ASP.NET Core Service Lifetimes',
  summary: 'Choose the correct service lifetime.', contentType: 'Lesson' as const,
  difficulty: 'Intermediate' as const, estimatedMinutes: 15,
  technologies: [{ value: 'AspNetCore', label: 'ASP.NET Core' }],
  topics: [{ slug: 'dependency-injection', name: 'Dependency Injection' }],
  progressStatus: 'NotStarted' as const,
}

describe('Learn catalog and reader', () => {
  it('normalizes URL filters and omits default query values', () => {
    expect(filtersFromQuery({ technology: 'AspNetCore', difficulty: 'Intermediate', page: '3' }))
      .toEqual({ technology: 'AspNetCore', difficulty: 'Intermediate', page: 3 })
    expect(filtersFromQuery({ technology: 'Ninja', difficulty: 'Expert', page: '-1' }))
      .toEqual({ technology: undefined, difficulty: undefined, page: 1 })
    expect(queryFromFilters({ page: 1 })).toEqual({})
  })

  it.each([
    ['NotStarted', 'Start lesson'], ['InProgress', 'Continue'], ['Completed', 'Read again'],
  ] as const)('renders %s catalog state as %s', (progressStatus, label) => {
    const wrapper = mount(LearningContentCard, {
      props: { item: { ...card, progressStatus }, returnTo: '/app/learn' },
      global: { stubs: {
        UBadge: { template: '<span><slot /></span>' },
        UButton: { template: '<a><slot /></a>' },
      } },
    })
    expect(wrapper.text()).toContain(label)
  })

  it('resets pagination when a filter changes and preserves it otherwise', () => {
    const filtered = withFilter({ technology: 'CSharp', difficulty: 'Intermediate', page: 4 },
      'technology', 'EfCore')
    expect(filtered).toEqual({ technology: 'EfCore', difficulty: 'Intermediate', page: 1 })
    expect(learningContentKeys.list(filtered)).toContain('EfCore:Intermediate:1')
  })

  it('renders a learning-oriented lesson card without internal fields', () => {
    const wrapper = mount(LearningContentCard, {
      props: { item: card, returnTo: '/app/learn?difficulty=Intermediate' },
      global: { stubs: {
        UBadge: { template: '<span><slot /></span>' },
        UButton: { props: ['to'], template: '<a :data-to="JSON.stringify(to)"><slot /></a>' },
      } },
    })
    expect(wrapper.text()).toContain('Start lesson')
    expect(wrapper.text()).toContain('15 min')
    expect(wrapper.text()).not.toContain('Published')
    expect(wrapper.html()).toContain('returnTo')
  })

  it('parses supported blocks and preserves code characters as text', () => {
    const blocks = parseMarkdown('# Heading\n\n- one\n- two\n\n```csharp\nList<T> values;\n```')
    expect(blocks.map(block => block.type)).toEqual(['heading', 'list', 'code'])
    expect(blocks.at(-1)).toMatchObject({ type: 'code', code: 'List<T> values;' })
  })

  it('rejects executable link protocols and never creates executable HTML', () => {
    expect(isSafeMarkdownUrl('javascript:alert(1)')).toBe(false)
    expect(isSafeMarkdownUrl('data:text/html,test')).toBe(false)
    expect(isSafeMarkdownUrl('https://learn.microsoft.com')).toBe(true)
    expect(parseInlineMarkdown('[unsafe](javascript:alert(1))')).toContainEqual({
      type: 'link', text: 'unsafe', url: 'javascript:alert(1', safe: false,
    })
    const wrapper = mount(LearningMarkdown, {
      props: { markdown: '<script>alert(1)</script>\n\n[unsafe](javascript:alert(1))' },
    })
    expect(wrapper.find('script').exists()).toBe(false)
    expect(wrapper.find('a').exists()).toBe(false)
    expect(wrapper.text()).toContain('<script>alert(1)</script>')
  })

  it('renders code blocks with horizontal-safe markup and accessible copy control', () => {
    vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn<() => Promise<void>>().mockResolvedValue(undefined) } })
    const wrapper = mount(LearningMarkdown, {
      props: { markdown: '```csharp\nservices.AddScoped<IService, Service>();\n```' },
    })
    expect(wrapper.get('pre code').text()).toContain('AddScoped')
    expect(wrapper.get('button').attributes('aria-label')).toBe('Copy csharp example')
  })

  it('builds a deterministic Knowledge draft from key takeaways only', () => {
    const lesson = { ...card, objectives: [], source: { type: 'Internal' as const, name: 'DevRecall', url: null },
      publishedAtUtc: '2026-08-13T00:00:00Z', progress: { status: 'Completed' as const,
        startedAtUtc: null, completedAtUtc: '2026-08-13T00:00:00Z', version: 1 },
      sections: [
        { position: 0, type: 'Explanation' as const, heading: null, bodyMarkdown: 'Long lesson body' },
        { position: 2, type: 'KeyTakeaway' as const, heading: null, bodyMarkdown: 'Second takeaway' },
        { position: 1, type: 'KeyTakeaway' as const, heading: null, bodyMarkdown: 'First takeaway' },
      ] } satisfies LearningContentDetail

    expect(buildLessonKnowledgeDraft(lesson, 'submission-1')).toEqual({
      title: lesson.title, content: '## Key takeaways\n\nFirst takeaway\n\nSecond takeaway',
      topicId: null, tagIds: [], submissionId: 'submission-1',
    })
  })

  it('starts with an empty note body when a lesson has no key takeaways', () => {
    const lesson = { ...card, objectives: [], sections: [],
      source: { type: 'Internal' as const, name: 'DevRecall', url: null },
      publishedAtUtc: '2026-08-13T00:00:00Z', progress: { status: 'Completed' as const,
        startedAtUtc: null, completedAtUtc: '2026-08-13T00:00:00Z', version: 1 } } satisfies LearningContentDetail
    expect(buildLessonKnowledgeDraft(lesson, 'submission-2').content).toBe('')
  })
})
