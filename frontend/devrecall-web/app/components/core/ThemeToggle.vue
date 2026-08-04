<script setup lang="ts">
import { onMounted, ref } from 'vue'

const colorMode = useColorMode()
const isDark = ref(colorMode.value === 'dark')

onMounted(() => { isDark.value = document.documentElement.classList.contains('dark') })

function toggleTheme() {
  isDark.value = !isDark.value
  colorMode.preference = isDark.value ? 'dark' : 'light'
}
</script>

<template>
  <UButton
    color="neutral"
    variant="ghost"
    :icon="isDark ? 'i-lucide-moon' : 'i-lucide-sun'"
    :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
    @click="toggleTheme"
  />
</template>
