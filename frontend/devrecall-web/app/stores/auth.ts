import type { CurrentUser, LoginRequest, RegisterRequest } from '~/types/auth'
import { ApiError } from '~/types/api'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  const initialized = ref(false)
  const api = useApi()

  async function restore() {
    if (initialized.value) return
    try {
      user.value = await api.get<CurrentUser>('/auth/me')
    } catch (error) {
      if (!(error instanceof ApiError) || error.problem.status !== 401) throw error
      user.value = null
    } finally {
      initialized.value = true
    }
  }

  async function login(request: LoginRequest) {
    user.value = await api.post<CurrentUser>('/auth/login', request)
    initialized.value = true
  }

  async function register(request: RegisterRequest) {
    user.value = await api.post<CurrentUser>('/auth/register', request)
    initialized.value = true
  }

  async function logout() {
    await api.post<undefined>('/auth/logout')
    user.value = null
    initialized.value = true
  }

  return { user, initialized, isAuthenticated: computed(() => user.value !== null), restore, login, register, logout }
})
