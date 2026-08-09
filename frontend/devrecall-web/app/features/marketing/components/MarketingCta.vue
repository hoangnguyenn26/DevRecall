<script setup lang="ts">
import { primaryPublicCta } from '../marketing.constants'

withDefaults(defineProps<{ final?: boolean }>(), { final: false })
</script>

<template>
  <div :class="final ? 'text-center' : ''">
    <div class="flex flex-col gap-3 sm:flex-row" :class="final ? 'justify-center' : ''">
      <UButton :to="primaryPublicCta.to" size="xl" trailing-icon="i-lucide-arrow-right">{{
        primaryPublicCta.label
      }}</UButton>
      <UButton v-if="!final" to="#how-it-works" size="xl" color="neutral" variant="outline"
        >See how it works</UButton
      >
    </div>
  </div>
</template>
