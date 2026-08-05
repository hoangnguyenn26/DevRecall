<script setup lang="ts">
import type { TodayDashboard } from '~/features/today/today.types'

defineProps<{ dashboard: TodayDashboard }>()
</script>

<template>
  <div class="space-y-7 sm:space-y-8">
    <TodayWelcomeHeader :display-name="dashboard.user.displayName" />
    <TodayOnboardingCallout v-if="!dashboard.user.hasCompletedOnboarding" />
    <TodayNextActionCard :action="dashboard.nextAction" />
    <TodayContinueLearning v-if="dashboard.recentActivity" :activity="dashboard.recentActivity" :generated-at-utc="dashboard.generatedAtUtc" />
    <TodayMetricsGrid :metrics="dashboard.metrics" />
    <div class="grid gap-6 lg:grid-cols-[minmax(0,1.25fr)_minmax(20rem,.75fr)]">
      <TodayStudyPlanSection
        :plan="dashboard.studyPlan"
        :active-recommendation-count="dashboard.recommendations.length"
      />
      <TodayRecommendationsSection :recommendations="dashboard.recommendations" />
      <TodayWeakTopicsSection :topics="dashboard.weakTopics" />
      <TodayActivitySection
        class="xl:order-first"
        :activity="dashboard.weeklyActivity"
        :target-days="dashboard.metrics.weeklyTargetDays"
      />
    </div>
  </div>
</template>
