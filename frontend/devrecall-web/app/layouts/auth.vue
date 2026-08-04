<template>
  <div class="grid min-h-dvh lg:grid-cols-2">
    <section class="hidden border-r border-default bg-muted lg:block">
      <div class="flex h-full items-center justify-center p-12">
        <div class="max-w-md">
          <NuxtLink to="/" class="brand"><UIcon name="i-lucide-brain-circuit" /> DevRecall</NuxtLink>
          <p class="mt-12 text-sm font-medium text-primary">Developer Learning OS</p>
          <h1 class="mt-3 text-4xl font-semibold tracking-tight">Turn learning into a repeatable system.</h1>
        </div>
      </div>
    </section>
    <main class="flex items-center justify-center p-6"><div class="w-full max-w-md"><slot /></div></main>
  </div>
</template>

<style scoped>
.brand { display: flex; align-items: center; gap: .5rem; font-size: 1.15rem; font-weight: 700; }
</style>
