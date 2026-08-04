<script setup lang="ts">
defineProps<{ title?: string; description?: string }>()
</script>

<template>
  <section class="space-y-4">
    <div v-if="title || description || $slots.actions" class="flex items-start justify-between gap-4">
      <div>
        <h2 v-if="title" class="text-lg font-semibold">{{ title }}</h2>
        <p v-if="description" class="mt-1 text-sm text-muted">{{ description }}</p>
      </div>
      <div v-if="$slots.actions" class="shrink-0"><slot name="actions" /></div>
    </div>
    <slot />
  </section>
</template>
