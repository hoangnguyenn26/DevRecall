<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import type { DueReviewItem, ProgressOverview, Recommendation, StudyPlanListItem, StudySessionListItem, WeakTopic } from '~/types/domain'
import { formatMinutes, resourceRoute } from '~/utils/format'
import { resolveNextAction } from '~/utils/nextAction'

definePageMeta({ layout: 'app' }); useSeoMeta({ title: 'Today' })
const api = useApi(); const loading = ref(true); const error = ref<unknown>()
const sessions = ref<StudySessionListItem[]>([]); const plans = ref<StudyPlanListItem[]>([]); const reviews = ref<DueReviewItem[]>([]); const recommendations = ref<Recommendation[]>([]); const weakTopics = ref<WeakTopic[]>([]); const overview = ref<ProgressOverview>()
const now = new Date(); const from = new Date(now); from.setUTCDate(now.getUTCDate() - 6); from.setUTCHours(0, 0, 0, 0)

async function load() {
  loading.value = true; error.value = undefined
  try {
    const [sessionPage, planPage, reviewPage, recommendationPage, weakPage, progress] = await Promise.all([
      api.get<PagedResponse<StudySessionListItem>>('/study-sessions', { page: 1, pageSize: 10 }),
      api.get<PagedResponse<StudyPlanListItem>>('/study-plans', { page: 1, pageSize: 10 }),
      api.get<PagedResponse<DueReviewItem>>('/review-items/due', { page: 1, pageSize: 5 }),
      api.get<PagedResponse<Recommendation>>('/recommendations', { status: 'Active', page: 1, pageSize: 5 }),
      api.get<PagedResponse<WeakTopic>>('/weak-topics', { page: 1, pageSize: 5 }),
      api.get<ProgressOverview>('/analytics/progress-overview', { fromUtc: from.toISOString(), toUtc: now.toISOString() }),
    ])
    sessions.value = sessionPage.items; plans.value = planPage.items; reviews.value = reviewPage.items; recommendations.value = recommendationPage.items; weakTopics.value = weakPage.items; overview.value = progress
  } catch (caught) { error.value = caught } finally { loading.value = false }
}

const nextAction = computed(() => resolveNextAction(
  sessions.value, plans.value, reviews.value, recommendations.value))
onMounted(load)
</script>

<template>
  <div>
    <CoreLoadingState v-if="loading" label="Preparing today" />
    <CoreErrorState v-else-if="error" :error="error" @retry="load" />
    <template v-else>
      <section class="hero-card"><div><p class="eyebrow">Your next best action</p><h1>{{ nextAction.label }}</h1><p>{{ nextAction.detail }}</p><UButton :to="nextAction.to" size="lg" :icon="nextAction.icon" label="Continue" /></div><div class="week-mark"><strong>{{ overview?.activeStudyDays ?? 0 }}/7</strong><span>active days</span></div></section>
      <section class="metrics"><article><span>Reviews due</span><strong>{{ reviews.length }}</strong></article><article><span>Study time</span><strong>{{ formatMinutes(overview?.studyMinutes ?? 0) }}</strong></article><article><span>Sessions</span><strong>{{ overview?.completedSessions ?? 0 }}</strong></article><article><span>Weekly progress</span><strong>{{ overview?.studyItemsCompleted ?? 0 }} items</strong></article></section>
      <section class="dashboard-grid">
        <UCard><template #header><div class="card-head"><h2>Current study plan</h2><UButton to="/app/study-plans" label="View all" variant="ghost" size="sm" /></div></template><article v-if="plans[0]" class="list-item"><div><strong>{{ plans[0].title }}</strong><p>{{ plans[0].itemCount }} items · {{ formatMinutes(plans[0].totalPlannedDurationMinutes) }}</p></div><CoreStatusBadge :value="plans[0].status" /></article><CoreEmptyState v-else title="No study plan yet" description="Generate a plan from your strongest recommendations." /></UCard>
        <UCard><template #header><div class="card-head"><h2>Recommendations</h2><UButton to="/app/recommendations" label="View all" variant="ghost" size="sm" /></div></template><div v-if="recommendations.length" class="list"><NuxtLink v-for="item in recommendations.slice(0, 3)" :key="item.recommendationId" :to="resourceRoute(item.resourceType, item.resourceId)" class="list-item"><div><strong>{{ item.resourceTitle }}</strong><p>{{ item.signalCount }} learning signals</p></div><CoreStatusBadge :value="item.priority" /></NuxtLink></div><CoreEmptyState v-else title="No active recommendations" description="Keep practicing and synchronize recommendations later." /></UCard>
        <UCard class="wide"><template #header><div class="card-head"><h2>Weak topics</h2><UButton to="/app/weak-topics" label="Explore" variant="ghost" size="sm" /></div></template><div v-if="weakTopics.length" class="topic-row"><article v-for="topic in weakTopics.slice(0, 4)" :key="topic.profileId"><div><strong>{{ topic.resourceTitle }}</strong><p>{{ topic.signalCount }} signals explain this score.</p></div><CoreStatusBadge :value="topic.level" /></article></div><CoreEmptyState v-else title="No weak topics detected" description="Weakness profiles appear after review and practice activity." /></UCard>
      </section>
    </template>
  </div>
</template>

<style scoped>
.hero-card { display: flex; justify-content: space-between; gap: 2rem; padding: clamp(1.5rem, 4vw, 3rem); border: 1px solid color-mix(in srgb, var(--ui-primary) 35%, var(--ui-border)); border-radius: 1rem; background: linear-gradient(135deg, color-mix(in srgb, var(--ui-primary) 10%, var(--ui-bg)), var(--ui-bg)); }.eyebrow { color: var(--ui-primary); font-weight: 700; }.hero-card h1 { margin: .35rem 0; font-size: clamp(1.8rem, 4vw, 3rem); font-weight: 750; letter-spacing: -.04em; }.hero-card h1 + p { margin-bottom: 1.5rem; color: var(--ui-text-muted); }.week-mark { align-self: center; display: grid; width: 8rem; aspect-ratio: 1; place-content: center; border: .55rem solid color-mix(in srgb, var(--ui-primary) 35%, var(--ui-border)); border-radius: 50%; text-align: center; }.week-mark strong { font-size: 1.7rem; }.week-mark span { color: var(--ui-text-muted); font-size: .75rem; }.metrics { display: grid; grid-template-columns: repeat(4, 1fr); gap: .75rem; margin: 1rem 0; }.metrics article { display: grid; gap: .3rem; padding: 1rem; border: 1px solid var(--ui-border); border-radius: .8rem; }.metrics span, .list-item p, .topic-row p { color: var(--ui-text-muted); font-size: .82rem; }.metrics strong { font-size: 1.35rem; }.dashboard-grid { display: grid; grid-template-columns: 1.4fr 1fr; gap: 1rem; }.wide { grid-column: 1 / -1; }.card-head, .list-item, .topic-row article { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }.card-head h2 { font-weight: 700; }.list { display: grid; gap: .65rem; }.list-item { padding: .6rem; border-radius: .6rem; }.list-item:hover { background: var(--ui-bg-elevated); }.topic-row { display: grid; grid-template-columns: repeat(2, 1fr); gap: .5rem 1rem; }.topic-row article { padding: .65rem; }
@media (max-width: 760px) { .week-mark { display: none; }.metrics { grid-template-columns: repeat(2, 1fr); }.dashboard-grid { grid-template-columns: 1fr; }.wide { grid-column: auto; }.topic-row { grid-template-columns: 1fr; } }
</style>
