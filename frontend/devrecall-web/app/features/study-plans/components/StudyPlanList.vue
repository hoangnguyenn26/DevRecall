<script setup lang="ts">
import type { StudyPlanListItem } from '../study-plan.types'
import { getStudyPlanStatusMeta } from '../study-plan.meta'
import { formatMinutes } from '~/utils/format'

const props = defineProps<{ plans: StudyPlanListItem[]; selectedId?: string }>()
const groups = computed(() => {
  const source = props.plans
  return [
    { label: 'Ready', plans: source.filter((plan) => plan.status === 'Ready') },
    { label: 'Draft', plans: source.filter((plan) => plan.status === 'Draft') },
    {
      label: 'Recent',
      plans: source
        .filter((plan) => plan.status !== 'Ready' && plan.status !== 'Draft')
        .slice(0, 10),
    },
  ].filter((group) => group.plans.length)
})
</script>

<template>
  <nav aria-label="Study plans" class="plan-list">
    <section v-for="group in groups" :key="group.label">
      <h2>{{ group.label }}</h2>
      <NuxtLink
        v-for="plan in group.plans"
        :key="plan.studyPlanId"
        :to="`/app/study-plans/${plan.studyPlanId}`"
        :aria-current="selectedId === plan.studyPlanId ? 'page' : undefined"
      >
        <span
          ><strong>{{ plan.title }}</strong
          ><small
            >{{ plan.itemCount }} items ·
            {{ formatMinutes(plan.totalPlannedDurationMinutes) }}</small
          ></span
        >
        <UBadge :color="getStudyPlanStatusMeta(plan.status).color" variant="subtle">{{
          getStudyPlanStatusMeta(plan.status).label
        }}</UBadge>
      </NuxtLink>
    </section>
  </nav>
</template>

<style scoped>
.plan-list {
  display: grid;
  gap: 1.25rem;
}
.plan-list section {
  display: grid;
  gap: 0.45rem;
}
.plan-list h2 {
  color: var(--ui-text-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
.plan-list a {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem;
  border: 1px solid transparent;
  border-radius: 0.65rem;
}
.plan-list a:hover,
.plan-list a[aria-current='page'] {
  border-color: var(--ui-border);
  background: var(--ui-bg-elevated);
}
.plan-list a span {
  display: grid;
  min-width: 0;
}
.plan-list strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.plan-list small {
  color: var(--ui-text-muted);
}
</style>
