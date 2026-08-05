<script setup lang="ts">
import KnowledgeWorkspace from '~/features/knowledge/components/KnowledgeWorkspace.vue'
definePageMeta({ layout: 'app', middleware: 'auth', workspace: true })
useSeoMeta({ title: 'Knowledge' })
</script>

<template>
  <ClientOnly>
    <KnowledgeWorkspace />
    <template #fallback><CoreLoadingState label="Opening knowledge workspace" /></template>
  </ClientOnly>
</template>
