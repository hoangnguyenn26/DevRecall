<script setup lang="ts">
import { ApiError } from '~/types/api'
import { createPracticeProgress } from '../../practice/usePracticeSession'
import { usePracticeExit } from '../../practice/usePracticeExit'
import { usePracticeShortcuts } from '../../practice/usePracticeShortcuts'
import type { PracticeShortcut } from '../../practice/practice.types'
import PracticeShell from '../../practice/components/PracticeShell.vue'
import PracticeLoadingState from '../../practice/components/PracticeLoadingState.vue'
import PracticeErrorState from '../../practice/components/PracticeErrorState.vue'
import PracticeEmptyState from '../../practice/components/PracticeEmptyState.vue'
import PracticeShortcutHelp from '../../practice/components/PracticeShortcutHelp.vue'
import { reviewRatings } from '../review.meta'
import type { ReviewRating } from '../review.types'
import { useReviewApi } from '../review.api'
import { useReviewSession } from '../useReviewSession'
import ReviewPrompt from './ReviewPrompt.vue'
import ReviewAnswer from './ReviewAnswer.vue'
import ReviewRatingActions from './ReviewRatingActions.vue'
import ReviewScheduleFeedback from './ReviewScheduleFeedback.vue'
import ReviewCompletionSummary from './ReviewCompletionSummary.vue'

const reviewApi = useReviewApi()
const { afterReviewEvaluation } = useLearningDataInvalidation()
const loading = ref(true)
const loadError = ref<unknown>()
const helpOpen = ref(false)
const session = useReviewSession((item, rating, submissionId) => reviewApi.rate(item.reviewItemId, { evaluation: rating, expectedReviewCount: item.reviewCount, submissionId }))
const progress = computed(() => createPracticeProgress(session.completedCount.value, session.items.value.length))
const context = computed(() => ({ module: 'Review' as const, title: 'Review', progress: progress.value }))
const hasUnsubmittedWork = computed(() => session.phase.value === 'answer')
const { exit } = usePracticeExit({ exitTo: '/app/review', hasUnsubmittedWork, busy: session.pending })

async function load(): Promise<void> {
  loading.value = true
  loadError.value = undefined
  try {
    const queue = await reviewApi.getDue(20)
    session.start(queue.items, queue.totalCount)
  } catch (error) { loadError.value = error } finally { loading.value = false }
}

function reveal(): void { session.reveal(); nextTick(() => document.querySelector<HTMLElement>('[aria-live="polite"] h2')?.focus()) }
async function rate(rating: ReviewRating): Promise<void> { if (await session.rate(rating)) await afterReviewEvaluation() }
function next(): void { session.next(); nextTick(() => document.querySelector<HTMLElement>('h1')?.focus()) }
function isConflict(): boolean { return session.mutationError.value instanceof ApiError && session.mutationError.value.problem.status === 409 }
function skipConflict(): void { session.mutationError.value = undefined; session.phase.value = 'feedback'; next() }
async function retryRating(): Promise<void> { if (session.lastRating.value) await rate(session.lastRating.value) }

const shortcuts = computed<PracticeShortcut[]>(() => [
  { id: 'help', keys: ['?'], label: 'Open shortcut help', enabled: () => !session.pending.value, execute: () => { helpOpen.value = !helpOpen.value } },
  { id: 'exit', keys: ['Escape'], label: 'Exit session', enabled: () => !session.pending.value, execute: () => helpOpen.value ? (helpOpen.value = false) : exit() },
  { id: 'reveal', keys: [' '], label: 'Reveal answer', enabled: () => session.phase.value === 'prompt' && !helpOpen.value, execute: reveal },
  ...reviewRatings.map((rating, index) => ({ id: `rate-${rating}`, keys: [String(index + 1)], label: rating, enabled: () => session.phase.value === 'answer' && !helpOpen.value, execute: () => rate(rating) })),
  { id: 'next', keys: ['Enter'], label: 'Next item', enabled: () => session.phase.value === 'feedback' && !helpOpen.value, execute: next },
])
usePracticeShortcuts(shortcuts)
onMounted(load)
</script>

<template>
  <PracticeShell :context="context" :busy="session.pending.value" @exit="exit">
    <PracticeLoadingState v-if="loading" />
    <PracticeErrorState v-else-if="loadError" :error="loadError" return-to="/app/review" @retry="load" />
    <PracticeEmptyState v-else-if="!session.items.value.length" description="There are no review items due right now." />
    <ReviewCompletionSummary v-else-if="session.phase.value === 'completed'" :results="session.results.value" :duration-minutes="session.durationMinutes.value" :remaining-due-count="session.remainingDueCount.value" />
    <template v-else-if="session.currentItem.value">
      <ReviewPrompt :title="session.currentItem.value.resourceTitle" :resource-type="session.currentItem.value.resourceType" />
      <ReviewAnswer v-if="session.phase.value !== 'prompt'" :answer="session.currentItem.value.resourcePreview" />
      <ReviewScheduleFeedback v-if="session.phase.value === 'feedback' && session.results.value.at(-1)" :result="session.results.value.at(-1)!" />
      <FeedbackAppErrorState v-if="session.mutationError.value" class="mx-auto mt-5 w-full max-w-2xl" title="Unable to save this review" :description="isConflict() ? 'This review item was already updated elsewhere.' : 'Your answer is still here. Retry with the same submission identifier.'" @retry="isConflict() ? load() : retryRating()" />
      <UButton v-if="isConflict()" class="mx-auto mt-3" color="neutral" variant="outline" @click="skipConflict">Skip to next</UButton>
    </template>
    <template v-if="!loading && !loadError && session.currentItem.value && session.phase.value !== 'completed'" #shortcut-help><PracticeShortcutHelp v-model:open="helpOpen" :shortcuts="shortcuts" /></template>
    <template v-if="!loading && !loadError && session.currentItem.value && session.phase.value !== 'completed'" #actions>
      <UButton v-if="session.phase.value === 'prompt'" class="w-full justify-center sm:w-auto" size="lg" icon="i-lucide-eye" @click="reveal">Show answer <UKbd class="ml-2">Space</UKbd></UButton>
      <ReviewRatingActions v-else-if="session.phase.value === 'answer' || session.phase.value === 'submitting'" :disabled="session.pending.value" @rate="rate" />
      <UButton v-else-if="session.phase.value === 'feedback'" class="w-full justify-center sm:w-auto" size="lg" trailing-icon="i-lucide-arrow-right" @click="next">Next <UKbd class="ml-2">Enter</UKbd></UButton>
    </template>
  </PracticeShell>
</template>
