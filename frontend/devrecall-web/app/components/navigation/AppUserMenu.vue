<script setup lang="ts">
import type { CurrentUser } from '~/types/auth'

defineProps<{ user: CurrentUser | null }>()
const auth = useAuth()
const toast = useToast()
const signingOut = ref(false)

async function signOut(): Promise<void> {
  if (signingOut.value) return
  signingOut.value = true
  try {
    await auth.logout()
    await navigateTo('/login', { replace: true })
  } catch (error) {
    const normalized = normalizeApiError(error)
    if (normalized.status === 401) {
      auth.clearSession()
      await navigateTo('/login', { replace: true })
      return
    }
    toast.add({
      title: 'Unable to log out',
      description: normalized.detail ?? 'Try again in a moment.',
      color: 'error',
      duration: 7000,
    })
  } finally {
    signingOut.value = false
  }
}

const items = computed(() => [
  [{ label: auth.user.value?.displayName ?? auth.user.value?.email ?? 'Account', type: 'label' as const }],
  [
    { label: 'Settings', icon: 'i-lucide-settings', to: '/app/settings' },
    { label: 'Log out', icon: 'i-lucide-log-out', disabled: signingOut.value, onSelect: signOut },
  ],
])
</script>

<template>
  <UDropdownMenu :items="items">
    <UButton color="neutral" variant="ghost" square :loading="signingOut" aria-label="Open user menu">
      <UAvatar :alt="user?.displayName ?? user?.email ?? 'Current user'" size="sm" />
    </UButton>
  </UDropdownMenu>
</template>
