<script setup lang="ts">
definePageMeta({ layout: 'app' })
const route = useRoute()
useSeoMeta({ title: 'Recommendation' })
</script>
<template><RecommendationsWorkspace :selected-id="String(route.params.id)" /></template>
