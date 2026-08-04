<script setup lang="ts">
withDefaults(defineProps<{ compact?: boolean }>(), { compact: false })
</script>

<template>
  <NuxtLink
    to="/app"
    class="flex min-w-0 items-center gap-2 rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
    aria-label="DevRecall Today"
  >
    <span class="flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary text-sm font-semibold text-inverted">D</span>
    <span v-if="!compact" class="truncate font-semibold tracking-tight">DevRecall</span>
  </NuxtLink>
</template>
