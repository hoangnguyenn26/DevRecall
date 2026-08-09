<script setup lang="ts">
import KnowledgeWorkspace from '~/features/knowledge/components/KnowledgeWorkspace.vue'
definePageMeta({ layout: 'app', middleware: 'auth', workspace: true })
useSeoMeta({ title: 'Knowledge' })
const route = useRoute()
const studySessionId = computed(() =>
  typeof route.query.studySession === 'string' ? route.query.studySession : undefined,
)
</script>

<template>
  <ClientOnly>
    <div
      v-if="studySessionId"
      class="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-primary/30 bg-primary/5 p-4"
    >
      <div>
        <strong>Study Session</strong>
        <p class="text-sm text-muted">
          Review this knowledge item, then return to mark it complete.
        </p>
      </div>
      <UButton :to="`/app/study-sessions/${studySessionId}`" label="Return to session" />
    </div>
    <KnowledgeWorkspace />
    <template #fallback><CoreLoadingState label="Opening knowledge workspace" /></template>
  </ClientOnly>
</template>
