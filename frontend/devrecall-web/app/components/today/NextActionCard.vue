<script setup lang="ts">
import { computed } from 'vue'
import type { TodayNextAction } from '~/features/today/today.types'
import { getTodayActionIcon, getTodayActionLabel } from '~/features/today/today.meta'
import { formatTodayMinutes } from '~/features/today/today.format'

const props = defineProps<{ action: TodayNextAction }>()
const icon = computed(() => getTodayActionIcon(props.action.icon))
const actionLabel = computed(() => getTodayActionLabel(props.action.type))
const contextText = computed(() => {
  const context = props.action.context
  if (!context) return ''
  if (props.action.type === 'ContinueStudySession' && context.remainingCount != null)
    return `${context.remainingCount} ${context.remainingCount === 1 ? 'item' : 'items'} remaining`
  if (context.plannedDurationMinutes == null) return ''
  const minutes = formatTodayMinutes(context.plannedDurationMinutes)
  return props.action.type === 'StartStudyPlan' || props.action.type === 'ContinueStudyPlan'
    ? `${context.remainingCount ?? 0} items · Estimated plan time: ${minutes}`
    : `Estimated lesson time: ~${minutes}`
})
</script>

<template>
  <section
    class="relative overflow-hidden rounded-2xl border border-primary/20 bg-primary/5 p-5 sm:p-6"
    aria-labelledby="next-best-action-title"
  >
    <div class="flex flex-col gap-5 sm:flex-row sm:items-center">
      <div class="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
        <UIcon :name="icon" class="size-5" aria-hidden="true" />
      </div>
      <div class="min-w-0 flex-1">
        <p class="text-xs font-semibold uppercase tracking-wider text-primary">Next best action</p>
        <p class="mt-3 text-sm font-medium text-toned">{{ actionLabel }}</p>
        <h2 id="next-best-action-title" class="mt-1 text-xl font-semibold tracking-tight sm:text-2xl">{{ action.title }}</h2>
        <p class="mt-3 max-w-2xl text-sm leading-6 text-muted">{{ action.description }}</p>
        <p v-if="contextText" class="mt-2 text-sm text-toned">{{ contextText }}</p>
      </div>
      <UButton
        :to="action.targetPath"
        class="w-full shrink-0 justify-center sm:w-auto"
        size="lg"
        :icon="icon"
      >
        {{ action.actionLabel }}
      </UButton>
    </div>
  </section>
</template>
