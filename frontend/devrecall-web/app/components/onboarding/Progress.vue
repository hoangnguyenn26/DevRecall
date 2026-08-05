<script setup lang="ts">
defineProps<{ currentStep: number; totalSteps: number }>()
</script>

<template>
  <div>
    <p class="text-sm text-muted" aria-live="polite">Step {{ currentStep }} of {{ totalSteps }}</p>
    <div class="mt-4 flex items-center gap-2" aria-label="Onboarding progress">
      <div v-for="index in totalSteps" :key="index" class="h-1.5 flex-1 rounded-full" :class="index <= currentStep ? 'bg-primary' : 'bg-elevated'" />
    </div>
  </div>
</template>
