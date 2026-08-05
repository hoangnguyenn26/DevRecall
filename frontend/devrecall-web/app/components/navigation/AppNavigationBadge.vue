<script setup lang="ts">
const props = defineProps<{ badgeKey: string }>()
const { indicators } = useNavigationIndicators()

const badge = computed(() => {
  if (props.badgeKey === 'reviewsDue' && indicators.value.reviewsDue > 0) {
    return { label: indicators.value.reviewsDue > 99 ? '99+' : String(indicators.value.reviewsDue), accessible: `${indicators.value.reviewsDue} items due`, color: 'warning' as const }
  }
  if (props.badgeKey === 'activeStudyPlan' && indicators.value.hasActiveStudyPlan) {
    return { label: 'Active', accessible: 'active plan available', color: 'primary' as const }
  }
  if (props.badgeKey === 'criticalWeakTopics' && indicators.value.criticalWeakTopics > 0) {
    return { label: String(indicators.value.criticalWeakTopics), accessible: `${indicators.value.criticalWeakTopics} critical topics`, color: 'error' as const }
  }
  return undefined
})
</script>

<template>
  <template v-if="badge">
    <UBadge :color="badge.color" variant="subtle" size="xs" aria-hidden="true">{{ badge.label }}</UBadge>
    <span class="sr-only">{{ badge.accessible }}</span>
  </template>
</template>
