<script setup lang="ts">
defineProps<{ error: unknown; returnTo: string }>()
defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="m-auto w-full max-w-xl" role="alert"><CoreErrorState :error="error" @retry="$emit('retry')" /><div class="mt-4 text-center"><UButton :to="returnTo" color="neutral" variant="outline">Return to module</UButton></div></div>
</template>
