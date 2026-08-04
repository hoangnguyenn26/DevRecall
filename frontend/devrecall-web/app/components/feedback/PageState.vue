<script setup lang="ts">
import type { NormalizedApiError } from '~/utils/normalize-api-error'

withDefaults(defineProps<{
  pending: boolean
  refreshing?: boolean
  error?: NormalizedApiError | null
  empty?: boolean
  emptyTitle?: string
  emptyDescription?: string
  emptyIcon?: string
}>(), {
  refreshing: false,
  error: null,
  empty: false,
  emptyTitle: 'Nothing here yet',
  emptyDescription: 'Create your first item to get started.',
  emptyIcon: 'i-lucide-inbox',
})
defineEmits<{ retry: [] }>()
</script>

<template>
  <FeedbackPageSkeleton v-if="pending" />
  <FeedbackAppEmptyState
    v-else-if="error?.status === 404"
    icon="i-lucide-file-question"
    title="Resource not found"
    description="This resource may have been archived or is no longer available."
  >
    <slot name="notFoundActions" />
  </FeedbackAppEmptyState>
  <FeedbackConcurrencyConflictAlert v-else-if="error?.status === 409" @reload="$emit('retry')" />
  <FeedbackAppErrorState
    v-else-if="error"
    :title="error.status === 403 ? 'Access denied' : error.title"
    :description="error.detail"
    :trace-id="error.status >= 500 ? error.traceId : undefined"
    @retry="$emit('retry')"
  />
  <FeedbackAppEmptyState v-else-if="empty" :icon="emptyIcon" :title="emptyTitle" :description="emptyDescription">
    <template v-if="$slots.emptyActions" #actions><slot name="emptyActions" /></template>
  </FeedbackAppEmptyState>
  <div v-else class="relative">
    <div v-if="refreshing" class="absolute right-0 top-0" role="status" aria-label="Refreshing">
      <UIcon name="i-lucide-loader-circle" class="size-4 animate-spin text-muted" />
    </div>
    <slot />
  </div>
</template>
