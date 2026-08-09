<script setup lang="ts">
import type { DsaAttempt, DsaProblemDetail } from '~/types/domain'
import DsaAttemptHistory from '~/features/dsa/history/DsaAttemptHistory.vue'
import { formatDateTime } from '~/utils/format'
definePageMeta({ layout: 'app' })
const route = useRoute()
const api = useApi()
const detail = ref<DsaProblemDetail>()
const selected = ref<DsaAttempt>()
const loading = ref(true)
const recording = ref(false)
const form = reactive({
  result: 'Solved',
  language: 'C#',
  solutionCode: '',
  approach: '',
  timeComplexity: '',
  spaceComplexity: '',
  durationMinutes: 30,
  notes: '',
})
const id = computed(() => String(route.params.id))
useSeoMeta({ title: () => detail.value?.title ?? 'DSA practice' })
async function load() {
  loading.value = true
  try {
    detail.value = await api.get<DsaProblemDetail>(`/dsa-problems/${id.value}`)
    if (detail.value.latestAttempt) await selectAttempt(detail.value.latestAttempt.id)
  } finally {
    loading.value = false
  }
}
async function selectAttempt(attemptId: string) {
  selected.value = await api.get<DsaAttempt>(`/dsa-problems/${id.value}/attempts/${attemptId}`)
}
async function recordAttempt() {
  await api.post(`/dsa-problems/${id.value}/attempts`, {
    ...form,
    language: form.language || null,
    solutionCode: form.solutionCode || null,
    approach: form.approach || null,
    timeComplexity: form.timeComplexity || null,
    spaceComplexity: form.spaceComplexity || null,
    notes: form.notes || null,
    attemptedAtUtc: new Date().toISOString(),
  })
  recording.value = false
  await load()
}
onMounted(load)
</script>
<template>
  <div>
    <CoreLoadingState v-if="loading" label="Loading coding workspace" /><template v-else-if="detail"
      ><CorePageHeader
        :title="detail.title"
        :description="`${detail.topics.join(' · ') || 'DSA problem'} · ${detail.attemptSummary.totalAttempts} attempts`"
        ><UButton
          :to="`/app/dsa/${id}/practice`"
          label="Start practice"
          icon="i-lucide-play" /><UButton
          label="Record attempt"
          color="neutral"
          variant="outline"
          icon="i-lucide-plus"
          @click="recording = true"
      /></CorePageHeader>
      <DsaAttemptHistory :problem-id="id" />
      <section class="workspace">
        <article class="problem">
          <CoreStatusBadge :value="detail.difficulty" />
          <h2>Problem</h2>
          <p>{{ detail.description }}</p>
          <a v-if="detail.externalUrl" :href="detail.externalUrl" target="_blank" rel="noreferrer"
            >Open source problem <UIcon name="i-lucide-external-link"
          /></a>
        </article>
        <article class="solution">
          <template v-if="selected"
            ><div class="section-head">
              <div>
                <h2>Attempt {{ selected.attemptNumber }}</h2>
                <p>{{ formatDateTime(selected.attemptedAtUtc) }}</p>
              </div>
              <CoreStatusBadge :value="selected.result" />
            </div>
            <h3>Approach</h3>
            <p>{{ selected.approach || 'No approach recorded.' }}</p>
            <h3>Solution</h3>
            <pre>{{ selected.solutionCode || '// No code recorded' }}</pre>
            <div class="complexity">
              <span>Time: {{ selected.timeComplexity || '—' }}</span
              ><span>Space: {{ selected.spaceComplexity || '—' }}</span
              ><span>{{ selected.durationMinutes }} minutes</span>
            </div></template
          ><CoreEmptyState
            v-else
            title="No attempt selected"
            description="Record an attempt to begin comparing your work."
          />
        </article>
        <aside class="attempts">
          <h2>Attempts</h2>
          <button
            v-for="attempt in detail.recentAttempts"
            :key="attempt.id"
            :class="{ active: selected?.id === attempt.id }"
            @click="selectAttempt(attempt.id)"
          >
            <span>#{{ attempt.attemptNumber }} · {{ attempt.result }}</span
            ><small>{{ attempt.durationMinutes }}m</small>
          </button>
        </aside>
      </section>
      <div v-if="recording" class="backdrop" @click.self="recording = false">
        <UCard class="attempt-modal"
          ><template #header><h2>Record completed attempt</h2></template>
          <form @submit.prevent="recordAttempt">
            <div class="three">
              <UFormField label="Result"
                ><select v-model="form.result">
                  <option>Solved</option>
                  <option>PartiallySolved</option>
                  <option>Failed</option>
                  <option>Skipped</option>
                </select></UFormField
              ><UFormField label="Language"><UInput v-model="form.language" /></UFormField
              ><UFormField label="Duration"
                ><UInput v-model.number="form.durationMinutes" type="number"
              /></UFormField>
            </div>
            <UFormField label="Approach"
              ><UTextarea v-model="form.approach" :rows="3" class="w-full" /></UFormField
            ><UFormField label="Solution code"
              ><UTextarea v-model="form.solutionCode" :rows="10" data-code class="w-full"
            /></UFormField>
            <div class="two">
              <UFormField label="Time complexity"
                ><UInput v-model="form.timeComplexity" /></UFormField
              ><UFormField label="Space complexity"
                ><UInput v-model="form.spaceComplexity"
              /></UFormField>
            </div>
            <UFormField label="Notes"><UTextarea v-model="form.notes" class="w-full" /></UFormField>
            <div class="actions">
              <UButton
                label="Cancel"
                color="neutral"
                variant="ghost"
                @click="recording = false"
              /><UButton type="submit" label="Save attempt" />
            </div></form
        ></UCard></div
    ></template>
  </div>
</template>
<style scoped>
.workspace {
  display: grid;
  grid-template-columns: minmax(17rem, 0.8fr) minmax(24rem, 1.2fr) minmax(11rem, 0.45fr);
  min-height: 34rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.85rem;
  overflow: hidden;
}
.workspace > * {
  min-width: 0;
  padding: 1.25rem;
}
.problem,
.solution {
  border-right: 1px solid var(--ui-border);
}
.workspace h2 {
  margin: 0.7rem 0;
  font-weight: 700;
}
.problem p {
  white-space: pre-wrap;
  line-height: 1.65;
  color: var(--ui-text-muted);
}
.problem a {
  display: inline-flex;
  margin-top: 1rem;
  color: var(--ui-primary);
}
.section-head {
  display: flex;
  justify-content: space-between;
}
.section-head p {
  color: var(--ui-text-muted);
  font-size: 0.8rem;
}
.solution h3 {
  margin-top: 1.2rem;
  font-weight: 650;
}
.solution > p {
  color: var(--ui-text-muted);
}
.solution pre {
  max-height: 18rem;
  margin-top: 0.5rem;
  padding: 1rem;
  overflow: auto;
  border-radius: 0.6rem;
  background: #0f172a;
  color: #e2e8f0;
  font-size: 0.8rem;
}
.complexity {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 1rem;
}
.complexity span {
  padding: 0.3rem 0.5rem;
  border-radius: 0.4rem;
  background: var(--ui-bg-muted);
  font-size: 0.75rem;
}
.attempts button {
  display: flex;
  width: 100%;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.55rem;
  border-radius: 0.45rem;
  text-align: left;
  font-size: 0.8rem;
}
.attempts button:hover,
.attempts button.active {
  background: var(--ui-bg-elevated);
  color: var(--ui-primary);
}
.backdrop {
  position: fixed;
  z-index: 100;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(2 6 23 / 0.55);
}
.attempt-modal {
  width: min(100%, 50rem);
  max-height: 92vh;
  overflow: auto;
}
.attempt-modal h2 {
  font-weight: 700;
}
.attempt-modal form {
  display: grid;
  gap: 0.75rem;
}
.attempt-modal select {
  padding: 0.5rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.5rem;
}
.three {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.5rem;
}
.two {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.5rem;
}
.actions {
  display: flex;
  justify-content: end;
  gap: 0.5rem;
}
@media (max-width: 950px) {
  .workspace {
    grid-template-columns: 1fr;
  }
  .problem,
  .solution {
    border-right: 0;
    border-bottom: 1px solid var(--ui-border);
  }
  .three {
    grid-template-columns: 1fr;
  }
}
</style>
