<script setup lang="ts">
defineProps<{ title: string; to?: string; actionLabel?: string }>()
</script>

<template>
  <header class="mb-4 flex items-center justify-between gap-4">
    <h2 class="text-base font-semibold tracking-tight">{{ title }}</h2>
    <UButton v-if="to" :to="to" variant="ghost" color="neutral" size="sm">{{ actionLabel ?? 'View all' }}</UButton>
  </header>
</template>
