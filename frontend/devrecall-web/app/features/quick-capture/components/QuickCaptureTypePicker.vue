<script setup lang="ts">
import { quickCaptureTypeMeta } from '../quick-capture.meta'
import type { QuickCaptureType } from '../quick-capture.types'
defineEmits<{ select: [type: QuickCaptureType] }>()
</script>

<template>
  <div class="grid gap-3" role="group" aria-label="Capture type">
    <button v-for="(meta, type, index) in quickCaptureTypeMeta" :key="type" type="button" :data-quick-capture-primary="index === 0 ? '' : undefined" class="flex items-center gap-3 rounded-xl border border-default p-4 text-left hover:border-primary" @click="$emit('select', type)">
      <UIcon :name="meta.icon" class="size-5 text-primary" />
      <span><strong class="block">{{ meta.label }}</strong><small class="text-muted">{{ meta.description }}</small></span>
    </button>
  </div>
</template>
