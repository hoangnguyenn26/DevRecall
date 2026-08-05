<script setup lang="ts">
import type { TodayRecommendation } from '~/features/today/today.types'

defineProps<{ recommendations: TodayRecommendation[] }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader title="Recommendations" :to="recommendations.length ? '/app/recommendations' : undefined" />
    <ul v-if="recommendations.length" class="divide-y divide-default rounded-xl border border-default">
      <li v-for="item in recommendations" :key="item.recommendationId">
        <NuxtLink
          :to="`/app/recommendations/${item.recommendationId}`"
          class="flex items-start justify-between gap-4 p-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary"
        >
          <div class="min-w-0">
            <p class="truncate text-sm font-medium">{{ item.isResourceAvailable ? item.resourceTitle : 'Unavailable resource' }}</p>
            <p class="mt-1 line-clamp-2 text-xs leading-5 text-muted">{{ item.reasonSummary }}</p>
          </div>
          <CoreStatusBadge :value="item.priority" />
        </NuxtLink>
      </li>
    </ul>
    <CoreEmptyState
      v-else
      icon="i-lucide-sparkles"
      title="No active recommendations"
      description="DevRecall will surface focused actions when your learning history reveals something worth revisiting."
    />
  </section>
</template>
