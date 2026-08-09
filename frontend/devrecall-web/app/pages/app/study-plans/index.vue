<script setup lang="ts">
import StudyPlansWorkspace from '~/features/study-plans/components/StudyPlansWorkspace.vue'
definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Study plans', robots: 'noindex, nofollow' })
const route = useRoute()
const api = useApi()
const generating = ref(false)
const generateOpen = ref(route.query.new === '1')
const form = reactive({ title: 'Focused study plan', totalDurationMinutes: 60 })
async function generate(): Promise<void> {
  if (generating.value) return
  generating.value = true
  try {
    const created = await api.post<{ studyPlanId: string }>('/study-plans/generate', {
      ...form,
      maximumCandidates: 100,
    })
    await navigateTo(`/app/study-plans/${created.studyPlanId}`)
  } finally {
    generating.value = false
  }
}
</script>

<template>
  <div>
    <CorePageHeader title="Study Plans" description="Plan your next focused learning block.">
      <UButton label="New plan" icon="i-lucide-plus" @click="generateOpen = true" />
    </CorePageHeader>
    <StudyPlansWorkspace />
    <UModal
      v-model:open="generateOpen"
      title="Generate study plan"
      description="Create a Draft from active recommendations within your time budget."
    >
      <template #body>
        <form class="grid gap-4" @submit.prevent="generate">
          <UFormField label="Plan name" required
            ><UInput v-model="form.title" maxlength="200" class="w-full"
          /></UFormField>
          <UFormField label="Time budget in minutes" required
            ><UInput
              v-model.number="form.totalDurationMinutes"
              type="number"
              min="15"
              max="480"
              class="w-full"
          /></UFormField>
          <div class="flex justify-end gap-2">
            <UButton
              label="Cancel"
              color="neutral"
              variant="ghost"
              @click="generateOpen = false"
            /><UButton type="submit" label="Generate Draft" :loading="generating" />
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>
