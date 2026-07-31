<script setup lang="ts">
defineProps<{ title: string; description?: string }>()
</script>

<template>
  <header class="page-header">
    <div><h1>{{ title }}</h1><p v-if="description">{{ description }}</p></div>
    <div class="actions"><slot /></div>
  </header>
</template>

<style scoped>
.page-header { display: flex; align-items: start; justify-content: space-between; gap: 1rem; margin-bottom: 1.5rem; }
h1 { font-size: clamp(1.5rem, 3vw, 2rem); font-weight: 700; letter-spacing: -.025em; } p { margin-top: .3rem; color: var(--ui-text-muted); }
.actions { display: flex; gap: .5rem; }
@media (max-width: 640px) { .page-header { flex-direction: column; } }
</style>
