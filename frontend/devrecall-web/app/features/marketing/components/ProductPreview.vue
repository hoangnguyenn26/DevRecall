<template>
  <figure class="mx-auto max-w-6xl">
    <div
      class="overflow-hidden rounded-2xl border border-default bg-elevated p-1.5 shadow-2xl shadow-primary/10 sm:p-2"
    >
      <div class="flex h-8 items-center gap-1.5 border-b border-default px-3" aria-hidden="true">
        <span class="size-2.5 rounded-full bg-error/70" /><span
          class="size-2.5 rounded-full bg-warning/70"
        /><span class="size-2.5 rounded-full bg-success/70" />
        <span class="ml-3 text-[0.65rem] text-muted">DevRecall · Today</span>
      </div>
      <img
        src="/images/devrecall-today-dashboard.png"
        alt="DevRecall Today workspace showing the next study action, recommendations, and weak topics."
        width="1294"
        height="856"
        fetchpriority="high"
        class="aspect-[1294/856] w-full bg-default object-cover object-top"
      >
    </div>
    <figcaption class="mt-4 text-center text-sm text-muted">
      Illustrative view of Today, the action-first starting point for a learning session.
    </figcaption>
  </figure>
</template>
