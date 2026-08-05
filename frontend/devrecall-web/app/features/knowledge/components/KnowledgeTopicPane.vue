<script setup lang="ts">
import type { KnowledgeTopicTree } from '../knowledge.types'
defineProps<{ tree: KnowledgeTopicTree | null; loading: boolean; error?: unknown; selectedId?: string; uncategorized: boolean; expanded: string[] }>()
defineEmits<{ select: [id?: string, uncategorized?: boolean]; toggle: [id: string]; retry: [] }>()
</script>

<template>
  <aside class="flex h-full min-h-0 flex-col bg-muted/30" aria-label="Knowledge topics">
    <div class="border-b border-default px-4 py-4"><p class="font-semibold">Topics</p><p class="text-xs text-muted">{{ tree?.totalKnowledgeCount ?? 0 }} notes</p></div>
    <CoreLoadingState v-if="loading" label="Loading topics" />
    <CoreErrorState v-else-if="error" :error="error" @retry="$emit('retry')" />
    <nav v-else class="min-h-0 flex-1 overflow-y-auto p-2" aria-label="Topic filters">
      <button type="button" class="mb-1 flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm" :class="!selectedId && !uncategorized ? 'bg-primary/10 text-primary' : 'hover:bg-elevated'" @click="$emit('select')"><span>All knowledge</span><span class="text-xs text-muted">{{ tree?.totalKnowledgeCount ?? 0 }}</span></button>
      <button type="button" class="mb-2 flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm" :class="uncategorized ? 'bg-primary/10 text-primary' : 'hover:bg-elevated'" @click="$emit('select', undefined, true)"><span>Uncategorized</span><span class="text-xs text-muted">{{ tree?.uncategorizedCount ?? 0 }}</span></button>
      <KnowledgeTopicBranch v-if="tree?.items.length" :items="tree.items" :selected-id="selectedId" :expanded="expanded" @select="$emit('select', $event)" @toggle="$emit('toggle', $event)" />
      <CoreEmptyState v-else title="No topics yet" description="Capture your first note to begin organizing knowledge." />
    </nav>
  </aside>
</template>
