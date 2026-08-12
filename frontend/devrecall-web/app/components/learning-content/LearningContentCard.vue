<script setup lang="ts">
import { computed } from 'vue'
import type { LearningContentListItem } from '~/features/learning-content/learning-content.types'
import { difficultyColor, visibleTags } from '~/features/learning-content/learning-content.meta'

const props = defineProps<{ item: LearningContentListItem; returnTo: string }>()
const technologies = computed(() => visibleTags(props.item.technologies))
const topics = computed(() => visibleTags(props.item.topics))
const target = computed(() => props.item.contentType === 'Lesson'
  ? { path: `/app/learn/${props.item.slug}`, query: { returnTo: props.returnTo } }
  : `/app/learn/${props.item.slug}`)
</script>

<template>
  <article class="content-card">
    <div class="card-meta"><UBadge :color="difficultyColor(item.difficulty)" variant="subtle">{{ item.difficulty }}</UBadge><span>{{ item.estimatedMinutes }} min</span></div>
    <div><h2>{{ item.title }}</h2><p>{{ item.summary }}</p></div>
    <div class="tags" aria-label="Lesson metadata">
      <span v-for="technology in technologies.visible" :key="technology.value">{{ technology.label }}</span>
      <span v-for="topic in topics.visible" :key="topic.slug">{{ topic.name }}</span>
      <span v-if="technologies.hiddenCount + topics.hiddenCount">+{{ technologies.hiddenCount + topics.hiddenCount }}</span>
    </div>
    <UButton :to="target" trailing-icon="i-lucide-arrow-right" variant="soft">
      {{ item.contentType === 'Lesson' ? 'Start lesson' : 'Open resource' }}
    </UButton>
  </article>
</template>

<style scoped>
.content-card{display:flex;min-height:18rem;flex-direction:column;gap:1rem;border:1px solid var(--ui-border);border-radius:1rem;background:var(--ui-bg-elevated);padding:1.2rem;box-shadow:0 1px 2px rgb(15 23 42/.04)}.content-card:hover{border-color:color-mix(in srgb,var(--ui-primary) 35%,var(--ui-border));box-shadow:0 12px 28px rgb(15 23 42/.07)}.card-meta{display:flex;align-items:center;justify-content:space-between;color:var(--ui-text-muted);font-size:.82rem}.content-card h2{font-size:1.08rem;font-weight:750;line-height:1.35}.content-card p{margin-top:.55rem;color:var(--ui-text-muted);font-size:.92rem;line-height:1.6}.tags{display:flex;flex-wrap:wrap;gap:.4rem;margin-top:auto}.tags span{border:1px solid var(--ui-border);border-radius:999px;padding:.24rem .55rem;color:var(--ui-text-muted);font-size:.73rem}.content-card :deep(a){align-self:flex-start}
</style>
