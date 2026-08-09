<script setup lang="ts">
import QuickCaptureTypePicker from './QuickCaptureTypePicker.vue'
import QuickKnowledgeForm from './QuickKnowledgeForm.vue'
import QuickInterviewForm from './QuickInterviewForm.vue'
import QuickDsaForm from './QuickDsaForm.vue'
import { quickCaptureTypeMeta } from '../quick-capture.meta'
import type { CreatedResourceReference, QuickCaptureType } from '../quick-capture.types'
import { useQuickCapture } from '../useQuickCapture'

const quickCapture = useQuickCapture(); const { afterCapture } = useLearningDataInvalidation()
const { refreshAfterMutation } = useBestEffortRefresh()
const router = useRouter()
const dirty = ref(false); const pending = ref(false); const created = ref<CreatedResourceReference | null>(null)
const title = computed(() => created.value ? 'Resource created' : quickCapture.type.value ? quickCaptureTypeMeta[quickCapture.type.value].label : 'Quick capture')
watch(quickCapture.type, () => { dirty.value = false; pending.value = false; created.value = null })
watch(() => quickCapture.open.value, async (open) => {
  if (!open) return
  await nextTick()
  requestAnimationFrame(() => document.querySelector<HTMLButtonElement>('[data-quick-capture-primary]')?.focus())
})
function requestClose(open: boolean) { if (open) return; if (pending.value) return; if (dirty.value && !window.confirm('Discard this capture?\n\nYour unsaved content will be lost.')) return; quickCapture.close() }
function canLeave(): boolean { if (!quickCapture.open.value) return true; if (pending.value) return false; if (dirty.value && !window.confirm('Discard this capture?\n\nYour unsaved content will be lost.')) return false; quickCapture.close(); return true }
async function onCreated(value: CreatedResourceReference) {
  created.value = value; dirty.value = false
  if (quickCapture.type.value) { const type = quickCapture.type.value; await refreshAfterMutation(() => afterCapture(type)) }
}
async function openResource() { if (!created.value) return; const path = created.value.targetPath; quickCapture.close(); await navigateTo(path) }
function continueHere() { quickCapture.close() }
function selectType(type: QuickCaptureType) { quickCapture.selectType(type) }
let removeRouteGuard: (() => void) | undefined
onMounted(() => { removeRouteGuard = router.beforeEach(() => canLeave()) })
onUnmounted(() => removeRouteGuard?.())
</script>

<template>
  <USlideover :open="quickCapture.open.value" :dismissible="!pending" :title="title" description="Capture without leaving your current workflow." @update:open="requestClose">
    <template #body>
      <div v-if="created" class="grid min-h-64 place-items-center content-center gap-4 text-center">
        <span class="grid size-12 place-items-center rounded-full bg-success/10 text-success"><UIcon name="i-lucide-check" class="size-6" /></span>
        <div><h2 class="font-semibold">{{ created.title }}</h2><p class="mt-1 text-sm text-muted">Your resource is ready.</p></div>
        <div class="flex gap-2"><UButton color="neutral" variant="ghost" @click="continueHere">Continue</UButton><UButton @click="openResource">Open resource</UButton></div>
      </div>
      <template v-else-if="quickCapture.type.value">
        <UButton class="mb-5" color="neutral" variant="ghost" icon="i-lucide-arrow-left" label="All capture types" :disabled="pending" @click="quickCapture.back" />
        <QuickKnowledgeForm v-if="quickCapture.type.value === 'KnowledgeNode'" @dirty="dirty = $event" @pending="pending = $event" @created="onCreated" />
        <QuickInterviewForm v-else-if="quickCapture.type.value === 'InterviewQuestion'" @dirty="dirty = $event" @pending="pending = $event" @created="onCreated" />
        <QuickDsaForm v-else @dirty="dirty = $event" @pending="pending = $event" @created="onCreated" />
      </template>
      <QuickCaptureTypePicker v-else @select="selectType" />
    </template>
  </USlideover>
</template>
