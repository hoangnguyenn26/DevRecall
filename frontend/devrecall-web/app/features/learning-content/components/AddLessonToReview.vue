<script setup lang="ts">
import { computed, ref } from 'vue'
import { useLearningContentApi } from '../learning-content.api'
import type { LearningContentReviewCandidate } from '../learning-content.types'
import { availableReviewCandidates } from '../lesson-review'
import { normalizeApiError } from '~/utils/normalize-api-error'

const props = defineProps<{ slug: string; candidates: LearningContentReviewCandidate[] }>()
const emit = defineEmits<{ added: [keys: string[]] }>()
const api = useLearningContentApi()
const invalidation = useLearningDataInvalidation()
const open = ref(false)
const selected = ref<string[]>([])
const submissionId = ref('')
const pending = ref(false)
const formError = ref('')
const successCount = ref<number | null>(null)
const expandedAnswers = ref<string[]>([])
const available = computed(() => availableReviewCandidates(props.candidates))
const allAdded = computed(() => props.candidates.length > 0 && available.value.length === 0)
const submitLabel = computed(() => selected.value.length === 0
  ? 'Add to Review'
  : `Add ${selected.value.length} to Review`)

function begin() {
  selected.value = []
  submissionId.value = crypto.randomUUID()
  formError.value = ''
  successCount.value = null
  expandedAnswers.value = []
  open.value = true
}

function toggleAnswer(key: string) {
  expandedAnswers.value = expandedAnswers.value.includes(key)
    ? expandedAnswers.value.filter(value => value !== key)
    : [...expandedAnswers.value, key]
}

async function submit() {
  if (pending.value || selected.value.length === 0) return
  pending.value = true
  formError.value = ''
  try {
    const result = await api.addToReview(props.slug, selected.value, submissionId.value)
    const keys = result.items.map(item => item.candidateKey)
    emit('added', keys)
    successCount.value = keys.length
    await invalidation.afterReviewItemsAdded()
  } catch (error) {
    const normalized = normalizeApiError(error)
    formError.value = normalized.detail ?? normalized.title
  } finally { pending.value = false }
}
</script>

<template>
  <div v-if="candidates.length">
    <p v-if="allAdded" class="inline-flex items-center gap-2 text-sm font-medium text-muted"><UIcon name="i-lucide-circle-check" class="text-success" />Key concepts are already in Review.</p>
    <UButton v-else color="neutral" variant="outline" icon="i-lucide-brain" @click="begin">Add to Review</UButton>
    <UModal :open="open" :dismissible="!pending" title="Remember key concepts" description="Select the concepts you want to practice recalling later." :ui="{ content: 'sm:max-w-2xl' }" @update:open="value => { if (!value && !pending) open = false }">
      <template #body>
        <div v-if="successCount !== null" class="space-y-5 py-2 text-center" aria-live="polite">
          <div class="mx-auto flex size-12 items-center justify-center rounded-full bg-success/10 text-success"><UIcon name="i-lucide-circle-check" class="size-6" /></div>
          <div><h3 class="text-lg font-semibold">{{ successCount }} {{ successCount === 1 ? 'concept' : 'concepts' }} added to Review</h3><p class="mt-1 text-sm text-muted">The existing Review scheduler will decide when they are due.</p></div>
          <div class="flex flex-col-reverse justify-center gap-2 sm:flex-row"><UButton color="neutral" variant="outline" @click="open = false">Done</UButton><UButton to="/app/review">Go to Review</UButton></div>
        </div>
        <form v-else class="space-y-5" @submit.prevent="submit">
          <fieldset class="space-y-2"><legend class="sr-only">Lesson concepts</legend>
            <article v-for="candidate in candidates" :key="candidate.key" class="rounded-xl border border-default p-3" :class="candidate.isInReview ? 'bg-elevated/40 text-muted' : 'hover:border-primary/40'">
              <div class="flex min-h-10 items-start gap-3">
                <input :id="`review-${candidate.key}`" v-model="selected" type="checkbox" :value="candidate.key" :disabled="candidate.isInReview || pending" class="mt-1 size-4 accent-primary">
                <div class="min-w-0 flex-1"><label :for="`review-${candidate.key}`" class="block text-sm font-medium leading-6" :class="candidate.isInReview ? '' : 'cursor-pointer'">{{ candidate.prompt }}</label><span v-if="candidate.isInReview" class="mt-1 block text-xs">Already in Review</span></div><UIcon v-if="candidate.isInReview" name="i-lucide-circle-check" class="mt-1 text-success" />
              </div>
              <UButton type="button" color="neutral" variant="link" size="xs" class="mt-1 px-0" :aria-expanded="expandedAnswers.includes(candidate.key)" :aria-controls="`answer-${candidate.key}`" @click="toggleAnswer(candidate.key)">{{ expandedAnswers.includes(candidate.key) ? 'Hide answer' : 'Preview answer' }}</UButton>
              <p v-if="expandedAnswers.includes(candidate.key)" :id="`answer-${candidate.key}`" class="mt-2 border-l-2 border-default pl-3 text-sm leading-6 text-muted">{{ candidate.answer }}</p>
            </article>
          </fieldset>
          <UAlert v-if="formError" color="error" variant="subtle" title="We couldn't add these concepts to Review" :description="`${formError} Your selections are still here, and your lesson remains completed.`" />
          <div class="flex flex-col-reverse justify-end gap-2 border-t border-default pt-4 sm:flex-row"><UButton type="button" color="neutral" variant="ghost" :disabled="pending" @click="open = false">Cancel</UButton><UButton type="submit" :loading="pending" :disabled="pending || selected.length === 0">{{ submitLabel }}</UButton></div>
        </form>
      </template>
    </UModal>
  </div>
</template>
