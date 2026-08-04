<script setup lang="ts">
const { online } = useNetworkStatus()
</script>

<template>
  <div v-if="!online" role="status" aria-live="polite" class="border-b border-warning/30 bg-warning/10 px-4 py-2 text-center text-sm text-warning">
    <UIcon name="i-lucide-wifi-off" class="mr-1 inline-block size-4" />
    You appear to be offline. Changes may not be saved.
  </div>
</template>
