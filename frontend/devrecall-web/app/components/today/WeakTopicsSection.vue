<script setup lang="ts">
import type { TodayWeakTopic } from '~/features/today/today.types'
import { getTodayResourcePath } from '~/features/today/today.meta'

defineProps<{ topics: TodayWeakTopic[] }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader title="Weak topics" to="/app/weak-topics" action-label="View weak topics" />
    <div v-if="topics.length" class="divide-y divide-default">
      <component
        :is="topic.isResourceAvailable ? resolveComponent('NuxtLink') : 'div'"
        v-for="topic in topics"
        :key="topic.weakTopicProfileId"
        :to="topic.isResourceAvailable ? getTodayResourcePath(topic.resourceType, topic.resourceId) : undefined"
        class="flex items-center justify-between gap-4 py-3"
      >
        <div class="min-w-0">
          <p class="truncate text-sm font-medium">{{ topic.resourceTitle }}</p>
          <p class="mt-1 text-xs text-muted">{{ topic.isResourceAvailable ? `Score ${Math.round(topic.score)}` : 'Unavailable resource' }}</p>
        </div>
        <CoreStatusBadge :value="topic.level" />
      </component>
    </div>
    <p v-else class="py-4 text-sm text-muted">No weak topics detected. Your evidence will appear here as you practice.</p>
  </section>
</template>
