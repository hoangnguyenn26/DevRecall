<script setup lang="ts">useHead({ meta: [{ name: 'robots', content: 'noindex,nofollow' }] })</script>
<template><main class="focus-layout"><NuxtLink to="/app/today" class="brand"><UIcon name="i-lucide-brain-circuit" /> DevRecall</NuxtLink><slot /></main></template>
<style scoped>.focus-layout { min-height: 100vh; padding: 1.25rem; }.brand { display: inline-flex; align-items: center; gap: .45rem; font-weight: 700; color: var(--ui-text-muted); }.focus-layout > :deep(:last-child) { width: min(100%, 58rem); margin: 5vh auto 0; }</style>
