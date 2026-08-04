export interface CurrentUser {
  id: string
  email: string
  displayName: string
}

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous'
export interface LoginRequest { email: string; password: string }
export interface RegisterRequest { email: string; displayName: string; password: string }
