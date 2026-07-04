import { Navigate, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../context/AuthContext'
import type { UserRoleKey } from '../utils/roles'
import AccessDenied from './AccessDenied'

type ProtectedRouteProps = {
  allowedRoles?: UserRoleKey[]
  children: ReactNode
}

function ProtectedRoute({ allowedRoles, children }: ProtectedRouteProps) {
  const { isAuthenticated, userRoleKey } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (allowedRoles && (!userRoleKey || !allowedRoles.includes(userRoleKey))) {
    return <AccessDenied />
  }

  return children
}

export default ProtectedRoute
