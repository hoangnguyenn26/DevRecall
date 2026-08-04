<script setup lang="ts">
import { computed } from 'vue'
import type { TodayActivityPoint } from '~/features/today/today.types'
import { formatTodayMinutes } from '~/features/today/today.format'

const props = defineProps<{ activity: TodayActivityPoint[] }>()
const maximum = computed(() => Math.max(1, ...props.activity.map(point => point.activityCount)))
const weekday = (value: string) => new Intl.DateTimeFormat(undefined, { weekday: 'short', timeZone: 'UTC' }).format(new Date(`${value}T00:00:00Z`))
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader title="Weekly activity" to="/app/analytics" action-label="View analytics" />
    <ul class="grid grid-cols-7 gap-2" aria-label="Activity over the current week">
      <li v-for="point in activity" :key="point.date" class="flex min-w-0 flex-col items-center gap-2">
        <div class="flex h-20 w-full items-end rounded-lg bg-elevated p-1" aria-hidden="true">
          <div
            class="w-full rounded-md bg-primary/70"
            :style="{ height: `${Math.max(point.activityCount === 0 ? 0 : 12, point.activityCount / maximum * 100)}%` }"
          />
        </div>
        <span class="text-xs font-medium">{{ weekday(point.date) }}</span>
        <span class="sr-only">{{ point.activityCount }} activities, {{ formatTodayMinutes(point.studyMinutes) }}</span>
      </li>
    </ul>
  </section>
</template>
