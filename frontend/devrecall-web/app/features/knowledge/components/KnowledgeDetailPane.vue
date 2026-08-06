<script setup lang="ts">
import type { KnowledgeDetail } from '../knowledge.types'
import { buildRelatedReason } from '../knowledge.edit'
import KnowledgeDetailHeader from './KnowledgeDetailHeader.vue'
const props = defineProps<{ detail: KnowledgeDetail | null; loading: boolean; error?: unknown }>()
defineEmits<{ retry: []; back: []; edit: []; delete: []; tag: [id: string]; related: [id: string] }>()
const contentBlocks = computed(() => (props.detail?.content ?? '').split(/```/).map((value, index) => ({
  value: index % 2 === 1 ? value.trim().replace(/^[a-z0-9+#.-]+\r?\n/i, '') : value,
  code: index % 2 === 1,
})))
const heading = useTemplateRef<HTMLElement>('heading')
function focusHeading() { heading.value?.focus() }
defineExpose({ focusHeading })
</script>

<template>
  <section class="h-full min-h-0 overflow-y-auto" aria-label="Knowledge detail">
    <div class="sticky top-0 z-10 flex items-center gap-2 border-b border-default bg-default/95 px-4 py-3 backdrop-blur md:hidden"><UButton icon="i-lucide-arrow-left" color="neutral" variant="ghost" aria-label="Back to knowledge list" @click="$emit('back')" /><span class="text-sm font-medium">Back to list</span></div>
    <CoreLoadingState v-if="loading" label="Loading note" />
    <CoreErrorState v-else-if="error" :error="error" @retry="$emit('retry')" />
    <CoreEmptyState v-else-if="!detail" title="Select a note" description="Choose an item from the list to read it without losing your place." />
    <template v-else>
      <KnowledgeDetailHeader :knowledge="detail" @edit="$emit('edit')" @delete="$emit('delete')" @tag="$emit('tag', $event)" />
      <article class="mx-auto max-w-[var(--devrecall-reading-width)] px-5 py-8 sm:px-8">
      <h1 ref="heading" tabindex="-1" class="text-2xl font-semibold tracking-tight outline-none sm:text-3xl">{{ detail.title }}</h1><p v-if="detail.description" class="mt-4 text-base text-muted">{{ detail.description }}</p>
      <div class="knowledge-content mt-8 text-[0.98rem] leading-7"><template v-if="detail.content"><template v-for="(block, index) in contentBlocks" :key="index"><pre v-if="block.code"><code>{{ block.value.trim() }}</code></pre><div v-else class="whitespace-pre-wrap">{{ block.value }}</div></template></template><p v-else class="text-muted">No content has been added yet.</p></div>
      <a v-if="detail.sourceUrl" :href="detail.sourceUrl" target="_blank" rel="noreferrer" class="mt-8 inline-flex items-center gap-1 text-sm text-primary">Open reference <UIcon name="i-lucide-external-link" /></a>
      <section v-if="detail.relatedItems.length" class="mt-12"><h2 class="mb-3 text-lg font-semibold">Related knowledge</h2><ul class="divide-y divide-default rounded-xl border border-default"><li v-for="item in detail.relatedItems" :key="item.id"><button class="block w-full p-4 text-left hover:bg-elevated/50" @click="$emit('related', item.id)"><span class="line-clamp-2 text-sm font-medium">{{ item.title }}</span><span class="mt-1 block text-xs text-muted">{{ buildRelatedReason(item) }}</span></button></li></ul></section>
      </article>
    </template>
  </section>
</template>

<style scoped>.knowledge-content pre { overflow-x: auto; margin: 1rem 0; border-radius: .75rem; padding: 1rem; background: var(--ui-bg-elevated); }.knowledge-content code { font-family: "JetBrains Mono", ui-monospace, monospace; white-space: pre; }</style>
