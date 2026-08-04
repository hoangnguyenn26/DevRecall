<script setup lang="ts">
import type { TodayRecommendation } from '~/features/today/today.types'

defineProps<{ recommendations: TodayRecommendation[] }>()
</script>

<template>
  <section class="rounded-2xl border border-default p-5">
    <TodaySectionHeader title="Active recommendations" to="/app/recommendations" />
    <div v-if="recommendations.length" class="divide-y divide-default">
      <NuxtLink
        v-for="item in recommendations"
        :key="item.recommendationId"
        :to="`/app/recommendations/${item.recommendationId}`"
        class="flex items-center justify-between gap-4 rounded-lg py-3 focus-visible:outline-2 focus-visible:outline-primary"
      >
        <div class="min-w-0">
          <p class="truncate text-sm font-medium">{{ item.resourceTitle }}</p>
          <p class="mt-1 text-xs text-muted">{{ item.type }}</p>
        </div>
        <CoreStatusBadge :value="item.priority" />
      </NuxtLink>
    </div>
    <p v-else class="py-4 text-sm text-muted">No active recommendations. Keep practicing to reveal the next focus area.</p>
  </section>
</template>
