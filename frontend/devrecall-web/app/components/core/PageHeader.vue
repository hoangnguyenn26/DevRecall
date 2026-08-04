<script setup lang="ts">
defineProps<{ title: string; description?: string; eyebrow?: string }>()
</script>

<template>
  <header class="page-header">
    <div class="min-w-0">
      <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p v-if="description" class="description">{{ description }}</p>
    </div>
    <div v-if="$slots.actions || $slots.default" class="actions">
      <slot name="actions"><slot /></slot>
    </div>
  </header>
</template>

<style scoped>
.page-header { display: flex; align-items: start; justify-content: space-between; gap: 1rem; margin-bottom: 1.5rem; }
h1 { overflow-wrap: anywhere; font-size: clamp(1.5rem, 3vw, 2rem); font-weight: 700; letter-spacing: -.025em; }.description { max-width: 42rem; margin-top: .5rem; color: var(--ui-text-muted); font-size: .9rem; line-height: 1.5rem; }.eyebrow { margin-bottom: .5rem; color: var(--ui-primary); font-size: .75rem; font-weight: 650; letter-spacing: .08em; text-transform: uppercase; }
.actions { display: flex; gap: .5rem; }
@media (max-width: 640px) { .page-header { flex-direction: column; } }
</style>
