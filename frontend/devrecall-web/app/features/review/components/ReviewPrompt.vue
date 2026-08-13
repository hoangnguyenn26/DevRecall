<script setup lang="ts">
import { reviewResourceLabels } from '../review.meta'
import type { ReviewSource } from '../review.types'
defineProps<{ title: string; resourceType: string; source?: ReviewSource }>()
</script>
<template>
  <article class="mx-auto flex w-full max-w-3xl flex-1 flex-col items-center justify-center text-center">
    <CoreStatusBadge :value="reviewResourceLabels[resourceType] ?? 'Review item'" />
    <h1 tabindex="-1" class="mt-5 max-w-[28ch] text-2xl font-semibold leading-tight outline-none sm:text-4xl">{{ title }}</h1>
    <p v-if="source?.type === 'LearningContent'" class="mt-3 text-xs text-muted">From lesson · {{ source.title }}</p>
    <p class="mt-4 text-muted">Think of your answer before revealing it.</p>
  </article>
</template>
