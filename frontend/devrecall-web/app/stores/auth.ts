import type { AuthStatus, CurrentUser, LoginRequest, RegisterRequest } from '~/types/auth'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { normalizeApiError } from '~/utils/normalize-api-error'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  const status = ref<AuthStatus>('unknown')
  const api = useApi()
  let restorePromise: Promise<void> | null = null

  const initialized = computed(() => status.value !== 'unknown')
  const isAuthenticated = computed(() => status.value === 'authenticated')

  async function performRestore(): Promise<void> {
    try {
      user.value = await api.get<CurrentUser>('/auth/me')
      status.value = 'authenticated'
    } catch (error) {
      const normalized = normalizeApiError(error)
      if (normalized.status === 401) {
        api.resetSecurityContext()
        clearSession()
        return
      }
      status.value = 'unknown'
      throw error
    }
  }

  async function restore(force = false): Promise<void> {
    if (!force && status.value !== 'unknown') return
    if (restorePromise) return await restorePromise
    restorePromise = performRestore()
    try {
      await restorePromise
    } finally {
      restorePromise = null
    }
  }

  async function login(request: LoginRequest): Promise<CurrentUser> {
    const currentUser = await api.post<CurrentUser>('/auth/login', request)
    api.resetSecurityContext()
    clearPersonalizedCache()
    user.value = currentUser
    status.value = 'authenticated'
    return currentUser
  }

  async function register(request: RegisterRequest): Promise<CurrentUser> {
    const currentUser = await api.post<CurrentUser>('/auth/register', request)
    api.resetSecurityContext()
    clearPersonalizedCache()
    user.value = currentUser
    status.value = 'authenticated'
    return currentUser
  }

  async function logout(): Promise<void> {
    await api.post<undefined>('/auth/logout')
    api.resetSecurityContext()
    clearSession()
  }

  function clearSession(): void {
    user.value = null
    status.value = 'anonymous'
    clearPersonalizedCache()
  }

  function clearPersonalizedCache(): void {
    if (typeof window !== 'undefined') {
      clearNuxtData()
      clearNuxtState()
    }
  }

  return { user, status, initialized, isAuthenticated, restore, login, register, logout, clearSession }
})
