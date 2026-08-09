<script setup lang="ts">
defineProps<{
  id: string
  number: string
  eyebrow: string
  title: string
  description: string
  points: readonly string[]
}>()
</script>

<template>
  <section :id="id" class="scroll-mt-24 border-t border-default py-20 sm:py-28">
    <div class="grid gap-10 lg:grid-cols-[0.85fr_1.15fr] lg:gap-16">
      <div>
        <p class="font-mono text-xs font-semibold tracking-[0.16em] text-primary uppercase">
          {{ number }} · {{ eyebrow }}
        </p>
        <h2 class="mt-4 text-3xl font-semibold tracking-tight text-pretty sm:text-5xl">
          {{ title }}
        </h2>
        <p class="mt-5 text-base leading-7 text-muted sm:text-lg">{{ description }}</p>
        <ul class="mt-7 grid gap-3 text-sm sm:grid-cols-2 lg:grid-cols-1">
          <li v-for="point in points" :key="point" class="flex gap-3">
            <UIcon
              name="i-lucide-check"
              class="mt-0.5 size-4 shrink-0 text-success"
              aria-hidden="true"
            /><span>{{ point }}</span>
          </li>
        </ul>
      </div>
      <div class="self-center"><slot /></div>
    </div>
  </section>
</template>
