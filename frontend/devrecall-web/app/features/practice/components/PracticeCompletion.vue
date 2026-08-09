<script setup lang="ts">
import type { PracticeCompletionSummary } from '../practice.types'
defineProps<{
  summary: PracticeCompletionSummary
  primaryTo?: string
  primaryLabel: string
  pending?: boolean
  handoffError?: unknown
}>()
defineEmits<{ primary: [] }>()
</script>

<template>
  <section class="m-auto w-full max-w-2xl text-center" role="status" aria-live="polite">
    <UIcon name="i-lucide-party-popper" class="mx-auto size-10 text-primary" />
    <h1 class="mt-4 text-3xl font-semibold">{{ summary.title }}</h1>
    <p class="mt-2 text-muted">
      {{ summary.completedCount }} items completed<span v-if="summary.durationMinutes">
        · {{ summary.durationMinutes }} minutes</span
      >
    </p>
    <dl
      v-if="summary.primaryMetricLabel || summary.secondaryMetrics?.length"
      class="mt-8 grid gap-3 sm:grid-cols-2"
    >
      <div v-if="summary.primaryMetricLabel" class="rounded-xl border border-default p-4">
        <dt class="text-sm text-muted">{{ summary.primaryMetricLabel }}</dt>
        <dd class="mt-1 text-2xl font-semibold">{{ summary.primaryMetricValue }}</dd>
      </div>
      <div
        v-for="metric in summary.secondaryMetrics"
        :key="metric.label"
        class="rounded-xl border border-default p-4"
      >
        <dt class="text-sm text-muted">{{ metric.label }}</dt>
        <dd class="mt-1 text-2xl font-semibold">{{ metric.value }}</dd>
      </div>
    </dl>
    <div v-if="handoffError" class="mt-6 rounded-xl border border-error p-4 text-left" role="alert">
      <strong>Practice saved</strong>
      <p class="text-sm text-muted">
        DevRecall could not update the Study Session. Retry only the session update.
      </p>
    </div>
    <div class="mt-8 flex flex-col justify-center gap-2 sm:flex-row">
      <UButton v-if="primaryTo" :to="primaryTo">{{ primaryLabel }}</UButton
      ><UButton v-else :loading="pending" @click="$emit('primary')">{{ primaryLabel }}</UButton
      ><UButton to="/app" color="neutral" variant="outline">Go to Today</UButton>
    </div>
  </section>
</template>
