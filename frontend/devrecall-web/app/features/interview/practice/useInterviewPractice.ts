import type { InterviewPractice, InterviewPracticePhase, InterviewPracticeResult, InterviewSelfRating } from './interview-practice.types'
import { computed, ref } from 'vue'

export type CompleteInterviewPractice = (request: {
  answer: string
  selfRating: InterviewSelfRating
  followUps: { followUpId: string; answer: string }[]
  startedAtUtc: string
  submissionId: string
}) => Promise<InterviewPracticeResult>

export function useInterviewPractice(complete: CompleteInterviewPractice, createId: () => string = () => crypto.randomUUID()) {
  const practice = ref<InterviewPractice>()
  const phase = ref<InterviewPracticePhase>('answering')
  const answer = ref('')
  const selfRating = ref<InterviewSelfRating>()
  const currentFollowUpIndex = ref(0)
  const currentFollowUpAnswer = ref('')
  const followUpAnswers = ref<Record<string, string>>({})
  const startedAtUtc = ref(new Date().toISOString())
  const submissionId = ref(createId())
  const result = ref<InterviewPracticeResult>()
  const error = ref<unknown>()
  const busy = computed(() => phase.value === 'submitting')
  const currentFollowUp = computed(() => practice.value?.followUps[currentFollowUpIndex.value])
  const dirty = computed(() => !!answer.value.trim() || !!selfRating.value || !!currentFollowUpAnswer.value.trim() || Object.keys(followUpAnswers.value).length > 0)

  function start(payload: InterviewPractice): void { practice.value = payload }
  function compare(): boolean {
    if (!answer.value.trim() || phase.value !== 'answering') return false
    phase.value = 'comparing'
    return true
  }
  function rate(rating: InterviewSelfRating): void { if (phase.value === 'comparing') selfRating.value = rating }
  async function continueAfterRating(): Promise<boolean> {
    if (!selfRating.value || phase.value !== 'comparing') return false
    if (practice.value?.followUps.length) { phase.value = 'follow-up'; return true }
    return submit()
  }
  async function nextFollowUp(skip = false): Promise<boolean> {
    const current = currentFollowUp.value
    if (!current || phase.value !== 'follow-up') return false
    if (!skip && currentFollowUpAnswer.value.trim()) followUpAnswers.value[current.followUpId] = currentFollowUpAnswer.value.trim()
    currentFollowUpAnswer.value = ''
    if (practice.value && currentFollowUpIndex.value < practice.value.followUps.length - 1) { currentFollowUpIndex.value++; return true }
    return submit()
  }
  async function submit(): Promise<boolean> {
    if (!practice.value || !selfRating.value || busy.value) return false
    const previous = phase.value
    phase.value = 'submitting'; error.value = undefined
    try {
      result.value = await complete({ answer: answer.value.trim(), selfRating: selfRating.value,
        followUps: Object.entries(followUpAnswers.value).map(([followUpId, value]) => ({ followUpId, answer: value })),
        startedAtUtc: startedAtUtc.value, submissionId: submissionId.value })
      phase.value = 'completed'
      return true
    } catch (caught) { error.value = caught; phase.value = previous; return false }
  }
  function reset(): void {
    phase.value = 'answering'; answer.value = ''; selfRating.value = undefined
    currentFollowUpIndex.value = 0; currentFollowUpAnswer.value = ''; followUpAnswers.value = {}
    startedAtUtc.value = new Date().toISOString(); submissionId.value = createId(); result.value = undefined; error.value = undefined
  }
  return { practice, phase, answer, selfRating, currentFollowUpIndex, currentFollowUp, currentFollowUpAnswer,
    startedAtUtc, submissionId, result, error, busy, dirty, start, compare, rate, continueAfterRating, nextFollowUp, submit, reset }
}
