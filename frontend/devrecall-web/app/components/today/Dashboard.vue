<script setup lang="ts">
import type { TodayDashboard } from '~/features/today/today.types'

defineProps<{ dashboard: TodayDashboard }>()
</script>

<template>
  <div class="space-y-7 sm:space-y-8">
    <TodayWelcomeHeader :display-name="dashboard.user.displayName" />
    <TodayNextActionCard :action="dashboard.nextAction" />
    <TodayOnboardingCallout v-if="!dashboard.user.hasCompletedOnboarding" />
  </div>
</template>
