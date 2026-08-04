export function useAppShell() {
  const sidebarCollapsed = useCookie<boolean>('devrecall-sidebar-collapsed', {
    default: () => false,
    sameSite: 'lax',
  })
  const mobileNavigationOpen = useState('app-shell:mobile-navigation-open', () => false)

  function toggleSidebar(): void {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }

  function openMobileNavigation(): void {
    mobileNavigationOpen.value = true
  }

  function closeMobileNavigation(): void {
    mobileNavigationOpen.value = false
  }

  return { sidebarCollapsed, mobileNavigationOpen, toggleSidebar, openMobileNavigation, closeMobileNavigation }
}
