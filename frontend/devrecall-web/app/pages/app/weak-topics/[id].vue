<script setup lang="ts">
definePageMeta({ layout: 'app' })
const route = useRoute()
useSeoMeta({ title: 'Weak Topic' })
</script>
<template><WeakTopicsWorkspace :selected-id="String(route.params.id)" /></template>
