<script setup lang="ts">
import type { ReviewSource } from '../review.types'

defineProps<{ source: ReviewSource; canOpen: boolean }>()
</script>

<template>
  <aside class="mx-auto mt-4 w-full max-w-3xl rounded-xl border border-default/70 px-4 py-3" aria-label="Review source">
    <p class="text-xs font-medium uppercase tracking-wide text-muted">From lesson</p>
    <div class="mt-1 flex flex-wrap items-center justify-between gap-3">
      <div>
        <p class="text-sm font-medium">{{ source.title }}</p>
        <p v-if="!source.isAvailable" class="mt-0.5 text-xs text-muted">Source lesson is unavailable. This review card remains usable.</p>
      </div>
      <UButton
        v-if="source.isAvailable && canOpen"
        :to="`/app/learn/${source.slug}`"
        target="_blank"
        rel="noopener"
        color="neutral"
        variant="ghost"
        size="sm"
      >
        Review lesson
      </UButton>
      <CoreStatusBadge v-else-if="!source.isAvailable" value="Unavailable" />
    </div>
  </aside>
</template>
