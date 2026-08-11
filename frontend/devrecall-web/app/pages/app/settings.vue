<script setup lang="ts">
import { useLearningProfileApi } from '~/features/learning-profile/learning-profile.api'
import { learningProfileKeys } from '~/features/learning-profile/learning-profile.query-keys'
import type { LearningProfile } from '~/features/learning-profile/learning-profile.types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Settings' })
const auth = useAuthStore()
const profileApi = useLearningProfileApi()
const profileQuery = useApiQuery<LearningProfile>(learningProfileKeys.current, profileApi.get)
</script>

<template>
  <div>
    <CorePageHeader title="Settings" description="Account and local application preferences." />
    <div class="settings-grid">
      <UCard>
        <template #header><h2>Account</h2></template>
        <dl><dt>Display name</dt><dd>{{ auth.user?.displayName }}</dd><dt>Email</dt><dd>{{ auth.user?.email }}</dd></dl>
      </UCard>
      <UCard>
        <template #header><div class="card-heading"><h2>Learning Profile</h2><UBadge v-if="profileQuery.data.value" :color="profileQuery.data.value.isConfigured ? 'success' : 'neutral'" variant="subtle">{{ profileQuery.data.value.isConfigured ? 'Configured' : 'Needs setup' }}</UBadge></div></template>
        <p v-if="profileQuery.isPending.value">Loading your learning profile…</p>
        <div v-else-if="profileQuery.error.value" role="alert"><p>We couldn't load your learning profile.</p><UButton size="sm" color="neutral" variant="ghost" @click="() => profileQuery.refresh()">Retry</UButton></div>
        <div v-else-if="profileQuery.data.value?.isConfigured" class="profile-overview">
          <strong>{{ profileQuery.data.value.targetRole?.label }} · {{ profileQuery.data.value.experienceLevel?.label }}</strong>
          <p>{{ profileQuery.data.value.technologies.length }} technologies · {{ profileQuery.data.value.goals.length }} goals · {{ profileQuery.data.value.availableMinutesPerDay }} min/day</p>
        </div>
        <p v-else>Help DevRecall understand what you're learning toward so future suggestions can better match your goals.</p>
        <template #footer><UButton to="/app/settings/learning-profile" variant="soft" trailing-icon="i-lucide-arrow-right">{{ profileQuery.data.value?.isConfigured ? 'Edit learning profile' : 'Set up learning profile' }}</UButton></template>
      </UCard>
    </div>
  </div>
</template>

<style scoped>
.settings-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.card-heading{display:flex;align-items:center;justify-content:space-between;gap:.75rem}h2{font-weight:700}dl{display:grid;grid-template-columns:10rem 1fr;gap:.7rem}dt,p{color:var(--ui-text-muted)}.profile-overview{display:grid;gap:.35rem}@media(max-width:720px){.settings-grid{grid-template-columns:1fr}}
</style>
