<script setup lang="ts">
import type { KnowledgeEditState, KnowledgeTagOption } from '../knowledge.types'
import KnowledgeTagPicker from './KnowledgeTagPicker.vue'
defineProps<{ topics: { label: string; value: string | null }[]; selectedTags: KnowledgeTagOption[]; saving: boolean; conflict: boolean; dirty: boolean }>()
defineEmits<{ save: []; cancel: []; reload: []; tagAdded: [tag: KnowledgeTagOption] }>()
const form = defineModel<KnowledgeEditState>({ required: true })
</script>

<template>
  <section class="flex h-full min-h-0 flex-col" aria-label="Edit knowledge">
<header class="sticky top-0 z-10 flex items-center justify-between gap-3 border-b border-default bg-default/95 px-4 py-3 backdrop-blur"><div><h2 class="font-semibold">Edit knowledge</h2><p class="text-xs text-muted">Ctrl/⌘ + S to save</p></div><div class="flex gap-1"><UButton color="neutral" variant="ghost" label="Cancel" @click="$emit('cancel')" /><UButton icon="i-lucide-save" label="Save" :loading="saving" :disabled="saving || !dirty" @click="$emit('save')" /></div></header>
    <form class="mx-auto grid w-full max-w-[46rem] gap-5 overflow-y-auto px-5 py-6 sm:px-8" @submit.prevent="$emit('save')">
      <FeedbackConcurrencyConflictAlert v-if="conflict" resource-label="knowledge item" @reload="$emit('reload')" />
      <UFormField label="Title" required><UInput v-model="form.title" name="title" autofocus class="w-full" maxlength="200" /></UFormField>
      <UFormField label="Topic"><USelect v-model="form.topicId" :items="topics" value-key="value" class="w-full" /></UFormField>
      <UFormField label="Tags"><KnowledgeTagPicker v-model="form.tagIds" :selected="selectedTags.filter(tag => form.tagIds.includes(tag.id))" @added="$emit('tagAdded', $event)" /></UFormField>
      <UFormField label="Content"><UTextarea v-model="form.content" name="content" :rows="18" autoresize class="w-full" data-code /></UFormField>
      <p class="sr-only" aria-live="polite">{{ saving ? 'Saving knowledge' : conflict ? 'A newer version is available' : '' }}</p>
    </form>
  </section>
</template>
