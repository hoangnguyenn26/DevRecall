<script setup lang="ts">
import WeakTopicsWorkspace from '~/features/weak-topics/components/WeakTopicsWorkspace.vue'
definePageMeta({ layout: 'app' })
const route = useRoute()
useSeoMeta({ title: 'Weak Topic' })
</script>
<template><WeakTopicsWorkspace :selected-id="String(route.params.id)" /></template>
