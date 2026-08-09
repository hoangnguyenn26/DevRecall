<script setup lang="ts">
import type { PracticeProgressModel } from '../practice.types'
defineProps<{ progress: PracticeProgressModel }>()
</script>

<template>
  <div class="flex items-center gap-3">
    <span v-if="progress.kind === 'steps'" class="whitespace-nowrap text-xs text-muted">{{ progress.label }} · {{ progress.currentStep }} / {{ progress.totalSteps }}</span>
    <span v-else class="whitespace-nowrap text-xs text-muted">{{ progress.completed }} / {{ progress.total }}</span>
    <div class="hidden h-1.5 w-24 overflow-hidden rounded-full bg-elevated sm:block" role="progressbar" :aria-valuenow="progress.kind === 'steps' ? progress.currentStep : progress.completed" aria-valuemin="0" :aria-valuemax="progress.kind === 'steps' ? progress.totalSteps : progress.total" :aria-label="progress.kind === 'steps' ? `${progress.label}, step ${progress.currentStep} of ${progress.totalSteps}` : `${progress.completed} of ${progress.total} completed`">
      <div class="h-full rounded-full bg-primary motion-reduce:transition-none" :style="{ width: `${progress.kind === 'steps' ? (progress.currentStep / progress.totalSteps) * 100 : progress.percent}%` }" />
    </div>
  </div>
</template>
