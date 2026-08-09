<script setup lang="ts">
import type { EChartsCoreOption } from 'echarts/core'
import LearningInsights from './LearningInsights.vue'
import { formatMinutes } from '~/utils/format'
import { comparisonText, factualTrend, parseAnalyticsRange } from '../analytics.meta'
import { useAnalyticsApi } from '../analytics.api'
import type {
  AnalyticsOverview,
  LearningInsights as LearningInsightsResult,
  LearningPerformance,
} from '../analytics.types'
const route = useRoute()
const router = useRouter()
const api = useAnalyticsApi()
const range = computed(() => parseAnalyticsRange(route.query.range))
const view = computed(() => (typeof route.query.view === 'string' ? route.query.view : 'overview'))
const overview = ref<AnalyticsOverview>()
const performance = ref<LearningPerformance>()
const insights = ref<LearningInsightsResult>()
const overviewError = ref<unknown>()
const performanceError = ref<unknown>()
const insightsError = ref<unknown>()
const loading = ref(true)
const performanceLoading = ref(true)
const insightsLoading = ref(true)
let overviewSequence = 0
let performanceSequence = 0
let insightsSequence = 0
async function loadOverview() {
  const sequence = ++overviewSequence
  loading.value = true
  overviewError.value = undefined
  try {
    const value = await api.overview(range.value)
    if (sequence === overviewSequence) overview.value = value
  } catch (error) {
    if (sequence === overviewSequence) overviewError.value = error
  } finally {
    if (sequence === overviewSequence) loading.value = false
  }
}
async function loadPerformance() {
  const sequence = ++performanceSequence
  performanceLoading.value = true
  performanceError.value = undefined
  try {
    const value = await api.performance(range.value)
    if (sequence === performanceSequence) performance.value = value
  } catch (error) {
    if (sequence === performanceSequence) performanceError.value = error
  } finally {
    if (sequence === performanceSequence) performanceLoading.value = false
  }
}
async function loadInsights() {
  const sequence = ++insightsSequence
  insightsLoading.value = true
  insightsError.value = undefined
  try {
    const value = await api.insights(range.value)
    if (sequence === insightsSequence) insights.value = value
  } catch (error) {
    if (sequence === insightsSequence) insightsError.value = error
  } finally {
    if (sequence === insightsSequence) insightsLoading.value = false
  }
}
function setQuery(key: string, value: string) {
  router.replace({ query: { ...route.query, [key]: value } })
}
watch(range, () => Promise.all([loadOverview(), loadPerformance(), loadInsights()]))
onMounted(() => {
  if (route.query.range !== range.value) setQuery('range', range.value)
  Promise.all([loadOverview(), loadPerformance(), loadInsights()])
})
const activityOption = computed<EChartsCoreOption>(() => ({
  aria: { enabled: true, description: 'Daily study minutes for the selected period.' },
  animation: false,
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: overview.value?.activity.map((x) => x.date) ?? [] },
  yAxis: { type: 'value', name: 'Minutes' },
  series: [
    {
      type: 'bar',
      data: overview.value?.activity.map((x) => x.studyMinutes) ?? [],
      color: '#4f46e5',
    },
  ],
}))
const hasActivity = computed(
  () =>
    (overview.value?.studyMinutes.current ?? 0) +
      (overview.value?.practiceActivities.current ?? 0) >
    0,
)
const modules = [
  {
    key: 'review',
    title: 'Review recall outcomes',
    labels: ['Again', 'Hard', 'Good', 'Easy'],
    to: '/app/review',
  },
  {
    key: 'interview',
    title: 'Interview self-ratings',
    labels: ['Needs work', 'Fair', 'Good', 'Strong'],
    to: '/app/interview',
  },
  {
    key: 'dsa',
    title: 'DSA attempt outcomes',
    labels: ['Solved', 'Partial', 'Could not solve', 'Skipped'],
    to: '/app/dsa',
  },
] as const
function distribution(key: string, previous = false) {
  if (!performance.value) return undefined
  const name = `${key}${previous ? 'Previous' : 'Current'}` as keyof LearningPerformance
  return performance.value[name] as LearningPerformance[keyof LearningPerformance] & {
    first: number
    second: number
    third: number
    fourth: number
    total: number
  }
}
</script>
<template>
  <div>
    <CorePageHeader
      title="Analytics"
      description="See what happened in your learning activity without turning it into a fake skill score."
      ><div class="ranges" aria-label="Analytics range">
        <UButton
          v-for="item in ['7d', '30d', '90d']"
          :key="item"
          :label="item"
          :variant="range === item ? 'solid' : 'outline'"
          @click="setQuery('range', item)"
        /></div
    ></CorePageHeader>
    <nav class="tabs" aria-label="Analytics views">
      <button
        v-for="item in ['overview', 'review', 'interview', 'dsa']"
        :key="item"
        :class="{ active: view === item }"
        @click="setQuery('view', item)"
      >
        {{ item === 'overview' ? 'Overview' : item.toUpperCase() }}
      </button>
    </nav>
    <CoreLoadingState v-if="loading" label="Loading learning activity" /><CoreErrorState
      v-else-if="overviewError"
      :error="overviewError"
      @retry="loadOverview"
    /><CoreEmptyState
      v-else-if="!hasActivity"
      title="Your learning activity will appear here"
      description="Complete reviews, practice interview questions, solve DSA problems, or finish study sessions to build your progress history."
    /><template v-else-if="overview"
      ><CoreLoadingState v-if="insightsLoading" label="Loading learning insights" />
      <CoreErrorState v-else-if="insightsError" :error="insightsError" @retry="loadInsights" />
      <LearningInsights v-else :items="insights?.items ?? []" />
      <section class="metrics">
        <article>
          <span>Study time</span><strong>{{ formatMinutes(overview.studyMinutes.current) }}</strong
          ><small>{{ comparisonText(overview.studyMinutes, 'minutes') }}</small>
        </article>
        <article>
          <span>Active days</span><strong>{{ overview.activeDays.current }}</strong
          ><small>{{ comparisonText(overview.activeDays, 'days') }}</small>
        </article>
        <article>
          <span>Sessions</span><strong>{{ overview.studySessions.current }}</strong
          ><small>{{ comparisonText(overview.studySessions, 'sessions') }}</small>
        </article>
        <article>
          <span>Practice activities</span><strong>{{ overview.practiceActivities.current }}</strong
          ><small>{{ comparisonText(overview.practiceActivities, 'activities') }}</small>
        </article>
      </section>
      <template v-if="view === 'overview'"
        ><AnalyticsChartContainer
          title="Study activity"
          :summary="`${overview.activeDays.current} active days and ${formatMinutes(overview.studyMinutes.current)} studied.`"
          :metric="formatMinutes(overview.studyMinutes.current)"
          :option="activityOption"
          ><table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Study minutes</th>
                <th>Practice activities</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="point in overview.activity" :key="point.date">
                <td>{{ point.date }}</td>
                <td>{{ point.studyMinutes }}</td>
                <td>{{ point.practiceCount }}</td>
              </tr>
            </tbody>
          </table></AnalyticsChartContainer
        >
        <section class="breakdown">
          <h2>Practice activity</h2>
          <p>
            Review <strong>{{ overview.reviewCount }}</strong>
          </p>
          <p>
            Interview <strong>{{ overview.interviewAttemptCount }}</strong>
          </p>
          <p>
            DSA <strong>{{ overview.dsaAttemptCount }}</strong>
          </p>
        </section></template
      ><template v-else
        ><CoreLoadingState
          v-if="performanceLoading"
          label="Loading performance trends" /><CoreErrorState
          v-else-if="performanceError"
          :error="performanceError"
          @retry="loadPerformance" />
        <section v-else-if="performance" class="performance">
          <article v-for="module in modules.filter((x) => x.key === view)" :key="module.key">
            <h2>{{ module.title }}</h2>
            <template v-if="distribution(module.key)?.total"
              ><p>{{ distribution(module.key)?.total }} activities this period</p>
              <div v-for="(label, index) in module.labels" :key="label" class="outcome">
                <span>{{ label }}</span
                ><strong>{{
                  distribution(module.key)?.[
                    ['first', 'second', 'third', 'fourth'][index] as 'first'
                  ]
                }}</strong>
              </div>
              <p class="trend">
                {{
                  factualTrend(distribution(module.key)!, distribution(module.key, true)!, [
                    'third',
                    'fourth',
                  ])
                }}
              </p></template
            ><CoreEmptyState
              v-else
              :title="`No ${module.key} practice in this period`"
              description="Other analytics sections remain available."
            /><UButton :to="module.to" label="Open history" variant="outline" />
          </article></section></template
    ></template>
  </div>
</template>
<style scoped>
.ranges,
.tabs {
  display: flex;
  gap: 0.4rem;
}
.tabs {
  margin-bottom: 1rem;
  border-bottom: 1px solid var(--ui-border);
}
.tabs button {
  padding: 0.7rem;
  text-transform: capitalize;
}
.tabs .active {
  color: var(--ui-primary);
  border-bottom: 2px solid var(--ui-primary);
}
.metrics {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.75rem;
  margin-bottom: 1rem;
}
.metrics article,
.breakdown,
.performance article {
  display: grid;
  gap: 0.4rem;
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.75rem;
}
.metrics span,
.metrics small,
.trend {
  color: var(--ui-text-muted);
}
.metrics strong {
  font-size: 1.6rem;
}
.breakdown {
  margin-top: 1rem;
}
.breakdown p,
.outcome {
  display: flex;
  justify-content: space-between;
  padding: 0.5rem 0;
  border-bottom: 1px solid var(--ui-border);
}
table {
  width: 100%;
  border-collapse: collapse;
}
th,
td {
  padding: 0.4rem;
  text-align: left;
  border-bottom: 1px solid var(--ui-border);
}
@media (max-width: 700px) {
  .metrics {
    grid-template-columns: repeat(2, 1fr);
  }
  .ranges {
    width: 100%;
    margin-top: 0.5rem;
  }
  .ranges > * {
    flex: 1;
  }
}
@media (max-width: 380px) {
  .metrics {
    grid-template-columns: 1fr;
  }
}
</style>
