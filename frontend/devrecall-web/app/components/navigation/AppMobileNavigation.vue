<script setup lang="ts">
defineProps<{ open: boolean }>()
const emit = defineEmits<{ 'update:open': [value: boolean] }>()
</script>

<template>
  <USlideover :open="open" side="left" title="Navigation" @update:open="emit('update:open', $event)">
    <template #body>
      <NavigationAppSidebar :collapsed="false" :show-toggle="false" class="h-full w-full border-r-0" @navigate="emit('update:open', false)" />
    </template>
  </USlideover>
</template>
