<script setup lang="ts">
import { computed } from 'vue'
import type { TodayNextAction } from '~/features/today/today.types'
import { getTodayActionIcon } from '~/features/today/today.meta'

const props = defineProps<{ action: TodayNextAction }>()
const icon = computed(() => getTodayActionIcon(props.action.icon))
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
        <h2 id="next-best-action-title" class="mt-2 text-xl font-semibold tracking-tight">{{ action.title }}</h2>
        <p class="mt-2 max-w-2xl text-sm leading-6 text-muted">{{ action.description }}</p>
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
