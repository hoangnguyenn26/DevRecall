<template>
  <div class="relative mx-auto hidden w-full max-w-lg lg:block" aria-hidden="true">
    <div class="absolute -inset-10 rounded-full bg-primary/10 blur-3xl" />
    <div class="relative overflow-hidden rounded-3xl border border-default bg-elevated/90 p-3 shadow-2xl shadow-primary/10 backdrop-blur">
      <div class="rounded-2xl border border-default bg-default p-5">
        <div class="flex items-center justify-between gap-4">
          <div>
            <p class="font-mono text-[0.65rem] font-semibold tracking-[0.18em] text-primary uppercase">Learning system</p>
            <p class="mt-2 text-lg font-semibold">One history. One next action.</p>
          </div>
          <span class="flex size-10 items-center justify-center rounded-xl bg-primary text-inverted">
            <UIcon name="i-lucide-brain-circuit" class="size-5" />
          </span>
        </div>

        <div class="relative mt-7 grid grid-cols-2 gap-3">
          <div class="absolute left-1/2 top-8 h-[calc(100%-4rem)] w-px -translate-x-1/2 bg-default" />
          <div
            v-for="(step, index) in [
              ['Capture', 'Knowledge'],
              ['Practice', 'Recall & solve'],
              ['Understand', 'Learning signals'],
              ['Focus', 'Plan & session'],
            ]"
            :key="step[0]"
            class="relative rounded-xl border border-default bg-elevated p-4"
          >
            <span class="font-mono text-[0.65rem] font-semibold text-primary">0{{ index + 1 }}</span>
            <p class="mt-3 text-sm font-semibold">{{ step[0] }}</p>
            <p class="mt-1 text-xs text-muted">{{ step[1] }}</p>
          </div>
        </div>

        <div class="mt-3 rounded-xl border border-primary/25 bg-primary/5 p-4">
          <div class="flex items-center justify-between gap-4">
            <div>
              <p class="text-[0.65rem] font-semibold tracking-[0.16em] text-primary uppercase">Next best action</p>
              <p class="mt-2 text-sm font-semibold">Continue backend interview prep</p>
            </div>
            <UIcon name="i-lucide-arrow-up-right" class="size-5 shrink-0 text-primary" />
          </div>
          <div class="mt-4 h-1.5 overflow-hidden rounded-full bg-muted">
            <div class="h-full w-3/5 rounded-full bg-primary" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
