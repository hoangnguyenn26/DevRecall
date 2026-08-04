<script setup lang="ts">
import type { CurrentUser } from '~/types/auth'

defineProps<{ user: CurrentUser | null }>()
const auth = useAuthStore()
const signingOut = ref(false)

async function signOut(): Promise<void> {
  signingOut.value = true
  try {
    await auth.logout()
    await navigateTo('/login')
  } finally {
    signingOut.value = false
  }
}

const items = computed(() => [
  [{ label: auth.user?.displayName ?? auth.user?.email ?? 'Account', type: 'label' as const }],
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
