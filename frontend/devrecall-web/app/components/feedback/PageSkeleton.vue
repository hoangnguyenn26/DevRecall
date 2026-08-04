<template>
  <div class="space-y-6" aria-label="Loading page" aria-busy="true">
    <div class="space-y-3">
      <USkeleton class="h-8 w-56" />
      <USkeleton class="h-4 w-full max-w-xl" />
    </div>
    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <USkeleton v-for="index in 4" :key="index" class="h-28 rounded-xl" />
    </div>
    <USkeleton class="h-80 rounded-xl" />
  </div>
</template>
