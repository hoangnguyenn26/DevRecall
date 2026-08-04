<script setup lang="ts">
import { useTodayApi } from '~/features/today/today.api'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Today', robots: 'noindex, nofollow' })

const todayApi = useTodayApi()
const { data: dashboard, error, isPending, refreshing, refresh } =
  useApiQuery('today-dashboard', () => todayApi.getDashboard())
</script>

<template>
  <CoreAppContainer size="wide" class="py-6 sm:py-8">
    <FeedbackPageState
      :pending="isPending"
      :refreshing="refreshing"
      :error="error"
      @retry="refresh"
    >
      <TodayDashboard v-if="dashboard" :dashboard="dashboard" />
    </FeedbackPageState>
  </CoreAppContainer>
</template>
