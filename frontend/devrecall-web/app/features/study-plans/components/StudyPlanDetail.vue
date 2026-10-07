<script setup lang="ts">
import type { StudyPlanDetail } from '../study-plan.types'
import { getStudyPlanStatusMeta, studyPlanResourceMeta } from '../study-plan.meta'
import { formatMinutes } from '~/utils/format'
import { resourceKindLabel } from '~/features/learning-content/learning-content.meta'

defineProps<{ detail: StudyPlanDetail }>()
const emit = defineEmits<{ edit: []; start: [] }>()
</script>

<template>
  <article class="detail">
    <header>
      <div>
        <UBadge :color="getStudyPlanStatusMeta(detail.status).color" variant="subtle">{{
          getStudyPlanStatusMeta(detail.status).label
        }}</UBadge>
        <h1>{{ detail.title }}</h1>
        <p>
          {{ detail.itemCount }} learning items ·
          {{ formatMinutes(detail.totalPlannedDurationMinutes) }}
        </p>
      </div>
      <UButton
        v-if="detail.status === 'Draft'"
        label="Edit plan"
        icon="i-lucide-pencil"
        @click="emit('edit')"
      />
      <UButton
        v-else-if="detail.status === 'Ready'"
        label="Start study"
        icon="i-lucide-play"
        @click="emit('start')"
      />
    </header>
    <ol class="preview">
      <li
        v-for="item in detail.items"
        :key="item.itemId"
        :class="{ unavailable: !item.isResourceAvailable }"
      >
        <span class="position">{{ item.position }}</span
        ><UIcon :name="studyPlanResourceMeta[item.resourceType].icon" />
        <div>
          <strong>{{ item.resourceTitle }}</strong>
          <p>
            {{
              item.isResourceAvailable
                ? studyPlanResourceMeta[item.resourceType].label
                : 'This learning item is no longer available.'
            }}
          </p>
        </div>
        <span>{{ item.plannedDurationMinutes }} min</span>
        <UButton
          v-if="item.resourceType === 'LearningContent' && item.isResourceAvailable && item.resourceKey"
          :to="`/app/learn/${item.resourceKey}`" :label="item.contentType === 'ExternalResource' ? 'Open resource' : 'Open lesson'" color="neutral" variant="ghost" />
        <p v-if="item.contentType === 'ExternalResource'">{{ resourceKindLabel(item.resourceKind) }} · {{ item.sourceName }}</p>
      </li>
    </ol>
    <CoreEmptyState
      v-if="!detail.items.length"
      title="This draft is empty"
      description="Edit the plan to add learning resources before marking it ready."
    />
  </article>
</template>

<style scoped>
.detail {
  display: grid;
  gap: 1.25rem;
}
.detail header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 1rem;
}
.detail h1 {
  margin-top: 0.5rem;
  font-size: 1.6rem;
  font-weight: 750;
}
.detail header p,
.preview p {
  color: var(--ui-text-muted);
}
.preview {
  display: grid;
  gap: 0.55rem;
}
.preview li {
  display: grid;
  grid-template-columns: 2rem auto minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.7rem;
  padding: 0.85rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.7rem;
}
.position {
  display: grid;
  place-items: center;
  width: 1.7rem;
  height: 1.7rem;
  border-radius: 999px;
  background: var(--ui-bg-elevated);
}
.unavailable {
  border-style: dashed;
}
.unavailable strong {
  color: var(--ui-text-muted);
}
@media (max-width: 600px) {
  .detail header {
    flex-direction: column;
  }
  .preview li {
    grid-template-columns: 2rem auto minmax(0, 1fr);
  }
  .preview li > span:last-child {
    grid-column: 3;
  }
}
</style>
