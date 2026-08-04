<script setup lang="ts">
import { computed } from 'vue'
import { getDayPeriod } from '~/features/today/today.format'

const props = defineProps<{ displayName: string }>()
const now = new Date()
const greeting = computed(() => `Good ${getDayPeriod(now)}, ${props.displayName}`)
const formattedDate = new Intl.DateTimeFormat(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
}).format(now)
</script>

<template>
  <header class="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
    <div>
      <h1 class="text-2xl font-semibold tracking-tight sm:text-3xl">{{ greeting }}</h1>
      <p class="mt-2 text-sm text-muted">Here is the best place to continue your learning today.</p>
    </div>
    <time class="text-sm text-muted">{{ formattedDate }}</time>
  </header>
</template>
