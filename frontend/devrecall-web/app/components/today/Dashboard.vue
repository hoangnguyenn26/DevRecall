<script setup lang="ts">
import type { TodayDashboard } from '~/features/today/today.types'

defineProps<{ dashboard: TodayDashboard }>()
</script>

<template>
  <div class="space-y-7 sm:space-y-8">
    <TodayWelcomeHeader :display-name="dashboard.user.displayName" />
    <TodayNextActionCard :action="dashboard.nextAction" />
    <TodayMetricsGrid :metrics="dashboard.metrics" />
    <div class="grid gap-6 lg:grid-cols-[minmax(0,1.25fr)_minmax(20rem,.75fr)]">
      <TodayStudyPlanSection
        :plan="dashboard.studyPlan"
        :can-generate="dashboard.recommendations.length > 0"
      />
      <TodayRecommendationsSection :recommendations="dashboard.recommendations" />
      <TodayActivitySection :activity="dashboard.weeklyActivity" />
      <TodayWeakTopicsSection :topics="dashboard.weakTopics" />
    </div>
  </div>
</template>
