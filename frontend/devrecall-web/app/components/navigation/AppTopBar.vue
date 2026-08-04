<script setup lang="ts">
defineEmits<{ openMobileNavigation: []; openCommandPalette: []; openQuickCapture: [] }>()
const auth = useAuth()
const isMac = ref(false)
onMounted(() => { isMac.value = /Mac|iPhone|iPad/.test(navigator.platform) })
</script>

<template>
  <header data-testid="app-topbar" class="sticky top-0 z-30 flex h-[var(--devrecall-topbar-height)] items-center border-b border-default bg-default/90 px-3 backdrop-blur sm:px-4">
    <UButton class="lg:hidden" color="neutral" variant="ghost" icon="i-lucide-menu" aria-label="Open navigation" @click="$emit('openMobileNavigation')" />
    <div class="ml-2 lg:hidden"><NavigationAppLogo /></div>

    <button
      type="button"
      class="ml-3 hidden h-9 w-full max-w-md items-center gap-2 rounded-lg border border-default bg-elevated/40 px-3 text-left text-sm text-muted transition-colors hover:bg-elevated sm:flex lg:ml-0"
      aria-label="Open command palette"
      @click="$emit('openCommandPalette')"
    >
      <UIcon name="i-lucide-search" class="size-4" />
      <span class="flex-1">Search or run a command</span>
      <UKbd>{{ isMac ? '⌘' : 'Ctrl' }}</UKbd><UKbd>K</UKbd>
    </button>

    <div class="ml-auto flex items-center gap-1">
      <UButton class="sm:hidden" color="neutral" variant="ghost" icon="i-lucide-search" aria-label="Open search and commands" @click="$emit('openCommandPalette')" />
      <CoreThemeToggle />
      <UButton color="neutral" variant="ghost" icon="i-lucide-plus" aria-label="Quick capture" @click="$emit('openQuickCapture')" />
      <ClientOnly>
        <NavigationAppUserMenu :user="auth.user.value" />
        <template #fallback>
          <UButton color="neutral" variant="ghost" square disabled aria-label="Open user menu">
            <UAvatar alt="Current user" size="sm" />
          </UButton>
        </template>
      </ClientOnly>
    </div>
  </header>
</template>
