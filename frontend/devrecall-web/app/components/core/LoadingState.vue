<script setup lang="ts">
withDefaults(defineProps<{ label?: string }>(), { label: 'Loading' })
</script>

<template>
  <div class="state" role="status" aria-live="polite">
    <UIcon name="i-lucide-loader-circle" class="size-5 animate-spin" />
    <span>{{ label }}…</span>
  </div>
</template>

<style scoped>.state { display: flex; min-height: 10rem; align-items: center; justify-content: center; gap: .6rem; color: var(--ui-text-muted); }</style>
