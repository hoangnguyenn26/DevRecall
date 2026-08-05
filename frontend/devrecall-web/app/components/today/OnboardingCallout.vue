<template>
  <section class="rounded-xl border border-primary/20 bg-primary/5 p-4 sm:p-5">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center">
      <div class="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
        <UIcon name="i-lucide-compass" class="size-5" aria-hidden="true" />
      </div>
      <div class="min-w-0 flex-1">
        <h2 class="font-semibold">Personalize your learning workspace</h2>
        <p class="mt-1 text-sm text-muted">Choose your goals and weekly commitment so progress reflects how you want to study.</p>
      </div>
      <UButton to="/app/onboarding" class="w-full justify-center sm:w-auto">Set up DevRecall</UButton>
    </div>
  </section>
</template>
