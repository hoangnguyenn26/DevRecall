<template>
  <main class="auth-layout">
    <NuxtLink to="/" class="brand"><UIcon name="i-lucide-brain-circuit" /> DevRecall</NuxtLink>
    <section><slot /></section>
  </main>
</template>

<style scoped>
.auth-layout { min-height: 100vh; display: grid; place-items: center; align-content: center; gap: 2rem; padding: 1.5rem; background: radial-gradient(circle at top, color-mix(in srgb, var(--ui-primary) 10%, transparent), transparent 35%); }
.brand { display: flex; align-items: center; gap: .5rem; font-size: 1.15rem; font-weight: 700; } section { width: min(100%, 28rem); }
</style>
