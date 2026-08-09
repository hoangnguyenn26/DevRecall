<script setup lang="ts">
import MarketingContainer from './MarketingContainer.vue'
import { primaryPublicCta, publicNavigation } from '../marketing.constants'

const mobileMenuOpen = ref(false)
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-default bg-default/90 backdrop-blur">
    <MarketingContainer class="flex h-16 items-center gap-4">
      <NuxtLink
        to="/"
        class="flex items-center gap-2 rounded-md font-semibold tracking-tight"
        aria-label="DevRecall home"
      >
        <span
          class="flex size-8 items-center justify-center rounded-lg bg-primary text-sm font-semibold text-inverted"
          aria-hidden="true"
          >D</span
        ><span>DevRecall</span>
      </NuxtLink>
      <nav class="ml-8 hidden items-center md:flex" aria-label="Public navigation">
        <UButton
          v-for="item in publicNavigation"
          :key="item.to"
          :to="item.to"
          color="neutral"
          variant="ghost"
          >{{ item.label }}</UButton
        >
      </nav>
      <div class="ml-auto hidden items-center gap-2 md:flex">
        <CoreThemeToggle />
        <UButton to="/login" color="neutral" variant="ghost">Sign in</UButton>
        <UButton :to="primaryPublicCta.to">{{ primaryPublicCta.label }}</UButton>
      </div>
      <UButton
        class="ml-auto md:hidden"
        color="neutral"
        variant="ghost"
        icon="i-lucide-menu"
        aria-label="Open navigation menu"
        @click="mobileMenuOpen = true"
      />
    </MarketingContainer>

    <USlideover
      v-model:open="mobileMenuOpen"
      title="Navigation"
      description="Explore DevRecall or open your learning workspace."
    >
      <template #body>
        <nav class="flex flex-col gap-2" aria-label="Mobile public navigation">
          <UButton
            v-for="item in publicNavigation"
            :key="item.to"
            :to="item.to"
            color="neutral"
            variant="ghost"
            block
            class="justify-start"
            @click="mobileMenuOpen = false"
            >{{ item.label }}</UButton
          >
          <USeparator class="my-2" />
          <UButton
            to="/login"
            color="neutral"
            variant="outline"
            block
            @click="mobileMenuOpen = false"
            >Sign in</UButton
          >
          <UButton :to="primaryPublicCta.to" block @click="mobileMenuOpen = false">{{
            primaryPublicCta.label
          }}</UButton>
          <div class="mt-2 flex justify-end"><CoreThemeToggle /></div>
        </nav>
      </template>
    </USlideover>
  </header>
</template>
