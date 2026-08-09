<script setup lang="ts">
defineProps<{
  icon: string
  eyebrow: string
  title: string
  description: string
  points: readonly string[]
}>()
</script>

<template>
  <article class="group rounded-2xl border border-default bg-elevated p-6 transition-[border-color,box-shadow,transform] hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-xl hover:shadow-primary/5 sm:p-8">
    <div class="flex size-11 items-center justify-center rounded-xl border border-primary/15 bg-primary/10 text-primary transition-colors group-hover:bg-primary group-hover:text-inverted">
      <UIcon :name="icon" class="size-5" />
    </div>
    <p class="mt-7 text-sm font-semibold text-primary">{{ eyebrow }}</p>
    <h3 class="mt-2 text-xl font-semibold tracking-tight text-pretty sm:text-2xl">{{ title }}</h3>
    <p class="mt-4 leading-7 text-muted">{{ description }}</p>
    <ul class="mt-6 space-y-3 text-sm">
      <li v-for="point in points" :key="point" class="flex gap-3">
        <UIcon
          name="i-lucide-check"
          class="mt-0.5 size-4 shrink-0 text-success"
          aria-hidden="true"
        /><span>{{ point }}</span>
      </li>
    </ul>
  </article>
</template>
