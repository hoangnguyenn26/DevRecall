<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import { resourceRoute } from '~/utils/format'

interface SearchResult { resourceType: string; resourceId: string; title: string; preview?: string; rank: number; metadata: Record<string, string> }
const auth = useAuthStore()
const api = useApi()
const route = useRoute()
const collapsed = useCookie('devrecall-sidebar-collapsed', { default: () => false })
const mobileOpen = ref(false)
const commandOpen = ref(false)
const searchText = ref('')
const searchResults = ref<SearchResult[]>([])
const searchPending = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | undefined
let searchController: AbortController | undefined

const groups = [
  { label: '', items: [{ label: 'Today', to: '/app/today', icon: 'i-lucide-sparkles' }] },
  { label: 'Learn', items: [{ label: 'Knowledge', to: '/app/knowledge', icon: 'i-lucide-library' }] },
  { label: 'Practice', items: [
    { label: 'Review', to: '/app/review', icon: 'i-lucide-refresh-cw' },
    { label: 'Interview', to: '/app/interview', icon: 'i-lucide-messages-square' },
    { label: 'DSA', to: '/app/dsa', icon: 'i-lucide-code-xml' },
  ] },
  { label: 'Plan', items: [
    { label: 'Recommendations', to: '/app/recommendations', icon: 'i-lucide-lightbulb' },
    { label: 'Study Plans', to: '/app/study-plans', icon: 'i-lucide-list-checks' },
    { label: 'Study Sessions', to: '/app/study-sessions', icon: 'i-lucide-timer' },
  ] },
  { label: 'Insights', items: [
    { label: 'Analytics', to: '/app/analytics', icon: 'i-lucide-chart-no-axes-combined' },
    { label: 'Weak Topics', to: '/app/weak-topics', icon: 'i-lucide-triangle-alert' },
  ] },
  { label: 'System', items: [{ label: 'Settings', to: '/app/settings', icon: 'i-lucide-settings' }] },
]

const shortcuts = groups.flatMap(group => group.items)
async function signOut() { await auth.logout(); await navigateTo('/login') }
function onKeydown(event: KeyboardEvent) {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault(); commandOpen.value = !commandOpen.value
  }
}
onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
watch(searchText, (value) => {
  clearTimeout(searchTimer); searchController?.abort(); searchResults.value = []
  if (value.trim().length < 2) return
  searchTimer = setTimeout(async () => {
    searchController = new AbortController(); searchPending.value = true
    try { searchResults.value = (await api.get<PagedResponse<SearchResult>>('/search', { q: value.trim(), page: 1, pageSize: 12 }, searchController.signal)).items }
    catch (error) { if (!(error instanceof DOMException && error.name === 'AbortError')) throw error }
    finally { searchPending.value = false }
  }, 250)
})
useHead({ meta: [{ name: 'robots', content: 'noindex,nofollow' }] })
</script>

<template>
  <div class="shell" :class="{ collapsed }">
    <header class="topbar">
      <button class="icon-button mobile-only" aria-label="Open navigation" @click="mobileOpen = true"><UIcon name="i-lucide-menu" /></button>
      <NuxtLink to="/app/today" class="brand"><UIcon name="i-lucide-brain-circuit" /><span>DevRecall</span></NuxtLink>
      <button class="command-trigger" @click="commandOpen = true"><UIcon name="i-lucide-search" /><span>Search or run a command</span><kbd>Ctrl K</kbd></button>
      <div class="top-actions"><UColorModeButton /><span class="user-name">{{ auth.user?.displayName }}</span><UButton icon="i-lucide-log-out" color="neutral" variant="ghost" aria-label="Sign out" @click="signOut" /></div>
    </header>

    <aside class="sidebar" :class="{ 'mobile-open': mobileOpen }">
      <div class="mobile-nav-head"><strong>Navigation</strong><button class="icon-button" @click="mobileOpen = false"><UIcon name="i-lucide-x" /></button></div>
      <nav aria-label="Primary navigation">
        <section v-for="group in groups" :key="group.label">
          <p v-if="group.label && !collapsed" class="nav-label">{{ group.label }}</p>
          <NuxtLink v-for="item in group.items" :key="item.to" :to="item.to" :title="item.label" class="nav-item" :class="{ active: route.path.startsWith(item.to) }" @click="mobileOpen = false">
            <UIcon :name="item.icon" /><span v-if="!collapsed">{{ item.label }}</span>
          </NuxtLink>
        </section>
      </nav>
      <button class="collapse-button" @click="collapsed = !collapsed"><UIcon :name="collapsed ? 'i-lucide-panel-left-open' : 'i-lucide-panel-left-close'" /><span v-if="!collapsed">Collapse</span></button>
    </aside>
    <div v-if="mobileOpen" class="scrim" @click="mobileOpen = false" />

    <main class="workspace"><slot /></main>

    <div v-if="commandOpen" class="dialog-backdrop" @click.self="commandOpen = false">
      <section class="command-dialog" role="dialog" aria-modal="true" aria-label="Command palette">
        <div class="command-input"><UIcon name="i-lucide-search" /><input v-model="searchText" autofocus placeholder="Search knowledge, interview, and DSA…"><UIcon v-if="searchPending" name="i-lucide-loader-circle" class="animate-spin" /></div>
        <template v-if="searchResults.length"><p class="nav-label">Resources</p><NuxtLink v-for="result in searchResults" :key="`${result.resourceType}-${result.resourceId}`" :to="resourceRoute(result.resourceType, result.resourceId)" class="command-item result" @click="commandOpen = false"><CoreStatusBadge :value="result.resourceType" /><span><strong>{{ result.title }}</strong><small>{{ result.preview }}</small></span></NuxtLink></template>
        <template v-else><p class="nav-label">Navigate</p><NuxtLink v-for="item in shortcuts" :key="item.to" :to="item.to" class="command-item" @click="commandOpen = false"><UIcon :name="item.icon" />{{ item.label }}</NuxtLink></template>
        <p class="command-note">Use Ctrl/⌘ + K anywhere. Search requires at least two characters.</p>
      </section>
    </div>
  </div>
</template>

<style scoped>
.shell { --sidebar-width: 15rem; min-height: 100vh; padding: 4rem 0 0 var(--sidebar-width); }
.shell.collapsed { --sidebar-width: 4.25rem; }
.topbar { position: fixed; z-index: 30; inset: 0 0 auto 0; height: 4rem; display: grid; grid-template-columns: var(--sidebar-width) minmax(14rem, 36rem) 1fr; align-items: center; gap: 1rem; padding: 0 1rem; border-bottom: 1px solid var(--ui-border); background: color-mix(in srgb, var(--ui-bg) 92%, transparent); backdrop-filter: blur(14px); }
.brand { display: flex; align-items: center; gap: .55rem; font-weight: 750; }
.brand svg { color: var(--ui-primary); font-size: 1.35rem; }
.command-trigger { display: flex; align-items: center; gap: .55rem; width: 100%; padding: .5rem .7rem; border: 1px solid var(--ui-border); border-radius: .65rem; color: var(--ui-text-muted); background: var(--ui-bg-muted); text-align: left; }
.command-trigger span { flex: 1; } kbd { font-size: .72rem; }
.top-actions { justify-self: end; display: flex; align-items: center; gap: .5rem; }.user-name { font-size: .85rem; color: var(--ui-text-muted); }
.sidebar { position: fixed; z-index: 25; inset: 4rem auto 0 0; width: var(--sidebar-width); display: flex; flex-direction: column; padding: .75rem; border-right: 1px solid var(--ui-border); background: var(--ui-bg); overflow-y: auto; }
.sidebar nav { flex: 1; }.sidebar section + section { margin-top: .8rem; }.nav-label { padding: .4rem .6rem; color: var(--ui-text-dimmed); font-size: .68rem; font-weight: 700; letter-spacing: .09em; text-transform: uppercase; }
.nav-item, .collapse-button, .command-item { display: flex; align-items: center; gap: .65rem; min-height: 2.5rem; padding: .5rem .65rem; border-radius: .6rem; color: var(--ui-text-muted); font-size: .9rem; }
.nav-item:hover, .nav-item.active, .collapse-button:hover, .command-item:hover { color: var(--ui-text); background: var(--ui-bg-elevated); }.nav-item.active { color: var(--ui-primary); font-weight: 650; }
.nav-item svg, .collapse-button svg { flex: 0 0 auto; font-size: 1.05rem; }.collapse-button { width: 100%; }
.workspace { min-height: calc(100vh - 4rem); padding: clamp(1rem, 3vw, 2rem); }.workspace > :deep(*) { max-width: var(--devrecall-content-width); margin-inline: auto; }
.mobile-only, .mobile-nav-head, .scrim { display: none; }.icon-button { display: inline-grid; place-items: center; padding: .5rem; }
.dialog-backdrop { position: fixed; z-index: 100; inset: 0; display: grid; place-items: start center; padding: 12vh 1rem 1rem; background: rgb(2 6 23 / .55); }
.command-dialog { width: min(100%, 36rem); max-height: 70vh; overflow: auto; padding: .75rem; border: 1px solid var(--ui-border); border-radius: .9rem; background: var(--ui-bg); box-shadow: 0 24px 70px rgb(0 0 0 / .25); }
.command-input { display: flex; align-items: center; gap: .6rem; padding: .7rem; border-bottom: 1px solid var(--ui-border); }.command-input input { width: 100%; outline: 0; background: transparent; }.command-note { padding: .75rem; color: var(--ui-text-dimmed); font-size: .8rem; }
.command-item.result { align-items: start; }.command-item.result > span { display: grid; }.command-item.result small { max-width: 28rem; overflow: hidden; color: var(--ui-text-muted); font-size: .75rem; text-overflow: ellipsis; white-space: nowrap; }
@media (max-width: 800px) {
  .shell, .shell.collapsed { padding-left: 0; }.topbar { grid-template-columns: auto 1fr auto; }.mobile-only { display: inline-grid; }.topbar .brand span, .command-trigger span, .command-trigger kbd, .user-name { display: none; }.command-trigger { width: 2.5rem; justify-content: center; border: 0; background: transparent; }
  .sidebar { width: min(18rem, 85vw); transform: translateX(-105%); transition: transform .2s ease; box-shadow: 12px 0 36px rgb(0 0 0 / .2); }.sidebar.mobile-open { transform: translateX(0); }.sidebar .nav-item span, .sidebar .collapse-button span, .sidebar .nav-label { display: initial !important; }.mobile-nav-head { display: flex; align-items: center; justify-content: space-between; padding: .4rem .5rem .8rem; }.collapse-button { display: none; }.scrim { display: block; position: fixed; z-index: 20; inset: 4rem 0 0; background: rgb(2 6 23 / .45); }
}
</style>
