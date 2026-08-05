<script setup lang="ts">
defineProps<{ label: string; value: string; description?: string; icon: string; to?: string; tone?: 'neutral' | 'warning' }>()
</script>

<template>
  <NuxtLink
    v-if="to"
    :to="to"
    class="block h-full rounded-xl border bg-default p-4 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
    :class="tone === 'warning' ? 'border-warning/40' : 'border-default'"
    :aria-label="`${label}: ${value}. ${description ?? ''}`"
  >
    <div class="flex items-center justify-between gap-3">
      <p class="text-sm text-muted">{{ label }}</p>
      <UIcon :name="icon" class="size-4 text-muted" aria-hidden="true" />
    </div>
    <p class="mt-3 text-2xl font-semibold tracking-tight">{{ value }}</p>
    <p v-if="description" class="mt-1 text-xs text-muted">{{ description }}</p>
  </NuxtLink>
  <div v-else class="h-full rounded-xl border border-default bg-default p-4">
    <div class="flex items-center justify-between gap-3">
      <p class="text-sm text-muted">{{ label }}</p>
      <UIcon :name="icon" class="size-4 text-muted" aria-hidden="true" />
    </div>
    <p class="mt-3 text-2xl font-semibold tracking-tight">{{ value }}</p>
    <p v-if="description" class="mt-1 text-xs text-muted">{{ description }}</p>
  </div>
</template>
