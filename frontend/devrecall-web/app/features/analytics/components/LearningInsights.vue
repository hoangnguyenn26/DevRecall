<script setup lang="ts">
import { insightAction } from '../analytics.meta'
import type { LearningInsight } from '../analytics.types'
defineProps<{ items: LearningInsight[] }>()
</script>
<template>
  <section class="insights" aria-labelledby="learning-insights-heading">
    <header>
      <h2 id="learning-insights-heading">Learning Insights</h2>
      <p>Factual patterns from your recent learning evidence.</p>
    </header>
    <CoreEmptyState
      v-if="!items.length"
      title="No strong learning insights for this period"
      description="Your activity data is still available below."
    />
    <div v-else class="cards">
      <article v-for="item in items" :key="`${item.type}-${item.title}`">
        <span class="tone">{{ item.tone }} · {{ item.priority }} priority</span>
        <h3>{{ item.title }}</h3>
        <p>{{ item.summary }}</p>
        <ul>
          <li v-for="signal in item.signals" :key="signal.type">
            <span>{{ signal.label }}</span
            ><strong>{{ signal.value }}</strong>
          </li>
        </ul>
        <UButton
          v-if="insightAction(item.action)"
          :to="insightAction(item.action)"
          :label="item.action?.label"
          variant="outline"
        />
        <p v-else-if="item.action" class="unavailable">
          Recommended action is no longer available.
        </p>
      </article>
    </div>
  </section>
</template>
<style scoped>
.insights {
  display: grid;
  gap: 0.8rem;
  margin-bottom: 1rem;
}
.insights header p,
.unavailable {
  color: var(--ui-text-muted);
}
.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
  gap: 0.75rem;
}
.cards article {
  display: grid;
  align-content: start;
  gap: 0.6rem;
  padding: 1rem;
  border: 1px solid var(--ui-border);
  border-radius: 0.75rem;
}
.tone {
  font-size: 0.8rem;
  font-weight: 700;
}
.cards h3 {
  font-weight: 750;
}
.cards > article > p {
  color: var(--ui-text-muted);
}
ul {
  display: grid;
}
li {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.4rem 0;
  border-bottom: 1px solid var(--ui-border);
}
</style>
