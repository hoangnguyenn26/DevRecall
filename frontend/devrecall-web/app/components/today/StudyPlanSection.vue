<script setup lang="ts">
import type { TodayStudyPlan } from '~/features/today/today.types'
import { formatTodayMinutes } from '~/features/today/today.format'

defineProps<{ plan: TodayStudyPlan | null; canGenerate: boolean }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader
      title="Current study plan"
      :to="plan ? `/app/study-plans/${plan.studyPlanId}` : undefined"
      :action-label="plan ? 'View plan' : undefined"
    />
    <template v-if="plan">
      <div class="flex items-start justify-between gap-4">
        <div>
          <h3 class="font-medium">{{ plan.title }}</h3>
          <p class="mt-1 text-sm text-muted">{{ plan.itemCount }} items · {{ formatTodayMinutes(plan.totalPlannedDurationMinutes) }}</p>
        </div>
        <CoreStatusBadge :value="plan.status" />
      </div>
      <ol v-if="plan.items.length" class="mt-5 divide-y divide-default">
        <li v-for="item in plan.items.slice(0, 3)" :key="item.itemId" class="flex items-center gap-3 py-3 text-sm">
          <span class="flex size-6 shrink-0 items-center justify-center rounded-full bg-elevated text-xs text-muted">{{ item.position }}</span>
          <span class="min-w-0 flex-1 truncate">{{ item.resourceTitle }}</span>
          <span class="text-xs text-muted">{{ formatTodayMinutes(item.plannedDurationMinutes) }}</span>
        </li>
      </ol>
    </template>
    <div v-else class="py-4">
      <p class="font-medium">No study plan yet</p>
      <p class="mt-2 text-sm leading-6 text-muted">Organize active recommendations into a focused session.</p>
      <UButton v-if="canGenerate" to="/app/study-plans?action=generate" class="mt-4" variant="soft" icon="i-lucide-list-plus">
        Generate plan
      </UButton>
    </div>
  </section>
</template>
