<script setup lang="ts">
import { useTodayApi } from '~/features/today/today.api'
import { useLearningProfileApi } from '~/features/learning-profile/learning-profile.api'
import { learningProfileKeys } from '~/features/learning-profile/learning-profile.query-keys'
import type { LearningProfile } from '~/features/learning-profile/learning-profile.types'
import { shouldShowLearningProfileSetup } from '~/features/learning-profile/learning-profile'
import { queryKeys } from '~/query/query-keys'

definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Today', robots: 'noindex, nofollow' })

const todayApi = useTodayApi()
const profileApi = useLearningProfileApi()
const profileQuery = useApiQuery<LearningProfile>(learningProfileKeys.current, profileApi.get)
const { data: dashboard, error, refreshError, isPending, refreshing, refresh } =
  useApiQuery(queryKeys.today, () => todayApi.getDashboard())
const actionError = computed(() => error.value ?? refreshError.value)
</script>

<template>
  <CoreAppContainer size="wide" class="py-6 sm:py-8">
    <div class="mb-3 flex justify-end"><UButton color="neutral" variant="ghost" icon="i-lucide-refresh-cw" label="Refresh" :loading="refreshing" :aria-busy="refreshing" @click="() => refresh()" /></div>
    <FeedbackPageState
      :pending="isPending"
      :refreshing="refreshing"
      :error="actionError ? { ...actionError, title: `We couldn't load today's next action.`, detail: `Retry to get the current action. Any learning action you already saved remains saved.` } : null"
      @retry="refresh"
    >
      <TodayDashboard v-if="dashboard" :dashboard="dashboard" />
    </FeedbackPageState>
    <UButton v-if="actionError" to="/app/learn" color="neutral" variant="outline" class="mt-4">Explore Learn</UButton>
    <TodayLearningProfileSetupCard
      v-if="dashboard && !actionError && shouldShowLearningProfileSetup(profileQuery.data.value)"
      class="mt-6"
    />
  </CoreAppContainer>
</template>
