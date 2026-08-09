<script setup lang="ts">
import type { FormSubmitEvent } from '@nuxt/ui'
import { registerSchema, type RegisterFormState } from '~/validation/auth'

definePageMeta({ layout: 'auth', middleware: 'guest' })
usePageSeo({
  title: 'Create account',
  description: 'Create your private DevRecall learning workspace.',
  path: '/register',
  robots: 'noindex, nofollow',
})

const auth = useAuth()
const toast = useToast()
const state = reactive<RegisterFormState>({
  displayName: '',
  email: '',
  password: '',
  confirmPassword: '',
})
const submitting = ref(false)
const formError = ref<string | null>(null)
const serverFieldErrors = ref<Record<string, string[]>>({})

watch(
  () => state.email,
  () => {
    delete serverFieldErrors.value.email
  },
)

async function submit(event: FormSubmitEvent<RegisterFormState>): Promise<void> {
  if (submitting.value) return
  submitting.value = true
  formError.value = null
  serverFieldErrors.value = {}

  try {
    await auth.register({
      email: event.data.email,
      displayName: event.data.displayName,
      password: event.data.password,
    })
    toast.add({
      title: 'Account created',
      description: 'Log in to start using DevRecall.',
      color: 'success',
      duration: 3500,
    })
    await navigateTo({ path: '/login', query: { registered: 'true' } })
  } catch (error) {
    const normalized = normalizeApiError(error)
    serverFieldErrors.value = { ...normalized.fieldErrors }
    if (normalized.code === 'IDENTITY_EMAIL_ALREADY_EXISTS') {
      serverFieldErrors.value.email = ['An account with this email already exists.']
    } else if (!Object.keys(serverFieldErrors.value).length) {
      formError.value =
        normalized.status === 429
          ? 'Too many attempts. Wait a moment and try again.'
          : (normalized.detail ?? 'Unable to create the account right now.')
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <UCard>
    <template #header>
      <h1 class="text-2xl font-semibold tracking-tight">Create your workspace</h1>
      <p class="mt-2 text-sm text-muted">Your learning data stays under your account.</p>
    </template>

    <UAlert
      v-if="formError"
      class="mb-5"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Unable to create account"
      :description="formError"
    />
    <UForm :schema="registerSchema" :state="state" class="space-y-5" @submit="submit">
      <UFormField
        label="Display name"
        name="displayName"
        required
        :error="serverFieldErrors.displayName?.[0]"
      >
        <UInput v-model="state.displayName" autocomplete="name" class="w-full" />
      </UFormField>
      <UFormField label="Email" name="email" required :error="serverFieldErrors.email?.[0]">
        <UInput
          v-model="state.email"
          type="email"
          autocomplete="email"
          inputmode="email"
          placeholder="you@example.com"
          class="w-full"
        />
      </UFormField>
      <UFormField
        label="Password"
        name="password"
        required
        hint="Use 8–128 characters."
        :error="serverFieldErrors.password?.[0]"
      >
        <AuthPasswordInput v-model="state.password" autocomplete="new-password" />
      </UFormField>
      <UFormField label="Confirm password" name="confirmPassword" required>
        <AuthPasswordInput v-model="state.confirmPassword" autocomplete="new-password" />
      </UFormField>
      <UButton type="submit" block :loading="submitting" :disabled="submitting"
        >Create account</UButton
      >
    </UForm>

    <template #footer>
      <p class="text-center text-sm text-muted">
        Already registered?
        <NuxtLink to="/login" class="font-medium text-primary hover:underline">Log in</NuxtLink>
      </p>
    </template>
  </UCard>
</template>
