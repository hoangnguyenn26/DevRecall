<script setup lang="ts">
import type { PracticeShellContext } from '../practice.types'
import PracticeProgress from './PracticeProgress.vue'
defineProps<{ context: PracticeShellContext; busy?: boolean }>()
defineEmits<{ exit: [] }>()
</script>

<template>
  <header class="border-b border-default bg-default">
    <div class="mx-auto flex h-14 w-full max-w-5xl items-center gap-3 px-4 sm:px-6">
      <UButton color="neutral" variant="ghost" icon="i-lucide-x" aria-label="Exit practice" :disabled="busy" @click="$emit('exit')" />
      <p class="min-w-0 flex-1 truncate text-sm font-medium">{{ context.title }}</p>
      <PracticeProgress v-if="context.progress" :progress="context.progress" />
    </div>
  </header>
</template>
