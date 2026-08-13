<script setup lang="ts">
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
const available = computed(() => availableReviewCandidates(props.candidates))
const allAdded = computed(() => props.candidates.length > 0 && available.value.length === 0)

function begin() {
  selected.value = []
  submissionId.value = crypto.randomUUID()
  formError.value = ''
  successCount.value = null
  open.value = true
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
    <p v-if="allAdded" class="inline-flex items-center gap-2 text-sm font-medium text-success"><UIcon name="i-lucide-circle-check" />All key concepts are in Review</p>
    <UButton v-else color="neutral" variant="outline" icon="i-lucide-brain" @click="begin">Add to Review</UButton>
    <UModal :open="open" :dismissible="!pending" title="Review key concepts" description="Select what you'd like DevRecall to help you remember." :ui="{ content: 'sm:max-w-2xl' }" @update:open="value => { if (!value && !pending) open = false }">
      <template #body>
        <div v-if="successCount !== null" class="space-y-5 py-2 text-center" aria-live="polite">
          <div class="mx-auto flex size-12 items-center justify-center rounded-full bg-success/10 text-success"><UIcon name="i-lucide-circle-check" class="size-6" /></div>
          <div><h3 class="text-lg font-semibold">{{ successCount }} {{ successCount === 1 ? 'concept' : 'concepts' }} added to Review</h3><p class="mt-1 text-sm text-muted">The existing Review scheduler will decide when they are due.</p></div>
          <div class="flex flex-col-reverse justify-center gap-2 sm:flex-row"><UButton color="neutral" variant="outline" @click="open = false">Done</UButton><UButton to="/app/review">Go to Review</UButton></div>
        </div>
        <form v-else class="space-y-5" @submit.prevent="submit">
          <fieldset class="space-y-2"><legend class="sr-only">Lesson concepts</legend>
            <label v-for="candidate in candidates" :key="candidate.key" class="flex min-h-14 items-start gap-3 rounded-xl border border-default p-3" :class="candidate.isInReview ? 'bg-elevated/40 text-muted' : 'cursor-pointer hover:border-primary/40'">
              <input v-model="selected" type="checkbox" :value="candidate.key" :disabled="candidate.isInReview || pending" class="mt-1 size-4 accent-primary">
              <span class="min-w-0 flex-1"><span class="block text-sm font-medium leading-6">{{ candidate.prompt }}</span><span v-if="candidate.isInReview" class="mt-1 block text-xs">Already in Review</span></span><UIcon v-if="candidate.isInReview" name="i-lucide-circle-check" class="mt-1 text-success" />
            </label>
          </fieldset>
          <UAlert v-if="formError" color="error" variant="subtle" title="We couldn't add these concepts to Review" :description="`${formError} Your selections are still here, and your lesson remains completed.`" />
          <div class="flex flex-col-reverse justify-end gap-2 border-t border-default pt-4 sm:flex-row"><UButton type="button" color="neutral" variant="ghost" :disabled="pending" @click="open = false">Cancel</UButton><UButton type="submit" :loading="pending" :disabled="pending || selected.length === 0">Add {{ selected.length || '' }}{{ selected.length ? ' to Review' : ' selected to Review' }}</UButton></div>
        </form>
      </template>
    </UModal>
  </div>
</template>
