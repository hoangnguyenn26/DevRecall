<script setup lang="ts">
import StudyPlansWorkspace from '~/features/study-plans/components/StudyPlansWorkspace.vue'
definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Study plan', robots: 'noindex, nofollow' })
const route = useRoute()
const id = computed(() => String(route.params.id))
</script>

<template>
  <div>
    <CorePageHeader title="Study Plans" description="Plan your next focused learning block." />
    <StudyPlansWorkspace :selected-id="id" />
  </div>
</template>
