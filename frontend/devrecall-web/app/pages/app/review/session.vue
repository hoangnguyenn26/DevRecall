<script setup lang="ts">
import ReviewSession from '~/features/review/components/ReviewSession.vue'
definePageMeta({ layout: 'focus', middleware: 'auth' })
useSeoMeta({ title: 'Review session', robots: 'noindex, nofollow' })
</script>
<template><ReviewSession /></template>
