<script setup lang="ts">
import type { KnowledgeTopic } from '../knowledge.types'
const props = defineProps<{ items: KnowledgeTopic[]; selectedId?: string; expanded: string[] }>()
const emit = defineEmits<{ select: [id: string]; toggle: [id: string] }>()
const expandedSet = computed(() => new Set(props.expanded))
</script>

<template>
  <ul class="space-y-0.5" role="list">
    <li v-for="topic in items" :key="topic.id">
      <div class="group flex min-w-0 items-center gap-1 rounded-lg" :class="selectedId === topic.id ? 'bg-primary/10 text-primary' : 'hover:bg-elevated'">
        <button v-if="topic.childCount" type="button" class="grid size-8 shrink-0 place-items-center rounded-md" :aria-label="`${expandedSet.has(topic.id) ? 'Collapse' : 'Expand'} ${topic.name}`" :aria-expanded="expandedSet.has(topic.id)" @click="emit('toggle', topic.id)">
          <UIcon name="i-lucide-chevron-right" class="size-4 transition-transform" :class="expandedSet.has(topic.id) && 'rotate-90'" />
        </button>
        <span v-else class="block size-8 shrink-0" />
        <button type="button" class="flex min-w-0 flex-1 items-center justify-between gap-2 py-2 pr-2 text-left text-sm" :aria-current="selectedId === topic.id ? 'page' : undefined" @click="emit('select', topic.id)">
          <span class="truncate">{{ topic.name }}</span><span class="text-xs text-muted">{{ topic.totalKnowledgeCount }}</span>
        </button>
      </div>
      <div v-if="topic.childCount && expandedSet.has(topic.id)" class="ml-4 border-l border-default pl-2">
        <KnowledgeTopicBranch :items="topic.children" :selected-id="selectedId" :expanded="expanded" @select="emit('select', $event)" @toggle="emit('toggle', $event)" />
      </div>
    </li>
  </ul>
</template>
