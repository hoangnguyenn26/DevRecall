<script setup lang="ts">
import type { PracticeShellContext } from '../practice.types'
import PracticeHeader from './PracticeHeader.vue'
defineProps<{ context: PracticeShellContext; busy?: boolean; wide?: boolean }>()
defineEmits<{ exit: [] }>()
</script>

<template>
  <div class="flex min-h-dvh flex-col bg-default">
    <PracticeHeader :context="context" :busy="busy" @exit="$emit('exit')" />
    <main class="flex min-h-0 flex-1 flex-col" :aria-busy="busy">
      <div class="mx-auto flex w-full flex-1 flex-col px-4 py-6 sm:px-6 sm:py-8" :class="wide ? 'max-w-7xl' : 'max-w-5xl'"><slot /></div>
    </main>
    <div v-if="$slots.actions" class="border-t border-default bg-default/95 px-4 py-3 backdrop-blur sm:sticky sm:bottom-0 sm:z-20 sm:px-6">
      <div class="mx-auto flex w-full max-w-5xl flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <slot name="shortcut-help" />
        <div class="flex w-full items-center gap-2 sm:ml-auto sm:w-auto"><slot name="actions" /></div>
      </div>
    </div>
  </div>
</template>
