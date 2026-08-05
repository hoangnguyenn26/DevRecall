<script setup lang="ts">
import type { TodayMetrics } from '~/features/today/today.types'
import { formatTodayMinutes } from '~/features/today/today.format'

defineProps<{ metrics: TodayMetrics }>()
</script>

<template>
  <section aria-label="Weekly learning overview" class="grid grid-cols-2 gap-3 lg:grid-cols-4">
    <TodayMetricCard
      label="Reviews due"
      :value="String(metrics.reviewsDue)"
      :description="metrics.reviewsDue === 0 ? 'Your queue is clear' : 'Scheduled for review'"
      icon="i-lucide-refresh-cw"
      to="/app/review"
      :tone="metrics.reviewsDue > 0 ? 'warning' : 'neutral'"
    />
    <TodayMetricCard
      label="Study time"
      :value="formatTodayMinutes(metrics.studyMinutesThisWeek)"
      :description="metrics.studyMinutesThisWeek === 0 ? 'Start with one focused activity' : 'Completed this week'"
      icon="i-lucide-clock-3"
      to="/app/analytics?tab=activity"
    />
    <TodayMetricCard
      label="Active days"
      :value="`${metrics.activeDaysThisWeek}/${metrics.weeklyTargetDays}`"
      description="Weekly target"
      icon="i-lucide-calendar-check"
      to="/app/analytics?tab=activity"
    />
    <TodayMetricCard
      label="Weekly progress"
      :value="`${Math.round(metrics.weeklyProgressPercent)}%`"
      description="Based on your active-day target"
      icon="i-lucide-target"
      to="/app/analytics"
    />
  </section>
</template>
