<script setup lang="ts">
import QuickCapturePanel from '~/features/quick-capture/components/QuickCapturePanel.vue'
import { useQuickCapture } from '~/features/quick-capture/useQuickCapture'
const {
  sidebarCollapsed,
  mobileNavigationOpen,
  toggleSidebar,
  openMobileNavigation,
  closeMobileNavigation,
} = useAppShell()
const { commandPaletteOpen, openCommandPalette } = useCommandPalette()
const quickCapture = useQuickCapture()
const route = useRoute()
const { pendingPrefix } = useNavigationShortcuts()
useGlobalShortcuts()
useHead({ meta: [{ name: 'robots', content: 'noindex,nofollow' }] })
</script>

<template>
  <div class="min-h-dvh overflow-x-clip bg-default">
    <div class="flex min-h-dvh">
      <NavigationAppSidebar
        class="hidden lg:flex"
        :collapsed="sidebarCollapsed"
        @toggle="toggleSidebar"
      />

      <div class="min-w-0 flex-1">
        <NavigationAppTopBar
          @open-mobile-navigation="openMobileNavigation"
          @open-command-palette="openCommandPalette()"
          @open-quick-capture="quickCapture.start()"
        />
        <FeedbackNetworkStatusBanner />
        <main class="min-h-[calc(100dvh-var(--devrecall-topbar-height))]" :class="route.meta.workspace ? 'p-2 lg:p-3' : 'px-4 py-6 sm:px-6 lg:px-8'">
          <div class="mx-auto w-full" :class="route.meta.workspace ? 'max-w-none' : 'max-w-[var(--devrecall-content-width)]'"><slot /></div>
        </main>
      </div>
    </div>

    <NavigationAppMobileNavigation
      :open="mobileNavigationOpen"
      @update:open="$event ? openMobileNavigation() : closeMobileNavigation()"
    />
    <NavigationAppCommandPalette v-model:open="commandPaletteOpen" />
    <QuickCapturePanel />

    <div
      v-if="pendingPrefix === 'g'"
      role="status"
      class="fixed bottom-6 left-1/2 z-50 -translate-x-1/2 rounded-lg border border-default bg-default px-4 py-2 text-sm shadow-lg"
    >
      <span class="text-muted">Go to:</span> T Today · K Knowledge · R Review · I Interview · D DSA
    </div>
  </div>
</template>
