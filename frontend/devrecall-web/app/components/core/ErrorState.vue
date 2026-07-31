<script setup lang="ts">
import { ApiError } from '~/types/api'
const props = defineProps<{ error: unknown }>()
defineEmits<{ retry: [] }>()
const message = computed(() => props.error instanceof ApiError ? props.error.problem.detail ?? props.error.problem.title : props.error instanceof Error ? props.error.message : 'An unexpected error occurred.')
const traceId = computed(() => props.error instanceof ApiError ? props.error.problem.traceId : undefined)
</script>

<template>
  <div class="state" role="alert">
    <UIcon name="i-lucide-circle-alert" class="size-7 text-error" />
    <strong>Something went wrong</strong>
    <p>{{ message }}</p>
    <small v-if="traceId">Trace: {{ traceId }}</small>
    <UButton label="Try again" color="neutral" variant="outline" @click="$emit('retry')" />
  </div>
</template>

<style scoped>.state { display: grid; min-height: 12rem; place-items: center; align-content: center; gap: .6rem; padding: 2rem; text-align: center; } p, small { color: var(--ui-text-muted); }</style>
