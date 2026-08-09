import type { DsaAttemptOutcome, DsaPracticePhase, DsaPracticeResult } from './dsa-practice.types'
import { computed, ref } from 'vue'

export type CompleteDsaPractice = (request: Record<string, unknown>) => Promise<DsaPracticeResult>

export function useDsaPractice(complete: CompleteDsaPractice, createId: () => string = () => crypto.randomUUID()) {
  const phase = ref<DsaPracticePhase>('solving')
  const solution = ref('')
  const language = ref('CSharp')
  const approach = ref('')
  const timeComplexity = ref('')
  const spaceComplexity = ref('')
  const outcome = ref<DsaAttemptOutcome>()
  const reflection = ref('')
  const startedAtUtc = ref(new Date().toISOString())
  const submissionId = ref(createId())
  const result = ref<DsaPracticeResult>()
  const error = ref<unknown>()
  const busy = computed(() => phase.value === 'submitting')
  const dirty = computed(() => [solution.value, approach.value, timeComplexity.value, spaceComplexity.value, reflection.value].some(value => value.trim()) || !!outcome.value)
  function finish(): boolean { if (phase.value !== 'solving') return false; phase.value = 'reflection'; return true }
  async function submit(): Promise<boolean> {
    if (!outcome.value || busy.value) return false
    phase.value = 'submitting'; error.value = undefined
    try {
      result.value = await complete({ result: outcome.value, language: language.value || null,
        solutionCode: solution.value.trim() || null, approach: approach.value.trim() || null,
        timeComplexity: timeComplexity.value.trim() || null, spaceComplexity: spaceComplexity.value.trim() || null,
        durationMinutes: 0, notes: reflection.value.trim() || null,
        attemptedAtUtc: startedAtUtc.value, startedAtUtc: startedAtUtc.value, submissionId: submissionId.value })
      phase.value = 'completed'; return true
    } catch (caught) { error.value = caught; phase.value = 'reflection'; return false }
  }
  function reset(): void {
    phase.value = 'solving'; solution.value = ''; language.value = 'CSharp'; approach.value = ''
    timeComplexity.value = ''; spaceComplexity.value = ''; outcome.value = undefined; reflection.value = ''
    startedAtUtc.value = new Date().toISOString(); submissionId.value = createId(); result.value = undefined; error.value = undefined
  }
  return { phase, solution, language, approach, timeComplexity, spaceComplexity, outcome, reflection,
    startedAtUtc, submissionId, result, error, busy, dirty, finish, submit, reset }
}
