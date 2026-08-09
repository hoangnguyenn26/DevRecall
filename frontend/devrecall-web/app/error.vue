<script setup lang="ts">
const props = defineProps<{ error: { statusCode?: number; statusMessage?: string; message?: string } }>()
const title = computed(() => props.error.statusCode === 404 ? 'Page not found' : 'Something went wrong')
</script>

<template>
  <div class="flex min-h-dvh items-center justify-center p-6">
    <div class="max-w-md text-center">
      <p class="text-sm font-medium text-primary">{{ error.statusCode ?? 500 }}</p>
      <h1 class="mt-3 text-3xl font-semibold">{{ title }}</h1>
      <p class="mt-3 text-muted">{{ error.statusMessage ?? error.message }}</p>
      <UButton class="mt-8" to="/">Back to DevRecall</UButton>
    </div>
  </div>
</template>
