<script setup lang="ts">
const props = defineProps<{ value: string }>()
const normalized = computed(() => props.value.toLowerCase())
const color = computed(() => {
  if (['active', 'ready', 'completed', 'easy', 'low'].includes(normalized.value)) return 'success'
  if (['critical', 'failed', 'cancelled', 'archived'].includes(normalized.value)) return 'error'
  if (['high', 'hard', 'overdue'].includes(normalized.value)) return 'warning'
  if (['medium', 'draft', 'inprogress', 'partiallysolved'].includes(normalized.value)) return 'primary'
  return 'neutral'
})
</script>

<template><UBadge :color="color" variant="subtle">{{ value }}</UBadge></template>
