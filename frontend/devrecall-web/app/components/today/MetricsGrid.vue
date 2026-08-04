<script setup lang="ts">
import type { TodayMetrics } from '~/features/today/today.types'
import { formatTodayMinutes } from '~/features/today/today.format'

defineProps<{ metrics: TodayMetrics }>()
</script>

<template>
  <section aria-label="This week's progress" class="grid grid-cols-2 gap-3 lg:grid-cols-4">
    <NuxtLink to="/app/review" class="rounded-xl focus-visible:outline-2 focus-visible:outline-primary">
      <TodayMetricCard
        label="Reviews due"
        :value="String(metrics.reviewsDue)"
        :description="metrics.reviewsDue === 0 ? 'Your queue is clear' : 'Scheduled items need attention'"
        icon="i-lucide-refresh-cw"
      />
    </NuxtLink>
    <TodayMetricCard
      label="Study time"
      :value="formatTodayMinutes(metrics.studyMinutesThisWeek)"
      :description="metrics.studyMinutesThisWeek === 0 ? 'Start with one focused activity' : 'Completed this week'"
      icon="i-lucide-clock-3"
    />
    <TodayMetricCard
      label="Active days"
      :value="`${metrics.activeDaysThisWeek}/${metrics.weeklyTargetDays}`"
      description="Monday to Sunday"
      icon="i-lucide-calendar-days"
    />
    <TodayMetricCard
      label="Weekly progress"
      :value="`${Math.round(metrics.weeklyProgressPercent)}%`"
      description="Based on your active-day target"
      icon="i-lucide-gauge"
    />
  </section>
</template>
