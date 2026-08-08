<script setup lang="ts">
import type { PracticeProgress } from '../practice.types'
defineProps<{ progress: PracticeProgress }>()
</script>

<template>
  <div class="flex items-center gap-3">
    <span class="whitespace-nowrap text-xs text-muted">{{ progress.completed }} / {{ progress.total }}</span>
    <div class="hidden h-1.5 w-24 overflow-hidden rounded-full bg-elevated sm:block" role="progressbar" :aria-valuenow="progress.completed" aria-valuemin="0" :aria-valuemax="progress.total" :aria-label="`${progress.completed} of ${progress.total} completed`">
      <div class="h-full rounded-full bg-primary motion-reduce:transition-none" :style="{ width: `${progress.percent}%` }" />
    </div>
  </div>
</template>
