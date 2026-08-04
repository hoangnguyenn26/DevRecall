<template>
  <div class="min-h-dvh bg-default text-default">
    <header class="border-b border-default">
      <CoreAppContainer size="wide" class="flex h-16 items-center">
        <NuxtLink to="/" class="font-semibold">DevRecall</NuxtLink>
        <nav class="ml-auto flex items-center gap-2" aria-label="Public navigation">
          <UButton to="/login" variant="ghost" color="neutral">Log in</UButton>
          <UButton to="/register">Get started</UButton>
        </nav>
      </CoreAppContainer>
    </header>
    <main><slot /></main>
  </div>
</template>
