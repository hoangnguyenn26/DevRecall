<script setup lang="ts">
type ContainerSize = 'reading' | 'standard' | 'wide' | 'full'

const props = withDefaults(defineProps<{ size?: ContainerSize }>(), { size: 'standard' })
const sizeClass: Record<ContainerSize, string> = {
  reading: 'max-w-[46rem]',
  standard: 'max-w-6xl',
  wide: 'max-w-[90rem]',
  full: 'max-w-none',
}
</script>

<template>
  <div class="mx-auto w-full px-4 sm:px-6 lg:px-8" :class="sizeClass[props.size]">
    <slot />
  </div>
</template>
