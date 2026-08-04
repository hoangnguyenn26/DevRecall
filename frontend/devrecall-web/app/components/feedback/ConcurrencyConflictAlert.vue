<script setup lang="ts">
withDefaults(defineProps<{ resourceLabel?: string }>(), { resourceLabel: 'item' })
defineEmits<{ reload: [] }>()
</script>

<template>
  <UAlert
    color="warning"
    variant="subtle"
    icon="i-lucide-refresh-cw"
    title="A newer version is available"
    :description="`This ${resourceLabel} changed after you opened it. Reload the latest version before trying again.`"
  >
    <template #actions>
      <UButton color="warning" variant="soft" size="sm" @click="$emit('reload')">Reload latest</UButton>
    </template>
  </UAlert>
</template>
