export type HealthResponse = {
  status: string
  service: string
  timestampUtc: string
}

export type SystemInfoResponse = {
  applicationName: string
  module: string
  architecture: string
  environment: string
}

export type ApiEnumValue = string | number

export type UserRole =
  | 'AdminOwner'
  | 'PropertyManager'
  | 'Tenant'
  | 'MaintenanceStaff'
  | number

export type PropertyResponse = {
  id: string
  name: string
  addressLine1: string
  addressLine2?: string | null
  city: string
  country: string
  status: ApiEnumValue
  createdAtUtc: string
  unitCount: number
}

export type PropertyCreateRequest = {
  name: string
  addressLine1: string
  addressLine2?: string | null
  city: string
  country: string
  status: ApiEnumValue
}

export type RentalUnitResponse = {
  id: string
  propertyId: string
  unitNumber: string
  floor?: string | null
  bedrooms?: number | null
  status: ApiEnumValue
  createdAtUtc: string
}

export type MaintenanceRequestResponse = {
  id: string
  rentalUnitId: string
  unitNumber: string
  propertyName?: string | null
  tenantProfileId: string
  tenantName: string
  assignedStaffProfileId?: string | null
  assignedStaffName?: string | null
  title: string
  description: string
  category: ApiEnumValue
  priority: ApiEnumValue
  status: ApiEnumValue
  createdAtUtc: string
  updatedAtUtc?: string | null
  completedAtUtc?: string | null
  commentCount: number
  attachmentCount: number
}

export type MaintenanceRequestCreateRequest = {
  rentalUnitId: string
  tenantProfileId: string
  title: string
  description: string
  category: ApiEnumValue
  priority: ApiEnumValue
}

export type MaintenanceRequestStatusUpdateRequest = {
  status: ApiEnumValue
}

export type MaintenanceRequestAssignRequest = {
  assignedStaffProfileId: string
}

export type MaintenanceRequestCommentCreateRequest = {
  userProfileId: string
  commentText: string
  isInternal: boolean
}

export type MaintenanceRequestCommentResponse = {
  id: string
  maintenanceRequestId: string
  userProfileId: string
  userFullName: string
  commentText: string
  isInternal: boolean
  createdAtUtc: string
}

export type LoginRequest = {
  email: string
  password: string
}

export type AuthUserResponse = {
  userProfileId: string
  fullName: string
  email: string
  role: UserRole
  roleDisplayName: string
  isActive: boolean
}

export type LoginResponse = {
  success: boolean
  message: string
  token?: string | null
  expiresAtUtc?: string | null
  user?: AuthUserResponse | null
}

export type DemoCredentialResponse = {
  role: string
  email: string
  password: string
  purpose: string
}

export type UserProfileSummaryResponse = {
  id: string
  fullName: string
  email: string
  role: UserRole
  roleDisplayName: string
}

export type AssignedUnitResponse = {
  id: string
  rentalUnitId: string
  propertyId: string
  propertyName: string
  unitNumber: string
  floor?: string | null
  bedrooms?: number | null
  status: ApiEnumValue
  leaseStartDateUtc: string
}

export type UserAccountSummaryResponse = {
  authUserAccountId: string
  userProfileId: string
  fullName: string
  email: string
  role: UserRole
  roleDisplayName: string
  isActive: boolean
  createdAtUtc: string
  lastLoginAtUtc?: string | null
  activeUnitCount: number
  requestCount: number
}

export type UserAccountDetailResponse = {
  authUserAccountId: string
  userProfileId: string
  fullName: string
  email: string
  role: UserRole
  roleDisplayName: string
  isActive: boolean
  createdAtUtc: string
  lastLoginAtUtc?: string | null
  activeTenantUnits: TenantUnitAssignmentResponse[]
  assignedStaffRequestCount: number
  tenantRequestCount: number
}

export type CreateInternalUserRequest = {
  fullName: string
  email: string
  password: string
  role: UserRole
}

export type UpdateUserProfileRequest = {
  fullName: string
}

export type UpdateAccountStatusRequest = {
  isActive: boolean
}

export type ResetUserPasswordRequest = {
  newPassword: string
}

export type TenantUnitAssignmentResponse = {
  assignmentId: string
  tenantProfileId: string
  tenantName: string
  rentalUnitId: string
  unitNumber: string
  propertyName: string
  isActive: boolean
  leaseStartDateUtc: string
  leaseEndDateUtc?: string | null
}

export type CreateTenantUnitAssignmentRequest = {
  tenantProfileId: string
  rentalUnitId: string
  leaseStartDateUtc?: string | null
}

export type EndTenantUnitAssignmentRequest = {
  leaseEndDateUtc?: string | null
  reason?: string | null
}

export type AvailableUnitResponse = {
  rentalUnitId: string
  propertyId: string
  propertyName: string
  unitNumber: string
  floor?: string | null
  bedrooms?: number | null
  status: ApiEnumValue
}
