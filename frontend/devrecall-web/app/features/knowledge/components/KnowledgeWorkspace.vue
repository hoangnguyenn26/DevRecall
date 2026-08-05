<script setup lang="ts">
import { ApiError } from '~/types/api'
import { queryKeys } from '~/query/query-keys'
import { useQuickCapture } from '~/features/quick-capture/useQuickCapture'
import type { KnowledgeDetail, KnowledgeDetailMode, KnowledgeEditState, KnowledgeListPage, KnowledgeSort, KnowledgeTagOption, KnowledgeTopic, KnowledgeTopicTree } from '../knowledge.types'
import { createKnowledgeEditState, isKnowledgeEditDirty } from '../knowledge.edit'
import KnowledgeDetailPane from './KnowledgeDetailPane.vue'
import KnowledgeEditor from './KnowledgeEditor.vue'
import KnowledgeListPane from './KnowledgeListPane.vue'
import KnowledgeTopicPane from './KnowledgeTopicPane.vue'

const api = useApi(); const route = useRoute(); const router = useRouter(); const toast = useToast()
const quickCapture = useQuickCapture(); const currentUser = useCurrentUser(); const topicDrawerOpen = ref(false)
const selectedId = computed(() => {
  const match = route.path.match(/^\/app\/knowledge\/([^/]+)$/)
  return match?.[1]
})
const topicId = computed(() => typeof route.query.topicId === 'string' ? route.query.topicId : undefined)
const uncategorized = computed(() => route.query.topicScope === 'uncategorized')
const searchQuery = computed(() => typeof route.query.query === 'string' ? route.query.query : '')
const sort = computed<KnowledgeSort>(() => ['created', 'title'].includes(String(route.query.sort)) ? route.query.sort as KnowledgeSort : 'updated')
const pageNumber = computed(() => Math.max(1, Number(route.query.page) || 1))
const tagIds = computed(() => typeof route.query.tagIds === 'string' ? [...new Set(route.query.tagIds.split(',').filter(Boolean))].sort() : [])

const { data: listPage, status: listStatus, error: listError, refresh: refreshList } = await useAsyncData<KnowledgeListPage>(queryKeys.knowledgeList,
  () => api.get('/knowledge', { topicId: topicId.value, topicScope: uncategorized.value ? 'uncategorized' : undefined, query: searchQuery.value, tagIds: tagIds.value.join(','), sort: sort.value, page: pageNumber.value, pageSize: 30 }),
  { server: false, watch: [topicId, uncategorized, searchQuery, tagIds, sort, pageNumber] })
const { data: topicTree, status: treeStatus, error: treeError, refresh: refreshTree } = await useAsyncData<KnowledgeTopicTree>(
  queryKeys.knowledgeTopics, () => api.get('/knowledge/topics/tree'), { server: false })
const { data: tagOptions } = await useAsyncData<KnowledgeTagOption[]>(queryKeys.knowledgeTags,
  () => api.get('/knowledge/tags', { take: 20 }), { server: false, default: () => [] })
const detail = ref<KnowledgeDetail | null>(null); const detailError = ref<unknown>(); const detailLoading = ref(false)
const mode = ref<KnowledgeDetailMode>('read'); const form = ref<KnowledgeEditState | null>(null); const initialForm = ref<KnowledgeEditState | null>(null)
const editorTags = ref<KnowledgeTagOption[]>([]); const saving = ref(false); const conflict = ref(false); const confirmDialog = useConfirmDialog()
const dirty = computed(() => Boolean(form.value && initialForm.value && isKnowledgeEditDirty(form.value, initialForm.value)))
useUnsavedChangesGuard(dirty)
let detailController: AbortController | undefined
async function loadDetail() {
  detailController?.abort(); detail.value = null; detailError.value = undefined; mode.value = 'read'; form.value = null; initialForm.value = null; conflict.value = false
  if (!selectedId.value) return
  detailController = new AbortController(); detailLoading.value = true
  try { detail.value = await api.get<KnowledgeDetail>(`/knowledge/${selectedId.value}`, undefined, detailController.signal) }
  catch (error) { if (!(error instanceof DOMException && error.name === 'AbortError')) detailError.value = error }
  finally { detailLoading.value = false }
}
watch(selectedId, loadDetail, { immediate: true })

function flattenTopics(items: KnowledgeTopic[], depth = 0): { label: string; value: string | null }[] {
  return items.flatMap(item => [{ label: `${'— '.repeat(depth)}${item.name}`, value: item.id }, ...flattenTopics(item.children, depth + 1)])
}
const topicOptions = computed(() => [{ label: 'Uncategorized', value: null }, ...flattenTopics(topicTree.value?.items ?? [])])
function startEdit() {
  if (!detail.value) return; const state = createKnowledgeEditState(detail.value)
  form.value = structuredClone(state); initialForm.value = structuredClone(state)
  editorTags.value = detail.value.tags.map(tag => ({ ...tag, normalizedName: tag.name.toUpperCase(), knowledgeCount: 0 }))
  conflict.value = false; mode.value = 'edit'
}
async function confirmDiscard(): Promise<boolean> {
  if (!dirty.value) return true
  return await confirmDialog.open({ title: 'Discard unsaved changes?', description: 'Your changes have not been saved.', confirmLabel: 'Discard changes', tone: 'danger' })
}
async function cancelEdit() { if (!await confirmDiscard()) return; mode.value = 'read'; form.value = null; initialForm.value = null; conflict.value = false }
function addEditorTag(tag: KnowledgeTagOption) { if (!editorTags.value.some(item => item.id === tag.id)) editorTags.value.push(tag) }
async function saveKnowledge() {
  if (!detail.value || !form.value || !dirty.value || saving.value || !form.value.title.trim()) return
  saving.value = true; conflict.value = false
  try {
    const response = await api.put<{ detail: KnowledgeDetail; changed: boolean; topicChanged: boolean }>(`/knowledge/${detail.value.id}`, {
      title: form.value.title, content: form.value.content, topicId: form.value.topicId,
      tagIds: [...new Set(form.value.tagIds)], expectedVersion: form.value.expectedVersion,
    })
    detail.value = response.detail; mode.value = 'read'; form.value = null; initialForm.value = null
    const refreshes: Promise<unknown>[] = [refreshList(), refreshNuxtData(queryKeys.knowledgeTags)]
    if (response.topicChanged) refreshes.push(refreshTree())
    await Promise.all(refreshes); toast.add({ title: 'Knowledge saved', color: 'success' })
  } catch (error) { if (error instanceof ApiError && error.problem.status === 409) conflict.value = true; else throw error }
  finally { saving.value = false }
}
async function reloadLatest() { if (!await confirmDiscard()) return; await loadDetail(); if (detail.value) startEdit() }
async function deleteKnowledge() {
  if (!detail.value) return
  const accepted = await confirmDialog.open({ title: `Delete “${detail.value.title}”?`, description: 'This removes it from your knowledge workspace. Related references will no longer point to it.', confirmLabel: 'Delete knowledge', tone: 'danger' })
  if (!accepted) return
  try {
    await api.delete(`/knowledge/${detail.value.id}`, { expectedVersion: detail.value.version })
    const previousPage = listPage.value?.items.length === 1 && pageNumber.value > 1 ? pageNumber.value - 1 : pageNumber.value
    await router.push({ path: '/app/knowledge', query: { ...route.query, page: previousPage === 1 ? undefined : previousPage } })
    await Promise.all([refreshList(), refreshTree(), refreshNuxtData(queryKeys.knowledgeTags)])
    toast.add({ title: 'Knowledge deleted' })
  } catch (error) { if (error instanceof ApiError && error.problem.status === 409) { conflict.value = true; mode.value = 'edit'; if (!form.value && detail.value) startEdit() } else throw error }
}

const expansionCookie = useCookie<Record<string, string[]>>('devrecall:knowledge-expanded', { default: () => ({}) })
const expansionKey = computed(() => currentUser.value?.id ?? 'anonymous')
const expanded = computed(() => expansionCookie.value[expansionKey.value] ?? [])
function toggleTopic(id: string) {
  const values = new Set(expanded.value)
  if (values.has(id)) values.delete(id)
  else values.add(id)
  expansionCookie.value = { ...expansionCookie.value, [expansionKey.value]: [...values] }
}
function findAncestors(items: KnowledgeTopic[], id: string, trail: string[] = []): string[] | null {
  for (const item of items) { if (item.id === id) return trail; const match = findAncestors(item.children, id, [...trail, item.id]); if (match) return match }
  return null
}
watch([topicId, topicTree], () => {
  if (!topicId.value || !topicTree.value) return
  const ancestors = findAncestors(topicTree.value.items, topicId.value) ?? []
  expansionCookie.value = { ...expansionCookie.value, [expansionKey.value]: [...new Set([...expanded.value, ...ancestors])] }
}, { immediate: true })

async function updateQuery(changes: Record<string, string | number | undefined>, clearSelection = false) {
  const query = Object.fromEntries(Object.entries({ ...route.query, ...changes })
    .filter(([, value]) => value !== undefined && value !== ''))
  await router.push({ path: clearSelection ? '/app/knowledge' : route.path, query })
}
async function guardedUpdateQuery(changes: Record<string, string | number | undefined>, clearSelection = false) {
  if (await confirmDiscard()) await updateQuery(changes, clearSelection)
}
async function selectTopic(id?: string, asUncategorized = false) {
  if (!await confirmDiscard()) return
  topicDrawerOpen.value = false
  await updateQuery({ topicId: id, topicScope: asUncategorized ? 'uncategorized' : undefined, page: undefined }, true)
}
async function selectKnowledge(id: string) { if (await confirmDiscard()) await router.push({ path: `/app/knowledge/${id}`, query: route.query }) }
async function backToList() { if (await confirmDiscard()) await router.push({ path: '/app/knowledge', query: route.query }) }
async function applyTagFilter(id: string) { if (!await confirmDiscard()) return; await updateQuery({ tagIds: id, page: undefined }, true) }
async function openRelated(id: string) { if (!await confirmDiscard()) return; await router.push({ path: `/app/knowledge/${id}`, query: { sort: route.query.sort } }) }
watch(listError, async error => {
  if (!(error instanceof ApiError) || error.problem.errorCode !== 'KNOWLEDGE_TOPIC_NOT_FOUND') return
  toast.add({ title: 'Topic is no longer available', description: 'The topic filter was cleared.', color: 'warning' })
  await updateQuery({ topicId: undefined, topicScope: undefined, page: undefined }, true)
})
function onKeyboard(event: KeyboardEvent) {
  const target = event.target as HTMLElement | null; const typing = target?.matches('input, textarea, select, [contenteditable="true"]')
  if (mode.value === 'read' && event.key.toLowerCase() === 'e' && !typing) { event.preventDefault(); startEdit() }
  if (mode.value === 'edit' && event.key.toLowerCase() === 's' && (event.ctrlKey || event.metaKey)) { event.preventDefault(); void saveKnowledge() }
  if (mode.value === 'edit' && event.key === 'Escape') { event.preventDefault(); void cancelEdit() }
}
onMounted(() => window.addEventListener('keydown', onKeyboard))
onBeforeUnmount(() => { detailController?.abort(); window.removeEventListener('keydown', onKeyboard) })
</script>

<template>
  <div class="flex h-[calc(100dvh-var(--devrecall-topbar-height)-2rem)] min-h-[36rem] flex-col overflow-hidden rounded-xl border border-default bg-default shadow-sm lg:h-[calc(100dvh-var(--devrecall-topbar-height)-3rem)]">
    <header class="flex min-h-16 items-center justify-between gap-3 border-b border-default px-3 sm:px-4"><div class="flex min-w-0 items-center gap-2"><UButton class="lg:hidden" icon="i-lucide-panel-left" color="neutral" variant="ghost" aria-label="Open topics" @click="topicDrawerOpen = true" /><div class="min-w-0"><h1 class="truncate text-lg font-semibold">Knowledge</h1><p class="hidden text-xs text-muted sm:block">Explore your connected learning notes.</p></div></div><UButton icon="i-lucide-plus" label="Capture note" @click="quickCapture.start('KnowledgeNode')" /></header>
    <div class="grid min-h-0 flex-1 grid-cols-1 lg:grid-cols-[17rem_minmax(19rem,25rem)_minmax(0,1fr)]">
      <KnowledgeTopicPane class="hidden border-r border-default lg:flex" :tree="topicTree ?? null" :loading="treeStatus === 'pending'" :error="treeError" :selected-id="topicId" :uncategorized="uncategorized" :expanded="expanded" @select="selectTopic" @toggle="toggleTopic" @retry="refreshTree" />
      <KnowledgeListPane class="border-r-0 border-default md:border-r" :class="selectedId ? 'hidden md:flex' : 'flex'" :page="listPage ?? null" :loading="listStatus === 'pending'" :error="listError" :selected-id="selectedId" :query="searchQuery" :sort="sort" :filtered="Boolean(topicId || uncategorized || tagIds.length)" :active-tag-ids="tagIds" :tag-options="tagOptions" @select="selectKnowledge" @search="guardedUpdateQuery({ query: $event || undefined, page: undefined }, true)" @sort="guardedUpdateQuery({ sort: $event === 'updated' ? undefined : $event, page: undefined })" @tags="guardedUpdateQuery({ tagIds: $event.length ? $event.join(',') : undefined, page: undefined }, true)" @page="guardedUpdateQuery({ page: $event === 1 ? undefined : $event })" @retry="refreshList" />
      <KnowledgeEditor v-if="selectedId && mode === 'edit' && form" v-model="form" class="block" :topics="topicOptions" :selected-tags="editorTags" :saving="saving" :conflict="conflict" :dirty="dirty" @save="saveKnowledge" @cancel="cancelEdit" @reload="reloadLatest" @tag-added="addEditorTag" />
      <KnowledgeDetailPane v-else :class="selectedId ? 'block' : 'hidden md:block'" :detail="detail" :loading="detailLoading" :error="detailError" @retry="loadDetail" @back="backToList" @edit="startEdit" @delete="deleteKnowledge" @tag="applyTagFilter" @related="openRelated" />
    </div>
    <USlideover v-model:open="topicDrawerOpen" side="left" title="Knowledge topics" description="Filter notes by topic."><template #body><KnowledgeTopicPane :tree="topicTree ?? null" :loading="treeStatus === 'pending'" :error="treeError" :selected-id="topicId" :uncategorized="uncategorized" :expanded="expanded" @select="selectTopic" @toggle="toggleTopic" @retry="refreshTree" /></template></USlideover>
  </div>
</template>
