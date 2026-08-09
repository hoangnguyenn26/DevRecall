<script setup lang="ts">
import { formatDateTime } from '~/utils/format'
import {
  recommendationAction,
  recommendationPriorityMeta,
  recommendationReason,
} from '../recommendation.meta'
import { useRecommendationApi } from '../recommendation.api'
import type { RecommendationDetail, RecommendationSummary } from '../recommendation.types'
const props = defineProps<{ selectedId?: string }>()
const api = useRecommendationApi()
const { afterRecommendationDismissed, afterRecommendationsChanged } = useLearningDataInvalidation()
const items = ref<RecommendationSummary[]>([])
const detail = ref<RecommendationDetail>()
const status = ref('Active')
const priority = ref('')
const loading = ref(true)
const detailLoading = ref(false)
const error = ref<unknown>()
const generating = ref(false)
const mutating = ref(false)
async function loadList() {
  loading.value = true
  error.value = undefined
  try {
    items.value = (await api.list(status.value, priority.value)).items
  } catch (cause) {
    error.value = cause
  } finally {
    loading.value = false
  }
}
async function loadDetail() {
  if (!props.selectedId) {
    detail.value = undefined
    return
  }
  detailLoading.value = true
  try {
    detail.value = await api.detail(props.selectedId)
  } catch (cause) {
    error.value = cause
  } finally {
    detailLoading.value = false
  }
}
async function generate() {
  generating.value = true
  try {
    await api.generate()
    await Promise.all([loadList(), afterRecommendationsChanged()])
  } finally {
    generating.value = false
  }
}
async function dismiss(item: RecommendationSummary) {
  mutating.value = true
  try {
    await api.dismiss(item.recommendationId, item.version)
    await Promise.all([loadList(), afterRecommendationDismissed()])
    if (props.selectedId) await navigateTo('/app/recommendations')
  } finally {
    mutating.value = false
  }
}
watch([status, priority], loadList)
watch(() => props.selectedId, loadDetail)
onMounted(() => Promise.all([loadList(), loadDetail()]))
</script>
<template>
  <div>
    <CorePageHeader
      title="Recommendations"
      description="Actions suggested by your recent learning evidence."
      ><UButton
        to="/app/study-plans?new=1"
        label="Create Study Plan"
        color="neutral"
        variant="outline" /><UButton
        label="Refresh recommendations"
        icon="i-lucide-refresh-cw"
        :loading="generating"
        @click="generate"
    /></CorePageHeader>
    <div class="workspace">
      <aside :class="{ hiddenMobile: selectedId }">
        <div class="filters">
          <label
            >Status<select v-model="status">
              <option>Active</option>
              <option>Dismissed</option>
              <option>Completed</option>
              <option>Expired</option>
            </select></label
          ><label
            >Priority<select v-model="priority">
              <option value="">All priorities</option>
              <option>Critical</option>
              <option>High</option>
              <option>Medium</option>
              <option>Low</option>
            </select></label
          >
        </div>
        <CoreLoadingState v-if="loading" label="Loading recommendations" /><CoreErrorState
          v-else-if="error && !selectedId"
          :error="error"
          @retry="loadList"
        /><CoreEmptyState
          v-else-if="!items.length"
          title="No recommendations right now"
          description="Keep practicing and reviewing. New recommendations will appear as DevRecall gathers useful learning signals."
        />
        <nav v-else aria-label="Recommendations">
          <NuxtLink
            v-for="item in items"
            :key="item.recommendationId"
            :to="`/app/recommendations/${item.recommendationId}`"
            :class="{ selected: item.recommendationId === selectedId }"
            :aria-label="`${item.resourceTitle}. ${item.priority} priority. ${item.type}.`"
            ><CoreStatusBadge :value="item.priority" /><strong>{{ item.resourceTitle }}</strong
            ><span>{{ item.type }} · {{ item.signalCount }} signals</span></NuxtLink
          >
        </nav>
      </aside>
      <main :class="{ hiddenMobile: !selectedId }">
        <NuxtLink class="back" to="/app/recommendations">← Recommendations</NuxtLink
        ><CoreLoadingState v-if="detailLoading" label="Loading recommendation" /><CoreErrorState
          v-else-if="error"
          :error="error"
          @retry="loadDetail"
        />
        <article v-else-if="detail">
          <header>
            <CoreStatusBadge :value="detail.priority" />
            <h1>{{ detail.resourceTitle }}</h1>
            <p>
              {{
                recommendationPriorityMeta[
                  detail.priority as keyof typeof recommendationPriorityMeta
                ]
              }}
            </p>
            <small>Recommended {{ formatDateTime(detail.generatedAtUtc) }}</small>
          </header>
          <section>
            <h2>Why this is recommended</h2>
            <ul>
              <li v-for="reason in detail.reasons" :key="reason.type">
                {{ recommendationReason(reason) }}
              </li>
            </ul>
          </section>
          <section>
            <h2>Suggested activity</h2>
            <p>{{ detail.type.replace(/([A-Z])/g, ' $1').trim() }}</p>
          </section>
          <footer>
            <UButton
              v-if="detail.isResourceAvailable"
              v-bind="recommendationAction(detail.type, detail.resourceType, detail.resourceId)"
            /><span v-else>Recommended resource is no longer available.</span
            ><UButton
              v-if="detail.status === 'Active'"
              :aria-label="`Dismiss recommendation: ${detail.resourceTitle}`"
              label="Dismiss"
              color="neutral"
              variant="ghost"
              :loading="mutating"
              @click="dismiss(detail)"
            />
          </footer>
        </article>
        <CoreEmptyState
          v-else
          title="Choose a recommendation"
          description="Select an action to see why DevRecall suggested it."
        />
      </main>
    </div>
  </div>
</template>
<style scoped>
.workspace {
  display: grid;
  grid-template-columns: 21rem minmax(0, 1fr);
  gap: 1rem;
}
.workspace > aside,
.workspace > main {
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.8rem;
}
.filters {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.filters label {
  display: grid;
  gap: 0.3rem;
  color: var(--ui-text-muted);
  font-size: 0.75rem;
}
.filters select {
  padding: 0.5rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.5rem;
}
nav {
  display: grid;
  gap: 0.35rem;
}
nav a {
  display: grid;
  gap: 0.3rem;
  padding: 0.8rem;
  border-radius: 0.6rem;
}
nav a:hover,
nav a.selected {
  background: var(--ui-bg-elevated);
}
nav span,
article p,
article small {
  color: var(--ui-text-muted);
}
article {
  display: grid;
  gap: 1.4rem;
}
article h1 {
  margin: 0.5rem 0;
  font-size: 1.7rem;
  font-weight: 750;
}
article h2 {
  margin-bottom: 0.6rem;
  font-weight: 700;
}
article ul {
  list-style: disc;
  padding-left: 1.3rem;
}
article footer {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.back {
  display: none;
  margin-bottom: 1rem;
}
@media (max-width: 760px) {
  .workspace {
    grid-template-columns: 1fr;
  }
  .hiddenMobile {
    display: none !important;
  }
  .back {
    display: inline-flex;
  }
  .filters {
    grid-template-columns: 1fr;
  }
  article footer > * {
    width: 100%;
  }
}
</style>
