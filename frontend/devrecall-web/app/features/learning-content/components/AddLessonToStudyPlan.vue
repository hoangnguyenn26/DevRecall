<script setup lang="ts">
import type { StudyPlanListItem } from '~/features/study-plans/study-plan.types'
import { useStudyPlanApi } from '~/features/study-plans/study-plan.api'
import { normalizeApiError } from '~/utils/normalize-api-error'

const props = defineProps<{ slug: string; completed: boolean }>()
const api = useStudyPlanApi()
const open = ref(false)
const loading = ref(false)
const plans = ref<Array<StudyPlanListItem & { containsLesson: boolean }>>([])
const selectedId = ref('')
const error = ref('')
const result = ref<{ planTitle: string; studyPlanId: string; added: boolean }>()
const submissionId = ref(crypto.randomUUID())

async function show() {
  open.value = true
  result.value = undefined
  error.value = ''
  if (props.completed) return
  loading.value = true
  try {
    plans.value = (await api.learningContentOptions(props.slug)).map(plan => ({
      ...plan, containsLesson: plan.alreadyContains,
    }))
    selectedId.value = plans.value.find(plan => !plan.containsLesson)?.studyPlanId ?? ''
  } catch (cause) { error.value = normalizeApiError(cause).detail ?? 'Study plans could not be loaded.' }
  finally { loading.value = false }
}
async function add() {
  const plan = plans.value.find(item => item.studyPlanId === selectedId.value)
  if (!plan || loading.value) return
  loading.value = true
  error.value = ''
  try {
    result.value = await api.addLearningContent(plan.studyPlanId, props.slug,
      plan.version, submissionId.value)
  } catch (cause) { error.value = normalizeApiError(cause).detail ?? 'The lesson could not be added.' }
  finally { loading.value = false }
}
</script>

<template>
  <UButton icon="i-lucide-calendar-plus" color="neutral" variant="outline" @click="show">Add to Study Plan</UButton>
  <UModal v-model:open="open" title="Add to Study Plan" description="Choose where you'd like to study this lesson.">
    <template #body>
      <div class="plan-dialog">
        <UAlert v-if="completed" color="neutral" variant="subtle" title="Already completed" description="Use Review or Read again to revisit this lesson." />
        <CoreLoadingState v-else-if="loading && !plans.length" label="Loading editable plans" />
        <template v-else-if="result">
          <UAlert color="success" variant="subtle" :title="result.added ? `Added to ${result.planTitle}` : `Already in ${result.planTitle}`" />
          <div class="actions"><UButton :to="`/app/study-plans/${result.studyPlanId}`">View plan</UButton><UButton color="neutral" variant="ghost" @click="open = false">Done</UButton></div>
        </template>
        <CoreEmptyState v-else-if="!plans.length" title="No editable study plan" description="Create a Draft plan before adding this lesson.">
          <UButton to="/app/study-plans">Create Study Plan</UButton>
        </CoreEmptyState>
        <template v-else>
          <label v-for="plan in plans" :key="plan.studyPlanId" class="plan-option" :class="{ disabled: plan.containsLesson }">
            <input v-model="selectedId" type="radio" :value="plan.studyPlanId" :disabled="plan.containsLesson"><span><strong>{{ plan.title }}</strong><small>{{ plan.containsLesson ? 'Already added' : `${plan.itemCount} items · ${plan.totalPlannedDurationMinutes} min` }}</small></span>
          </label>
          <UAlert v-if="error" color="error" variant="subtle" title="Lesson wasn't added" :description="error" />
          <div class="actions"><UButton color="neutral" variant="ghost" @click="open = false">Cancel</UButton><UButton :loading="loading" :disabled="!selectedId" @click="add">Add to plan</UButton></div>
        </template>
      </div>
    </template>
  </UModal>
</template>

<style scoped>.plan-dialog{display:grid;gap:1rem}.plan-option{display:flex;gap:.75rem;border:1px solid var(--ui-border);border-radius:.75rem;padding:.85rem;cursor:pointer}.plan-option span{display:grid}.plan-option small{color:var(--ui-text-muted)}.plan-option.disabled{opacity:.65;cursor:not-allowed}.actions{display:flex;justify-content:flex-end;gap:.5rem}</style>
