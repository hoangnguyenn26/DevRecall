<script setup lang="ts">
import { computed } from 'vue'
import type { UiStatusMeta, UiTone } from '~/types/ui-meta'

const props = defineProps<{ meta?: UiStatusMeta; value?: string }>()
const normalized = computed(() => (props.value ?? props.meta?.label ?? '').toLowerCase())
const fallbackTone = computed<UiTone>(() => {
  if (['active', 'ready', 'completed', 'easy', 'low'].includes(normalized.value)) return 'success'
  if (['critical', 'failed', 'cancelled', 'archived'].includes(normalized.value)) return 'error'
  if (['high', 'hard', 'overdue'].includes(normalized.value)) return 'warning'
  if (['medium', 'draft', 'inprogress', 'partiallysolved'].includes(normalized.value)) return 'primary'
  return 'neutral'
})
const color = computed(() => props.meta?.tone ?? fallbackTone.value)
const label = computed(() => props.meta?.label ?? props.value ?? 'Unknown')
const icon = computed(() => props.meta?.icon)
</script>

<template><UBadge :color="color" variant="subtle" :icon="icon">{{ label }}</UBadge></template>
