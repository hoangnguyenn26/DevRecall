import { computed, ref } from 'vue'
import type { DueReviewItem, ReviewCardPhase, ReviewRating, ReviewResult } from './review.types'

export type ReviewRateAction = (item: DueReviewItem, rating: ReviewRating, submissionId: string) => Promise<ReviewResult>

export function useReviewSession(rateAction: ReviewRateAction, createSubmissionId: () => string = () => crypto.randomUUID()) {
  const items = ref<DueReviewItem[]>([])
  const currentIndex = ref(0)
  const completedCount = ref(0)
  const phase = ref<ReviewCardPhase>('prompt')
  const currentSubmissionId = ref<string | null>(null)
  const lastRating = ref<ReviewRating | null>(null)
  const results = ref<ReviewResult[]>([])
  const mutationError = ref<unknown>()
  const totalDueCount = ref(0)
  const startedAt = ref(Date.now())
  const currentItem = computed(() => items.value[currentIndex.value] ?? null)
  const pending = computed(() => phase.value === 'submitting')

  function start(queue: DueReviewItem[], dueCount: number): void {
    items.value = queue
    totalDueCount.value = dueCount
    currentIndex.value = 0
    completedCount.value = 0
    results.value = []
    currentSubmissionId.value = null
    lastRating.value = null
    mutationError.value = undefined
    startedAt.value = Date.now()
    phase.value = queue.length ? 'prompt' : 'completed'
  }

  function reveal(): void {
    if (phase.value === 'prompt' && currentItem.value) phase.value = 'answer'
  }

  async function rate(rating: ReviewRating): Promise<boolean> {
    const item = currentItem.value
    if (!item || phase.value !== 'answer') return false
    phase.value = 'submitting'
    mutationError.value = undefined
    const submissionId = currentSubmissionId.value ?? createSubmissionId()
    currentSubmissionId.value = submissionId
    lastRating.value = rating
    try {
      const result = await rateAction(item, rating, submissionId)
      results.value.push(result)
      completedCount.value++
      phase.value = 'feedback'
      return true
    } catch (error) {
      mutationError.value = error
      phase.value = 'answer'
      return false
    }
  }

  function next(): void {
    if (phase.value !== 'feedback') return
    currentIndex.value++
    currentSubmissionId.value = null
    lastRating.value = null
    mutationError.value = undefined
    phase.value = currentItem.value ? 'prompt' : 'completed'
  }

  const remainingDueCount = computed(() => Math.max(0, totalDueCount.value - completedCount.value))
  const durationMinutes = computed(() => Math.max(1, Math.round((Date.now() - startedAt.value) / 60000)))
  return { items, currentIndex, completedCount, phase, results, mutationError, currentItem, pending, lastRating, remainingDueCount, durationMinutes, start, reveal, rate, next }
}
