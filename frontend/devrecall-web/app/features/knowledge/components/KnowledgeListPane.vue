<script setup lang="ts">
import { formatRelativeKnowledgeDate, knowledgeSortOptions } from '../knowledge.meta'
import type { KnowledgeListPage, KnowledgeSort, KnowledgeTagOption } from '../knowledge.types'

const props = defineProps<{ page: KnowledgeListPage | null; loading: boolean; error?: unknown; selectedId?: string; activeId?: string; query: string; sort: KnowledgeSort; filtered: boolean; activeTagIds: string[]; tagOptions: KnowledgeTagOption[] }>()
const emit = defineEmits<{ select: [id: string]; activate: [id: string]; listFocus: []; search: [value: string]; sort: [value: KnowledgeSort]; page: [value: number]; tags: [value: string[]]; retry: [] }>()
const searchValue = ref(props.query)
const searchInput = useTemplateRef<HTMLElement>('searchInput')
const listScroller = useTemplateRef<HTMLElement>('listScroller')
const itemElements = new Map<string, HTMLElement>()
const filterDrawerOpen = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined

watch(() => props.query, value => { searchValue.value = value })
watch(searchValue, value => { clearTimeout(timer); timer = setTimeout(() => emit('search', value.trim()), 300) })
function toggleTag(id: string) { emit('tags', props.activeTagIds.includes(id) ? props.activeTagIds.filter(value => value !== id) : [...props.activeTagIds, id].sort()) }
function setItemElement(id: string, element: Element | null) { if (element instanceof HTMLElement) itemElements.set(id, element); else itemElements.delete(id) }
function focusSearch() { searchInput.value?.querySelector('input')?.focus() }
function focusActive() { if (props.activeId) itemElements.get(props.activeId)?.focus() }
function getScrollTop() { return listScroller.value?.scrollTop ?? 0 }
function restoreList(scrollTop: number, focusedId?: string) { if (listScroller.value) listScroller.value.scrollTop = scrollTop; if (focusedId) itemElements.get(focusedId)?.focus() }
defineExpose({ focusSearch, focusActive, getScrollTop, restoreList })
</script>

<template>
  <section class="flex h-full min-h-0 flex-col" aria-label="Knowledge list">
    <div class="space-y-3 border-b border-default p-3">
      <div ref="searchInput"><UInput v-model="searchValue" icon="i-lucide-search" placeholder="Search knowledge…" aria-label="Search knowledge" class="w-full" /></div>
      <div class="flex items-center justify-between gap-2">
        <span class="text-xs text-muted">{{ page?.totalCount ?? 0 }} results</span>
        <div class="flex gap-2">
          <UButton class="md:hidden" icon="i-lucide-list-filter" color="neutral" variant="outline" size="sm" label="Filters" @click="filterDrawerOpen = true" />
          <details class="relative hidden md:block"><summary class="cursor-pointer list-none rounded-md border border-default px-2.5 py-1.5 text-xs">Tags{{ activeTagIds.length ? ` (${activeTagIds.length})` : '' }}</summary><div class="absolute right-0 z-30 mt-1 max-h-56 w-56 overflow-y-auto rounded-lg border border-default bg-default p-2 shadow-lg"><label v-for="tag in tagOptions" :key="tag.id" class="flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-elevated"><input type="checkbox" :checked="activeTagIds.includes(tag.id)" @change="toggleTag(tag.id)"><span class="min-w-0 flex-1 truncate">{{ tag.name }}</span><span class="text-xs text-muted">{{ tag.knowledgeCount }}</span></label><p v-if="!tagOptions.length" class="p-2 text-xs text-muted">No tags yet.</p></div></details>
          <USelect :model-value="sort" :items="knowledgeSortOptions" value-key="value" class="hidden w-36 md:block" aria-label="Sort knowledge" @update:model-value="$emit('sort', $event as KnowledgeSort)" />
        </div>
      </div>
      <div v-if="activeTagIds.length" class="flex flex-wrap gap-1.5"><button v-for="id in activeTagIds" :key="id" class="rounded-full bg-primary/10 px-2 py-1 text-xs text-primary" @click="toggleTag(id)">{{ tagOptions.find(tag => tag.id === id)?.name ?? 'Tag' }} ×</button><button class="text-xs text-muted underline" @click="$emit('tags', [])">Clear tags</button></div>
    </div>
    <CoreLoadingState v-if="loading" label="Loading knowledge" />
    <CoreErrorState v-else-if="error" :error="error" @retry="$emit('retry')" />
    <CoreEmptyState v-else-if="!page?.items.length" :title="query ? 'No matching knowledge' : filtered ? 'Nothing in this topic' : 'Capture your first idea'" :description="query ? 'Try a broader phrase or clear the search.' : filtered ? 'Choose another topic or capture a note here.' : 'Use Quick Capture to save a concept while it is fresh.'" />
    <ul v-else ref="listScroller" class="min-h-0 flex-1 overflow-y-auto" aria-label="Knowledge notes">
      <li v-for="item in page.items" :key="item.id" class="border-b border-default">
        <a :ref="element => setItemElement(item.id, element as Element | null)" :href="`/app/knowledge/${item.id}`" class="block p-4 text-left hover:bg-muted/50 focus-visible:outline-2 focus-visible:outline-primary" :class="{ 'bg-primary/5': selectedId === item.id, 'ring-1 ring-inset ring-primary/40': activeId === item.id && selectedId !== item.id }" :aria-current="selectedId === item.id ? 'page' : undefined" :tabindex="activeId === item.id ? 0 : -1" @focus="$emit('activate', item.id); $emit('listFocus')" @click.prevent="$emit('select', item.id)">
          <div class="flex items-start justify-between gap-3"><h2 class="font-medium">{{ item.title }}</h2><span class="shrink-0 text-xs text-muted">{{ formatRelativeKnowledgeDate(item.updatedAtUtc) }}</span></div>
          <p class="mt-1 line-clamp-2 text-sm text-muted">{{ item.summary || 'No summary yet.' }}</p>
          <div class="mt-2 flex flex-wrap items-center gap-1.5 text-xs text-muted"><span v-if="item.topicName">{{ item.topicName }}</span><UBadge v-for="tag in item.tags.slice(0, 2)" :key="tag.id" color="neutral" variant="soft" size="sm">{{ tag.name }}</UBadge><span v-if="item.tags.length > 2">+{{ item.tags.length - 2 }}</span></div>
        </a>
      </li>
    </ul>
    <div v-if="page && page.totalPages > 1" class="flex items-center justify-between border-t border-default p-3 text-sm"><UButton icon="i-lucide-chevron-left" color="neutral" variant="ghost" :disabled="page.page <= 1" aria-label="Previous page" @click="$emit('page', page.page - 1)" /><span>Page {{ page.page }} of {{ page.totalPages }}</span><UButton icon="i-lucide-chevron-right" color="neutral" variant="ghost" :disabled="page.page >= page.totalPages" aria-label="Next page" @click="$emit('page', page.page + 1)" /></div>
    <USlideover v-model:open="filterDrawerOpen" title="Knowledge filters" description="Filter and sort the current list."><template #body><div class="space-y-6"><USelect :model-value="sort" :items="knowledgeSortOptions" value-key="value" class="w-full" aria-label="Sort knowledge" @update:model-value="$emit('sort', $event as KnowledgeSort)" /><fieldset><legend class="mb-2 text-sm font-medium">Tags</legend><label v-for="tag in tagOptions" :key="tag.id" class="flex items-center gap-2 py-2"><input type="checkbox" :checked="activeTagIds.includes(tag.id)" @change="toggleTag(tag.id)"><span class="flex-1">{{ tag.name }}</span><span class="text-xs text-muted">{{ tag.knowledgeCount }}</span></label></fieldset><UButton v-if="activeTagIds.length" color="neutral" variant="outline" label="Clear filters" block @click="$emit('tags', []); filterDrawerOpen = false" /></div></template></USlideover>
  </section>
</template>
