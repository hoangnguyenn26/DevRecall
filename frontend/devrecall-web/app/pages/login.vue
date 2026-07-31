<script setup lang="ts">
definePageMeta({ layout: 'auth' })
useSeoMeta({ title: 'Sign in', robots: 'noindex' })
const auth = useAuthStore(); const route = useRoute(); const email = ref(''); const password = ref(''); const pending = ref(false); const error = ref<unknown>()
async function submit() { pending.value = true; error.value = undefined; try { await auth.login({ email: email.value, password: password.value }); await navigateTo(typeof route.query.redirect === 'string' ? route.query.redirect : '/app/today') } catch (caught) { error.value = caught } finally { pending.value = false } }
</script>
<template><UCard><template #header><h1>Welcome back</h1><p>Continue your learning loop.</p></template><form class="form" @submit.prevent="submit"><UFormField label="Email" required><UInput v-model="email" type="email" autocomplete="email" class="w-full" /></UFormField><UFormField label="Password" required><UInput v-model="password" type="password" autocomplete="current-password" class="w-full" /></UFormField><p v-if="error" class="form-error">{{ error instanceof Error ? error.message : 'Unable to sign in.' }}</p><UButton type="submit" block :loading="pending" label="Sign in" /></form><template #footer><p>New to DevRecall? <NuxtLink to="/register">Create an account</NuxtLink></p></template></UCard></template>
<style scoped>h1 { font-size: 1.5rem; font-weight: 700; }h1 + p, :deep(.u-card-footer) { color: var(--ui-text-muted); }.form { display: grid; gap: 1rem; }.form-error { color: var(--ui-error); font-size: .85rem; }a { color: var(--ui-primary); font-weight: 600; }</style>
