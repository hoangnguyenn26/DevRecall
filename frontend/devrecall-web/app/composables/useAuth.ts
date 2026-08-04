export function useAuth() {
  const store = useAuthStore()

  return {
    user: computed(() => store.user),
    status: computed(() => store.status),
    isAuthenticated: computed(() => store.isAuthenticated),
    restoreSession: store.restore,
    login: store.login,
    register: store.register,
    logout: store.logout,
    clearSession: store.clearSession,
  }
}
