import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  getCurrentUser,
  login as loginRequest,
  setAuthorizationToken,
} from '../api/propCareApi'
import type { AuthUserResponse } from '../types/api'
import {
  clearStoredUser,
  clearToken,
  getStoredUser,
  getToken,
  setStoredUser,
  setToken,
} from '../utils/authStorage'
import { getUserRoleKey, type UserRoleKey } from '../utils/roles'

type AuthContextValue = {
  user: AuthUserResponse | null
  token: string | null
  userRoleKey: UserRoleKey | null
  isAuthenticated: boolean
  isAdminOwner: boolean
  isPropertyManager: boolean
  isTenant: boolean
  isMaintenanceStaff: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  refreshCurrentUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setTokenState] = useState<string | null>(() => getToken())
  const [user, setUser] = useState<AuthUserResponse | null>(() => getStoredUser())

  useEffect(() => {
    setAuthorizationToken(token)
  }, [token])

  const userRoleKey = useMemo(() => getUserRoleKey(user?.role), [user])

  function persistAuth(nextToken: string, nextUser: AuthUserResponse) {
    setToken(nextToken)
    setStoredUser(nextUser)
    setTokenState(nextToken)
    setUser(nextUser)
    setAuthorizationToken(nextToken)
  }

  function logout() {
    clearToken()
    clearStoredUser()
    setTokenState(null)
    setUser(null)
    setAuthorizationToken(null)
  }

  async function login(email: string, password: string) {
    const response = await loginRequest({ email, password })
    if (!response.success || !response.token || !response.user) {
      throw new Error(response.message || 'Login failed.')
    }

    persistAuth(response.token, response.user)
  }

  async function refreshCurrentUser() {
    if (!token) {
      logout()
      return
    }

    try {
      const currentUser = await getCurrentUser()
      setStoredUser(currentUser)
      setUser(currentUser)
    } catch {
      logout()
    }
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      userRoleKey,
      isAuthenticated: Boolean(token && user),
      isAdminOwner: userRoleKey === 'AdminOwner',
      isPropertyManager: userRoleKey === 'PropertyManager',
      isTenant: userRoleKey === 'Tenant',
      isMaintenanceStaff: userRoleKey === 'MaintenanceStaff',
      login,
      logout,
      refreshCurrentUser,
    }),
    [token, user, userRoleKey],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }

  return value
}
