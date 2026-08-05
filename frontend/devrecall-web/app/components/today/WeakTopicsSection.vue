<script setup lang="ts">
import type { TodayWeakTopic } from '~/features/today/today.types'

defineProps<{ topics: TodayWeakTopic[] }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader title="Weak topics" :to="topics.length ? '/app/weak-topics' : undefined" action-label="View all" />
    <div v-if="topics.length" class="divide-y divide-default">
      <NuxtLink
        v-for="topic in topics"
        :key="topic.weakTopicProfileId"
        :to="`/app/weak-topics/${topic.weakTopicProfileId}`"
        class="block rounded-xl py-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
      >
        <div class="flex items-start justify-between gap-3">
          <div class="min-w-0">
            <p class="truncate text-sm font-medium">{{ topic.isResourceAvailable ? topic.resourceTitle : 'Unavailable resource' }}</p>
            <p class="mt-1 line-clamp-2 text-xs leading-5 text-muted">{{ topic.summary }}</p>
          </div>
          <CoreStatusBadge :value="topic.level" />
        </div>
        <div class="mt-2 flex items-center justify-between text-xs text-muted">
          <span>{{ topic.resourceType }}</span><span>Score {{ Number(topic.score).toFixed(1) }}</span>
        </div>
      </NuxtLink>
    </div>
    <CoreEmptyState
      v-else
      icon="i-lucide-circle-check"
      title="No weak topics detected"
      description="Keep practicing. DevRecall will highlight topics that repeatedly need attention."
    />
  </section>
</template>
