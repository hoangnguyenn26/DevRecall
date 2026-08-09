<script setup lang="ts">
import type { PracticeShortcut } from '~/features/practice/practice.types'
import type { DsaAttemptOutcome, DsaPractice } from '~/features/dsa/practice/dsa-practice.types'
import { useDsaPractice } from '~/features/dsa/practice/useDsaPractice'
import { useDsaPracticeApi } from '~/features/dsa/practice/dsa-practice.api'
import { usePracticeExit } from '~/features/practice/usePracticeExit'
import { usePracticeShortcuts } from '~/features/practice/usePracticeShortcuts'
import { useDraftClipboard } from '~/features/practice/useDraftClipboard'
import PracticeShell from '~/features/practice/components/PracticeShell.vue'
import PracticeLoadingState from '~/features/practice/components/PracticeLoadingState.vue'
import PracticeErrorState from '~/features/practice/components/PracticeErrorState.vue'
import PracticeCompletion from '~/features/practice/components/PracticeCompletion.vue'
import PracticeShortcutHelp from '~/features/practice/components/PracticeShortcutHelp.vue'

definePageMeta({ layout: 'focus', middleware: 'auth' })
useSeoMeta({ title: 'DSA practice', robots: 'noindex, nofollow' })
const route = useRoute()
const problemId = computed(() => String(route.params.id))
const api = useDsaPracticeApi()
const session = useDsaPractice(api.complete(problemId.value))
const practice = ref<DsaPractice>()
const loading = ref(true)
const loadError = ref<unknown>()
const shortcutsOpen = ref(false)
const toast = useToast()
const { afterDsaPractice } = useLearningDataInvalidation()
const { copyDraft } = useDraftClipboard()
const outcomes: { value: DsaAttemptOutcome; label: string; description: string }[] = [
  { value: 'Solved', label: 'Solved', description: 'I solved it independently.' },
  {
    value: 'Skipped',
    label: 'Solved with help',
    description: 'I finished after using hints or references.',
  },
  {
    value: 'PartiallySolved',
    label: 'Partial',
    description: 'I made meaningful progress but did not finish.',
  },
  {
    value: 'Failed',
    label: 'Could not solve',
    description: 'I could not find a working approach.',
  },
]
const context = computed(() => ({
  module: 'Dsa' as const,
  title: practice.value?.title || 'DSA practice',
  startedAtUtc: session.startedAtUtc.value,
  progress: {
    kind: 'steps' as const,
    currentStep:
      session.phase.value === 'solving' ? 1 : session.phase.value === 'reflection' ? 2 : 3,
    totalSteps: 3,
    label:
      session.phase.value === 'solving'
        ? 'Solve'
        : session.phase.value === 'reflection'
          ? 'Reflect'
          : 'Complete',
  },
}))
const exit = usePracticeExit({
  exitTo: `/app/dsa/${problemId.value}`,
  hasUnsubmittedWork: session.dirty,
  busy: session.busy,
  title: 'Leave DSA practice?',
  description: 'Your current solution and notes have not been saved.',
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
    id: 'continue',
    keys: ['Ctrl', 'Enter'],
    label: session.phase.value === 'reflection' ? 'Save attempt' : 'Finish attempt',
    enabled: () =>
      session.phase.value === 'solving' ||
      (session.phase.value === 'reflection' && !!session.outcome.value),
    execute: () => {
      if (session.phase.value === 'reflection') return save()
      session.finish()
    },
  },
  {
    id: 'exit',
    keys: ['Escape'],
    label: 'Exit practice',
    enabled: () => !session.busy.value,
    execute: () => { if (shortcutsOpen.value) { shortcutsOpen.value = false; return } return exit.exit() },
  },
])
usePracticeShortcuts(shortcuts)
async function load() {
  loading.value = true
  loadError.value = undefined
  try {
    practice.value = await api.get(problemId.value)
  } catch (error) {
    loadError.value = error
  } finally {
    loading.value = false
  }
}
async function save() {
  if (await session.submit()) {
    try {
      await afterDsaPractice(problemId.value)
    } catch {
      toast.add({
        title: 'Attempt saved',
        description: 'History will refresh when you return.',
        color: 'warning',
      })
    }
  }
}
onMounted(load)
</script>

<template>
  <PracticeShell :context="context" :busy="session.busy.value" wide @exit="exit.exit">
    <PracticeLoadingState v-if="loading" label="Preparing DSA practice" />
    <PracticeErrorState v-else-if="loadError" :error="loadError" return-to="/app/dsa" @retry="load" />
    <PracticeCompletion
      v-else-if="session.phase.value === 'completed' && session.result.value"
      :summary="{
        title: 'Attempt recorded',
        completedCount: 1,
        durationMinutes: session.result.value.durationMinutes,
        primaryMetricLabel: 'Outcome',
        primaryMetricValue: outcomes.find((item) => item.value === session.result.value?.result)
          ?.label,
        secondaryMetrics: [
          { label: 'Time', value: session.result.value.timeComplexity || 'Not recorded' },
          { label: 'Space', value: session.result.value.spaceComplexity || 'Not recorded' },
        ],
      }"
      :primary-to="`/app/dsa/${problemId}?attempt=${session.result.value.id}`"
      primary-label="View attempt history"
    />
    <section v-else-if="practice" class="grid min-w-0 gap-6 xl:grid-cols-[42fr_58fr]">
      <article class="min-w-0 rounded-xl border border-default p-5 sm:p-6">
        <div class="flex flex-wrap items-center gap-2">
          <CoreStatusBadge :value="practice.difficulty" /><span
            v-for="topic in practice.topics"
            :key="topic"
            class="text-xs text-muted"
            >{{ topic }}</span
          >
        </div>
        <h1 class="mt-4 text-2xl font-semibold">{{ practice.title }}</h1>
        <p
          v-if="practice.description"
          class="mt-5 whitespace-pre-wrap break-words leading-7 text-muted"
        >
          {{ practice.description }}
        </p>
        <p v-else class="mt-5 text-muted">The problem statement is hosted externally.</p>
        <a
          v-if="practice.externalUrl"
          :href="practice.externalUrl"
          target="_blank"
          rel="noopener noreferrer"
          class="mt-5 inline-flex items-center gap-2 text-primary"
          >Open problem source <UIcon name="i-lucide-external-link"
        /></a>
      </article>
      <article class="min-w-0 rounded-xl border border-default p-5 sm:p-6">
        <template v-if="session.phase.value === 'solving'"
          ><div class="grid gap-4 sm:grid-cols-2">
            <UFormField label="Language"><UInput v-model="session.language.value" /></UFormField
            ><UFormField label="Approach"
              ><UInput
                v-model="session.approach.value"
                placeholder="Sliding window, dynamic programming…"
            /></UFormField>
          </div>
          <UFormField label="Solution or code" class="mt-5"
            ><UTextarea
              v-model="session.solution.value"
              :rows="17"
              spellcheck="false"
              class="w-full font-mono text-sm"
          /></UFormField>
          <p class="mt-2 text-sm text-muted">
            Code is recorded for reflection only. DevRecall does not execute or judge it.
          </p></template
        >
        <template
          v-else-if="session.phase.value === 'reflection' || session.phase.value === 'submitting'"
          ><h2 class="text-xl font-semibold">Finish attempt</h2>
          <fieldset class="mt-6">
            <legend class="font-medium">Outcome</legend>
            <div class="mt-3 grid gap-3 sm:grid-cols-2">
              <button
                v-for="item in outcomes"
                :key="item.value"
                type="button"
                class="rounded-xl border p-4 text-left"
                :class="
                  session.outcome.value === item.value
                    ? 'border-primary bg-primary/10'
                    : 'border-default'
                "
                @click="session.outcome.value = item.value"
              >
                <strong>{{ item.label }}</strong
                ><span class="mt-1 block text-sm text-muted">{{ item.description }}</span>
              </button>
            </div>
          </fieldset>
          <div class="mt-6 grid gap-4 sm:grid-cols-2">
            <UFormField label="Time complexity"
              ><UInput v-model="session.timeComplexity.value" placeholder="O(n)" /></UFormField
            ><UFormField label="Space complexity"
              ><UInput v-model="session.spaceComplexity.value" placeholder="O(1)"
            /></UFormField>
          </div>
          <UFormField label="What did you learn from this attempt?" class="mt-5"
            ><UTextarea v-model="session.reflection.value" :rows="6" class="w-full" /></UFormField
          ><div v-if="session.error.value" class="mt-5">
            <CoreErrorState :error="session.error.value" />
            <div class="mt-3 flex flex-wrap gap-2">
              <UButton label="Copy solution" color="neutral" variant="outline" @click="copyDraft(session.solution.value, 'Solution')" />
              <UButton to="/app/dsa" label="Return to DSA" color="neutral" variant="ghost" />
            </div>
          </div></template>
      </article>
    </section>
    <template v-if="!loading && !loadError && session.phase.value !== 'completed'" #shortcut-help
      ><PracticeShortcutHelp v-model:open="shortcutsOpen" :shortcuts="shortcuts"
    /></template>
    <template v-if="session.phase.value === 'solving'" #actions
      ><UButton class="w-full sm:w-auto" @click="session.finish">Finish attempt</UButton></template
    >
    <template
      v-else-if="session.phase.value === 'reflection' || session.phase.value === 'submitting'"
      #actions
      ><UButton color="neutral" variant="ghost" @click="session.phase.value = 'solving'"
        >Back to solution</UButton
      ><UButton :disabled="!session.outcome.value" :loading="session.busy.value" @click="save"
        >Save attempt</UButton
      ></template
    >
  </PracticeShell>
</template>
