<template>
  <section class="setup-card" aria-labelledby="learning-profile-setup-title">
    <div>
      <p class="eyebrow">Personalize your learning</p>
      <h2 id="learning-profile-setup-title">Tell DevRecall what you're working toward</h2>
      <p>Set your role, technologies and goals so future learning suggestions can better match your direction.</p>
    </div>
    <UButton to="/app/settings/learning-profile" variant="soft" trailing-icon="i-lucide-arrow-right">
      Set up learning profile
    </UButton>
  </section>
</template>

<style scoped>
.setup-card{display:flex;align-items:center;justify-content:space-between;gap:1.25rem;border:1px solid var(--ui-border);border-radius:.85rem;background:var(--ui-bg-elevated);padding:1rem 1.1rem}.eyebrow{margin-bottom:.2rem;color:var(--ui-primary);font-size:.75rem;font-weight:750;text-transform:uppercase;letter-spacing:.06em}h2{font-weight:750}p:last-child{margin-top:.2rem;color:var(--ui-text-muted);font-size:.9rem}@media(max-width:640px){.setup-card{align-items:flex-start;flex-direction:column}}
</style>
