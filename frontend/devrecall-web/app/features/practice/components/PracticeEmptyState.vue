<script setup lang="ts">
withDefaults(defineProps<{ title?: string; description?: string; returnTo?: string }>(), { title: 'You are all caught up', description: 'There are no practice items due right now.', returnTo: '/app' })
</script>

<template><div class="m-auto w-full max-w-xl"><CoreEmptyState :title="title" :description="description" icon="i-lucide-circle-check-big"><UButton class="mt-4" :to="returnTo">Return to Today</UButton></CoreEmptyState></div></template>
