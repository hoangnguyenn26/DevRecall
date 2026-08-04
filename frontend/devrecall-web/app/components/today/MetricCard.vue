<script setup lang="ts">
defineProps<{ label: string; value: string; description?: string; icon: string }>()
</script>

<template>
  <div class="h-full rounded-xl border border-default bg-default p-4">
    <div class="flex items-center justify-between gap-3">
      <p class="text-sm text-muted">{{ label }}</p>
      <UIcon :name="icon" class="size-4 text-muted" aria-hidden="true" />
    </div>
    <p class="mt-3 text-2xl font-semibold tracking-tight">{{ value }}</p>
    <p v-if="description" class="mt-1 text-xs text-muted">{{ description }}</p>
  </div>
</template>
