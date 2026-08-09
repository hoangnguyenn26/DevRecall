<script setup lang="ts">
import type { DsaAttempt, DsaAttemptOverview, PagedResult } from '~/types/domain'
import { dsaAttemptOutcomeLabel, formatPracticeDuration } from '~/features/practice-history/practice-history'
import { queryKeys } from '~/query/query-keys'

const props = defineProps<{ problemId: string }>(); const route = useRoute(); const api = useApi()
const selected = ref<DsaAttempt>(); const previous = ref<DsaAttempt>(); const detailError = ref<unknown>(); const detailLoading = ref(false)
const { data, status, error, refresh } = await useAsyncData(queryKeys.dsaAttempts(props.problemId), () => api.get<PagedResult<DsaAttemptOverview>>(`/dsa-problems/${props.problemId}/attempts?page=1&pageSize=10`))
const attempts = computed(() => data.value?.items ?? []); const selectedId = computed(() => typeof route.query.attempt === 'string' ? route.query.attempt : undefined)
async function loadAttempt(): Promise<void> {
  selected.value = undefined; previous.value = undefined; detailError.value = undefined
  if (!selectedId.value) return
  detailLoading.value = true
  try { selected.value = await api.get<DsaAttempt>(`/dsa-problems/${props.problemId}/attempts/${selectedId.value}`); const index = attempts.value.findIndex(item => item.id === selectedId.value); const previousId = attempts.value[index + 1]?.id; if (previousId) previous.value = await api.get<DsaAttempt>(`/dsa-problems/${props.problemId}/attempts/${previousId}`) } catch (caught) { detailError.value = caught } finally { detailLoading.value = false }
}
watch(selectedId, loadAttempt, { immediate: true })
async function copySolution(): Promise<void> { if (selected.value?.solutionCode) await navigator.clipboard.writeText(selected.value.solutionCode) }
</script>

<template>
  <section class="history" aria-labelledby="dsa-history-heading">
    <div class="heading"><div><h2 id="dsa-history-heading">Attempt history</h2><p>Review recorded solutions and reflections.</p></div><UButton :to="`/app/dsa/${problemId}/practice`" label="Practice again" icon="i-lucide-rotate-ccw" /></div>
    <CoreLoadingState v-if="status === 'pending'" label="Loading attempt history" /><CoreErrorState v-else-if="error" title="Unable to load attempt history" :error="error" @retry="refresh" />
    <CoreEmptyState v-else-if="!attempts.length" title="No attempts recorded" description="Start a focused attempt and record your solution and reflection."><UButton :to="`/app/dsa/${problemId}/practice`" label="Start practice" /></CoreEmptyState>
    <ol v-else class="rows"><li v-for="attempt in attempts" :key="attempt.id"><NuxtLink :to="{ query: { ...route.query, attempt: attempt.id } }"><time :datetime="attempt.attemptedAtUtc">{{ new Date(attempt.attemptedAtUtc).toLocaleDateString() }}</time><strong>{{ dsaAttemptOutcomeLabel[attempt.result] || attempt.result }}</strong><span>{{ attempt.durationMinutes }}m · {{ attempt.timeComplexity || '—' }}</span></NuxtLink></li></ol>
    <CoreLoadingState v-if="detailLoading" label="Loading attempt" /><div v-else-if="detailError" class="detail"><CoreErrorState title="Attempt not found" description="This attempt is unavailable. The problem remains open." :error="detailError" /><UButton :to="{ query: {} }" label="Return to problem" color="neutral" variant="outline" /></div>
    <article v-else-if="selected" class="detail">
      <div class="heading"><div><h3>Attempt #{{ selected.attemptNumber }}</h3><p>{{ dsaAttemptOutcomeLabel[selected.result] || selected.result }} · {{ selected.durationSeconds != null ? formatPracticeDuration(selected.durationSeconds) : `${selected.durationMinutes} minutes` }}</p></div><UButton :to="{ query: {} }" label="Close" color="neutral" variant="ghost" /></div>
      <p v-if="selected.problemTitleSnapshot" class="snapshot">{{ selected.problemTitleSnapshot }} · {{ selected.difficultySnapshot }}</p><div class="heading"><h4>Solution</h4><UButton label="Copy" color="neutral" variant="ghost" size="xs" :disabled="!selected.solutionCode" @click="copySolution" /></div><pre><code>{{ selected.solutionCode || '// No code recorded' }}</code></pre><h4>Complexity</h4><p>Time {{ selected.timeComplexity || '—' }} · Space {{ selected.spaceComplexity || '—' }}</p><h4>What I learned</h4><p>{{ selected.notes || 'No reflection recorded for this attempt.' }}</p>
      <section v-if="previous" class="comparison"><h4>Selected vs previous</h4><dl><dt>Outcome</dt><dd>{{ dsaAttemptOutcomeLabel[previous.result] || previous.result }} → {{ dsaAttemptOutcomeLabel[selected.result] || selected.result }}</dd><dt>Duration</dt><dd>{{ previous.durationMinutes }}m → {{ selected.durationMinutes }}m</dd><dt>Time complexity</dt><dd>{{ previous.timeComplexity || '—' }} → {{ selected.timeComplexity || '—' }}</dd><dt>Space complexity</dt><dd>{{ previous.spaceComplexity || '—' }} → {{ selected.spaceComplexity || '—' }}</dd><dt>Reflection</dt><dd>{{ previous.notes ? 'Recorded' : 'Not recorded' }} → {{ selected.notes ? 'Recorded' : 'Not recorded' }}</dd></dl><div class="solutions"><div><h5>Selected solution</h5><pre><code>{{ selected.solutionCode || '// No code recorded' }}</code></pre></div><div><h5>Previous solution</h5><pre><code>{{ previous.solutionCode || '// No code recorded' }}</code></pre></div></div></section>
    </article>
  </section>
</template>

<style scoped>
.history{margin-bottom:1rem;padding:1rem;border:1px solid var(--ui-border);border-radius:.85rem}.heading{display:flex;align-items:start;justify-content:space-between;gap:1rem}.heading h2,.heading h3{font-size:1.1rem;font-weight:700}.heading p,.snapshot{color:var(--ui-text-muted);font-size:.85rem}.rows{display:grid;gap:.4rem;margin-top:1rem}.rows a{display:grid;grid-template-columns:8rem 10rem 1fr;gap:1rem;padding:.7rem;border-radius:.55rem;color:inherit}.rows a:hover,.rows a:focus-visible{background:var(--ui-bg-elevated);outline:2px solid var(--ui-primary)}.rows span{color:var(--ui-text-muted)}.detail{margin-top:1rem;padding:1rem;border-top:1px solid var(--ui-border)}.detail h4{margin-top:1rem;font-weight:700}.detail>p{color:var(--ui-text-muted)}pre{max-width:100%;overflow:auto;margin-top:.5rem;padding:1rem;border-radius:.6rem;background:#0f172a;color:#e2e8f0;font-size:.8rem}.comparison{margin-top:1rem;padding:1rem;border-radius:.6rem;background:var(--ui-bg-muted)}dl{display:grid;grid-template-columns:9rem 1fr;gap:.4rem;margin-top:.5rem}dt{font-weight:650}.solutions{display:grid;grid-template-columns:1fr 1fr;gap:1rem}.solutions h5{margin-top:1rem;font-weight:650}@media(max-width:640px){.rows a{grid-template-columns:1fr;gap:.15rem}.solutions{grid-template-columns:1fr}dl{grid-template-columns:1fr}}
</style>
