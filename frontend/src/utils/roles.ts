import type { AuthUserResponse, UserRole } from '../types/api'

export type UserRoleKey =
  | 'AdminOwner'
  | 'PropertyManager'
  | 'Tenant'
  | 'MaintenanceStaff'

const roleNumbers: Record<number, UserRoleKey> = {
  0: 'AdminOwner',
  1: 'PropertyManager',
  2: 'Tenant',
  3: 'MaintenanceStaff',
}

const roleLabels: Record<UserRoleKey, string> = {
  AdminOwner: 'Admin / Owner',
  PropertyManager: 'Property Manager',
  Tenant: 'Tenant',
  MaintenanceStaff: 'Maintenance Staff',
}

export function getUserRoleKey(role?: UserRole | null): UserRoleKey | null {
  if (role === null || role === undefined) {
    return null
  }

  if (typeof role === 'number') {
    return roleNumbers[role] ?? null
  }

  if (role === 'AdminOwner' || role === 'PropertyManager' || role === 'Tenant' || role === 'MaintenanceStaff') {
    return role
  }

  return null
}

export function getRoleDisplayName(role?: UserRole | null): string {
  const roleKey = getUserRoleKey(role)
  return roleKey ? roleLabels[roleKey] : 'Unknown role'
}

export function userHasRole(
  user: AuthUserResponse | null,
  allowedRoles: UserRoleKey[],
): boolean {
  const roleKey = getUserRoleKey(user?.role)
  return Boolean(roleKey && allowedRoles.includes(roleKey))
}
