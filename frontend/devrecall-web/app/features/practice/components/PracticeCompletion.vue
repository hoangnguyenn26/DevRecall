<script setup lang="ts">
import type { PracticeCompletionSummary } from '../practice.types'
defineProps<{ summary: PracticeCompletionSummary; primaryTo: string; primaryLabel: string }>()
</script>

<template>
  <section class="m-auto w-full max-w-2xl text-center" role="status" aria-live="polite">
    <UIcon name="i-lucide-party-popper" class="mx-auto size-10 text-primary" /><h1 class="mt-4 text-3xl font-semibold">{{ summary.title }}</h1><p class="mt-2 text-muted">{{ summary.completedCount }} items completed<span v-if="summary.durationMinutes"> · {{ summary.durationMinutes }} minutes</span></p>
    <dl v-if="summary.primaryMetricLabel || summary.secondaryMetrics?.length" class="mt-8 grid gap-3 sm:grid-cols-2"><div v-if="summary.primaryMetricLabel" class="rounded-xl border border-default p-4"><dt class="text-sm text-muted">{{ summary.primaryMetricLabel }}</dt><dd class="mt-1 text-2xl font-semibold">{{ summary.primaryMetricValue }}</dd></div><div v-for="metric in summary.secondaryMetrics" :key="metric.label" class="rounded-xl border border-default p-4"><dt class="text-sm text-muted">{{ metric.label }}</dt><dd class="mt-1 text-2xl font-semibold">{{ metric.value }}</dd></div></dl>
    <div class="mt-8 flex flex-col justify-center gap-2 sm:flex-row"><UButton :to="primaryTo">{{ primaryLabel }}</UButton><UButton to="/app" color="neutral" variant="outline">Go to Today</UButton></div>
  </section>
</template>
