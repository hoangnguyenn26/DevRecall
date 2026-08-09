<script setup lang="ts">
defineProps<{ id?: string; eyebrow?: string; title: string; description?: string }>()
</script>

<template>
  <section :id="id" class="scroll-mt-24 py-20 sm:py-28">
    <div class="max-w-3xl">
      <p v-if="eyebrow" class="text-sm font-semibold text-primary">{{ eyebrow }}</p>
      <h2 class="mt-3 text-3xl font-semibold tracking-tight text-pretty sm:text-5xl">
        {{ title }}
      </h2>
      <p v-if="description" class="mt-5 max-w-2xl text-base leading-7 text-muted sm:text-lg">
        {{ description }}
      </p>
    </div>
    <div class="mt-10"><slot /></div>
  </section>
</template>
