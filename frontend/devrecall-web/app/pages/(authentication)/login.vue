<script setup lang="ts">
import type { FormSubmitEvent } from '@nuxt/ui'
import { loginSchema, type LoginFormState } from '~/validation/auth'

definePageMeta({ layout: 'auth', middleware: 'guest' })
usePageSeo({
  title: 'Log in',
  description: 'Continue your DevRecall learning system.',
  path: '/login',
  robots: 'noindex, nofollow',
})

const route = useRoute()
const auth = useAuth()
const { resolveAuthenticatedDestination } = useAuthNavigation()
const toast = useToast()
const state = reactive<LoginFormState>({
  email: typeof route.query.email === 'string' ? route.query.email : '',
  password: '',
})
const submitting = ref(false)
const formError = ref<string | null>(null)

async function submit(event: FormSubmitEvent<LoginFormState>): Promise<void> {
  if (submitting.value) return
  submitting.value = true
  formError.value = null

  try {
    await auth.login(event.data)
    toast.add({
      title: 'Signed in',
      description: 'Welcome back to DevRecall.',
      color: 'success',
      duration: 3500,
    })
    await navigateTo(await resolveAuthenticatedDestination(route.query.returnTo))
  } catch (error) {
    const normalized = normalizeApiError(error)
    if (normalized.status === 401 || normalized.code === 'IDENTITY_INVALID_CREDENTIALS') {
      formError.value = 'The email or password is incorrect.'
      state.password = ''
    } else if (normalized.status === 429) {
      formError.value = 'Too many attempts. Wait a moment and try again.'
    } else {
      formError.value = normalized.detail ?? 'Unable to log in right now.'
      toast.add({
        title: normalized.title,
        description: normalized.traceId ? `Reference: ${normalized.traceId}` : undefined,
        color: 'error',
        duration: 7000,
      })
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <UCard>
    <template #header>
      <h1 class="text-2xl font-semibold tracking-tight">Welcome back</h1>
      <p class="mt-2 text-sm text-muted">Continue your learning system.</p>
    </template>

    <UAlert
      v-if="formError"
      class="mb-5"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Unable to log in"
      :description="formError"
      aria-live="assertive"
    />

    <UForm :schema="loginSchema" :state="state" class="space-y-5" @submit="submit">
      <UFormField label="Email" name="email" required>
        <UInput
          v-model="state.email"
          type="email"
          autocomplete="email"
          inputmode="email"
          placeholder="you@example.com"
          autofocus
          class="w-full"
        />
      </UFormField>
      <UFormField label="Password" name="password" required>
        <AuthPasswordInput v-model="state.password" autocomplete="current-password" />
      </UFormField>
      <UButton type="submit" block :loading="submitting" :disabled="submitting">Log in</UButton>
    </UForm>

    <template #footer>
      <p class="text-center text-sm text-muted">
        New to DevRecall?
        <NuxtLink to="/register" class="font-medium text-primary hover:underline"
          >Create an account</NuxtLink
        >
      </p>
    </template>
  </UCard>
</template>
