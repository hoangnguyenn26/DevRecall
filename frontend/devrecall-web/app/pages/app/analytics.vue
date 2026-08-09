<script setup lang="ts">
import AnalyticsWorkspace from '~/features/analytics/components/AnalyticsWorkspace.vue'
definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Analytics', robots: 'noindex, nofollow' })
</script>
<template><AnalyticsWorkspace /></template>
