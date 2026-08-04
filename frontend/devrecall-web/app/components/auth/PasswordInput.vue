<script setup lang="ts">
const model = defineModel<string>({ required: true })
defineProps<{ autocomplete: 'current-password' | 'new-password' }>()
const visible = ref(false)
</script>

<template>
  <UInput v-model="model" :type="visible ? 'text' : 'password'" :autocomplete="autocomplete" class="w-full">
    <template #trailing>
      <UButton
        type="button"
        color="neutral"
        variant="ghost"
        size="xs"
        :icon="visible ? 'i-lucide-eye-off' : 'i-lucide-eye'"
        :aria-label="visible ? 'Hide password' : 'Show password'"
        @click="visible = !visible"
      />
    </template>
  </UInput>
</template>
