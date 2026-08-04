<script setup lang="ts">
defineProps<{ icon?: string; title: string; description: string }>()
</script>

<template>
  <div class="flex min-h-64 flex-col items-center justify-center rounded-xl border border-dashed border-default p-8 text-center">
    <UIcon v-if="icon" :name="icon" class="size-8 text-muted" />
    <h3 class="mt-4 text-base font-semibold">{{ title }}</h3>
    <p class="mt-2 max-w-md text-sm leading-6 text-muted">{{ description }}</p>
    <div v-if="$slots.actions || $slots.default" class="mt-6 flex items-center gap-2">
      <slot name="actions"><slot /></slot>
    </div>
  </div>
</template>
