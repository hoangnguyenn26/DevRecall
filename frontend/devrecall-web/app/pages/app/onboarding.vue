<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import type { LearningFocusArea, LearningGoal, OnboardingFormState } from '~/features/onboarding/onboarding.types'
import { canContinueOnboarding, focusAreas, learningGoals, onboardingDefaults, toggleFocusArea } from '~/features/onboarding/onboarding.meta'
import { useOnboardingApi } from '~/features/onboarding/onboarding.api'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Set up DevRecall', robots: 'noindex, nofollow' })

const step = ref(1)
const totalSteps = 4
const form = reactive<OnboardingFormState>(onboardingDefaults())
const api = useOnboardingApi()
const confirmDialog = useConfirmDialog()
const toast = useToast()
const { refreshLearningEntryPoints } = useLearningDataInvalidation()
const submitting = ref(false)
const submitError = ref('')
const canContinue = computed(() => canContinueOnboarding(step.value, form))
const focusLimitReached = computed(() => form.focusAreas.length >= 4)

function selectGoal(goal: LearningGoal): void { form.goal = goal }
function selectFocusArea(area: LearningFocusArea): void { form.focusAreas = toggleFocusArea(form.focusAreas, area) }
function next(): void { if (canContinue.value && step.value < totalSteps) step.value++ }
function back(): void { if (step.value > 1) step.value-- }

async function finish(): Promise<void> {
  if (submitting.value || !form.goal || !canContinueOnboarding(4, form)) return
  submitting.value = true
  submitError.value = ''
  try {
    await api.complete({
      goal: form.goal,
      focusAreas: form.focusAreas,
      dailyCommitmentMinutes: form.dailyCommitmentMinutes,
      weeklyTargetDays: form.weeklyTargetDays,
    })
    await refreshLearningEntryPoints()
    toast.add({ title: 'Your workspace is ready', color: 'success' })
    await navigateTo('/app')
  }
  catch {
    submitError.value = 'We could not save your setup. Review your choices and try again.'
  }
  finally { submitting.value = false }
}

async function skip(): Promise<void> {
  if (submitting.value) return
  const accepted = await confirmDialog.open({
    title: 'Skip setup?',
    description: 'DevRecall will use a default goal of 30 minutes per day, 5 days per week. You can change this later in Settings.',
    confirmLabel: 'Use defaults',
    tone: 'neutral',
  })
  if (!accepted) return
  submitting.value = true
  confirmDialog.setPending(true)
  try {
    await api.skip()
    await refreshLearningEntryPoints()
    await navigateTo('/app')
  }
  catch { submitError.value = 'We could not skip setup. Please try again.' }
  finally { submitting.value = false; confirmDialog.setPending(false) }
}
</script>

<template>
  <CoreAppContainer size="reading" class="py-6 sm:py-10">
    <section class="rounded-2xl border border-default bg-default p-5 shadow-sm sm:p-8">
      <header>
        <p class="font-semibold">DevRecall setup</p>
        <OnboardingProgress :current-step="step" :total-steps="totalSteps" class="mt-2 text-right" />
      </header>

      <div class="min-h-[25rem] py-8 sm:py-10">
        <div v-if="step === 1" class="mx-auto flex max-w-xl flex-col items-center py-8 text-center">
          <div class="flex size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary"><UIcon name="i-lucide-compass" class="size-7" /></div>
          <h1 class="mt-6 text-2xl font-semibold tracking-tight sm:text-3xl">Build a learning system around your goals</h1>
          <p class="mt-4 leading-7 text-muted">DevRecall uses your practice history, reviews and study plans to help decide what deserves attention next.</p>
          <UButton class="mt-8" size="lg" @click="next">Set up my workspace</UButton>
          <UButton class="mt-2" color="neutral" variant="ghost" @click="skip">Skip for now</UButton>
        </div>

        <fieldset v-else-if="step === 2">
          <legend class="text-2xl font-semibold tracking-tight">What is your main learning goal?</legend>
          <p class="mt-2 text-sm text-muted">Choose the outcome that matters most right now.</p>
          <div class="mt-6 grid gap-3 sm:grid-cols-2">
            <button
              v-for="goal in learningGoals" :key="goal.value" type="button"
              class="rounded-xl border p-4 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              :class="form.goal === goal.value ? 'border-primary bg-primary/5' : 'border-default hover:bg-elevated/50'"
              :aria-pressed="form.goal === goal.value" @click="selectGoal(goal.value)"
            >
              <UIcon :name="goal.icon" class="size-5 text-primary" /><span class="mt-3 block font-medium">{{ goal.label }}</span><span class="mt-1 block text-sm leading-5 text-muted">{{ goal.description }}</span>
            </button>
          </div>
        </fieldset>

        <fieldset v-else-if="step === 3">
          <legend class="text-2xl font-semibold tracking-tight">What are you focusing on?</legend>
          <p class="mt-2 text-sm text-muted">Choose up to four areas. At least one is required.</p>
          <div class="mt-6 grid gap-3 sm:grid-cols-2">
            <label
              v-for="area in focusAreas" :key="area.value"
              class="flex cursor-pointer items-center gap-3 rounded-xl border p-4 transition-colors"
              :class="form.focusAreas.includes(area.value) ? 'border-primary bg-primary/5' : 'border-default'"
            >
              <input
                type="checkbox" class="size-4 accent-primary" :checked="form.focusAreas.includes(area.value)"
                :disabled="focusLimitReached && !form.focusAreas.includes(area.value)"
                :aria-describedby="focusLimitReached ? 'focus-limit' : undefined"
                @change="selectFocusArea(area.value)"
              ><span class="font-medium">{{ area.label }}</span>
            </label>
          </div>
          <p id="focus-limit" class="mt-3 text-sm text-muted" aria-live="polite">{{ focusLimitReached ? 'Four areas selected. Remove one to choose another.' : `${form.focusAreas.length} of 4 selected.` }}</p>
        </fieldset>

        <div v-else>
          <h1 class="text-2xl font-semibold tracking-tight">Set a flexible commitment</h1>
          <p class="mt-2 text-sm text-muted">This is a progress target, not a hard limit.</p>
          <fieldset class="mt-7"><legend class="font-medium">Daily duration</legend>
            <div class="mt-3 flex flex-wrap gap-2">
              <UButton v-for="minutes in [15, 30, 45, 60]" :key="minutes" type="button" color="neutral" :variant="form.dailyCommitmentMinutes === minutes ? 'solid' : 'outline'" @click="form.dailyCommitmentMinutes = minutes">{{ minutes }} min</UButton>
            </div>
            <UFormField class="mt-4 max-w-56" label="Custom minutes" help="Between 10 and 180 minutes."><UInput v-model.number="form.dailyCommitmentMinutes" type="number" min="10" max="180" class="w-full" /></UFormField>
          </fieldset>
          <fieldset class="mt-7"><legend class="font-medium">Weekly target</legend>
            <div class="mt-3 flex flex-wrap gap-2">
              <UButton v-for="days in [3, 5, 7]" :key="days" type="button" color="neutral" :variant="form.weeklyTargetDays === days ? 'solid' : 'outline'" @click="form.weeklyTargetDays = days">{{ days }} days</UButton>
            </div>
          </fieldset>
        </div>
      </div>

      <p v-if="submitError" role="alert" class="mb-4 rounded-lg bg-error/10 p-3 text-sm text-error">{{ submitError }}</p>
      <footer v-if="step > 1" class="flex flex-col-reverse gap-3 border-t border-default pt-5 sm:flex-row sm:items-center">
        <UButton color="neutral" variant="ghost" :disabled="submitting" @click="skip">Skip for now</UButton>
        <div class="flex flex-1 justify-end gap-2">
          <UButton color="neutral" variant="ghost" :disabled="submitting" @click="back">Back</UButton>
          <UButton v-if="step < totalSteps" :disabled="!canContinue" @click="next">Continue</UButton>
          <UButton v-else :disabled="!canContinue" :loading="submitting" @click="finish">Finish setup</UButton>
        </div>
      </footer>
    </section>
  </CoreAppContainer>
</template>
