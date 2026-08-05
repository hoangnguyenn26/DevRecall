<script setup lang="ts">
definePageMeta({ layout: 'app', middleware: 'auth' })
const route = useRoute()
await navigateTo({ path: '/app/knowledge', query: { node: String(route.params.id) } }, { replace: true })
</script>

<template><span class="sr-only">Opening knowledge resource</span></template>
