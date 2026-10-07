<script setup lang="ts">
import { discoverReasons, type DiscoverLesson, type DiscoverResource } from './discover'
import { difficultyColor, resourceKindLabel } from '../learning-content/learning-content.meta'
defineProps<{ item: DiscoverLesson | DiscoverResource; resource?: boolean }>()
</script>

<template>
  <article class="discover-card">
    <h3>{{ item.title }}</h3>
    <div v-if="resource && 'sourceName' in item" class="card-meta">{{ resourceKindLabel(item.resourceKind) }} · {{ item.sourceName }}</div>
    <p>{{ item.summary }}</p>
    <div class="card-meta"><UBadge :color="difficultyColor(item.difficulty)" variant="subtle">{{ item.difficulty }}</UBadge><span>{{ resource ? '~' : '' }}{{ item.estimatedMinutes }} min</span></div>
    <div v-if="!resource" class="tags"><span v-for="technology in item.technologies.slice(0, 3)" :key="technology.value">{{ technology.label }}</span><span v-for="topic in item.topics.slice(0, 2)" :key="topic.slug">{{ topic.name }}</span></div>
    <div class="reason"><h4>{{ resource ? 'Why this resource' : 'Why this lesson' }}</h4><ul><li v-for="reason in discoverReasons(item, resource)" :key="reason">{{ reason }}</li></ul></div>
    <UButton :to="{ path: `/app/learn/${item.slug}`, query: { returnTo: '/app/discover' } }" trailing-icon="i-lucide-arrow-right" variant="soft">{{ resource ? 'Open resource' : 'Open lesson' }}</UButton>
  </article>
</template>

<style scoped>
.discover-card{display:flex;flex-direction:column;gap:.85rem;border:1px solid var(--ui-border);border-radius:1rem;padding:1.25rem;background:var(--ui-bg-elevated)}.card-meta{display:flex;justify-content:space-between;align-items:center;font-size:.85rem;color:var(--ui-text-muted)}h3{font-weight:700;font-size:1.1rem}p{color:var(--ui-text-muted);line-height:1.6}.tags{display:flex;flex-wrap:wrap;gap:.5rem;font-size:.75rem}.tags span{border:1px solid var(--ui-border);border-radius:999px;padding:.2rem .5rem}.reason{margin-top:auto;font-size:.85rem;color:var(--ui-text-muted);line-height:1.6}.reason h4{font-weight:600;margin-bottom:.25rem}.discover-card :deep(a){align-self:flex-start}
</style>
