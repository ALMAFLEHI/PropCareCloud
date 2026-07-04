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
