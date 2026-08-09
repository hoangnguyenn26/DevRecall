<script setup lang="ts">
import RecommendationsWorkspace from '~/features/recommendations/components/RecommendationsWorkspace.vue'
definePageMeta({ layout: 'app' })
const route = useRoute()
useSeoMeta({ title: 'Recommendation' })
</script>
<template><RecommendationsWorkspace :selected-id="String(route.params.id)" /></template>
