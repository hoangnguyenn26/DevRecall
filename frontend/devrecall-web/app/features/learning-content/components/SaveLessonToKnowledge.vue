<script setup lang="ts">
import type { KnowledgeTagOption, KnowledgeTopic, KnowledgeTopicTree } from '~/features/knowledge/knowledge.types'
import { queryKeys } from '~/query/query-keys'
import { normalizeApiError } from '~/utils/normalize-api-error'
import { useLearningContentApi } from '../learning-content.api'
import { buildLessonKnowledgeDraft } from '../lesson-knowledge'
import type { LearningContentDetail, SavedLessonKnowledge } from '../learning-content.types'

const props = defineProps<{ lesson: LearningContentDetail }>()
const open = ref(false)
const api = useApi()
const learningApi = useLearningContentApi()
const confirmDialog = useConfirmDialog()
const pending = ref(false)
const result = ref<SavedLessonKnowledge | null>(null)
const errors = ref<Record<string, string[]>>({})
const formError = ref('')
const initial = ref(buildLessonKnowledgeDraft(props.lesson, crypto.randomUUID()))
const form = reactive({ ...initial.value })
const { data: topicTree, refresh: refreshTopics } = await useAsyncData<KnowledgeTopicTree>(queryKeys.knowledgeTopics,
  () => api.get('/knowledge/topics/tree'), { server: false, immediate: false })
const { data: tagOptions, refresh: refreshTags } = await useAsyncData<KnowledgeTagOption[]>(queryKeys.knowledgeTags(),
  () => api.get('/knowledge/tags', { take: 20 }), { server: false, immediate: false, default: () => [] })

function flattenTopics(items: KnowledgeTopic[], depth = 0): { label: string; value: string | null }[] {
  return items.flatMap(item => [{ label: `${'— '.repeat(depth)}${item.name}`, value: item.id },
    ...flattenTopics(item.children, depth + 1)])
}
const topics = computed(() => [{ label: 'Uncategorized', value: null }, ...flattenTopics(topicTree.value?.items ?? [])])
const tags = computed(() => (tagOptions.value ?? []).map(tag => ({ label: tag.name, value: tag.id })))
const dirty = computed(() => !result.value && JSON.stringify(form) !== JSON.stringify(initial.value))

function begin() {
  initial.value = buildLessonKnowledgeDraft(props.lesson, crypto.randomUUID())
  Object.assign(form, initial.value)
  errors.value = {}
  formError.value = ''
  result.value = null
  open.value = true
  void Promise.all([refreshTopics(), refreshTags()])
}

async function close() {
  if (pending.value) return
  if (dirty.value && !(await confirmDialog.open({ title: 'Discard this note draft?',
    description: 'Your lesson remains completed, but these unsaved note edits will be lost.',
    confirmLabel: 'Discard draft', tone: 'danger' }))) return
  open.value = false
}

async function save() {
  if (pending.value) return
  if (!form.title.trim()) { errors.value = { title: ['Title is required.'] }; return }
  pending.value = true
  errors.value = {}
  formError.value = ''
  try {
    result.value = await learningApi.saveToKnowledge(props.lesson.slug, {
      ...form, title: form.title.trim(), content: form.content.trim(), tagIds: [...form.tagIds],
    })
    clearNuxtData(key => key.startsWith(queryKeys.knowledgeListBase)
      || key === queryKeys.knowledgeTopics || key.startsWith('knowledge:tags:'))
  } catch (error) {
    const normalized = normalizeApiError(error)
    errors.value = normalized.fieldErrors
    formError.value = normalized.detail ?? normalized.title
  } finally { pending.value = false }
}
</script>

<template>
  <UButton icon="i-lucide-bookmark-plus" @click="begin">Save to Knowledge</UButton>
  <UModal :open="open" :dismissible="false" title="Keep what matters" description="Turn the lesson's key takeaways into a personal note." :ui="{ content: 'sm:max-w-2xl' }">
    <template #body>
      <div v-if="result" class="space-y-5 py-2 text-center" aria-live="polite">
        <div class="mx-auto flex size-12 items-center justify-center rounded-full bg-success/10 text-success"><UIcon name="i-lucide-circle-check" class="size-6" /></div>
        <div><h3 class="text-lg font-semibold">Saved to Knowledge</h3><p class="mt-1 text-sm text-muted">Your note is independent from the lesson and can be edited anytime.</p></div>
        <div class="flex flex-col-reverse justify-center gap-2 sm:flex-row"><UButton color="neutral" variant="outline" @click="open = false">Done</UButton><UButton :to="`/app/knowledge/${result.id}`">View note</UButton></div>
      </div>
      <form v-else class="space-y-5" @submit.prevent="save">
        <UFormField label="Title" required :error="errors.title?.[0]"><UInput v-model="form.title" autofocus class="w-full" maxlength="200" /></UFormField>
        <UFormField label="Note" hint="Started from this lesson's key takeaways" :error="errors.content?.[0]"><UTextarea v-model="form.content" :rows="10" class="w-full font-mono text-sm" /></UFormField>
        <div class="grid gap-4 sm:grid-cols-2">
          <UFormField label="Topic" :error="errors.topicId?.[0]"><USelect v-model="form.topicId" :items="topics" value-key="value" class="w-full" /></UFormField>
          <UFormField label="Tags" hint="Optional" :error="errors.tagIds?.[0]"><USelect v-model="form.tagIds" multiple :items="tags" value-key="value" class="w-full" placeholder="Select existing tags" /></UFormField>
        </div>
        <UAlert v-if="formError" color="error" variant="subtle" title="The note wasn't saved" :description="`${formError} Your lesson completion is unchanged.`" />
        <div class="flex flex-col-reverse justify-end gap-2 border-t border-default pt-4 sm:flex-row"><UButton type="button" color="neutral" variant="ghost" :disabled="pending" @click="close">Cancel</UButton><UButton type="submit" :loading="pending" :disabled="pending">Save note</UButton></div>
      </form>
    </template>
  </UModal>
</template>
