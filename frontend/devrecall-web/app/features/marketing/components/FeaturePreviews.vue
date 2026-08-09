<script setup lang="ts">
defineProps<{ kind: 'knowledge' | 'practice' | 'study' | 'insights' | 'today' }>()
</script>

<template>
  <div
    class="rounded-2xl border border-default bg-elevated p-4 shadow-xl shadow-primary/5 sm:p-6"
    aria-hidden="true"
  >
    <template v-if="kind === 'knowledge'">
      <div class="grid gap-3 sm:grid-cols-[0.9fr_1.1fr]">
        <div class="rounded-xl border border-default bg-default p-4 text-sm">
          <p class="font-semibold">Knowledge</p>
          <div class="mt-4 space-y-2 text-muted">
            <p>Backend</p>
            <p class="pl-3">├ ASP.NET Core</p>
            <p class="pl-6 text-default">├ Dependency Injection</p>
            <p class="pl-6">├ Middleware</p>
            <p class="pl-6">└ Authentication</p>
            <p class="pl-3">└ EF Core</p>
          </div>
        </div>
        <div class="rounded-xl border border-default bg-default p-4">
          <p class="text-xs font-semibold text-primary">ASP.NET Core</p>
          <p class="mt-3 text-lg font-semibold">Dependency Injection</p>
          <p class="mt-3 text-sm text-muted">
            Register services by lifetime and resolve dependencies through constructors.
          </p>
          <div class="mt-5 flex flex-wrap gap-2">
            <span class="rounded-full bg-muted px-2 py-1 text-xs">Architecture</span
            ><span class="rounded-full bg-muted px-2 py-1 text-xs">Backend</span>
          </div>
          <p class="mt-5 text-xs text-muted">Related · Service Lifetimes · Dependency Inversion</p>
        </div>
      </div>
    </template>

    <template v-else-if="kind === 'practice'">
      <div class="grid gap-3 md:grid-cols-3">
        <div class="rounded-xl border border-default bg-default p-4">
          <p class="text-xs font-semibold text-primary">REVIEW</p>
          <p class="mt-5 font-medium">When should a scoped service be used?</p>
          <div class="mt-6 rounded-lg bg-muted px-3 py-2 text-center text-sm text-muted">
            Reveal answer
          </div>
        </div>
        <div class="rounded-xl border border-default bg-default p-4">
          <p class="text-xs font-semibold text-primary">INTERVIEW</p>
          <p class="mt-5 font-medium">Explain dependency injection.</p>
          <p class="mt-4 text-xs text-muted">Your answer · Reference answer</p>
          <p class="mt-6 text-sm">
            Self-rating: <span class="font-semibold text-success">Good</span>
          </p>
        </div>
        <div class="rounded-xl border border-default bg-default p-4">
          <p class="text-xs font-semibold text-primary">DSA</p>
          <p class="mt-5 font-medium">Number of Islands</p>
          <dl class="mt-4 grid grid-cols-2 gap-2 text-xs">
            <dt class="text-muted">Outcome</dt>
            <dd>Solved</dd>
            <dt class="text-muted">Time</dt>
            <dd>22m</dd>
            <dt class="text-muted">Complexity</dt>
            <dd>O(n)</dd>
          </dl>
        </div>
      </div>
    </template>

    <template v-else-if="kind === 'study'">
      <div class="rounded-xl border border-default bg-default p-5">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-xs font-semibold text-primary">STUDY PLAN</p>
            <p class="mt-2 text-lg font-semibold">Backend Interview Preparation</p>
          </div>
          <span class="rounded-full bg-primary/10 px-2 py-1 text-xs text-primary">~60 min</span>
        </div>
        <ol class="mt-6 space-y-3 text-sm">
          <li class="grid grid-cols-[auto_1fr_auto] gap-3">
            <span>1</span><span>Dependency Injection · Interview</span
            ><span class="text-muted">15m</span>
          </li>
          <li class="grid grid-cols-[auto_1fr_auto] gap-3">
            <span>2</span><span>EF Core Tracking · Knowledge</span
            ><span class="text-muted">15m</span>
          </li>
          <li class="grid grid-cols-[auto_1fr_auto] gap-3">
            <span>3</span><span>Number of Islands · DSA</span><span class="text-muted">30m</span>
          </li>
        </ol>
        <div
          class="mt-6 rounded-lg bg-primary px-4 py-2 text-center text-sm font-medium text-inverted"
        >
          Start study
        </div>
        <p class="mt-4 text-center text-xs text-muted">
          Session 2 of 3 · Continue Number of Islands
        </p>
      </div>
    </template>

    <template v-else-if="kind === 'insights'">
      <div class="grid gap-3 sm:grid-cols-[1.1fr_0.9fr]">
        <div class="rounded-xl border border-default bg-default p-5">
          <p class="text-xs font-semibold text-warning">NEEDS ATTENTION</p>
          <p class="mt-2 text-lg font-semibold">Dependency Injection</p>
          <p class="mt-5 text-xs font-semibold text-muted">WHY</p>
          <ul class="mt-2 space-y-2 text-sm">
            <li>· 3 difficult reviews</li>
            <li>· 2 Fair interview self-ratings</li>
          </ul>
          <div class="mt-5 rounded-lg bg-primary/10 p-3 text-sm">
            <span class="text-xs text-primary">RECOMMENDED</span>
            <p class="mt-1 font-medium">Practice Service Lifetimes</p>
          </div>
        </div>
        <div class="rounded-xl border border-default bg-default p-5">
          <p class="text-xs text-muted">INTERVIEW PRACTICE</p>
          <p class="mt-4 text-2xl font-semibold">6 / 9</p>
          <p class="text-sm text-success">Good or Strong</p>
          <div class="mt-6 flex h-20 items-end gap-2">
            <span class="h-1/3 flex-1 rounded-t bg-primary/25" /><span
              class="h-1/2 flex-1 rounded-t bg-primary/35"
            /><span class="h-2/3 flex-1 rounded-t bg-primary/55" /><span
              class="h-full flex-1 rounded-t bg-primary"
            />
          </div>
          <p class="mt-3 text-xs text-muted">Previous period · 2 / 8</p>
        </div>
      </div>
    </template>

    <template v-else>
      <div class="rounded-xl border border-default bg-default p-5">
        <p class="text-xs font-semibold tracking-[0.16em] text-muted">TODAY</p>
        <div class="mt-5 rounded-xl border border-primary/30 bg-primary/5 p-5">
          <p class="text-xs font-semibold text-primary">NEXT BEST ACTION</p>
          <p class="mt-2 text-lg font-semibold">Continue Backend Interview Preparation</p>
          <p class="mt-2 text-sm text-muted">2 of 4 items completed</p>
          <div class="mt-4 h-2 overflow-hidden rounded-full bg-muted">
            <div class="h-full w-1/2 rounded-full bg-primary" />
          </div>
        </div>
        <div class="mt-3 grid grid-cols-2 gap-3">
          <div class="rounded-lg bg-muted p-4">
            <p class="text-xs text-muted">Reviews due</p>
            <p class="mt-1 text-2xl font-semibold">6</p>
          </div>
          <div class="rounded-lg bg-muted p-4">
            <p class="text-xs text-muted">Needs attention</p>
            <p class="mt-1 font-semibold">Dependency Injection</p>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
