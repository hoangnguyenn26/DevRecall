import { get, post } from './httpClient'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export function login(request: LoginRequest) {
  return post<CurrentUser>('/auth/login', request)
}

export function getCurrentUser() {
  return get<CurrentUser>('/auth/me')
}

export function logout() {
  return post<void>('/auth/logout')
}
