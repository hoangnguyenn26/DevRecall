<script setup lang="ts">
import PublicFooter from '~/features/marketing/components/PublicFooter.vue'
import PublicHeader from '~/features/marketing/components/PublicHeader.vue'
</script>

<template>
  <div class="min-h-dvh bg-default text-default">
    <a
      href="#main-content"
      class="fixed left-4 top-3 z-50 -translate-y-20 rounded-md bg-primary px-4 py-2 text-sm font-medium text-inverted transition-transform focus:translate-y-0"
      >Skip to content</a
    >
    <PublicHeader />
    <main id="main-content" tabindex="-1"><slot /></main>
    <PublicFooter />
  </div>
</template>
