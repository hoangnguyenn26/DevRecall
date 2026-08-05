<script setup lang="ts">
import type { TodayRecentActivity } from '~/features/today/today.types'
const props = defineProps<{ activity: TodayRecentActivity; generatedAtUtc: string }>()
const relativeTime = computed(() => {
  const seconds = Math.max(0, (Date.parse(props.generatedAtUtc) - Date.parse(props.activity.occurredAtUtc)) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
})
const icon = computed(() => `i-lucide-${props.activity.icon}`)
</script>

<template>
  <section class="flex flex-col gap-3 rounded-xl border border-default p-4 sm:flex-row sm:items-center">
    <div class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-elevated"><UIcon :name="icon" class="size-4 text-muted" /></div>
    <div class="min-w-0 flex-1"><p class="text-xs font-medium text-muted">Continue learning</p><p class="mt-1 truncate text-sm font-semibold">{{ activity.title }}</p><p class="mt-1 text-xs text-muted">{{ activity.description }} · {{ relativeTime }}</p></div>
    <UButton :to="activity.targetPath" color="neutral" variant="outline" label="Continue" icon="i-lucide-arrow-right" trailing />
  </section>
</template>
