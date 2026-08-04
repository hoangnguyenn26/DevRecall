<script setup lang="ts">
withDefaults(defineProps<{ title?: string; description?: string; traceId?: string }>(), {
  title: 'Unable to load this section',
  description: 'Check your connection and try again.',
  traceId: undefined,
})
defineEmits<{ retry: [] }>()
</script>

<template>
  <div>
    <UAlert color="error" variant="subtle" icon="i-lucide-circle-alert" :title="title" :description="description">
      <template #actions>
        <UButton size="sm" color="error" variant="soft" @click="$emit('retry')">Try again</UButton>
      </template>
    </UAlert>
    <p v-if="traceId" class="mt-2 break-all text-xs text-muted">Reference: {{ traceId }}</p>
  </div>
</template>
