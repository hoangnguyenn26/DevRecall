<script setup lang="ts">
import { useLearningContentApi } from '~/features/learning-content/learning-content.api'
import { learningContentKeys } from '~/features/learning-content/learning-content.query-keys'
import type { LearningContentDetail } from '~/features/learning-content/learning-content.types'
import { difficultyColor, visibleTags, resourceKindLabel, safeResourceUrl } from '~/features/learning-content/learning-content.meta'
import SaveLessonToKnowledge from '~/features/learning-content/components/SaveLessonToKnowledge.vue'
import AddLessonToReview from '~/features/learning-content/components/AddLessonToReview.vue'
import AddLessonToStudyPlan from '~/features/learning-content/components/AddLessonToStudyPlan.vue'
import { useStudySessionApi } from '~/features/study-sessions/study-session.api'
import { markReviewCandidatesAdded } from '~/features/learning-content/lesson-review'
import { lessonReturnTo, lessonReturnLabel } from '~/features/learning-content/lesson-return'
import { normalizeApiError, type NormalizedApiError } from '~/utils/normalize-api-error'

definePageMeta({ layout: 'app' })
const route = useRoute()
const slug = computed(() => String(route.params.slug))
const api = useLearningContentApi()
const lessonQuery = useApiQuery<LearningContentDetail>(
  learningContentKeys.detail(slug.value), () => api.detail(slug.value))
const lesson = computed(() => lessonQuery.data.value)
const resourceUrl = computed(() => safeResourceUrl(lesson.value?.source.url ?? null))
const returnTo = computed(() => lessonReturnTo(route.query.returnTo))
const returnLabel = computed(() => lessonReturnLabel(returnTo.value))
const technologies = computed(() => visibleTags(lesson.value?.technologies ?? []))
const topics = computed(() => visibleTags(lesson.value?.topics ?? []))
const mutationPending = ref(false)
const mutationError = ref<NormalizedApiError | null>(null)
const sessionApi = useStudySessionApi()
const progressSync = useLearningContentProgressSync()
const sessionId = computed(() => typeof route.query.studySession === 'string' ? route.query.studySession : '')
const sessionItemId = computed(() => typeof route.query.studyItem === 'string' ? route.query.studyItem : '')
const sessionAttachError = ref('')
const pendingSessionEvidenceId = ref('')
const sessionCompletionSubmissionId = ref(crypto.randomUUID())

async function attachToStudySession(evidenceId: string) {
  if (!sessionId.value || !sessionItemId.value) return
  const session = await sessionApi.detail(sessionId.value)
  const item = session.items.find(candidate => candidate.id === sessionItemId.value
    && candidate.resourceType === 'LearningContent' && candidate.resourceId === lesson.value?.id)
  if (!item) throw new Error('This lesson does not match the Study Session item.')
  await sessionApi.completeItem(session.id, item.id, session.version,
    sessionCompletionSubmissionId.value, evidenceId)
  pendingSessionEvidenceId.value = ''
  sessionAttachError.value = ''
}
async function retrySessionAttachment() {
  if (!pendingSessionEvidenceId.value || mutationPending.value) return
  mutationPending.value = true
  try { await attachToStudySession(pendingSessionEvidenceId.value) }
  catch (error) { sessionAttachError.value = normalizeApiError(error).detail ?? 'Return to the session and retry this item.' }
  finally { mutationPending.value = false }
}

async function mutate(action: 'start' | 'complete') {
  if (!lesson.value?.progress || lesson.value.contentType !== 'Lesson' || mutationPending.value) return
  mutationPending.value = true
  mutationError.value = null
  try {
    const progress = action === 'start' ? await api.start(slug.value)
      : await api.complete(slug.value, lesson.value.progress.version)
    lesson.value.progress = progress
    lesson.value.progressStatus = progress.status
    progressSync.afterProgressChanged(action)
    if (action === 'complete' && progress.completionEvidenceId && sessionId.value && sessionItemId.value) {
      pendingSessionEvidenceId.value = progress.completionEvidenceId
      try {
        await attachToStudySession(progress.completionEvidenceId)
      } catch (error) {
        sessionAttachError.value = normalizeApiError(error).detail ?? 'Return to the session and retry this item.'
      }
    }
  } catch (error) {
    mutationError.value = normalizeApiError(error)
  } finally { mutationPending.value = false }
}
function markCandidatesInReview(keys: string[]) {
  if (!lesson.value) return
  lesson.value.reviewCandidates = markReviewCandidatesAdded(lesson.value.reviewCandidates, keys)
}
useSeoMeta({ title: () => lesson.value?.title ?? 'Lesson' })
</script>

<template>
  <main class="lesson-page">
    <UButton :to="sessionId ? `/app/study-sessions/${sessionId}` : returnTo" icon="i-lucide-arrow-left" color="neutral" variant="ghost">{{ sessionId ? 'Back to Study Session' : returnLabel }}</UButton>
    <CoreLoadingState v-if="lessonQuery.isPending.value" label="Loading learning content" />
    <section v-else-if="lessonQuery.error.value?.status === 404" class="lesson-state">
      <CoreEmptyState title="Learning content not found" description="It may have been removed or is no longer available." icon="i-lucide-book-x">
        <UButton :to="returnTo">{{ returnLabel }}</UButton>
      </CoreEmptyState>
    </section>
    <CoreErrorState v-else-if="lessonQuery.error.value" title="We couldn't load this learning content" description="The app remains available. Retry when you're ready." :error="lessonQuery.error.value" @retry="lessonQuery.refresh" />
    <article v-else-if="lesson?.contentType === 'ExternalResource'" class="lesson">
      <header class="lesson-header">
        <p class="section-kicker">{{ resourceKindLabel(lesson.resourceKind) }} · {{ lesson.source.name }}</p>
        <h1>{{ lesson.title }}</h1>
        <div class="primary-meta"><UBadge :color="difficultyColor(lesson.difficulty)" variant="subtle">{{ lesson.difficulty }}</UBadge><span>~{{ lesson.estimatedMinutes }} min reading estimate</span></div>
        <div class="tags" aria-label="Resource topics and technologies"><span v-for="item in technologies.visible" :key="item.value">{{ item.label }}</span><span v-for="item in topics.visible" :key="item.slug">{{ item.name }}</span></div>
      </header>
      <section class="lesson-section">
        <h2>Why this resource</h2>
        <p class="summary">{{ lesson.summary }}</p>
        <p>This is a curated reference hosted by {{ lesson.source.name }}, not a DevRecall lesson. The reading estimate is for planning only.</p>
        <UButton v-if="resourceUrl" :to="resourceUrl" target="_blank" rel="noopener noreferrer" trailing-icon="i-lucide-external-link">Read on {{ lesson.source.name }}</UButton>
        <p v-else>This source link is unavailable.</p>
        <AddLessonToStudyPlan :slug="lesson.slug" :completed="false" content-type="ExternalResource" />
        <p class="muted">Plan a study task for later. Opening this source does not complete the task or record lesson progress.</p>
        <p class="calm-status">You'll leave DevRecall in a new tab. Opening this resource does not record progress, completion or study time.</p>
      </section>
    </article>
    <article v-else-if="lesson?.contentType === 'Lesson' && lesson.progress" class="lesson">
      <header class="lesson-header">
        <UAlert v-if="sessionId" color="primary" variant="subtle" title="Studying in a Study Session" description="Complete the lesson to attach fresh learning evidence to the current session item." />
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
          <AddLessonToStudyPlan v-if="lesson.progress.status !== 'Completed'" :slug="lesson.slug" :completed="false" />
        </div>
        <UAlert v-if="mutationError" color="error" variant="subtle" title="Progress wasn't updated" :description="mutationError.detail" />
        <UButton v-if="mutationError?.status === 409" color="neutral" variant="outline" @click="() => lessonQuery.refresh()">Reload latest progress</UButton>
        <div v-if="sessionAttachError" class="session-attach-error"><UAlert color="warning" variant="subtle" title="Lesson completed, but the Study Session wasn't updated" :description="sessionAttachError" /><UButton color="neutral" variant="outline" :loading="mutationPending" @click="retrySessionAttachment">Retry session update</UButton></div>
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
      <section class="completion-panel" aria-labelledby="lesson-completion" aria-live="polite">
        <template v-if="lesson.progress.status !== 'Completed'">
          <div><h2 id="lesson-completion">You've reached the end of this lesson.</h2><p>Completion is explicit and records one learning event.</p></div>
          <UButton :loading="mutationPending" :disabled="mutationPending" @click="mutate('complete')">{{ mutationPending ? 'Completing...' : 'Complete lesson' }}</UButton>
        </template>
        <template v-else>
          <div class="completion-success"><UIcon name="i-lucide-circle-check" /><div><h2 id="lesson-completion">Lesson completed</h2><p>Your completion is recorded. Choose what is worth keeping, or simply move on.</p></div></div>
        </template>
      </section>
      <section v-if="lesson.progress.status === 'Completed'" class="post-actions" aria-labelledby="post-actions-title">
        <div><h2 id="post-actions-title">Keep what matters</h2><p>Turn the important parts of this lesson into something you can revisit or recall later.</p></div>
        <div class="action-grid">
          <article><UIcon name="i-lucide-notebook-pen" /><div><h3>Save your notes</h3><p>Keep ideas you want to revisit, connect, or expand later.</p></div><SaveLessonToKnowledge :lesson="lesson" /></article>
          <article v-if="lesson.reviewCandidates.length"><UIcon name="i-lucide-refresh-cw" /><div><h3>Remember key concepts</h3><p>Choose the ideas you'd like DevRecall to help you recall later.</p></div><AddLessonToReview :slug="lesson.slug" :candidates="lesson.reviewCandidates" @added="markCandidatesInReview" /></article>
        </div>
        <UButton class="post-action-back" :to="sessionId ? `/app/study-sessions/${sessionId}` : returnTo" color="neutral" variant="outline" icon="i-lucide-arrow-left">{{ sessionId ? 'Back to Study Session' : returnLabel }}</UButton>
      </section>
      <footer v-if="lesson.progress.status !== 'Completed'"><UButton :to="sessionId ? `/app/study-sessions/${sessionId}` : returnTo" icon="i-lucide-arrow-left" color="neutral" variant="outline">{{ sessionId ? 'Back to Study Session' : returnLabel }}</UButton></footer>
    </article>
  </main>
</template>

<style scoped>
.lesson-page{width:min(100%,54rem);margin-inline:auto}.lesson-page>:first-child{margin-bottom:1rem}.lesson{display:grid;gap:2rem}.lesson-header{display:grid;gap:1rem;border-bottom:1px solid var(--ui-border);padding:1rem 0 2rem}.primary-meta{display:flex;align-items:center;gap:.75rem;color:var(--ui-text-muted);font-size:.86rem}.primary-meta span{display:flex;align-items:center;gap:.3rem}.lesson h1{max-width:48rem;font-size:clamp(2rem,5vw,3.25rem);font-weight:800;letter-spacing:-.035em;line-height:1.08}.summary{max-width:46rem;color:var(--ui-text-muted);font-size:1.08rem;line-height:1.7}.tags{display:flex;flex-wrap:wrap;gap:.45rem}.tags span{border:1px solid var(--ui-border);border-radius:999px;padding:.3rem .65rem;color:var(--ui-text-muted);font-size:.78rem}.objectives{border:1px solid color-mix(in srgb,var(--ui-primary) 24%,var(--ui-border));border-radius:1rem;background:color-mix(in srgb,var(--ui-primary) 5%,var(--ui-bg-elevated));padding:1.3rem}.objectives h2,.lesson-section h2{font-size:1.35rem;font-weight:750;line-height:1.3}.objectives ul{display:grid;gap:.7rem;margin-top:1rem}.objectives li{display:flex;align-items:flex-start;gap:.65rem;line-height:1.55}.objectives li :deep(svg){margin-top:.2rem;color:var(--ui-primary)}.lesson-section{display:grid;gap:1rem;min-width:0}.section-codeexample{border:1px solid var(--ui-border);border-radius:1rem;background:var(--ui-bg-elevated);padding:1.2rem}.section-keytakeaway{border-left:4px solid var(--ui-primary);border-radius:.25rem 1rem 1rem .25rem;background:color-mix(in srgb,var(--ui-primary) 7%,var(--ui-bg-elevated));padding:1.25rem}.section-kicker{color:var(--ui-primary);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.external-source{display:flex;align-items:center;justify-content:space-between;gap:1rem;border-top:1px solid var(--ui-border);padding-top:1.4rem}.external-source p{color:var(--ui-text-muted)}footer{border-top:1px solid var(--ui-border);padding:1.5rem 0 3rem}.lesson-state{padding-block:3rem}@media(max-width:480px){.lesson-page{padding-inline:.15rem}.lesson h1{font-size:2rem}.summary{font-size:1rem}.objectives,.section-codeexample,.section-keytakeaway{padding:1rem}.external-source{align-items:flex-start;flex-direction:column}}
.progress-action{display:flex;align-items:center;gap:.75rem;flex-wrap:wrap}.calm-status{color:var(--ui-text-muted);font-size:.9rem}.completion-panel{display:flex;align-items:center;justify-content:space-between;gap:1rem;border:1px solid color-mix(in srgb,var(--ui-primary) 24%,var(--ui-border));border-radius:1rem;background:color-mix(in srgb,var(--ui-primary) 5%,var(--ui-bg-elevated));padding:1.25rem}.completion-panel h2{font-size:1.1rem;font-weight:750}.completion-panel p{margin-top:.3rem;color:var(--ui-text-muted);font-size:.9rem}@media(max-width:480px){.completion-panel{align-items:flex-start;flex-direction:column}}
.completion-success{display:flex;align-items:flex-start;gap:.75rem}.completion-success>:deep(svg){margin-top:.1rem;color:var(--ui-success);font-size:1.35rem}
.post-actions{display:grid;gap:1.25rem;border-radius:1.25rem;background:var(--ui-bg-elevated);padding:1.4rem}.post-actions>div:first-child p,.action-grid article p{margin-top:.3rem;color:var(--ui-text-muted);font-size:.9rem}.post-actions h2{font-size:1.2rem;font-weight:750}.action-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.action-grid article{display:grid;align-content:start;gap:1rem;border:1px solid var(--ui-border);border-radius:1rem;background:var(--ui-bg);padding:1rem}.action-grid article>:deep(svg){color:var(--ui-primary)}.action-grid h3{font-weight:700}@media(max-width:640px){.action-grid{grid-template-columns:1fr}}
.post-action-back{justify-self:start}
</style>
