<script setup lang="ts">
import { useTodayApi } from '~/features/today/today.api'
import { queryKeys } from '~/query/query-keys'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Today', robots: 'noindex, nofollow' })

const todayApi = useTodayApi()
const { data: dashboard, error, refreshError, isPending, refreshing, refresh } =
  useApiQuery(queryKeys.today, () => todayApi.getDashboard())
</script>

<template>
  <CoreAppContainer size="wide" class="py-6 sm:py-8">
    <UAlert v-if="refreshError" class="mb-4" color="warning" variant="subtle" title="Dashboard refresh failed" description="Your existing dashboard is still available. Try again when you are ready." />
    <div class="mb-3 flex justify-end"><UButton color="neutral" variant="ghost" icon="i-lucide-refresh-cw" label="Refresh" :loading="refreshing" :aria-busy="refreshing" @click="() => refresh()" /></div>
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
