<script setup lang="ts">
import type { StudyPlanEditItem, StudyPlanResourceType } from '../study-plan.types'
import { useGlobalSearch } from '~/features/global-search/useGlobalSearch'

const emit = defineEmits<{ select: [item: StudyPlanEditItem]; close: [] }>()
const props = defineProps<{ selected: Array<{ resourceType: string; resourceId: string }> }>()
const query = ref('')
const search = useGlobalSearch()
watch(query, search.search)
function resourceType(type: string): StudyPlanResourceType | undefined {
  if (type === 'Knowledge') return 'KnowledgeNode'
  if (type === 'InterviewQuestion' || type === 'DsaProblem') return type
}
type SearchResource = {
  resourceId: string
  resourceType: string
  title: string
  summary?: string | null
}
function add(result: SearchResource): void {
  const type = resourceType(result.resourceType)
  if (!type) return
  emit('select', {
    itemId: `new:${crypto.randomUUID()}`,
    sourceType: 'Manual',
    resourceType: type,
    resourceId: result.resourceId,
    resourceTitle: result.title,
    resourcePreview: result.summary ?? undefined,
    isResourceAvailable: true,
    plannedDurationMinutes: 15,
    position: 0,
  })
}
function isSelected(result: SearchResource): boolean {
  const type = resourceType(result.resourceType)
  return (
    !!type &&
    props.selected.some(
      (item) => item.resourceType === type && item.resourceId === result.resourceId,
    )
  )
}
</script>

<template>
  <UModal
    :open="true"
    title="Add learning item"
    description="Search Knowledge, Interview questions, and DSA problems."
    @update:open="(value) => !value && emit('close')"
  >
    <template #body>
      <UInput
        v-model="query"
        autofocus
        icon="i-lucide-search"
        placeholder="Search learning resources"
        class="w-full"
      />
      <CoreLoadingState v-if="search.pending.value" label="Searching resources" class="mt-4" />
      <CoreErrorState v-else-if="search.error.value" :error="search.error.value" class="mt-4" />
      <CoreEmptyState
        v-else-if="query.trim().length >= 2 && !search.response.value?.results.length"
        title="No resources found"
        description="Try a different title or topic."
        class="mt-4"
      />
      <ul v-else class="mt-4 grid gap-2">
        <li
          v-for="result in search.response.value?.results"
          :key="`${result.resourceType}:${result.resourceId}`"
        >
          <button class="resource-result" :disabled="isSelected(result)" @click="add(result)">
            <span
              ><strong>{{ result.title }}</strong
              ><small>{{ result.resourceType }}</small></span
            >
            <span>{{ isSelected(result) ? 'Already added' : 'Add' }}</span>
          </button>
        </li>
      </ul>
    </template>
  </UModal>
</template>

<style scoped>
.resource-result {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.8rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.65rem;
  text-align: left;
}
.resource-result:not(:disabled):hover {
  border-color: var(--ui-primary);
}
.resource-result:disabled {
  opacity: 0.55;
}
.resource-result span:first-child {
  display: grid;
}
.resource-result small {
  color: var(--ui-text-muted);
}
</style>
