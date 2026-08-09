<script setup lang="ts">
import MarketingContainer from './MarketingContainer.vue'
import { primaryPublicCta } from '../marketing.constants'
</script>

<template>
  <footer class="border-t border-default">
    <MarketingContainer class="grid gap-8 py-10 sm:grid-cols-[1fr_auto_auto]">
      <div>
        <NuxtLink to="/" class="font-semibold tracking-tight">DevRecall</NuxtLink>
        <p class="mt-2 max-w-sm text-sm text-muted">
          A learning workspace for developers who want to remember, practice, and keep moving.
        </p>
      </div>
      <nav aria-label="Product links">
        <p class="text-sm font-medium">Product</p>
        <NuxtLink to="/features" class="mt-3 block text-sm text-muted hover:text-default"
          >Features</NuxtLink
        >
      </nav>
      <nav aria-label="Account links">
        <p class="text-sm font-medium">Account</p>
        <NuxtLink to="/login" class="mt-3 block text-sm text-muted hover:text-default"
          >Sign in</NuxtLink
        ><NuxtLink
          :to="primaryPublicCta.to"
          class="mt-2 block text-sm text-muted hover:text-default"
          >Register</NuxtLink
        >
      </nav>
      <p class="text-sm text-muted sm:col-span-3">© {{ new Date().getFullYear() }} DevRecall</p>
    </MarketingContainer>
  </footer>
</template>
