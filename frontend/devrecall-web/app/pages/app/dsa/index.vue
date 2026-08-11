<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import type { DsaProblemListItem } from '~/types/domain'
definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'DSA' })
const api = useApi()
const items = ref<DsaProblemListItem[]>([])
const loading = ref(true)
const error = ref<unknown>()
const difficulty = ref('')
const topic = ref('')
const creating = ref(false)
const form = reactive({
  title: '',
  description: '',
  difficulty: 'Medium',
  source: '',
  externalUrl: '',
  topics: '',
})
async function load() {
  loading.value = true
  try {
    items.value = (
      await api.get<PagedResponse<DsaProblemListItem>>('/dsa-problems', {
        difficulty: difficulty.value,
        topic: topic.value,
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
  const created = await api.post<{ id: string }>('/dsa-problems', {
    ...form,
    source: form.source || null,
    externalUrl: form.externalUrl || null,
    topics: form.topics
      .split(',')
      .map((x) => x.trim())
      .filter(Boolean),
  })
  await navigateTo(`/app/dsa/${created.id}`)
}
watch([difficulty, topic], load)
onMounted(load)
</script>
<template>
  <div>
    <CorePageHeader
      title="DSA"
      description="Practice patterns, record evidence, and compare how your solutions improve."
      ><UButton label="New problem" icon="i-lucide-plus" @click="creating = true"
    /></CorePageHeader>
    <div class="filters">
      <UInput v-model="topic" aria-label="Filter problems by topic" placeholder="Filter by topic" /><select
        v-model="difficulty" aria-label="Filter problems by difficulty">
        <option value="">All difficulties</option>
        <option>Easy</option>
        <option>Medium</option>
        <option>Hard</option>
      </select>
    </div>
    <CoreLoadingState v-if="loading" label="Loading problems" /><CoreErrorState
      v-else-if="error"
      :error="error"
      @retry="load"
    /><CoreEmptyState
      v-else-if="!items.length"
      title="No DSA problems"
      description="Add a problem to begin tracking attempts."
    >
      <UButton label="Add your first problem" icon="i-lucide-plus" @click="creating = true" />
    </CoreEmptyState>
    <div v-else class="problem-list">
      <NuxtLink v-for="item in items" :key="item.id" :to="`/app/dsa/${item.id}`"
        ><div>
          <div class="meta">
            <CoreStatusBadge :value="item.difficulty" /><span>{{ item.source || 'Personal' }}</span>
          </div>
          <h2>{{ item.title }}</h2>
          <p>{{ item.topics.join(' · ') || 'No topics' }}</p>
        </div>
        <UIcon name="i-lucide-chevron-right"
      /></NuxtLink>
    </div>
    <div v-if="creating" class="backdrop" @click.self="creating = false">
      <UCard class="modal"
        ><template #header><h2>Add DSA problem</h2></template>
        <form @submit.prevent="create">
          <UFormField label="Title" required
            ><UInput v-model="form.title" class="w-full" /></UFormField
          ><UFormField label="Description" required
            ><UTextarea v-model="form.description" :rows="5" class="w-full"
          /></UFormField>
          <div class="two">
            <UFormField label="Difficulty"
              ><select v-model="form.difficulty">
                <option>Easy</option>
                <option>Medium</option>
                <option>Hard</option>
              </select></UFormField
            ><UFormField label="Source"><UInput v-model="form.source" class="w-full" /></UFormField>
          </div>
          <UFormField label="Topics" hint="Comma-separated"
            ><UInput v-model="form.topics" class="w-full" /></UFormField
          ><UFormField label="External URL"
            ><UInput v-model="form.externalUrl" type="url" class="w-full"
          /></UFormField>
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
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.filters select,
form select {
  padding: 0.5rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.5rem;
}
.problem-list {
  display: grid;
  gap: 0.6rem;
}
.problem-list > a {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.7rem;
}
.problem-list > a:hover {
  border-color: color-mix(in srgb, var(--ui-primary) 45%, var(--ui-border));
}
.meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--ui-text-muted);
  font-size: 0.8rem;
}
.problem-list h2 {
  margin: 0.55rem 0 0.2rem;
  font-weight: 700;
}
.problem-list p {
  color: var(--ui-text-muted);
  font-size: 0.85rem;
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
  width: min(100%, 40rem);
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
  gap: 0.6rem;
}
.actions {
  display: flex;
  justify-content: end;
  gap: 0.5rem;
}
</style>
