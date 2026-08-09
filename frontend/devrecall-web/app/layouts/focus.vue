<script setup lang="ts">
useSeoMeta({ robots: 'noindex, nofollow' })
const { setShortcutScope } = useShortcutScope(); const { closeCommandPalette } = useCommandPalette()
onMounted(() => { closeCommandPalette(); setShortcutScope('focus') })
onBeforeUnmount(() => setShortcutScope('global'))
</script>

<template>
  <div class="min-h-dvh bg-default text-default">
    <slot />
  </div>
</template>
