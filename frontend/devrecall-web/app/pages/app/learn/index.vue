<script setup lang="ts">
import { useLearningContentApi } from '~/features/learning-content/learning-content.api'
import type { LearningContentPage } from '~/features/learning-content/learning-content.types'
import { filtersFromQuery, learningContentDifficulties, learningContentTechnologies,
  queryFromFilters, withFilter } from '~/features/learning-content/learning-content.filters'
import { normalizeApiError, type NormalizedApiError } from '~/utils/normalize-api-error'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Learn' })
const route = useRoute()
const router = useRouter()
const api = useLearningContentApi()
const page = ref<LearningContentPage | null>(null)
const loading = ref(true)
const error = ref<NormalizedApiError | null>(null)
let requestController: AbortController | undefined
const filters = computed(() => filtersFromQuery(route.query))
const returnTo = computed(() => route.fullPath)

const technology = computed({
  get: () => filters.value.technology ?? '',
  set: value => updateFilters(withFilter(filters.value, 'technology', value)),
})
const difficulty = computed({
  get: () => filters.value.difficulty ?? '',
  set: value => updateFilters(withFilter(filters.value, 'difficulty', value)),
})

function updateFilters(value: typeof filters.value) {
  return router.push({ path: '/app/learn', query: queryFromFilters(value) })
}

async function load() {
  requestController?.abort()
  const controller = new AbortController()
  requestController = controller
  loading.value = true
  error.value = null
  try {
    page.value = await api.list(filters.value, controller.signal)
  } catch (caught) {
    if (controller.signal.aborted) return
    error.value = normalizeApiError(caught)
    page.value = null
  } finally {
    if (requestController === controller) loading.value = false
  }
}

watch(() => route.query, async (query) => {
  const normalized = queryFromFilters(filtersFromQuery(query))
  const current = new URLSearchParams(Object.entries(query).flatMap(([key, value]) =>
    typeof value === 'string' ? [[key, value]] : [])).toString()
  const canonical = new URLSearchParams(Object.entries(normalized).map(([key, value]) => [key, String(value)])).toString()
  if (current !== canonical) {
    await router.replace({ path: '/app/learn', query: normalized })
    return
  }
  await load()
}, { immediate: true, deep: true })
onBeforeUnmount(() => requestController?.abort())
</script>

<template>
  <div class="learn-page">
    <CorePageHeader title="Learn" description="Build stronger engineering fundamentals with focused, practical lessons." />
    <section class="filter-bar" aria-label="Filter lessons">
      <label>Technology<select v-model="technology"><option value="">All technologies</option><option v-for="item in learningContentTechnologies" :key="item[0]" :value="item[0]">{{ item[1] }}</option></select></label>
      <label>Level<select v-model="difficulty"><option value="">All levels</option><option v-for="item in learningContentDifficulties" :key="item" :value="item">{{ item }}</option></select></label>
    </section>

    <div v-if="loading" class="catalog-grid" aria-label="Loading lessons" aria-busy="true">
      <UCard v-for="index in 3" :key="index"><div class="skeleton-card"><USkeleton class="h-5 w-24" /><USkeleton class="h-6 w-3/4" /><USkeleton class="h-16 w-full" /><USkeleton class="h-9 w-28" /></div></UCard>
    </div>
    <CoreErrorState v-else-if="error" title="We couldn't load learning content" description="Your filters are preserved. Try the request again." :error="error" @retry="load" />
    <CoreEmptyState v-else-if="!page?.items.length" title="No lessons match these filters" description="Try another technology or level.">
      <UButton to="/app/learn" variant="soft">Clear filters</UButton>
    </CoreEmptyState>
    <div v-else class="catalog-grid">
      <LearningContentLearningContentCard v-for="item in page.items" :key="item.slug" :item="item" :return-to="returnTo" />
    </div>

    <nav v-if="page && page.totalPages > 1" class="pagination" aria-label="Learning content pages">
      <UButton color="neutral" variant="outline" :disabled="page.page <= 1" @click="updateFilters({ ...filters, page: page.page - 1 })">Previous</UButton>
      <span>Page {{ page.page }} of {{ page.totalPages }}</span>
      <UButton color="neutral" variant="outline" :disabled="page.page >= page.totalPages" @click="updateFilters({ ...filters, page: page.page + 1 })">Next</UButton>
    </nav>
  </div>
</template>

<style scoped>
.learn-page{display:grid;gap:1.25rem}.filter-bar{display:flex;flex-wrap:wrap;gap:.75rem;border:1px solid var(--ui-border);border-radius:.85rem;background:var(--ui-bg-elevated);padding:.85rem}.filter-bar label{display:grid;min-width:13rem;gap:.35rem;color:var(--ui-text-muted);font-size:.78rem;font-weight:650}.filter-bar select{border:1px solid var(--ui-border);border-radius:.55rem;background:var(--ui-bg);padding:.62rem .75rem;color:var(--ui-text);font-size:.9rem}.catalog-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,18rem),1fr));gap:1rem}.skeleton-card{display:grid;gap:1rem}.pagination{display:flex;align-items:center;justify-content:center;gap:1rem;color:var(--ui-text-muted);font-size:.875rem}@media(max-width:480px){.filter-bar{display:grid}.filter-bar label{min-width:0}.pagination{justify-content:space-between}.pagination span{font-size:.78rem}}
</style>
