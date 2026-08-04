<script setup lang="ts">
definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Today', robots: 'noindex, nofollow' })
await navigateTo('/app/today', { replace: true })
</script>

<template><div /></template>
