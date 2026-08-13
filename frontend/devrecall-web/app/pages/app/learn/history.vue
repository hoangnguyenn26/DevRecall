<script setup lang="ts">
import { useLearningContentApi } from '~/features/learning-content/learning-content.api'
import type { LearningContentHistoryPage } from '~/features/learning-content/learning-content.types'
import { normalizeApiError, type NormalizedApiError } from '~/utils/normalize-api-error'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Learning history' })
const route = useRoute()
const router = useRouter()
const api = useLearningContentApi()
const page = computed(() => {
  const value = Number(route.query.page ?? 1)
  return Number.isInteger(value) && value > 0 ? value : 1
})
const history = ref<LearningContentHistoryPage>()
const loading = ref(true)
const error = ref<NormalizedApiError | null>(null)
let controller: AbortController | undefined
async function load() {
  controller?.abort()
  const requestController = new AbortController()
  controller = requestController
  loading.value = true
  error.value = null
  try { history.value = await api.history(page.value, requestController.signal) }
  catch (caught) {
    if (!requestController.signal.aborted) error.value = normalizeApiError(caught)
  } finally { if (controller === requestController) loading.value = false }
}
watch(page, load, { immediate: true })
onBeforeUnmount(() => controller?.abort())
function goToPage(value: number) {
  return router.push({ path: '/app/learn/history', query: value === 1 ? {} : { page: value } })
}
</script>

<template>
  <div class="history-page">
    <CorePageHeader title="Learning history" description="Lessons you explicitly completed, in chronological order.">
      <UButton to="/app/learn" label="Browse lessons" icon="i-lucide-arrow-left" color="neutral" variant="outline" />
    </CorePageHeader>
    <CoreLoadingState v-if="loading" label="Loading learning history" />
    <CoreErrorState v-else-if="error" title="We couldn't load your learning history" :error="error" @retry="load" />
    <CoreEmptyState v-else-if="!history?.items.length" title="No completed lessons yet" description="Lessons you complete will appear here." icon="i-lucide-history">
      <UButton to="/app/learn" label="Browse lessons" />
    </CoreEmptyState>
    <ol v-else class="history-list" aria-label="Completed lessons">
      <li v-for="item in history!.items" :key="item.evidenceId">
        <div><h2>{{ item.title }}</h2><time :datetime="item.completedAtUtc">Completed {{ new Date(item.completedAtUtc).toLocaleString() }}</time></div>
        <UButton v-if="item.isSourceAvailable && item.sourceSlug" :to="`/app/learn/${item.sourceSlug}`" label="Read again" color="neutral" variant="outline" />
        <UBadge v-else color="neutral" variant="subtle">Unavailable</UBadge>
      </li>
    </ol>
    <nav v-if="history && history.totalPages > 1" class="pagination" aria-label="Learning history pages">
      <UButton label="Previous" color="neutral" variant="outline" :disabled="page <= 1" @click="goToPage(page - 1)" />
      <span>Page {{ page }} of {{ history.totalPages }}</span>
      <UButton label="Next" color="neutral" variant="outline" :disabled="page >= history.totalPages" @click="goToPage(page + 1)" />
    </nav>
  </div>
</template>

<style scoped>
.history-page{display:grid;gap:1.25rem}.history-list{display:grid;gap:.75rem}.history-list li{display:flex;align-items:center;justify-content:space-between;gap:1rem;border:1px solid var(--ui-border);border-radius:.85rem;background:var(--ui-bg-elevated);padding:1rem}.history-list h2{font-weight:750}.history-list time{display:block;margin-top:.3rem;color:var(--ui-text-muted);font-size:.85rem}.pagination{display:flex;align-items:center;justify-content:center;gap:1rem;color:var(--ui-text-muted);font-size:.875rem}@media(max-width:520px){.history-list li{align-items:flex-start;flex-direction:column}.pagination{justify-content:space-between}.pagination span{font-size:.78rem}}
</style>
