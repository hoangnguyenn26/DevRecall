<script setup lang="ts">
import type { PracticeShortcut } from '~/features/practice/practice.types'
import type { InterviewSelfRating } from '~/features/interview/practice/interview-practice.types'
import { useInterviewPractice } from '~/features/interview/practice/useInterviewPractice'
import { useInterviewPracticeApi } from '~/features/interview/practice/interview-practice.api'
import { createPracticeProgress } from '~/features/practice/usePracticeSession'
import { usePracticeExit } from '~/features/practice/usePracticeExit'
import { usePracticeShortcuts } from '~/features/practice/usePracticeShortcuts'
import { useDraftClipboard } from '~/features/practice/useDraftClipboard'
import PracticeShell from '~/features/practice/components/PracticeShell.vue'
import PracticeLoadingState from '~/features/practice/components/PracticeLoadingState.vue'
import PracticeErrorState from '~/features/practice/components/PracticeErrorState.vue'
import PracticeCompletion from '~/features/practice/components/PracticeCompletion.vue'
import PracticeShortcutHelp from '~/features/practice/components/PracticeShortcutHelp.vue'

definePageMeta({ layout: 'focus', middleware: 'auth' })
useSeoMeta({ title: 'Interview practice', robots: 'noindex, nofollow' })
const route = useRoute()
const questionId = computed(() => String(route.params.id))
const api = useInterviewPracticeApi()
const session = useInterviewPractice(api.complete(questionId.value))
const loading = ref(true)
const loadError = ref<unknown>()
const shortcutsOpen = ref(false)
const toast = useToast()
const { afterInterviewPractice } = useLearningDataInvalidation()
const { copyDraft } = useDraftClipboard()
const ratings: { value: InterviewSelfRating; label: string; description: string }[] = [
  {
    value: 'NeedsWork',
    label: 'Needs work',
    description: 'I struggled to structure or explain the answer.',
  },
  {
    value: 'Fair',
    label: 'Fair',
    description: 'I covered the basics but missed important details.',
  },
  { value: 'Good', label: 'Good', description: 'My answer was clear and mostly complete.' },
  {
    value: 'Strong',
    label: 'Strong',
    description: 'My answer was concise, structured and confident.',
  },
]
const context = computed(() => ({
  module: 'Interview' as const,
  title: session.practice.value?.category || 'Interview practice',
  startedAtUtc: session.startedAtUtc.value,
  progress:
    session.phase.value === 'follow-up'
      ? createPracticeProgress(
          session.currentFollowUpIndex.value,
          session.practice.value?.followUps.length ?? 0,
        )
      : {
          kind: 'steps' as const,
          currentStep:
            session.phase.value === 'answering' ? 1 : session.phase.value === 'comparing' ? 2 : 3,
          totalSteps: 3,
          label:
            session.phase.value === 'answering'
              ? 'Answer'
              : session.phase.value === 'comparing'
                ? 'Compare'
                : 'Complete',
        },
}))
const exit = usePracticeExit({
  exitTo: `/app/interview/${questionId.value}`,
  hasUnsubmittedWork: session.dirty,
  busy: session.busy,
  title: 'Leave interview practice?',
  description: 'Your current practice answer has not been saved.',
})
const shortcuts = computed<PracticeShortcut[]>(() => [
  {
    id: 'help',
    keys: ['?'],
    label: 'Show shortcuts',
    enabled: () => true,
    execute: () => {
      shortcutsOpen.value = true
    },
  },
  {
    id: 'exit',
    keys: ['Escape'],
    label: 'Exit practice',
    enabled: () => !session.busy.value,
    execute: () => { if (shortcutsOpen.value) { shortcutsOpen.value = false; return } return exit.exit() },
  },
  {
    id: 'compare',
    keys: ['Ctrl', 'Enter'],
    label: session.phase.value === 'follow-up' ? 'Continue follow-up' : 'Compare answer',
    enabled: () => session.phase.value === 'answering' || session.phase.value === 'follow-up',
    execute: () => {
      if (session.phase.value === 'follow-up') return finishFollowUp(false)
      session.compare()
    },
  },
  ...ratings.map((rating, index): PracticeShortcut => ({
    id: rating.value,
    keys: [String(index + 1)],
    label: rating.label,
    enabled: () => session.phase.value === 'comparing',
    execute: () => session.rate(rating.value),
  })),
])
usePracticeShortcuts(shortcuts)
async function load() {
  loading.value = true
  loadError.value = undefined
  try {
    session.start(await api.get(questionId.value))
  } catch (error) {
    loadError.value = error
  } finally {
    loading.value = false
  }
}
async function invalidateCompletion(): Promise<void> {
  try {
    await afterInterviewPractice(questionId.value)
  } catch {
    toast.add({
      title: 'Practice saved',
      description: 'History will refresh when you return.',
      color: 'warning',
    })
  }
}
async function finishRating() {
  if ((await session.continueAfterRating()) && session.phase.value === 'completed')
    await invalidateCompletion()
}
async function finishFollowUp(skip: boolean) {
  if ((await session.nextFollowUp(skip)) && session.phase.value === 'completed')
    await invalidateCompletion()
}
onMounted(load)
</script>

<template>
  <PracticeShell :context="context" :busy="session.busy.value" @exit="exit.exit">
    <PracticeLoadingState v-if="loading" label="Preparing interview practice" />
    <PracticeErrorState v-else-if="loadError" :error="loadError" return-to="/app/interview" @retry="load" />
    <PracticeCompletion
      v-else-if="session.phase.value === 'completed' && session.result.value"
      :summary="{
        title: 'Practice complete',
        completedCount: 1,
        durationMinutes: Math.max(1, Math.ceil(session.result.value.durationSeconds / 60)),
        primaryMetricLabel: 'Self-rating',
        primaryMetricValue: ratings.find((item) => item.value === session.result.value?.selfRating)
          ?.label,
        secondaryMetrics: [
          { label: 'Follow-ups practiced', value: String(session.result.value.followUpsAnswered) },
        ],
      }"
      :primary-to="`/app/interview/${questionId}?attempt=${session.result.value.attemptId}`"
      primary-label="View attempt history"
    />
    <section v-else-if="session.practice.value" class="mx-auto w-full max-w-3xl">
      <p class="text-sm font-medium text-primary">
        {{ session.practice.value.category }} · {{ session.practice.value.difficulty }}
      </p>
      <h1 class="mt-3 text-2xl font-semibold leading-snug">
        {{
          session.phase.value === 'follow-up'
            ? session.currentFollowUp.value?.question
            : session.practice.value.question
        }}
      </h1>
      <template v-if="session.phase.value === 'answering'"
        ><UFormField label="Your answer" class="mt-8"
          ><UTextarea
            v-model="session.answer.value"
            :rows="12"
            autoresize
            class="w-full"
            placeholder="Explain the concept in your own words…"
        /></UFormField>
        <p class="mt-2 text-sm text-muted">
          Write your answer before comparing it with the reference. Ctrl/⌘ + Enter to compare.
        </p></template
      >
      <template v-else-if="session.phase.value === 'comparing'">
        <section class="mt-8 rounded-xl border border-default p-5">
          <h2 class="font-semibold">Your answer</h2>
          <p class="mt-3 whitespace-pre-wrap text-muted">{{ session.answer.value }}</p>
        </section>
        <section
          class="mt-4 rounded-xl border border-primary/30 bg-primary/5 p-5"
          tabindex="-1"
          aria-live="polite"
        >
          <h2 class="font-semibold">Reference answer</h2>
          <p class="mt-1 text-sm text-muted">Look for missing concepts, structure and examples.</p>
          <p class="mt-4 whitespace-pre-wrap">
            {{
              session.practice.value.referenceAnswer?.content ||
              'No published reference answer is available yet.'
            }}
          </p>
        </section>
        <fieldset class="mt-8">
          <legend class="text-lg font-semibold">How would you rate your answer?</legend>
          <div class="mt-4 grid gap-3 sm:grid-cols-2">
            <button
              v-for="rating in ratings"
              :key="rating.value"
              type="button"
              class="rounded-xl border p-4 text-left transition"
              :class="
                session.selfRating.value === rating.value
                  ? 'border-primary bg-primary/10'
                  : 'border-default hover:border-primary/50'
              "
              @click="session.rate(rating.value)"
            >
              <strong>{{ rating.label }}</strong
              ><span class="mt-1 block text-sm text-muted">{{ rating.description }}</span>
            </button>
          </div>
        </fieldset>
      </template>
      <template v-else-if="session.phase.value === 'follow-up' && session.currentFollowUp.value"
        ><p class="mt-2 text-sm text-muted">
          Follow-up {{ session.currentFollowUpIndex.value + 1 }} of
          {{ session.practice.value.followUps.length }} · Optional
        </p>
        <UFormField label="Your follow-up answer" class="mt-8"
          ><UTextarea
            v-model="session.currentFollowUpAnswer.value"
            :rows="9"
            autoresize
            class="w-full" /></UFormField
      ></template>
      <div v-if="session.error.value" class="mt-6">
        <CoreErrorState :error="session.error.value" />
        <div class="mt-3 flex flex-wrap gap-2">
          <UButton
            label="Copy answer"
            color="neutral"
            variant="outline"
            @click="copyDraft(session.answer.value, 'Answer')"
          /><UButton
            to="/app/interview"
            label="Return to Interview"
            color="neutral"
            variant="ghost"
          />
        </div>
      </div>
    </section>
    <template v-if="!loading && !loadError && session.phase.value !== 'completed'" #shortcut-help
      ><PracticeShortcutHelp v-model:open="shortcutsOpen" :shortcuts="shortcuts"
    /></template>
    <template v-if="session.phase.value === 'answering'" #actions
      ><UButton
        class="w-full sm:w-auto"
        :disabled="!session.answer.value.trim()"
        @click="session.compare"
        >Compare answer</UButton
      ></template
    >
    <template v-else-if="session.phase.value === 'comparing'" #actions
      ><UButton
        class="w-full sm:w-auto"
        :disabled="!session.selfRating.value"
        :loading="session.busy.value"
        @click="finishRating"
        >Continue</UButton
      ></template
    >
    <template v-else-if="session.phase.value === 'follow-up'" #actions
      ><UButton color="neutral" variant="ghost" @click="finishFollowUp(true)">Skip</UButton
      ><UButton :loading="session.busy.value" @click="finishFollowUp(false)"
        >Continue</UButton
      ></template
    >
  </PracticeShell>
</template>
