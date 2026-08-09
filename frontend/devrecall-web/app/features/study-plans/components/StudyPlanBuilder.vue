<script setup lang="ts">
import { ApiError } from '~/types/api'
import type { StudyPlanDetail, StudyPlanEditItem } from '../study-plan.types'
import { studyPlanResourceMeta } from '../study-plan.meta'
import { useStudyPlanEditor } from '../useStudyPlanEditor'
import { useStudyPlanApi } from '../study-plan.api'
import LearningResourcePicker from './LearningResourcePicker.vue'

const props = defineProps<{ detail: StudyPlanDetail }>()
const emit = defineEmits<{
  saved: [detail: StudyPlanDetail]
  ready: [detail: StudyPlanDetail]
  cancel: []
}>()
const detailRef = computed(() => props.detail)
const editor = useStudyPlanEditor(detailRef)
const api = useStudyPlanApi()
const toast = useToast()
const confirmDialog = useConfirmDialog()
const pending = ref(false)
const pickerOpen = ref(false)
const conflict = ref(false)
const liveMessage = ref('')
useUnsavedChangesGuard(editor.dirty)

async function save(): Promise<void> {
  if (!editor.dirty.value || !editor.valid.value || pending.value) return
  pending.value = true
  conflict.value = false
  try {
    await api.saveDraft(props.detail.studyPlanId, editor.form.value)
    const saved = await api.detail(props.detail.studyPlanId)
    editor.acceptSaved(saved)
    emit('saved', saved)
    toast.add({ title: 'Study plan saved', color: 'success' })
  } catch (error) {
    if (error instanceof ApiError && error.problem.status === 409) conflict.value = true
    else throw error
  } finally {
    pending.value = false
  }
}
async function markReady(): Promise<void> {
  if (editor.dirty.value || !editor.form.value.items.length || pending.value) return
  pending.value = true
  try {
    await api.markReady(props.detail.studyPlanId, editor.form.value.expectedVersion)
    emit('ready', await api.detail(props.detail.studyPlanId))
  } finally {
    pending.value = false
  }
}
async function requestCancel(): Promise<void> {
  if (
    editor.dirty.value &&
    !(await confirmDialog.open({
      title: 'Discard unsaved changes?',
      description: 'Your Study Plan edits have not been saved.',
      confirmLabel: 'Discard changes',
      tone: 'danger',
    }))
  )
    return
  editor.reset()
  emit('cancel')
}
async function reloadLatest(): Promise<void> {
  if (
    !(await confirmDialog.open({
      title: 'Reload latest plan?',
      description: 'Your local Study Plan edits will be discarded.',
      confirmLabel: 'Reload latest',
      tone: 'danger',
    }))
  )
    return
  const latest = await api.detail(props.detail.studyPlanId)
  conflict.value = false
  editor.acceptSaved(latest)
  emit('saved', latest)
}
function move(index: number, offset: number): void {
  const title = editor.form.value.items[index]?.resourceTitle
  editor.move(index, offset)
  if (title)
    liveMessage.value = `${title} moved to position ${index + offset + 1} of ${editor.form.value.items.length}.`
}
function add(item: StudyPlanEditItem): void {
  editor.add(item)
  pickerOpen.value = false
}
function shortcut(event: KeyboardEvent): void {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
    event.preventDefault()
    void save()
  }
}
onMounted(() => window.addEventListener('keydown', shortcut))
onBeforeUnmount(() => window.removeEventListener('keydown', shortcut))
</script>

<template>
  <div class="builder">
    <div class="builder-head">
      <div>
        <h2>Edit study plan</h2>
        <p>Changes are saved together as one draft version.</p>
      </div>
      <UButton
        label="Cancel"
        color="neutral"
        variant="ghost"
        :disabled="pending"
        @click="requestCancel"
      />
    </div>
    <div v-if="conflict" class="alert" role="alert">
      <div>
        <strong>This plan changed elsewhere.</strong>
        <p>Your local edits are preserved. Reload the latest plan before saving again.</p>
      </div>
      <UButton label="Reload latest" color="warning" variant="outline" @click="reloadLatest" />
    </div>
    <UFormField label="Plan name" required
      ><UInput v-model="editor.form.value.title" maxlength="200" class="w-full" :disabled="pending"
    /></UFormField>
    <section aria-labelledby="learning-items-title">
      <div class="items-head">
        <div>
          <h3 id="learning-items-title">Learning items</h3>
          <p>
            {{ editor.form.value.items.length }} items · {{ editor.totalMinutes.value }} minutes
          </p>
        </div>
        <UButton
          label="Add learning item"
          icon="i-lucide-plus"
          color="neutral"
          variant="outline"
          :disabled="pending"
          @click="pickerOpen = true"
        />
      </div>
      <ol class="items">
        <li v-for="(item, index) in editor.form.value.items" :key="item.itemId">
          <UIcon name="i-lucide-grip-vertical" class="handle" />
          <div class="identity">
            <strong>{{ item.resourceTitle }}</strong
            ><span
              ><UIcon :name="studyPlanResourceMeta[item.resourceType].icon" />
              {{ studyPlanResourceMeta[item.resourceType].label }}</span
            >
          </div>
          <UFormField :label="`Planned minutes for ${item.resourceTitle}`"
            ><UInput
              v-model.number="item.plannedDurationMinutes"
              type="number"
              min="5"
              max="180"
              :disabled="pending"
          /></UFormField>
          <div class="actions">
            <UButton
              :aria-label="`Move ${item.resourceTitle} up`"
              icon="i-lucide-arrow-up"
              color="neutral"
              variant="ghost"
              :disabled="pending || index === 0"
              @click="move(index, -1)"
            /><UButton
              :aria-label="`Move ${item.resourceTitle} down`"
              icon="i-lucide-arrow-down"
              color="neutral"
              variant="ghost"
              :disabled="pending || index === editor.form.value.items.length - 1"
              @click="move(index, 1)"
            /><UButton
              :aria-label="`Remove ${item.resourceTitle} from this study plan`"
              icon="i-lucide-trash-2"
              color="error"
              variant="ghost"
              :disabled="pending"
              @click="editor.remove(item.itemId)"
            />
          </div>
        </li>
      </ol>
    </section>
    <p aria-live="polite" class="sr-only">{{ liveMessage }}</p>
    <footer>
      <div>
        <strong>{{ editor.totalMinutes.value }} minutes</strong
        ><span>{{ editor.form.value.items.length }} learning items</span>
      </div>
      <div>
        <UButton
          label="Save draft"
          :loading="pending"
          :disabled="!editor.dirty.value || !editor.valid.value"
          @click="save"
        /><UButton
          label="Mark ready"
          color="success"
          :disabled="pending || editor.dirty.value || !editor.form.value.items.length"
          @click="markReady"
        />
      </div>
      <p v-if="editor.dirty.value">Save the draft before marking it ready.</p>
    </footer>
    <LearningResourcePicker
      v-if="pickerOpen"
      :selected="editor.form.value.items"
      @select="add"
      @close="pickerOpen = false"
    />
  </div>
</template>

<style scoped>
.builder {
  display: grid;
  gap: 1.25rem;
}
.builder-head,
.items-head,
footer,
footer > div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.builder h2 {
  font-size: 1.2rem;
  font-weight: 700;
}
.builder h3 {
  font-weight: 700;
}
.builder p,
.identity span,
footer span {
  color: var(--ui-text-muted);
}
.alert {
  padding: 1rem;
  border: 1px solid var(--ui-warning);
  border-radius: 0.7rem;
  background: color-mix(in srgb, var(--ui-warning) 8%, transparent);
}
.items {
  display: grid;
  gap: 0.6rem;
  margin-top: 0.75rem;
}
.items li {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) 10rem auto;
  align-items: center;
  gap: 0.75rem;
  padding: 0.8rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.7rem;
}
.handle {
  color: var(--ui-text-dimmed);
}
.identity {
  display: grid;
}
.identity span {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.8rem;
}
.actions {
  display: flex;
}
footer {
  flex-wrap: wrap;
  padding-top: 1rem;
  border-top: 1px solid var(--ui-border);
}
footer > div:first-child {
  display: grid;
}
footer > p {
  flex-basis: 100%;
  text-align: right;
  font-size: 0.8rem;
}
@media (max-width: 720px) {
  .items li {
    grid-template-columns: minmax(0, 1fr) 7rem;
  }
  .handle {
    display: none;
  }
  .actions {
    grid-column: 1 / -1;
  }
  .builder-head,
  .items-head,
  footer {
    align-items: stretch;
    flex-direction: column;
  }
  footer > div {
    width: 100%;
  }
}
</style>
