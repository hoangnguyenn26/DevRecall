import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import Progress from '~/components/onboarding/Progress.vue'
import { canContinueOnboarding, onboardingDefaults, toggleFocusArea } from './onboarding.meta'
import { useOnboardingApi } from './onboarding.api'
import type { LearningFocusArea } from './onboarding.types'

describe('new-user onboarding foundation', () => {
  it('starts with the documented commitment defaults', () => {
    expect(onboardingDefaults()).toEqual({
      goal: null,
      focusAreas: [],
      dailyCommitmentMinutes: 30,
      weeklyTargetDays: 5,
    })
  })

  it('requires one learning goal before leaving the goal step', () => {
    const state = onboardingDefaults()
    expect(canContinueOnboarding(2, state)).toBe(false)
    state.goal = 'PrepareForInterviews'
    expect(canContinueOnboarding(2, state)).toBe(true)
  })

  it('requires at least one focus area', () => {
    const state = onboardingDefaults()
    expect(canContinueOnboarding(3, state)).toBe(false)
    state.focusAreas = ['DotNet']
    expect(canContinueOnboarding(3, state)).toBe(true)
  })

  it('does not select more than four focus areas', () => {
    const selected: LearningFocusArea[] = [
      'CSharp', 'DotNet', 'SystemDesign', 'Databases',
    ]
    expect(toggleFocusArea(selected, 'InterviewCommunication')).toEqual(selected)
  })

  it('allows a selected focus area to be removed at the limit', () => {
    const selected: LearningFocusArea[] = [
      'CSharp', 'DotNet', 'SystemDesign', 'Databases',
    ]
    expect(toggleFocusArea(selected, 'DotNet')).not.toContain('DotNet')
  })

  it('validates custom commitment boundaries', () => {
    const state = { ...onboardingDefaults(), goal: 'PracticeAlgorithms' as const, focusAreas: ['DotNet' as const] }
    state.dailyCommitmentMinutes = 9
    expect(canContinueOnboarding(4, state)).toBe(false)
    state.dailyCommitmentMinutes = 180
    state.weeklyTargetDays = 7
    expect(canContinueOnboarding(4, state)).toBe(true)
  })

  it('uses the dedicated complete and skip endpoints', async () => {
    const post = vi.fn<(path: string, body?: unknown) => Promise<{ hasCompleted: boolean }>>()
      .mockResolvedValue({ hasCompleted: true })
    vi.stubGlobal('useApi', () => ({ get: vi.fn<() => Promise<never>>(), post }))
    const api = useOnboardingApi()
    const request = {
      goal: 'PracticeAlgorithms' as const,
      focusAreas: ['AlgorithmsAndDataStructures' as const],
      dailyCommitmentMinutes: 30,
      weeklyTargetDays: 5,
    }

    await api.complete(request)
    await api.skip()

    expect(post).toHaveBeenNthCalledWith(1, '/onboarding/complete', request)
    expect(post).toHaveBeenNthCalledWith(2, '/onboarding/skip')
  })

  it('exposes progress as text as well as a visual indicator', () => {
    const wrapper = mount(Progress, { props: { currentStep: 2, totalSteps: 4 } })
    expect(wrapper.text()).toContain('Step 2 of 4')
    expect(wrapper.get('[aria-label="Onboarding progress"]')).toBeTruthy()
  })
})
