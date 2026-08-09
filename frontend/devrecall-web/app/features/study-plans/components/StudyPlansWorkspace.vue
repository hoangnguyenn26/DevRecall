<script setup lang="ts">
import type { StudyPlanDetail, StudyPlanListItem } from '../study-plan.types'
import { useStudyPlanApi } from '../study-plan.api'
import StudyPlanList from './StudyPlanList.vue'
import StudyPlanBuilder from './StudyPlanBuilder.vue'
import StudyPlanDetailView from './StudyPlanDetail.vue'

const props = defineProps<{ selectedId?: string }>()
const api = useStudyPlanApi()
const { refreshToday } = useLearningDataInvalidation()
const plans = ref<StudyPlanListItem[]>([])
const detail = ref<StudyPlanDetail>()
const listLoading = ref(true)
const detailLoading = ref(false)
const listError = ref<unknown>()
const detailError = ref<unknown>()
const editing = ref(false)

async function loadList(): Promise<void> {
  listLoading.value = true
  listError.value = undefined
  try {
    plans.value = (await api.list()).items
  } catch (error) {
    listError.value = error
  } finally {
    listLoading.value = false
  }
}
async function loadDetail(): Promise<void> {
  if (!props.selectedId) {
    detail.value = undefined
    return
  }
  detailLoading.value = true
  detailError.value = undefined
  editing.value = false
  try {
    detail.value = await api.detail(props.selectedId)
  } catch (error) {
    detailError.value = error
  } finally {
    detailLoading.value = false
  }
}
async function accept(detailValue: StudyPlanDetail): Promise<void> {
  detail.value = detailValue
  await Promise.all([loadList(), refreshToday()])
}
watch(() => props.selectedId, loadDetail)
onMounted(() => Promise.all([loadList(), loadDetail()]))
</script>

<template>
  <div class="workspace">
    <aside :class="{ hiddenMobile: selectedId }">
      <CoreLoadingState v-if="listLoading" label="Loading study plans" />
      <CoreErrorState v-else-if="listError" :error="listError" @retry="loadList" />
      <template v-else-if="plans.length"
        ><StudyPlanList :plans="plans" :selected-id="selectedId"
      /></template>
      <CoreEmptyState
        v-else
        title="Build your first study plan"
        description="Turn recommendations into a focused set of learning activities."
        ><template #actions
          ><UButton to="/app/recommendations" label="View recommendations" /></template
      ></CoreEmptyState>
    </aside>
    <main :class="{ hiddenMobile: !selectedId }">
      <NuxtLink v-if="selectedId" to="/app/study-plans" class="mobile-back"
        ><UIcon name="i-lucide-arrow-left" /> Study Plans</NuxtLink
      >
      <CoreLoadingState v-if="detailLoading" label="Loading study plan" />
      <CoreErrorState v-else-if="detailError" :error="detailError" @retry="loadDetail" />
      <StudyPlanBuilder
        v-else-if="detail && editing"
        :detail="detail"
        @cancel="editing = false"
        @saved="accept"
        @ready="
          (value) => {
            editing = false
            accept(value)
          }
        "
      />
      <StudyPlanDetailView v-else-if="detail" :detail="detail" @edit="editing = true" />
      <CoreEmptyState
        v-else
        title="Select a study plan"
        description="Choose a Ready, Draft, or recent plan to inspect its learning items."
      />
    </main>
  </div>
</template>

<style scoped>
.workspace {
  display: grid;
  grid-template-columns: minmax(15rem, 20rem) minmax(0, 1fr);
  min-height: 32rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.85rem;
  overflow: hidden;
}
.workspace > aside {
  padding: 1rem;
  border-right: 1px solid var(--ui-border);
  background: color-mix(in srgb, var(--ui-bg-elevated) 45%, transparent);
}
.workspace > main {
  min-width: 0;
  padding: 1.25rem;
}
.mobile-back {
  display: none;
  align-items: center;
  gap: 0.35rem;
  margin-bottom: 1rem;
  color: var(--ui-text-muted);
}
@media (max-width: 760px) {
  .workspace {
    display: block;
    border: 0;
  }
  .workspace > aside,
  .workspace > main {
    padding: 0;
    border: 0;
  }
  .hiddenMobile {
    display: none;
  }
  .mobile-back {
    display: flex;
  }
}
</style>
