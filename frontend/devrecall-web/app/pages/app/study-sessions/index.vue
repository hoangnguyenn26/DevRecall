<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import type { StudySessionListItem } from '~/types/domain'
import { formatMinutes, formatDateTime } from '~/utils/format'
definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Study sessions' })
const api = useApi()
const items = ref<StudySessionListItem[]>([])
const loading = ref(true)
const error = ref<unknown>()
const status = ref('')
async function load() {
  loading.value = true
  error.value = undefined
  try {
    items.value = (
      await api.get<PagedResponse<StudySessionListItem>>('/study-sessions', {
        status: status.value,
        page: 1,
        pageSize: 50,
      })
    ).items
  } catch (caught) {
    error.value = caught
  } finally {
    loading.value = false
  }
}
watch(status, load)
onMounted(load)
</script>
<template>
  <div>
    <CorePageHeader
      title="Study sessions"
      description="Execute a plan one focused item at a time."
    /><select v-model="status" class="filter" aria-label="Filter study sessions by status">
      <option value="">All statuses</option>
      <option>Draft</option>
      <option>InProgress</option>
      <option>Completed</option>
      <option>Cancelled</option></select
    ><CoreLoadingState v-if="loading" label="Loading sessions" /><CoreErrorState
      v-else-if="error"
      :error="error"
      @retry="load"
    /><CoreEmptyState
      v-else-if="!items.length"
      title="No study sessions"
      description="Convert a ready study plan to create one."
    >
      <UButton label="Open study plans" to="/app/study-plans" icon="i-lucide-calendar-range" />
    </CoreEmptyState>
    <div v-else class="sessions">
      <NuxtLink v-for="session in items" :key="session.id" :to="`/app/study-sessions/${session.id}`"
        ><div>
          <div class="meta">
            <CoreStatusBadge :value="session.status" /><span>{{
              formatDateTime(session.startedAtUtc ?? session.createdAtUtc)
            }}</span>
          </div>
          <h2>{{ session.title }}</h2>
          <p>
            {{ session.completedItems }}/{{ session.totalItems }} complete ·
            {{ formatMinutes(session.actualDurationMinutes ?? session.plannedDurationMinutes) }}
          </p>
        </div>
        <UIcon name="i-lucide-chevron-right"
      /></NuxtLink>
    </div>
  </div>
</template>
<style scoped>
.filter {
  margin-bottom: 1rem;
  padding: 0.5rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.5rem;
}
.sessions {
  display: grid;
  gap: 0.6rem;
}
.sessions a {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.7rem;
}
.sessions a:hover {
  border-color: var(--ui-primary);
}
.meta {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  color: var(--ui-text-muted);
  font-size: 0.78rem;
}
.sessions h2 {
  margin: 0.5rem 0 0.2rem;
  font-weight: 700;
}
.sessions p {
  color: var(--ui-text-muted);
}
</style>
