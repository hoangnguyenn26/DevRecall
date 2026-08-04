<script setup lang="ts">
import { recommendationPriorityMeta, studyPlanStatusMeta } from '~/utils/status-meta'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Design System', robots: 'noindex, nofollow' })
</script>

<template>
  <CoreAppContainer size="wide">
    <div class="space-y-12 py-8">
      <CorePageHeader eyebrow="Internal" title="DevRecall Design System" description="Visual foundation for the Developer Learning OS.">
        <template #actions><CoreThemeToggle /></template>
      </CorePageHeader>

      <CorePageSection title="Actions" description="Primary and secondary action hierarchy.">
        <div class="flex flex-wrap gap-3">
          <UButton>Primary action</UButton>
          <UButton color="neutral" variant="soft">Secondary</UButton>
          <UButton color="neutral" variant="ghost">Tertiary</UButton>
          <UButton color="error" variant="soft">Destructive</UButton>
        </div>
      </CorePageSection>

      <CorePageSection title="Status">
        <div class="flex flex-wrap gap-2">
          <CoreStatusBadge v-for="meta in studyPlanStatusMeta" :key="meta.label" :meta="meta" />
          <CoreStatusBadge v-for="meta in recommendationPriorityMeta" :key="meta.label" :meta="meta" />
        </div>
      </CorePageSection>

      <CorePageSection title="Cards">
        <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          <UCard><template #header><h3 class="font-semibold">Review queue</h3></template><p class="text-sm text-muted">Twelve items are due today.</p><template #footer><UButton size="sm">Start review</UButton></template></UCard>
        </div>
      </CorePageSection>

      <CorePageSection title="Feedback states">
        <div class="grid gap-6 lg:grid-cols-2">
          <FeedbackAppEmptyState icon="i-lucide-book-open" title="No knowledge yet" description="Capture your first concept to start building your learning system."><template #actions><UButton size="sm">Create knowledge</UButton></template></FeedbackAppEmptyState>
          <FeedbackAppErrorState trace-id="example-trace-id" @retry="() => undefined" />
        </div>
      </CorePageSection>
    </div>
  </CoreAppContainer>
</template>
