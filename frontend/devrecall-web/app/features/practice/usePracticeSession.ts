import { computed, ref } from 'vue'
import type { PracticePhase, PracticeProgress } from './practice.types'

export function createPracticeProgress(completed: number, total: number): PracticeProgress {
  const safeTotal = Math.max(0, total)
  const safeCompleted = Math.min(Math.max(0, completed), safeTotal)
  return {
    current: safeCompleted < safeTotal ? safeCompleted + 1 : safeTotal,
    total: safeTotal,
    completed: safeCompleted,
    percent: safeTotal === 0 ? 0 : Math.round((safeCompleted / safeTotal) * 1000) / 10,
  }
}

export function usePracticeSession(initialPhase: PracticePhase = 'loading') {
  const phase = ref<PracticePhase>(initialPhase)
  const error = ref<unknown>()
  const busy = computed(() => phase.value === 'submitting')

  async function submit(action: () => Promise<void>): Promise<boolean> {
    if (busy.value) return false
    phase.value = 'submitting'
    error.value = undefined
    try {
      await action()
      return true
    } catch (caught) {
      error.value = caught
      return false
    } finally {
      if (phase.value === 'submitting') phase.value = 'active'
    }
  }

  return { phase, error, busy, submit }
}
