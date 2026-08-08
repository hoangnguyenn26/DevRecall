<script setup lang="ts">
import type { PracticeShortcut } from '../practice.types'
defineProps<{ shortcuts: PracticeShortcut[] }>()
const open = defineModel<boolean>('open', { default: false })
</script>

<template>
  <div>
    <UButton color="neutral" variant="ghost" icon="i-lucide-keyboard" aria-label="Practice keyboard shortcuts" @click="open = true"><span class="hidden sm:inline">Press ? for shortcuts</span></UButton>
    <UModal v-model:open="open" title="Keyboard shortcuts" description="Available in the current practice step.">
      <template #body><dl class="space-y-3"><div v-for="shortcut in shortcuts.filter(item => item.enabled())" :key="shortcut.id" class="flex items-center justify-between gap-4"><dt class="text-sm">{{ shortcut.label }}</dt><dd class="flex gap-1"><UKbd v-for="key in shortcut.keys" :key="key">{{ key }}</UKbd></dd></div></dl></template>
    </UModal>
  </div>
</template>
