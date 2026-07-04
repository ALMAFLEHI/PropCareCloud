import type { AuthUserResponse } from '../types/api'

const tokenKey = 'propcare.auth.token'
const userKey = 'propcare.auth.user'

export function getToken(): string | null {
  return localStorage.getItem(tokenKey)
}

export function setToken(token: string) {
  localStorage.setItem(tokenKey, token)
}

export function clearToken() {
  localStorage.removeItem(tokenKey)
}

export function getStoredUser(): AuthUserResponse | null {
  const rawUser = localStorage.getItem(userKey)
  if (!rawUser) {
    return null
  }

  try {
    return JSON.parse(rawUser) as AuthUserResponse
  } catch {
    clearStoredUser()
    return null
  }
}

export function setStoredUser(user: AuthUserResponse) {
  localStorage.setItem(userKey, JSON.stringify(user))
}

export function clearStoredUser() {
  localStorage.removeItem(userKey)
}
