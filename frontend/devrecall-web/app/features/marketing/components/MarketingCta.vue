<script setup lang="ts">
withDefaults(defineProps<{ final?: boolean }>(), { final: false })
const auth = useAuth()
const action = computed(() =>
  auth.isAuthenticated.value
    ? { label: 'Open DevRecall', to: '/app' }
    : { label: 'Start learning', to: '/register' },
)
</script>

<template>
  <div :class="final ? 'text-center' : ''">
    <div class="flex flex-col gap-3 sm:flex-row" :class="final ? 'justify-center' : ''">
      <UButton :to="action.to" size="xl" trailing-icon="i-lucide-arrow-right">{{
        action.label
      }}</UButton>
      <UButton v-if="!final" to="#how-it-works" size="xl" color="neutral" variant="outline"
        >See how it works</UButton
      >
    </div>
  </div>
</template>
