<script setup lang="ts">
import { formatDateTime } from '~/utils/format'
import { weakTopicAction, weakTopicLevels, weakTopicReason } from '../weak-topic.meta'
import { useWeakTopicApi } from '../weak-topic.api'
import type { WeakTopicDetail, WeakTopicSummary } from '../weak-topic.types'
const props = defineProps<{ selectedId?: string }>()
const api = useWeakTopicApi()
const { refreshTodayAndNavigation } = useLearningDataInvalidation()
const items = ref<WeakTopicSummary[]>([])
const detail = ref<WeakTopicDetail>()
const level = ref('')
const listLoading = ref(true)
const detailLoading = ref(false)
const error = ref<unknown>()
const recalculating = ref(false)
async function loadList() {
  listLoading.value = true
  error.value = undefined
  try {
    items.value = (await api.list(level.value)).items
  } catch (cause) {
    error.value = cause
  } finally {
    listLoading.value = false
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
async function recalculate() {
  recalculating.value = true
  try {
    await api.recalculateAll()
    await Promise.all([loadList(), loadDetail(), refreshTodayAndNavigation()])
  } finally {
    recalculating.value = false
  }
}
watch(level, loadList)
watch(() => props.selectedId, loadDetail)
onMounted(() => Promise.all([loadList(), loadDetail()]))
</script>
<template>
  <div>
    <CorePageHeader title="Weak Topics" description="Areas that currently need more attention."
      ><UButton
        label="Recalculate"
        icon="i-lucide-refresh-cw"
        :loading="recalculating"
        @click="recalculate"
    /></CorePageHeader>
    <div class="workspace">
      <aside :class="{ hiddenMobile: selectedId }">
        <label for="weak-level">Attention level</label
        ><select id="weak-level" v-model="level">
          <option value="">All levels</option>
          <option v-for="(_, key) in weakTopicLevels" :key="key">{{ key }}</option></select
        ><CoreLoadingState v-if="listLoading" label="Loading weak topics" /><CoreErrorState
          v-else-if="error && !selectedId"
          :error="error"
          @retry="loadList"
        /><CoreEmptyState
          v-else-if="!items.length"
          title="No major weak areas detected"
          description="Keep practicing and reviewing. DevRecall will surface recurring weak signals here."
        />
        <nav v-else aria-label="Weak topics">
          <NuxtLink
            v-for="item in items"
            :key="item.profileId"
            :to="`/app/weak-topics/${item.profileId}`"
            :class="{ selected: item.profileId === selectedId }"
            ><CoreStatusBadge :value="item.level" /><strong>{{ item.resourceTitle }}</strong
            ><span
              >{{ weakTopicLevels[item.level as keyof typeof weakTopicLevels]?.description }} ·
              {{ item.signalCount }} signals</span
            ></NuxtLink
          >
        </nav>
      </aside>
      <main :class="{ hiddenMobile: !selectedId }">
        <NuxtLink class="back" to="/app/weak-topics">← Weak Topics</NuxtLink
        ><CoreLoadingState v-if="detailLoading" label="Loading weakness evidence" /><CoreErrorState
          v-else-if="error"
          :error="error"
          @retry="loadDetail"
        />
        <article v-else-if="detail">
          <header>
            <CoreStatusBadge :value="detail.level" />
            <h1>{{ detail.resourceTitle }}</h1>
            <p>{{ weakTopicLevels[detail.level as keyof typeof weakTopicLevels]?.description }}</p>
            <small>Calculated {{ formatDateTime(detail.calculatedAtUtc) }}</small>
          </header>
          <section>
            <h2>Why this needs attention</h2>
            <ul>
              <li v-for="reason in detail.reasons" :key="reason.type">
                {{ weakTopicReason(reason.type, reason.count) }}
              </li>
            </ul>
          </section>
          <section>
            <h2>Recent signals</h2>
            <ol>
              <li
                v-for="signal in detail.contributions"
                :key="`${signal.signalType}-${signal.occurredAtUtc}`"
              >
                <span>{{ weakTopicReason(signal.signalType, 1) }}</span
                ><time :datetime="signal.occurredAtUtc">{{
                  formatDateTime(signal.occurredAtUtc)
                }}</time>
              </li>
            </ol>
          </section>
          <footer>
            <UButton
              v-if="detail.isResourceAvailable"
              v-bind="weakTopicAction(detail.resourceType, detail.resourceId)"
            /><span v-else>Related resource is no longer available.</span
            ><UButton
              to="/app/recommendations"
              label="View recommendations"
              color="neutral"
              variant="outline"
            />
          </footer>
        </article>
        <CoreEmptyState
          v-else
          title="Choose a weak topic"
          description="Select an area to understand the evidence behind it."
        />
      </main>
    </div>
  </div>
</template>
<style scoped>
.workspace {
  display: grid;
  grid-template-columns: 20rem minmax(0, 1fr);
  gap: 1rem;
}
.workspace > aside,
.workspace > main {
  border: 1px solid var(--ui-border);
  border-radius: 0.8rem;
  padding: 1rem;
}
.workspace aside {
  display: grid;
  align-content: start;
  gap: 0.6rem;
}
.workspace label {
  font-size: 0.8rem;
  color: var(--ui-text-muted);
}
select {
  padding: 0.55rem;
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
nav a.selected,
nav a:hover {
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
article header h1 {
  margin: 0.5rem 0;
  font-size: 1.7rem;
  font-weight: 750;
}
article h2 {
  margin-bottom: 0.65rem;
  font-weight: 700;
}
article ul {
  list-style: disc;
  padding-left: 1.3rem;
}
article ol {
  display: grid;
}
article ol li {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.65rem 0;
  border-bottom: 1px solid var(--ui-border);
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
  article footer > * {
    width: 100%;
  }
}
</style>
