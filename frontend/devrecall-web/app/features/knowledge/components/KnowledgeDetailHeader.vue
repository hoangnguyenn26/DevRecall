<script setup lang="ts">
import { formatRelativeKnowledgeDate } from '../knowledge.meta'
import type { KnowledgeDetail } from '../knowledge.types'
defineProps<{ knowledge: KnowledgeDetail }>()
const emit = defineEmits<{ edit: []; delete: []; tag: [id: string] }>()
const toast = useToast()
const menuItems = computed(() => [[
  { label: 'Copy internal link', icon: 'i-lucide-link', onSelect: async () => { await navigator.clipboard.writeText(window.location.href); toast.add({ title: 'Link copied' }) } },
  { label: 'Delete', icon: 'i-lucide-trash-2', color: 'error' as const, onSelect: () => emit('delete') },
]])
</script>

<template>
  <header class="border-b border-default px-5 py-4 sm:px-8"><div class="mx-auto flex max-w-[46rem] items-start justify-between gap-4"><div class="min-w-0"><div class="flex flex-wrap items-center gap-2 text-xs text-muted"><span>{{ knowledge.topicName ?? 'Uncategorized' }}</span><span aria-hidden="true">·</span><time :datetime="knowledge.updatedAtUtc">{{ formatRelativeKnowledgeDate(knowledge.updatedAtUtc) }}</time></div><div v-if="knowledge.tags.length" class="mt-2 flex flex-wrap gap-1.5"><button v-for="tag in knowledge.tags" :key="tag.id" class="rounded-full bg-elevated px-2 py-1 text-xs hover:text-primary" @click="$emit('tag', tag.id)">{{ tag.name }}</button></div></div><div class="flex shrink-0 gap-1"><UButton color="neutral" variant="soft" icon="i-lucide-pencil" label="Edit" @click="$emit('edit')" /><UDropdownMenu :items="menuItems"><UButton color="neutral" variant="ghost" icon="i-lucide-ellipsis" aria-label="More knowledge actions" /></UDropdownMenu></div></div></header>
</template>
