<script setup lang="ts">
import type { NormalizedApiError } from '~/utils/normalize-api-error'

withDefaults(defineProps<{
  pending: boolean
  refreshing?: boolean
  error?: NormalizedApiError | null
  empty?: boolean
  minimumHeight?: string
}>(), { refreshing: false, error: null, empty: false, minimumHeight: '10rem' })
defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="relative" :style="{ minHeight: minimumHeight }" :aria-busy="pending || refreshing">
    <div v-if="pending" class="space-y-3" role="status" aria-label="Loading section">
      <USkeleton class="h-5 w-1/3" /><USkeleton class="h-16 w-full" /><USkeleton class="h-16 w-full" />
    </div>
    <FeedbackAppErrorState
      v-else-if="error"
      :title="error.title"
      :description="error.detail"
      :trace-id="error.status >= 500 ? error.traceId : undefined"
      @retry="$emit('retry')"
    />
    <FeedbackAppEmptyState v-else-if="empty" title="Nothing here yet" description="Activity will appear here when it becomes available." />
    <template v-else><slot /></template>
    <UIcon v-if="refreshing" name="i-lucide-loader-circle" class="absolute right-0 top-0 size-4 animate-spin text-muted" aria-label="Refreshing" />
  </div>
</template>
