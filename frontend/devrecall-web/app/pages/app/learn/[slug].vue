<script setup lang="ts">
import { useLearningContentApi } from '~/features/learning-content/learning-content.api'
import { learningContentKeys } from '~/features/learning-content/learning-content.query-keys'
import type { LearningContentDetail } from '~/features/learning-content/learning-content.types'
import { difficultyColor, visibleTags } from '~/features/learning-content/learning-content.meta'
import SaveLessonToKnowledge from '~/features/learning-content/components/SaveLessonToKnowledge.vue'
import AddLessonToReview from '~/features/learning-content/components/AddLessonToReview.vue'
import { markReviewCandidatesAdded } from '~/features/learning-content/lesson-review'
import { normalizeApiError, type NormalizedApiError } from '~/utils/normalize-api-error'

definePageMeta({ layout: 'app' })
const route = useRoute()
const slug = computed(() => String(route.params.slug))
const api = useLearningContentApi()
const lessonQuery = useApiQuery<LearningContentDetail>(
  learningContentKeys.detail(slug.value), () => api.detail(slug.value))
const lesson = computed(() => lessonQuery.data.value)
const returnTo = computed(() => {
  const value = typeof route.query.returnTo === 'string' ? route.query.returnTo : '/app/learn'
  return value === '/app/learn' || value.startsWith('/app/learn?') ? value : '/app/learn'
})
const technologies = computed(() => visibleTags(lesson.value?.technologies ?? []))
const topics = computed(() => visibleTags(lesson.value?.topics ?? []))
const mutationPending = ref(false)
const mutationError = ref<NormalizedApiError | null>(null)
const completedJustNow = ref(false)

async function mutate(action: 'start' | 'complete') {
  if (!lesson.value || mutationPending.value) return
  mutationPending.value = true
  mutationError.value = null
  try {
    const progress = action === 'start' ? await api.start(slug.value)
      : await api.complete(slug.value, lesson.value.progress.version)
    lesson.value.progress = progress
    lesson.value.progressStatus = progress.status
    completedJustNow.value = action === 'complete'
  } catch (error) {
    mutationError.value = normalizeApiError(error)
  } finally { mutationPending.value = false }
}
function markCandidatesInReview(keys: string[]) {
  if (!lesson.value) return
  lesson.value.reviewCandidates = markReviewCandidatesAdded(lesson.value.reviewCandidates, keys)
}
function readAgain() { window.scrollTo({ top: 0, behavior: 'smooth' }) }
useSeoMeta({ title: () => lesson.value?.title ?? 'Lesson' })
</script>

<template>
  <main class="lesson-page">
    <UButton :to="returnTo" icon="i-lucide-arrow-left" color="neutral" variant="ghost">Back to Learn</UButton>
    <CoreLoadingState v-if="lessonQuery.isPending.value" label="Loading lesson" />
    <section v-else-if="lessonQuery.error.value?.status === 404" class="lesson-state">
      <CoreEmptyState title="Lesson not found" description="It may have been removed or is no longer available." icon="i-lucide-book-x">
        <UButton :to="returnTo">Back to Learn</UButton>
      </CoreEmptyState>
    </section>
    <CoreErrorState v-else-if="lessonQuery.error.value" title="We couldn't load this lesson" description="The app remains available. Retry this lesson when you're ready." :error="lessonQuery.error.value" @retry="lessonQuery.refresh" />
    <article v-else-if="lesson" class="lesson">
      <header class="lesson-header">
        <div class="primary-meta"><UBadge :color="difficultyColor(lesson.difficulty)" variant="subtle">{{ lesson.difficulty }}</UBadge><span><UIcon name="i-lucide-clock-3" /> {{ lesson.estimatedMinutes }} min</span></div>
        <h1>{{ lesson.title }}</h1>
        <p class="summary">{{ lesson.summary }}</p>
        <div class="tags" aria-label="Lesson topics and technologies">
          <span v-for="item in technologies.visible" :key="item.value">{{ item.label }}</span>
          <span v-for="item in topics.visible" :key="item.slug">{{ item.name }}</span>
          <span v-if="technologies.hiddenCount + topics.hiddenCount">+{{ technologies.hiddenCount + topics.hiddenCount }}</span>
        </div>
        <div class="progress-action">
          <UBadge v-if="lesson.progress.status === 'InProgress'" key="in-progress" color="primary" variant="subtle">In progress</UBadge>
          <UBadge v-else-if="lesson.progress.status === 'Completed'" key="completed" color="success" variant="subtle">Completed</UBadge>
          <UButton v-if="lesson.progress.status === 'NotStarted'" :loading="mutationPending" :disabled="mutationPending" @click="mutate('start')">Start lesson</UButton>
          <span v-else-if="lesson.progress.status === 'InProgress'" class="calm-status">Continue learning at your own pace.</span>
          <span v-else class="calm-status">Read again anytime. Your completion remains recorded.</span>
        </div>
        <UAlert v-if="mutationError" color="error" variant="subtle" title="Progress wasn't updated" :description="mutationError.detail" />
        <UButton v-if="mutationError?.status === 409" color="neutral" variant="outline" @click="() => lessonQuery.refresh()">Reload latest progress</UButton>
      </header>

      <section class="objectives" aria-labelledby="lesson-objectives">
        <h2 id="lesson-objectives">What you'll learn</h2>
        <ul><li v-for="objective in lesson.objectives" :key="objective.position"><UIcon name="i-lucide-circle-check" /><span>{{ objective.text }}</span></li></ul>
      </section>

      <section v-for="section in lesson.sections" :key="section.position" class="lesson-section" :class="`section-${section.type.toLowerCase()}`">
        <p v-if="section.type === 'CodeExample'" class="section-kicker">Example</p>
        <h2 v-if="section.heading">{{ section.heading }}</h2>
        <h2 v-else-if="section.type === 'KeyTakeaway'">Key takeaway</h2>
        <LearningContentLearningMarkdown :markdown="section.bodyMarkdown" />
      </section>

      <aside v-if="lesson.source.type === 'External'" class="external-source">
        <div><strong>External resource</strong><p>{{ lesson.source.name }}</p></div>
        <UButton v-if="lesson.source.url" :to="lesson.source.url" target="_blank" rel="noopener noreferrer" trailing-icon="i-lucide-external-link">Open original resource</UButton>
      </aside>
      <section class="completion-panel" aria-labelledby="lesson-completion">
        <template v-if="lesson.progress.status !== 'Completed'">
          <div><h2 id="lesson-completion">You've reached the end of this lesson.</h2><p>Completion is explicit and records one learning event.</p></div>
          <UButton :loading="mutationPending" :disabled="mutationPending" @click="mutate('complete')">Complete lesson</UButton>
        </template>
        <template v-else>
          <div><h2 id="lesson-completion">Lesson completed</h2><p>{{ completedJustNow ? 'Your learning evidence has been recorded.' : `Completed ${new Date(lesson.progress.completedAtUtc!).toLocaleDateString()}.` }}</p></div>
          <UBadge color="success" variant="subtle">Completed</UBadge>
        </template>
      </section>
      <section v-if="lesson.progress.status === 'Completed'" class="post-actions" aria-labelledby="post-actions-title">
        <div><h2 id="post-actions-title">What would you like to do next?</h2><p>Keep useful notes, or choose concepts you want to recall without looking.</p></div>
        <div class="action-grid">
          <article><UIcon name="i-lucide-book-open" /><div><h3>Save your notes</h3><p>Keep ideas you want to revisit or expand later.</p></div><SaveLessonToKnowledge :lesson="lesson" /></article>
          <article v-if="lesson.reviewCandidates.length"><UIcon name="i-lucide-brain" /><div><h3>Remember key concepts</h3><p>Choose ideas you'd like to recall from memory later.</p></div><AddLessonToReview :slug="lesson.slug" :candidates="lesson.reviewCandidates" @added="markCandidatesInReview" /></article>
        </div>
        <div class="flex flex-wrap gap-2"><UButton :to="returnTo">Back to Learn</UButton><UButton color="neutral" variant="ghost" @click="readAgain">Read again</UButton></div>
      </section>
      <footer><UButton :to="returnTo" icon="i-lucide-arrow-left" color="neutral" variant="outline">Back to Learn</UButton></footer>
    </article>
  </main>
</template>

<style scoped>
.lesson-page{width:min(100%,54rem);margin-inline:auto}.lesson-page>:first-child{margin-bottom:1rem}.lesson{display:grid;gap:2rem}.lesson-header{display:grid;gap:1rem;border-bottom:1px solid var(--ui-border);padding:1rem 0 2rem}.primary-meta{display:flex;align-items:center;gap:.75rem;color:var(--ui-text-muted);font-size:.86rem}.primary-meta span{display:flex;align-items:center;gap:.3rem}.lesson h1{max-width:48rem;font-size:clamp(2rem,5vw,3.25rem);font-weight:800;letter-spacing:-.035em;line-height:1.08}.summary{max-width:46rem;color:var(--ui-text-muted);font-size:1.08rem;line-height:1.7}.tags{display:flex;flex-wrap:wrap;gap:.45rem}.tags span{border:1px solid var(--ui-border);border-radius:999px;padding:.3rem .65rem;color:var(--ui-text-muted);font-size:.78rem}.objectives{border:1px solid color-mix(in srgb,var(--ui-primary) 24%,var(--ui-border));border-radius:1rem;background:color-mix(in srgb,var(--ui-primary) 5%,var(--ui-bg-elevated));padding:1.3rem}.objectives h2,.lesson-section h2{font-size:1.35rem;font-weight:750;line-height:1.3}.objectives ul{display:grid;gap:.7rem;margin-top:1rem}.objectives li{display:flex;align-items:flex-start;gap:.65rem;line-height:1.55}.objectives li :deep(svg){margin-top:.2rem;color:var(--ui-primary)}.lesson-section{display:grid;gap:1rem;min-width:0}.section-codeexample{border:1px solid var(--ui-border);border-radius:1rem;background:var(--ui-bg-elevated);padding:1.2rem}.section-keytakeaway{border-left:4px solid var(--ui-primary);border-radius:.25rem 1rem 1rem .25rem;background:color-mix(in srgb,var(--ui-primary) 7%,var(--ui-bg-elevated));padding:1.25rem}.section-kicker{color:var(--ui-primary);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.external-source{display:flex;align-items:center;justify-content:space-between;gap:1rem;border-top:1px solid var(--ui-border);padding-top:1.4rem}.external-source p{color:var(--ui-text-muted)}footer{border-top:1px solid var(--ui-border);padding:1.5rem 0 3rem}.lesson-state{padding-block:3rem}@media(max-width:480px){.lesson-page{padding-inline:.15rem}.lesson h1{font-size:2rem}.summary{font-size:1rem}.objectives,.section-codeexample,.section-keytakeaway{padding:1rem}.external-source{align-items:flex-start;flex-direction:column}}
.progress-action{display:flex;align-items:center;gap:.75rem;flex-wrap:wrap}.calm-status{color:var(--ui-text-muted);font-size:.9rem}.completion-panel{display:flex;align-items:center;justify-content:space-between;gap:1rem;border:1px solid color-mix(in srgb,var(--ui-primary) 24%,var(--ui-border));border-radius:1rem;background:color-mix(in srgb,var(--ui-primary) 5%,var(--ui-bg-elevated));padding:1.25rem}.completion-panel h2{font-size:1.1rem;font-weight:750}.completion-panel p{margin-top:.3rem;color:var(--ui-text-muted);font-size:.9rem}@media(max-width:480px){.completion-panel{align-items:flex-start;flex-direction:column}}
.post-actions{display:grid;gap:1.25rem;border-radius:1.25rem;background:var(--ui-bg-elevated);padding:1.4rem}.post-actions>div:first-child p,.action-grid article p{margin-top:.3rem;color:var(--ui-text-muted);font-size:.9rem}.post-actions h2{font-size:1.2rem;font-weight:750}.action-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.action-grid article{display:grid;align-content:start;gap:1rem;border:1px solid var(--ui-border);border-radius:1rem;background:var(--ui-bg);padding:1rem}.action-grid article>:deep(svg){color:var(--ui-primary)}.action-grid h3{font-weight:700}@media(max-width:640px){.action-grid{grid-template-columns:1fr}}
</style>
