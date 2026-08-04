export function useAuth() {
  const store = useAuthStore()

  return {
    user: computed(() => store.user),
    status: computed(() => !store.initialized ? 'unknown' : store.isAuthenticated ? 'authenticated' : 'anonymous'),
    isAuthenticated: computed(() => store.isAuthenticated),
    restoreSession: store.restore,
    clearSession: () => store.$patch({ user: null, initialized: true }),
  }
}
