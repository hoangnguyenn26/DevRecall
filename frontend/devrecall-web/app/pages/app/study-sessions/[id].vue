<script setup lang="ts">
import StudySessionWorkspace from '~/features/study-sessions/components/StudySessionWorkspace.vue'
definePageMeta({ layout: 'focus', middleware: 'auth' })
useSeoMeta({ title: 'Study session', robots: 'noindex, nofollow' })
const route = useRoute()
</script>

<template><StudySessionWorkspace :session-id="String(route.params.id)" /></template>
