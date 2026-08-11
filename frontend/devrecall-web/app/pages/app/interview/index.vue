<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import type { InterviewQuestionListItem } from '~/types/domain'
definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Interview' })
const api = useApi()
const items = ref<InterviewQuestionListItem[]>([])
const loading = ref(true)
const error = ref<unknown>()
const topic = ref('')
const difficulty = ref('')
const creating = ref(false)
const form = reactive({ title: '', question: '', topic: '', difficulty: 'Medium', notes: '' })
async function load() {
  loading.value = true
  try {
    items.value = (
      await api.get<PagedResponse<InterviewQuestionListItem>>('/interview-questions', {
        topic: topic.value,
        difficulty: difficulty.value,
        page: 1,
        pageSize: 50,
      })
    ).items
  } catch (caught) {
    error.value = caught
  } finally {
    loading.value = false
  }
}
async function create() {
  const result = await api.post<{ id: string }>('/interview-questions', {
    ...form,
    notes: form.notes || null,
  })
  creating.value = false
  await navigateTo(`/app/interview/${result.id}`)
}
watch([topic, difficulty], load)
onMounted(load)
</script>
<template>
  <div>
    <CorePageHeader
      title="Interview"
      description="Build answers you can explain clearly under pressure."
      ><UButton icon="i-lucide-plus" label="New question" @click="creating = true"
    /></CorePageHeader>
    <div class="filters">
      <UInput v-model="topic" aria-label="Filter questions by topic" placeholder="Filter by topic" icon="i-lucide-search" /><select
        v-model="difficulty" aria-label="Filter questions by difficulty"
      >
        <option value="">All difficulties</option>
        <option>Easy</option>
        <option>Medium</option>
        <option>Hard</option>
      </select>
    </div>
    <CoreLoadingState v-if="loading" label="Loading questions" /><CoreErrorState
      v-else-if="error"
      :error="error"
      @retry="load"
    /><CoreEmptyState
      v-else-if="!items.length"
      title="No interview questions"
      description="Capture a question you want to answer with confidence."
    >
      <UButton label="Add your first question" icon="i-lucide-plus" @click="creating = true" />
    </CoreEmptyState>
    <div v-else class="cards">
      <NuxtLink v-for="item in items" :key="item.id" :to="`/app/interview/${item.id}`"
        ><div class="meta">
          <CoreStatusBadge :value="item.difficulty" /><span>{{ item.topic }}</span>
        </div>
        <h2>{{ item.title }}</h2>
        <span class="open">Practice <UIcon name="i-lucide-arrow-right" /></span
      ></NuxtLink>
    </div>
    <div v-if="creating" class="backdrop" @click.self="creating = false">
      <UCard class="modal"
        ><template #header><h2>New interview question</h2></template>
        <form @submit.prevent="create">
          <UFormField label="Title" required
            ><UInput v-model="form.title" class="w-full" /></UFormField
          ><UFormField label="Question" required
            ><UTextarea v-model="form.question" :rows="5" class="w-full"
          /></UFormField>
          <div class="two">
            <UFormField label="Topic" required
              ><UInput v-model="form.topic" class="w-full" /></UFormField
            ><UFormField label="Difficulty"
              ><select v-model="form.difficulty">
                <option>Easy</option>
                <option>Medium</option>
                <option>Hard</option>
              </select></UFormField
            >
          </div>
          <UFormField label="Notes"><UTextarea v-model="form.notes" class="w-full" /></UFormField>
          <div class="actions">
            <UButton
              label="Cancel"
              color="neutral"
              variant="ghost"
              @click="creating = false"
            /><UButton type="submit" label="Create" />
          </div></form
      ></UCard>
    </div>
  </div>
</template>
<style scoped>
.filters {
  display: flex;
  gap: 0.6rem;
  margin-bottom: 1rem;
}
.filters select,
form select {
  padding: 0.5rem 0.7rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.55rem;
}
.cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr));
  gap: 0.75rem;
}
.cards > a {
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.75rem;
  transition: 0.15s;
}
.cards > a:hover {
  transform: translateY(-2px);
  border-color: color-mix(in srgb, var(--ui-primary) 45%, var(--ui-border));
}
.meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: var(--ui-text-muted);
  font-size: 0.78rem;
}
.cards h2 {
  min-height: 3rem;
  margin: 1rem 0;
  font-weight: 700;
}
.open {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  color: var(--ui-primary);
  font-size: 0.82rem;
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
.modal {
  width: min(100%, 38rem);
}
.modal h2 {
  font-weight: 700;
}
.modal form {
  display: grid;
  gap: 0.8rem;
}
.two {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.7rem;
}
.actions {
  display: flex;
  justify-content: end;
  gap: 0.5rem;
}
</style>
