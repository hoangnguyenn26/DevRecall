import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '~/types/api'
import type { CurrentUser } from '~/types/auth'
import { normalizeApiError } from '~/utils/normalize-api-error'
import { resolveSafeRedirect } from '~/utils/resolve-safe-redirect'
import { loginSchema, registerSchema } from '~/validation/auth'

const currentUser: CurrentUser = { id: 'user-1', email: 'learner@example.com', displayName: 'Learner' }
const get = vi.fn<(path: string) => Promise<CurrentUser>>()
const post = vi.fn<(path: string, body?: unknown) => Promise<CurrentUser | undefined>>()

vi.stubGlobal('useApi', () => ({ get, post }))
const { useAuthStore } = await import('~/stores/auth')

describe('authentication foundation', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    get.mockReset()
    post.mockReset()
  })

  it('stores the current user after a valid login', async () => {
    post.mockResolvedValue(currentUser)
    const auth = useAuthStore()
    await auth.login({ email: currentUser.email, password: 'Example123!' })
    expect(auth.user).toEqual(currentUser)
    expect(auth.status).toBe('authenticated')
  })

  it('does not authenticate a registration response because the backend does not create a session', async () => {
    post.mockResolvedValue(currentUser)
    const auth = useAuthStore()
    await auth.register({ email: currentUser.email, displayName: currentUser.displayName, password: 'Example123!' })
    expect(auth.status).toBe('unknown')
    expect(auth.user).toBeNull()
  })

  it('deduplicates concurrent session restoration', async () => {
    let resolveRequest!: (user: CurrentUser) => void
    get.mockReturnValue(new Promise(resolve => { resolveRequest = resolve }))
    const auth = useAuthStore()
    const first = auth.restore()
    const second = auth.restore()
    expect(get).toHaveBeenCalledTimes(1)
    resolveRequest(currentUser)
    await Promise.all([first, second])
    expect(auth.status).toBe('authenticated')
  })

  it('marks a 401 restore as anonymous', async () => {
    get.mockRejectedValue(new ApiError({ status: 401, title: 'Unauthorized' }))
    const auth = useAuthStore()
    await auth.restore()
    expect(auth.status).toBe('anonymous')
  })

  it('keeps status unknown when session verification fails', async () => {
    get.mockRejectedValue(new ApiError({ status: 503, title: 'Unavailable' }))
    const auth = useAuthStore()
    await expect(auth.restore()).rejects.toBeInstanceOf(ApiError)
    expect(auth.status).toBe('unknown')
  })

  it('clears the authenticated state only after logout succeeds', async () => {
    post.mockResolvedValueOnce(currentUser).mockResolvedValueOnce(undefined)
    const auth = useAuthStore()
    await auth.login({ email: currentUser.email, password: 'Example123!' })
    await auth.logout()
    expect(auth.status).toBe('anonymous')
    expect(auth.user).toBeNull()
  })

  it('accepts safe internal redirects and rejects external redirects', () => {
    expect(resolveSafeRedirect('/app/study-plans?status=Draft')).toBe('/app/study-plans?status=Draft')
    expect(resolveSafeRedirect('https://malicious.example')).toBe('/app')
    expect(resolveSafeRedirect('//malicious.example')).toBe('/app')
    expect(resolveSafeRedirect('/\\malicious.example')).toBe('/app')
    expect(resolveSafeRedirect('/login?redirect=/app')).toBe('/app')
  })

  it('validates login fields without duplicating backend authentication rules', () => {
    expect(loginSchema.safeParse({ email: 'invalid', password: '' }).success).toBe(false)
    expect(loginSchema.safeParse({ email: currentUser.email, password: 'anything' }).success).toBe(true)
  })

  it('blocks mismatched registration passwords and follows the backend length policy', () => {
    const base = { displayName: 'Learner', email: currentUser.email, password: '12345678' }
    expect(registerSchema.safeParse({ ...base, confirmPassword: 'different' }).success).toBe(false)
    expect(registerSchema.safeParse({ ...base, confirmPassword: base.password }).success).toBe(true)
    expect(registerSchema.safeParse({ ...base, password: 'short', confirmPassword: 'short' }).success).toBe(false)
  })

  it('normalizes the backend errorCode extension and validation errors', () => {
    const normalized = normalizeApiError(new ApiError({
      status: 409,
      title: 'Conflict',
      errorCode: 'IDENTITY_EMAIL_ALREADY_EXISTS',
      errors: { email: ['Already exists.'] },
    }))
    expect(normalized.code).toBe('IDENTITY_EMAIL_ALREADY_EXISTS')
    expect(normalized.fieldErrors.email).toEqual(['Already exists.'])
  })
})
