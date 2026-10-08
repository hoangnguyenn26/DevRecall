import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import NextActionCard from '~/components/today/NextActionCard.vue'
import Dashboard from '~/components/today/Dashboard.vue'
import { useTodayApi } from './today.api'
import { buildWeeklyActivityDescription, buildWeeklyActivityInsight, formatTodayMinutes, getDayPeriod } from './today.format'
import { getTodayActionIcon } from './today.meta'
import type { TodayDashboard, TodayNextAction } from './today.types'

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
  it('keeps priority conflicts in the backend and renders one calm learning action', () => {
    const actions: TodayNextAction[] = [
      { ...action, type: 'StartReview', title: '5 cards are due', description: 'Scheduled recall is ready.', actionLabel: 'Start review', targetPath: '/app/review' },
      { ...action, type: 'ContinueStudySession', title: 'Backend Foundations', description: 'You already started this study session.', actionLabel: 'Continue session', targetPath: '/app/study-sessions/session', context: { resourceId: 'session', resourceType: null, resourceTitle: 'Backend Foundations', plannedDurationMinutes: null, remainingCount: 3, priority: 'Critical' } },
      { ...action, type: 'ContinueLearning', title: 'Middleware Pipeline', description: 'Continue the lesson you started.', actionLabel: 'Continue lesson', targetPath: '/app/learn/middleware?returnTo=/app' },
      { ...action, type: 'StartStudyPlan', title: 'Planned documentation', description: 'You planned this work for study.', actionLabel: 'Open study plan', targetPath: '/app/study-plans/plan', context: { resourceId: 'plan', resourceType: 'StudyPlan', resourceTitle: 'Planned documentation', plannedDurationMinutes: 45, remainingCount: 2, priority: null } },
      { ...action, type: 'LearnRecommendedContent', title: 'CancellationToken', description: 'Matches your focus: .NET', actionLabel: 'Open lesson', targetPath: '/app/learn/cancellation?returnTo=/app' },
    ]
    for (const nextAction of actions) {
      const wrapper = mount(NextActionCard, { props: { action: nextAction }, global: { stubs } })
      expect(wrapper.findAll('a')).toHaveLength(1)
      expect(wrapper.get('a').attributes('href')).toBe(nextAction.targetPath)
      expect(wrapper.text()).toContain(nextAction.title)
      expect(wrapper.text()).toContain(nextAction.description)
      expect(wrapper.text()).not.toMatch(/Critical|% complete|priority score|Mastered/)
      if (nextAction.type === 'ContinueStudySession') expect(wrapper.text()).toContain('3 items remaining')
      if (nextAction.type === 'StartStudyPlan') expect(wrapper.text()).toContain('Estimated plan time: 45 min')
      wrapper.unmount()
    }
  })

  it('does not render competing dashboards or recent activity beside the primary', () => {
    const dashboard: TodayDashboard = {
      generatedAtUtc: '2026-10-08T00:00:00Z', user: { userId: 'owner', displayName: 'Learner', hasCompletedOnboarding: true },
      nextAction: action, recentActivity: { type: 'StudySession', resourceId: 'same-session', title: 'Duplicate session', description: 'Resume', occurredAtUtc: '', targetPath: '/app/study-sessions/same-session', icon: 'timer' },
      metrics: { reviewsDue: 5, studyMinutesThisWeek: 30, activeDaysThisWeek: 1, weeklyTargetDays: 5, weeklyProgressPercent: 20 },
      studyPlan: null, recommendations: [], weakTopics: [], weeklyActivity: [],
    }
    const wrapper = mount(Dashboard, { props: { dashboard }, global: { components: { TodayNextActionCard: NextActionCard }, stubs: { ...stubs, TodayWelcomeHeader: true, TodayOnboardingCallout: true } } })
    expect(wrapper.findAll('a')).toHaveLength(1)
    expect(wrapper.text()).not.toContain('Duplicate session')
    expect(wrapper.find('today-activity-section').exists()).toBe(false)
    expect(wrapper.find('today-recommendations-section').exists()).toBe(false)
  })

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

  it('builds a text alternative and deterministic activity insight', () => {
    const activity = [
      { date: '2026-08-03', studyMinutes: 20 },
      { date: '2026-08-04', studyMinutes: 45 },
      { date: '2026-08-05', studyMinutes: 0 },
    ]
    expect(buildWeeklyActivityDescription(activity)).toContain('2 active days')
    expect(buildWeeklyActivityInsight(activity, 5)).toBe('You studied on 2 of 5 target days this week.')
  })
})
