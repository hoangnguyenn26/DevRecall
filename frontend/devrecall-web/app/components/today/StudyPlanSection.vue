<script setup lang="ts">
import type { TodayStudyPlan } from '~/features/today/today.types'
import { formatTodayMinutes } from '~/features/today/today.format'

defineProps<{ plan: TodayStudyPlan | null; activeRecommendationCount: number }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader
      title="Current study plan"
      :to="plan ? `/app/study-plans/${plan.studyPlanId}` : undefined"
      :action-label="plan?.status === 'Draft' ? 'Continue planning' : plan ? 'View plan' : undefined"
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
        <li v-for="item in plan.items" :key="item.itemId" class="flex items-center gap-3 py-3 text-sm">
          <span class="flex size-6 shrink-0 items-center justify-center rounded-full bg-elevated text-xs text-muted">{{ item.position }}</span>
          <span class="min-w-0 flex-1 truncate" :class="item.isResourceAvailable ? undefined : 'text-muted'">
            {{ item.isResourceAvailable ? item.resourceTitle : 'Unavailable resource' }}
          </span>
          <span class="text-xs text-muted">{{ formatTodayMinutes(item.plannedDurationMinutes) }}</span>
        </li>
      </ol>
      <p v-if="plan.itemCount > plan.items.length" class="mt-3 text-xs text-muted">+{{ plan.itemCount - plan.items.length }} more</p>
    </template>
    <div v-else class="py-4">
      <p class="font-medium">No study plan yet</p>
      <p class="mt-2 text-sm leading-6 text-muted">
        {{ activeRecommendationCount > 0 ? 'Organize your active recommendations into a focused learning plan.' : 'Study plans will help organize your next focused learning session.' }}
      </p>
      <UButton v-if="activeRecommendationCount > 0" to="/app/study-plans?action=generate" class="mt-4" color="neutral" variant="soft" icon="i-lucide-list-plus">
        Generate plan
      </UButton>
    </div>
  </section>
</template>
