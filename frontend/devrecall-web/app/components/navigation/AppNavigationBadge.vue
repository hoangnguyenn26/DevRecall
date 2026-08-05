<script setup lang="ts">
const props = defineProps<{ badgeKey: string }>()
const { indicators } = useNavigationIndicators()

const badge = computed(() => {
  if (props.badgeKey === 'reviewsDue' && indicators.value.reviewsDue > 0) {
    return { label: indicators.value.reviewsDue > 99 ? '99+' : String(indicators.value.reviewsDue), color: 'warning' as const }
  }
  if (props.badgeKey === 'activeStudyPlan' && indicators.value.hasActiveStudyPlan) {
    return { label: 'Active', color: 'primary' as const }
  }
  if (props.badgeKey === 'criticalWeakTopics' && indicators.value.criticalWeakTopics > 0) {
    return { label: String(indicators.value.criticalWeakTopics), color: 'error' as const }
  }
  return undefined
})
</script>

<template>
  <UBadge v-if="badge" :color="badge.color" variant="subtle" size="xs" :aria-label="`${badge.label} ${props.badgeKey}`">{{ badge.label }}</UBadge>
</template>
