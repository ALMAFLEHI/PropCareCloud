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

export function isAdminOwner(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['AdminOwner'])
}

export function isPropertyManager(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['PropertyManager'])
}

export function isTenant(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['Tenant'])
}

export function isMaintenanceStaff(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['MaintenanceStaff'])
}

export function isAdminOrManager(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['AdminOwner', 'PropertyManager'])
}

export function canManageProperties(user: AuthUserResponse | null): boolean {
  return isAdminOrManager(user)
}

export function canManageUsers(user: AuthUserResponse | null): boolean {
  return isAdminOwner(user)
}

export function canManageTenantRegistrations(user: AuthUserResponse | null): boolean {
  return isAdminOrManager(user)
}

export function canAssignRequests(user: AuthUserResponse | null): boolean {
  return isAdminOrManager(user)
}

export function canCreateMaintenanceRequest(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['AdminOwner', 'PropertyManager', 'Tenant'])
}

export function canCreateTenantRequest(user: AuthUserResponse | null): boolean {
  return isTenant(user)
}

export function canUpdateRequestStatus(user: AuthUserResponse | null): boolean {
  return userHasRole(user, ['AdminOwner', 'PropertyManager', 'MaintenanceStaff'])
}

export function canUpdateAnyRequestStatus(user: AuthUserResponse | null): boolean {
  return isAdminOrManager(user)
}

export function canUpdateAssignedWorkStatus(user: AuthUserResponse | null): boolean {
  return isMaintenanceStaff(user)
}

export function getAllowedStatusValues(user: AuthUserResponse | null): string[] {
  if (isMaintenanceStaff(user)) {
    return ['3', '4']
  }

  if (isAdminOrManager(user)) {
    return ['0', '1', '2', '3', '4', '5']
  }

  return []
}
