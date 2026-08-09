<script setup lang="ts">
import { ApiError } from '~/types/api'
import type { PracticeShellContext } from '~/features/practice/practice.types'
import PracticeShell from '~/features/practice/components/PracticeShell.vue'
import PracticeLoadingState from '~/features/practice/components/PracticeLoadingState.vue'
import PracticeErrorState from '~/features/practice/components/PracticeErrorState.vue'
import { studySessionItemTarget } from '../study-session.actions'
import { useStudySessionApi } from '../study-session.api'
import type { StudySessionDetail, StudySessionItem } from '../study-session.types'

const props = defineProps<{ sessionId: string }>()
const api = useStudySessionApi()
const { afterStudySessionChanged } = useLearningDataInvalidation()
const confirmDialog = useConfirmDialog()
const detail = ref<StudySessionDetail>()
const loading = ref(true)
const error = ref<unknown>()
const pending = ref(false)
const conflict = ref(false)
const reflection = ref('')
const savedReflection = ref('')
const reflectionSaved = ref(false)
const reflectionDirty = computed(() => reflection.value.trim() !== savedReflection.value)
useUnsavedChangesGuard(reflectionDirty)
const submissionIds = new Map<string, string>()
const current = computed(() =>
  detail.value?.items.find((item) => item.id === detail.value?.currentItemId),
)
const handled = computed(
  () => (detail.value?.progress.completedItems ?? 0) + (detail.value?.progress.skippedItems ?? 0),
)
const context = computed<PracticeShellContext>(() => ({
  module: 'StudySession',
  title: detail.value?.title ?? 'Study Session',
  startedAtUtc: detail.value?.startedAtUtc,
  progress: {
    current: Math.min(handled.value + 1, detail.value?.progress.totalItems ?? 0),
    total: detail.value?.progress.totalItems ?? 0,
    completed: handled.value,
    percent: detail.value?.progress.completionPercentage ?? 0,
  },
}))
function submissionId(itemId: string, action: string): string {
  const key = `${itemId}:${action}`
  if (!submissionIds.has(key)) submissionIds.set(key, crypto.randomUUID())
  return submissionIds.get(key)!
}
async function load(): Promise<void> {
  loading.value = true
  error.value = undefined
  conflict.value = false
  try {
    detail.value = await api.detail(props.sessionId)
    reflection.value = detail.value.reflection ?? ''
    savedReflection.value = reflection.value.trim()
  } catch (cause) {
    error.value = cause
  } finally {
    loading.value = false
  }
}
async function skip(item: StudySessionItem): Promise<void> {
  if (
    !detail.value ||
    pending.value ||
    !(await confirmDialog.open({
      title: 'Skip this learning item?',
      description: 'You can continue with the rest of the session.',
      confirmLabel: 'Skip item',
    }))
  )
    return
  pending.value = true
  try {
    await api.skipItem(
      detail.value.id,
      item.id,
      detail.value.version,
      submissionId(item.id, 'skip'),
    )
    await Promise.all([load(), afterStudySessionChanged()])
  } catch (cause) {
    if (cause instanceof ApiError && cause.problem.status === 409) conflict.value = true
    else error.value = cause
  } finally {
    pending.value = false
  }
}
async function completeKnowledge(item: StudySessionItem): Promise<void> {
  if (!detail.value || pending.value) return
  pending.value = true
  try {
    await api.completeItem(
      detail.value.id,
      item.id,
      detail.value.version,
      submissionId(item.id, 'complete'),
    )
    await Promise.all([load(), afterStudySessionChanged()])
  } catch (cause) {
    if (cause instanceof ApiError && cause.problem.status === 409) conflict.value = true
    else error.value = cause
  } finally {
    pending.value = false
  }
}
async function exit(): Promise<void> {
  if (
    !(await confirmDialog.open({
      title: 'Leave this study session?',
      description:
        'Your completed and skipped items are saved. You can continue this session later.',
      confirmLabel: 'Leave session',
    }))
  )
    return
  await navigateTo('/app')
}
function planDifference(): string {
  if (!detail.value || detail.value.actualDurationMinutes === undefined) return ''
  const difference = detail.value.actualDurationMinutes - detail.value.plannedDurationMinutes
  if (difference === 0) return 'Matched the planned session length'
  return `${Math.abs(difference)} minutes ${difference > 0 ? 'over plan' : 'under the planned session length'}`
}
function resourceTarget(item: StudySessionItem): string | undefined {
  if (!item.isResourceAvailable) return undefined
  if (item.resourceType === 'KnowledgeNode') return `/app/knowledge/${item.resourceId}`
  if (item.resourceType === 'InterviewQuestion') return `/app/interview/${item.resourceId}`
  if (item.resourceType === 'DsaProblem') return `/app/dsa/${item.resourceId}`
}
async function saveReflection(): Promise<void> {
  if (!detail.value || pending.value || !reflectionDirty.value) return
  pending.value = true
  reflectionSaved.value = false
  try {
    const result = await api.updateReflection(
      detail.value.id,
      reflection.value.trim() || undefined,
      detail.value.version,
    )
    detail.value.version = result.version
    detail.value.reflection = result.reflection
    reflection.value = result.reflection ?? ''
    savedReflection.value = reflection.value.trim()
    reflectionSaved.value = true
  } catch (cause) {
    if (cause instanceof ApiError && cause.problem.status === 409) conflict.value = true
    else error.value = cause
  } finally {
    pending.value = false
  }
}
onMounted(load)
</script>

<template>
  <PracticeShell :context="context" :busy="pending" wide @exit="exit">
    <PracticeLoadingState v-if="loading" label="Loading study session" />
    <PracticeErrorState
      v-else-if="error"
      :error="error"
      return-to="/app/study-sessions"
      @retry="load"
    />
    <div v-else-if="detail" class="session">
      <div v-if="conflict" class="conflict" role="alert">
        <div>
          <strong>This study session changed elsewhere.</strong>
          <p>Reload to continue from the latest pending item.</p>
        </div>
        <UButton label="Reload session" @click="load" />
      </div>
      <section
        v-if="detail.status === 'Completed'"
        class="completion summary-heading"
        role="status"
        aria-live="polite"
      >
        <UIcon name="i-lucide-circle-check-big" />
        <h1>Study session complete</h1>
        <p>{{ detail.title }}</p>
        <strong
          >{{ detail.progress.completedItems }} completed ·
          {{ detail.progress.skippedItems }} skipped</strong
        ><UButton to="/app" label="Return to Today" />
      </section>
      <div v-if="detail.status === 'Completed'" class="summary-details">
        <div
          class="metrics"
          :aria-label="`${detail.progress.completedItems} of ${detail.progress.totalItems} items completed, ${detail.progress.skippedItems} skipped`"
        >
          <span
            ><strong>{{ detail.plannedDurationMinutes }} min</strong>Planned</span
          >
          <span
            ><strong>{{ detail.actualDurationMinutes ?? '—' }} min</strong>Elapsed</span
          >
          <p>{{ planDifference() }}</p>
        </div>
        <section class="summary-items">
          <h2>Session items</h2>
          <article v-for="item in detail.items" :key="item.id">
            <UIcon
              :name="
                item.status === 'Completed' ? 'i-lucide-circle-check' : 'i-lucide-circle-minus'
              "
            />
            <div>
              <strong>{{ item.resourceTitle }}</strong>
              <p>
                {{ item.resourceType }} · {{ item.plannedDurationMinutes }} min · {{ item.status }}
              </p>
              <p v-if="item.hasEvidence && item.evidence">
                {{
                  item.evidence.kind === 'InterviewAttempt' ? 'Practice attempt' : 'DSA attempt'
                }}: {{ item.evidence.outcome }} ·
                {{ Math.ceil(item.evidence.durationSeconds / 60) }} min<span
                  v-if="item.evidence.timeComplexity"
                >
                  · {{ item.evidence.timeComplexity }}</span
                >
              </p>
              <p v-else-if="item.hasEvidence">Practice evidence unavailable</p>
              <p v-if="!item.isResourceAvailable">Resource unavailable</p>
            </div>
            <UButton
              v-if="resourceTarget(item)"
              :to="resourceTarget(item)"
              :aria-label="`Open ${item.resourceTitle}`"
              label="Open"
              color="neutral"
              variant="ghost"
            />
          </article>
        </section>
        <section class="reflection">
          <h2>Reflection</h2>
          <label for="session-reflection">What do you want to remember from this session?</label>
          <UTextarea id="session-reflection" v-model="reflection" :maxlength="4000" :rows="5" />
          <div>
            <span aria-live="polite">{{ reflectionSaved ? 'Saved' : '' }}</span
            ><UButton
              label="Save reflection"
              :loading="pending"
              :disabled="!reflectionDirty"
              @click="saveReflection"
            />
          </div>
        </section>
      </div>
      <template v-if="detail.status !== 'Completed'">
        <section v-if="current" class="current-card">
          <p>Current learning item</p>
          <h1>{{ current.resourceTitle }}</h1>
          <span>{{ current.resourceType }} · ~{{ current.plannedDurationMinutes }} min</span>
          <p v-if="!current.isResourceAvailable" class="unavailable">
            This item can no longer be opened. Skip it to continue.
          </p>
          <div>
            <UButton
              v-if="studySessionItemTarget(detail.id, current)"
              :to="studySessionItemTarget(detail.id, current)!"
              :label="
                current.resourceType === 'KnowledgeNode' ? 'Open knowledge' : 'Start practice'
              "
              icon="i-lucide-play"
            /><UButton
              v-if="current.resourceType === 'KnowledgeNode' && current.isResourceAvailable"
              label="Mark complete"
              color="success"
              variant="outline"
              @click="completeKnowledge(current)"
            /><UButton
              :label="`Skip ${current.resourceTitle}`"
              color="neutral"
              variant="outline"
              @click="skip(current)"
            />
          </div>
        </section>
        <aside>
          <header>
            <strong>{{ handled }} of {{ detail.progress.totalItems }} handled</strong
            ><span>~{{ detail.remainingPlannedMinutes }} minutes remaining</span>
          </header>
          <ol>
            <li
              v-for="item in detail.items"
              :key="item.id"
              :class="{ current: item.id === current?.id }"
              :aria-label="`${item.resourceTitle}, ${item.id === current?.id ? 'current item' : item.status.toLowerCase()}`"
            >
              <UIcon
                :name="
                  item.status === 'Completed'
                    ? 'i-lucide-circle-check'
                    : item.status === 'Skipped'
                      ? 'i-lucide-circle-minus'
                      : item.id === current?.id
                        ? 'i-lucide-arrow-right-circle'
                        : 'i-lucide-circle'
                "
              /><span
                ><strong>{{ item.resourceTitle }}</strong
                ><small>{{ item.id === current?.id ? 'Current' : item.status }}</small></span
              ><em>{{ item.plannedDurationMinutes }}m</em>
            </li>
          </ol>
        </aside>
      </template>
    </div>
  </PracticeShell>
</template>

<style scoped>
.session {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 22rem;
  gap: 1rem;
  align-items: start;
}
.conflict {
  grid-column: 1 / -1;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid var(--ui-warning);
  border-radius: 0.75rem;
}
.conflict p,
.current-card > p,
.current-card > span,
aside span,
aside small {
  color: var(--ui-text-muted);
}
.current-card,
aside {
  border: 1px solid var(--ui-border);
  border-radius: 0.85rem;
  padding: 1.25rem;
}
.current-card {
  display: grid;
  min-height: 25rem;
  place-items: center;
  align-content: center;
  gap: 0.8rem;
  text-align: center;
}
.current-card h1 {
  max-width: 26ch;
  font-size: 2rem;
  font-weight: 750;
}
.current-card > div {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.5rem;
}
.unavailable {
  color: var(--ui-error) !important;
}
aside header {
  display: grid;
  margin-bottom: 1rem;
}
aside ol {
  display: grid;
  gap: 0.35rem;
}
aside li {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.6rem;
  padding: 0.65rem;
  border-radius: 0.55rem;
}
aside li.current {
  background: var(--ui-bg-elevated);
}
aside li span {
  display: grid;
}
aside em {
  color: var(--ui-text-muted);
  font-style: normal;
}
.completion {
  grid-column: 1 / -1;
  display: grid;
  min-height: 28rem;
  place-items: center;
  align-content: center;
  gap: 0.8rem;
  text-align: center;
}
.completion > svg {
  font-size: 3rem;
  color: var(--ui-success);
}
.completion h1 {
  font-size: 2rem;
  font-weight: 750;
}
.summary-details {
  grid-column: 1 / -1;
  display: grid;
  gap: 1.25rem;
  max-width: 52rem;
  width: 100%;
  margin: 0 auto 2rem;
}
.metrics {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 1rem;
  background: var(--ui-bg-elevated);
  border-radius: 0.75rem;
}
.metrics span {
  display: grid;
  min-width: 8rem;
  color: var(--ui-text-muted);
}
.metrics strong {
  color: var(--ui-text);
  font-size: 1.15rem;
}
.metrics p {
  flex-basis: 100%;
  color: var(--ui-text-muted);
}
.summary-items,
.reflection {
  display: grid;
  gap: 0.75rem;
}
.summary-items h2,
.reflection h2 {
  font-size: 1.1rem;
  font-weight: 700;
}
.summary-items article {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 0.75rem;
  padding: 1rem 0;
  border-top: 1px solid var(--ui-border);
}
.summary-items article p,
.reflection label {
  color: var(--ui-text-muted);
}
.reflection > div {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 1rem;
}
@media (max-width: 760px) {
  .session {
    grid-template-columns: 1fr;
  }
  .session aside {
    order: -1;
  }
  .current-card {
    min-height: 20rem;
  }
}
</style>
