<script setup lang="ts">
import type { KnowledgeTagOption } from '../knowledge.types'
const props = defineProps<{ modelValue: string[]; selected: KnowledgeTagOption[] }>()
const emit = defineEmits<{ 'update:modelValue': [value: string[]]; added: [tag: KnowledgeTagOption] }>()
const api = useApi(); const query = ref(''); const options = ref<KnowledgeTagOption[]>([]); const loading = ref(false); const error = ref(false); const creating = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined
async function search() { loading.value = true; error.value = false; try { options.value = await api.get('/knowledge/tags', { query: query.value, take: 20 }) } catch { error.value = true } finally { loading.value = false } }
watch(query, () => { clearTimeout(timer); timer = setTimeout(search, 275) }); onMounted(search)
function add(tag: KnowledgeTagOption) { if (props.modelValue.length >= 10 || props.modelValue.includes(tag.id)) return; emit('update:modelValue', [...props.modelValue, tag.id]); emit('added', tag) }
function remove(id: string) { emit('update:modelValue', props.modelValue.filter(value => value !== id)) }
const exactMatch = computed(() => options.value.some(tag => tag.name.localeCompare(query.value.trim(), undefined, { sensitivity: 'accent' }) === 0))
async function create() { if (!query.value.trim() || creating.value) return; creating.value = true; try { const tag = await api.post<KnowledgeTagOption>('/knowledge/tags', { name: query.value }); add(tag); query.value = ''; await search() } finally { creating.value = false } }
</script>

<template>
  <div class="space-y-2"><div class="flex flex-wrap gap-1.5" aria-live="polite"><span v-for="tag in selected" :key="tag.id" class="inline-flex max-w-full items-center gap-1 rounded-full bg-primary/10 px-2 py-1 text-xs text-primary"><span class="truncate">{{ tag.name }}</span><button type="button" :aria-label="`Remove ${tag.name}`" @click="remove(tag.id)"><UIcon name="i-lucide-x" /></button></span></div><UInput v-model="query" icon="i-lucide-search" placeholder="Find or create tags…" aria-label="Search tags" :disabled="modelValue.length >= 10" /><p v-if="modelValue.length >= 10" class="text-xs text-warning">Maximum 10 tags selected.</p><p v-if="error" class="text-xs text-error">Tags could not be loaded. <button type="button" class="underline" @click="search">Retry</button></p><div v-else class="max-h-40 overflow-y-auto rounded-lg border border-default"><button v-for="tag in options.filter(item => !modelValue.includes(item.id))" :key="tag.id" type="button" class="flex w-full justify-between px-3 py-2 text-left text-sm hover:bg-elevated" @click="add(tag)"><span>{{ tag.name }}</span><span class="text-xs text-muted">{{ tag.knowledgeCount }}</span></button><button v-if="query.trim() && !exactMatch" type="button" class="w-full px-3 py-2 text-left text-sm text-primary hover:bg-elevated" :disabled="creating" @click="create">Create “{{ query.trim() }}”</button><p v-if="loading" class="p-3 text-xs text-muted">Searching tags…</p></div></div>
</template>
