<script setup lang="ts">
import { recommendationPriorityMeta, studyPlanStatusMeta } from '~/utils/status-meta'
import type { NormalizedApiError } from '~/utils/normalize-api-error'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Design System', robots: 'noindex, nofollow' })
const toast = useToast()
const confirmDialog = useConfirmDialog()
const demoError: NormalizedApiError = {
  status: 503,
  code: 'SERVICE_UNAVAILABLE',
  title: 'Unable to load this section',
  detail: 'The service is temporarily unavailable. Try again in a moment.',
  fieldErrors: {},
  traceId: '00-example-correlation-id',
}

async function showConfirmation(): Promise<void> {
  const accepted = await confirmDialog.open({
    title: 'Discard this draft?',
    description: 'Your unsaved changes will be lost.',
    confirmLabel: 'Discard draft',
    tone: 'danger',
  })
  if (accepted) toast.add({ title: 'Draft discarded', color: 'success' })
}
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

      <CorePageSection title="Loading and refresh" description="Initial loading reserves page space; background refresh keeps current content visible.">
        <div class="grid gap-6 lg:grid-cols-2">
          <FeedbackSectionState :pending="true" />
          <FeedbackSectionState :pending="false" :refreshing="true"><UCard><p class="text-sm text-muted">Existing content remains available while refreshing.</p></UCard></FeedbackSectionState>
        </div>
      </CorePageSection>

      <CorePageSection title="API feedback" description="Stable states for recoverable errors, concurrency and offline work.">
        <div class="space-y-4">
          <FeedbackAppErrorState :title="demoError.title" :description="demoError.detail" :trace-id="demoError.traceId" @retry="() => undefined" />
          <FeedbackConcurrencyConflictAlert resource-label="study plan" @reload="() => undefined" />
          <UAlert color="warning" variant="subtle" icon="i-lucide-wifi-off" title="You appear to be offline" description="Changes may not be saved." />
        </div>
      </CorePageSection>

      <CorePageSection title="Interaction feedback" description="Global confirmation and toast patterns shared by feature workflows.">
        <div class="flex flex-wrap gap-3">
          <UButton color="error" variant="soft" @click="showConfirmation">Open confirmation</UButton>
          <UButton color="neutral" variant="soft" @click="toast.add({ title: 'Knowledge saved', color: 'success' })">Show success toast</UButton>
        </div>
      </CorePageSection>
    </div>
  </CoreAppContainer>
</template>
