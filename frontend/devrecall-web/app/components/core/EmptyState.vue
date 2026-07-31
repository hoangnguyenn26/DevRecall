<script setup lang="ts">
withDefaults(defineProps<{ title: string; description?: string; icon?: string }>(), { description: '', icon: 'i-lucide-inbox' })
</script>

<template>
  <div class="state">
    <UIcon :name="icon" class="size-8 text-muted" />
    <h2>{{ title }}</h2>
    <p v-if="description">{{ description }}</p>
    <slot />
  </div>
</template>

<style scoped>
.state { display: grid; min-height: 12rem; place-items: center; align-content: center; gap: .5rem; padding: 2rem; text-align: center; }
h2 { font-size: 1rem; font-weight: 650; } p { max-width: 30rem; color: var(--ui-text-muted); font-size: .9rem; }
</style>
