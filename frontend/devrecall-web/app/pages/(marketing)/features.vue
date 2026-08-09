<script setup lang="ts">
import MarketingContainer from '~/features/marketing/components/MarketingContainer.vue'

definePageMeta({ layout: 'public' })
usePageSeo({
  title: 'Features',
  description:
    'Explore how DevRecall connects knowledge, deliberate practice, reviews, learning insights, and focused study.',
  path: '/features',
})
</script>

<template>
  <MarketingContainer class="py-20 sm:py-28">
    <p class="text-sm font-semibold text-primary">DevRecall features</p>
    <h1 class="mt-4 max-w-3xl text-4xl font-semibold tracking-tight sm:text-6xl">
      One workspace for a learning loop you can continue.
    </h1>
    <p class="mt-6 max-w-2xl text-lg leading-8 text-muted">
      Capture what you learn, practice deliberately, understand recurring weak areas, and turn the
      evidence into a focused next action.
    </p>
    <UButton to="/register" size="xl" class="mt-8">Start learning</UButton>
  </MarketingContainer>
</template>
