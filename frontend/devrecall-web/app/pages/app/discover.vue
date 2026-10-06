<script setup lang="ts">
import DiscoverCard from '~/features/discover/DiscoverCard.vue'
import { discoverKeys, type DiscoverResult } from '~/features/discover/discover'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Discover' })
const api = useApi()
const query = useApiQuery<DiscoverResult>(discoverKeys.current, () => api.get('/discover'))
const sections = computed(() => [
  { id: 'recommended', title: 'Recommended for you', description: 'Lessons matching your current learning focus.', items: query.data.value?.recommended ?? [] },
  { id: 'weak', title: 'Strengthen weak areas', description: 'Lessons connected to your existing learning evidence.', items: query.data.value?.basedOnWeakTopics ?? [] },
  { id: 'goals', title: 'Based on your goals', description: 'Matches your declared goals. Shown by publication date, not ranked by fit.', items: query.data.value?.basedOnGoals ?? [] },
].filter(section => section.items.length))
</script>

<template>
  <div class="discover-page">
    <CorePageHeader title="Discover" description="Find a useful place to learn next. You choose what to start."><UButton v-if="query.data.value?.profileConfigured" to="/app/settings/learning-profile" color="neutral" variant="ghost">Edit learning profile</UButton></CorePageHeader>
    <div v-if="query.isPending.value" class="lesson-grid" aria-label="Loading learning suggestions" aria-busy="true">
      <UCard v-for="index in 4" :key="index"><div class="skeleton"><USkeleton class="h-5 w-24" /><USkeleton class="h-6 w-3/4" /><USkeleton class="h-20 w-full" /><USkeleton class="h-9 w-28" /></div></UCard>
    </div>
    <CoreErrorState v-else-if="query.error.value" title="We couldn't load Discover" description="You can still explore the full lesson catalog." :error="query.error.value" @retry="query.refresh" />
    <template v-else-if="query.data.value">
      <section v-for="section in sections" :key="section.id" :aria-labelledby="`discover-${section.id}`">
        <h2 :id="`discover-${section.id}`">{{ section.title }}</h2><p class="section-description">{{ section.description }}</p>
        <div class="lesson-grid"><DiscoverCard v-for="item in section.items" :key="item.slug" :item="item" /></div>
      </section>
      <section v-if="!query.data.value.profileConfigured" class="setup" aria-labelledby="discover-setup">
        <h2 id="discover-setup">Personalize your learning</h2><p>Add your technologies and goals to get more relevant suggestions. You can explore lessons without a profile.</p>
        <UButton to="/app/settings/learning-profile" color="neutral" variant="outline">Set up learning profile</UButton>
      </section>
      <CoreEmptyState v-if="query.data.value.profileConfigured && !sections.length" title="No matching lessons right now" description="You can still explore the full learning catalog." />
    </template>
    <section class="explore" aria-labelledby="discover-explore"><div><h2 id="discover-explore">Explore all lessons</h2><p>Choose something beyond your current goals, or continue a lesson you've started.</p></div><UButton to="/app/learn" color="neutral" variant="outline" trailing-icon="i-lucide-arrow-right">Browse lessons</UButton></section>
  </div>
</template>

<style scoped>
.discover-page{display:grid;gap:2rem;max-width:70rem}h2{font-size:1.15rem;font-weight:700}.section-description,.setup p,.explore p{color:var(--ui-text-muted);margin:.4rem 0 1rem;line-height:1.6}.lesson-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.setup,.explore{border:1px solid var(--ui-border);border-radius:1rem;padding:1.25rem;background:var(--ui-bg-elevated)}.explore{display:flex;align-items:center;justify-content:space-between;gap:1rem}.explore p{margin-bottom:0}.skeleton{display:grid;gap:1rem}@media(max-width:640px){.lesson-grid{grid-template-columns:1fr}.explore{align-items:flex-start;flex-direction:column}}
</style>
