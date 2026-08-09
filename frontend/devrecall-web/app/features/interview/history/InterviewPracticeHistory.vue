<script setup lang="ts">
import type { InterviewPracticeAttemptDetail, InterviewPracticeAttemptSummary, PagedResult } from '~/types/domain'
import { formatPracticeDuration, interviewComparison, interviewSelfRatingLabel } from '~/features/practice-history/practice-history'
import { queryKeys } from '~/query/query-keys'

const props = defineProps<{ questionId: string }>()
const route = useRoute(); const api = useApi(); const selected = ref<InterviewPracticeAttemptDetail>(); const previous = ref<InterviewPracticeAttemptDetail>(); const detailError = ref<unknown>(); const detailLoading = ref(false)
const { data, status, error, refresh } = await useAsyncData(queryKeys.interviewAttempts(props.questionId), () => api.get<PagedResult<InterviewPracticeAttemptSummary>>(`/interview-questions/${props.questionId}/attempts?page=1&pageSize=10`))
const attempts = computed(() => data.value?.items ?? [])
const selectedId = computed(() => typeof route.query.attempt === 'string' ? route.query.attempt : undefined)
const comparison = computed(() => selected.value && previous.value ? interviewComparison(selected.value, previous.value) : [])

async function loadAttempt(): Promise<void> {
  selected.value = undefined; previous.value = undefined; detailError.value = undefined
  if (!selectedId.value) return
  detailLoading.value = true
  try {
    selected.value = await api.get<InterviewPracticeAttemptDetail>(`/interview-questions/${props.questionId}/attempts/${selectedId.value}`)
    const index = attempts.value.findIndex(item => item.attemptId === selectedId.value); const previousId = attempts.value[index + 1]?.attemptId
    if (previousId) previous.value = await api.get<InterviewPracticeAttemptDetail>(`/interview-questions/${props.questionId}/attempts/${previousId}`)
  } catch (caught) { detailError.value = caught } finally { detailLoading.value = false }
}
watch(selectedId, loadAttempt, { immediate: true })
</script>

<template>
  <section class="history" aria-labelledby="interview-history-heading">
    <div class="heading"><div><h2 id="interview-history-heading">Practice history</h2><p>Immutable records of how your answers evolve.</p></div><UButton :to="`/app/interview/${questionId}/practice`" label="Practice again" icon="i-lucide-rotate-ccw" /></div>
    <CoreLoadingState v-if="status === 'pending'" label="Loading practice history" />
    <CoreErrorState v-else-if="error" title="Unable to load practice history" :error="error" @retry="refresh" />
    <CoreEmptyState v-else-if="!attempts.length" title="No practice attempts yet" description="Practice this question to record how your answer evolves over time."><UButton :to="`/app/interview/${questionId}/practice`" label="Practice question" /></CoreEmptyState>
    <ol v-else class="rows"><li v-for="attempt in attempts" :key="attempt.attemptId"><NuxtLink :to="{ query: { ...route.query, attempt: attempt.attemptId } }"><time :datetime="attempt.completedAtUtc">{{ new Date(attempt.completedAtUtc).toLocaleDateString() }}</time><strong>{{ interviewSelfRatingLabel[attempt.selfRating] || attempt.selfRating }}</strong><span>{{ formatPracticeDuration(attempt.durationSeconds) }} · {{ attempt.followUpsAnswered }} follow-ups</span></NuxtLink></li></ol>
    <CoreLoadingState v-if="detailLoading" label="Loading attempt" />
    <div v-else-if="detailError" class="detail-error"><CoreErrorState title="Attempt not found" description="This attempt is unavailable. The question remains open." :error="detailError" /><UButton :to="{ query: {} }" label="Return to question" color="neutral" variant="outline" /></div>
    <article v-else-if="selected" class="detail" aria-labelledby="interview-attempt-heading">
      <div class="heading"><div><h3 id="interview-attempt-heading">Practice attempt</h3><p>{{ new Date(selected.completedAtUtc).toLocaleString() }} · {{ formatPracticeDuration(selected.durationSeconds) }}</p></div><UButton :to="{ query: {} }" label="Close" color="neutral" variant="ghost" /></div>
      <h4>Question</h4><p>{{ selected.questionSnapshot }}</p><h4>Your answer</h4><p class="content">{{ selected.answerSnapshot }}</p><h4>Reference answer</h4><p class="content">{{ selected.referenceAnswerSnapshot || 'No reference answer was available for this attempt.' }}</p><h4>Self-rating</h4><p>{{ interviewSelfRatingLabel[selected.selfRating] || selected.selfRating }}</p>
      <section v-if="selected.followUps.length"><h4>Follow-ups</h4><div v-for="item in selected.followUps" :key="item.followUpId" class="follow-up"><strong>{{ item.questionSnapshot }}</strong><p>{{ item.answerSnapshot }}</p></div></section>
      <section v-if="previous" class="comparison"><h4>Selected vs previous</h4><ul><li v-for="line in comparison" :key="line">{{ line }}</li></ul><div class="answers"><div><h5>Selected answer</h5><p>{{ selected.answerSnapshot }}</p></div><div><h5>Previous answer</h5><p>{{ previous.answerSnapshot }}</p></div></div></section>
    </article>
  </section>
</template>

<style scoped>
.history{margin-bottom:1rem;padding:1rem;border:1px solid var(--ui-border);border-radius:.85rem}.heading{display:flex;align-items:start;justify-content:space-between;gap:1rem}.heading h2,.heading h3{font-size:1.1rem;font-weight:700}.heading p{color:var(--ui-text-muted);font-size:.85rem}.rows{margin-top:1rem;display:grid;gap:.4rem}.rows a{display:grid;grid-template-columns:8rem 7rem 1fr;gap:1rem;padding:.7rem;border-radius:.55rem;color:inherit}.rows a:hover,.rows a:focus-visible{background:var(--ui-bg-elevated);outline:2px solid var(--ui-primary)}.rows span{color:var(--ui-text-muted)}.detail,.detail-error{margin-top:1rem;padding:1rem;border-top:1px solid var(--ui-border)}.detail h4{margin-top:1rem;font-weight:700}.content,.follow-up p,.answers p{white-space:pre-wrap;color:var(--ui-text-muted)}.follow-up,.comparison{margin-top:.75rem;padding:.8rem;border-radius:.55rem;background:var(--ui-bg-muted)}.comparison ul{margin:.5rem 0 0 1rem;list-style:disc}.answers{display:grid;grid-template-columns:1fr 1fr;gap:1rem;margin-top:1rem}.answers h5{font-weight:650}@media(max-width:640px){.rows a{grid-template-columns:1fr;gap:.15rem}.answers{grid-template-columns:1fr}}
</style>
