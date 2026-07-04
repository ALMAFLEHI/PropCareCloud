import axios from 'axios'
import type {
  AssignedUnitResponse,
  AuthUserResponse,
  AvailableUnitResponse,
  CreateInternalUserRequest,
  CreateTenantUnitAssignmentRequest,
  DemoCredentialResponse,
  EndTenantUnitAssignmentRequest,
  HealthResponse,
  LoginRequest,
  LoginResponse,
  MaintenanceRequestAssignRequest,
  MaintenanceRequestCommentResponse,
  MaintenanceRequestCreateRequest,
  MaintenanceRequestResponse,
  MaintenanceRequestStatusUpdateRequest,
  PropertyCreateRequest,
  PropertyResponse,
  RentalUnitResponse,
  ResetUserPasswordRequest,
  SystemInfoResponse,
  TenantUnitAssignmentResponse,
  UpdateAccountStatusRequest,
  UpdateUserProfileRequest,
  UserAccountDetailResponse,
  UserAccountSummaryResponse,
  UserRole,
  UserProfileSummaryResponse,
} from '../types/api'
import { getToken } from '../utils/authStorage'

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL?.trim() || 'http://localhost:5015'

const propCareApi = axios.create({
  baseURL: apiBaseUrl,
  timeout: 5000,
})

propCareApi.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

export function setAuthorizationToken(token: string | null) {
  if (token) {
    propCareApi.defaults.headers.common.Authorization = `Bearer ${token}`
    return
  }

  delete propCareApi.defaults.headers.common.Authorization
}

export async function login(payload: LoginRequest): Promise<LoginResponse> {
  const response = await propCareApi.post<LoginResponse>('/api/auth/login', payload)
  return response.data
}

export async function getCurrentUser(): Promise<AuthUserResponse> {
  const response = await propCareApi.get<AuthUserResponse>('/api/auth/me')
  return response.data
}

export async function getDemoCredentials(): Promise<DemoCredentialResponse[]> {
  const response = await propCareApi.get<DemoCredentialResponse[]>(
    '/api/auth/demo-credentials',
  )
  return response.data
}

export async function ensureDemoAccounts(): Promise<void> {
  await propCareApi.post('/api/auth/ensure-demo-accounts')
}

export async function getHealth(): Promise<HealthResponse> {
  const response = await propCareApi.get<HealthResponse>('/api/health')
  return response.data
}

export async function getSystemInfo(): Promise<SystemInfoResponse> {
  const response = await propCareApi.get<SystemInfoResponse>('/api/system-info')
  return response.data
}

export async function getProperties(): Promise<PropertyResponse[]> {
  const response = await propCareApi.get<PropertyResponse[]>('/api/properties')
  return response.data
}

export async function getPropertyById(id: string): Promise<PropertyResponse> {
  const response = await propCareApi.get<PropertyResponse>(`/api/properties/${id}`)
  return response.data
}

export async function createProperty(
  payload: PropertyCreateRequest,
): Promise<PropertyResponse> {
  const response = await propCareApi.post<PropertyResponse>(
    '/api/properties',
    payload,
  )
  return response.data
}

export async function getUnitsByProperty(
  propertyId: string,
): Promise<RentalUnitResponse[]> {
  const response = await propCareApi.get<RentalUnitResponse[]>(
    `/api/properties/${propertyId}/units`,
  )
  return response.data
}

export async function getMaintenanceRequests(): Promise<
  MaintenanceRequestResponse[]
> {
  const response = await propCareApi.get<MaintenanceRequestResponse[]>(
    '/api/maintenance-requests',
  )
  return response.data
}

export async function getMaintenanceRequestById(
  id: string,
): Promise<MaintenanceRequestResponse> {
  const response = await propCareApi.get<MaintenanceRequestResponse>(
    `/api/maintenance-requests/${id}`,
  )
  return response.data
}

export async function createMaintenanceRequest(
  payload: MaintenanceRequestCreateRequest,
): Promise<MaintenanceRequestResponse> {
  const response = await propCareApi.post<MaintenanceRequestResponse>(
    '/api/maintenance-requests',
    payload,
  )
  return response.data
}

export async function updateMaintenanceRequestStatus(
  id: string,
  payload: MaintenanceRequestStatusUpdateRequest,
): Promise<MaintenanceRequestResponse> {
  const response = await propCareApi.patch<MaintenanceRequestResponse>(
    `/api/maintenance-requests/${id}/status`,
    payload,
  )
  return response.data
}

export async function assignMaintenanceRequest(
  id: string,
  payload: MaintenanceRequestAssignRequest,
): Promise<MaintenanceRequestResponse> {
  const response = await propCareApi.patch<MaintenanceRequestResponse>(
    `/api/maintenance-requests/${id}/assign`,
    payload,
  )
  return response.data
}

export async function getMaintenanceRequestComments(
  id: string,
): Promise<MaintenanceRequestCommentResponse[]> {
  const response = await propCareApi.get<MaintenanceRequestCommentResponse[]>(
    `/api/maintenance-requests/${id}/comments`,
  )
  return response.data
}

export async function getMaintenanceStaff(): Promise<UserProfileSummaryResponse[]> {
  const response = await propCareApi.get<UserProfileSummaryResponse[]>(
    '/api/user-profiles/maintenance-staff',
  )
  return response.data
}

export async function getTenants(): Promise<UserProfileSummaryResponse[]> {
  const response = await propCareApi.get<UserProfileSummaryResponse[]>(
    '/api/user-profiles/tenants',
  )
  return response.data
}

export async function getMyAssignedUnits(): Promise<AssignedUnitResponse[]> {
  const response = await propCareApi.get<AssignedUnitResponse[]>(
    '/api/user-profiles/me/assigned-units',
  )
  return response.data
}

export async function getAdminUsers(filters?: {
  role?: UserRole | ''
  isActive?: boolean | ''
}): Promise<UserAccountSummaryResponse[]> {
  const response = await propCareApi.get<UserAccountSummaryResponse[]>(
    '/api/admin/users',
    {
      params: {
        role: filters?.role || undefined,
        isActive:
          typeof filters?.isActive === 'boolean' ? filters.isActive : undefined,
      },
    },
  )
  return response.data
}

export async function getAdminUserByProfileId(
  userProfileId: string,
): Promise<UserAccountDetailResponse> {
  const response = await propCareApi.get<UserAccountDetailResponse>(
    `/api/admin/users/${userProfileId}`,
  )
  return response.data
}

export async function createInternalUser(
  payload: CreateInternalUserRequest,
): Promise<UserAccountSummaryResponse> {
  const response = await propCareApi.post<UserAccountSummaryResponse>(
    '/api/admin/users/internal',
    payload,
  )
  return response.data
}

export async function updateUserProfile(
  userProfileId: string,
  payload: UpdateUserProfileRequest,
): Promise<UserAccountSummaryResponse> {
  const response = await propCareApi.put<UserAccountSummaryResponse>(
    `/api/admin/users/${userProfileId}/profile`,
    payload,
  )
  return response.data
}

export async function updateAccountStatus(
  userProfileId: string,
  payload: UpdateAccountStatusRequest,
): Promise<void> {
  await propCareApi.patch(`/api/admin/users/${userProfileId}/status`, payload)
}

export async function resetUserPassword(
  userProfileId: string,
  payload: ResetUserPasswordRequest,
): Promise<void> {
  await propCareApi.patch(`/api/admin/users/${userProfileId}/password`, payload)
}

export async function getTenantUnitAssignments(): Promise<
  TenantUnitAssignmentResponse[]
> {
  const response = await propCareApi.get<TenantUnitAssignmentResponse[]>(
    '/api/admin/users/tenant-assignments',
  )
  return response.data
}

export async function assignTenantToUnit(
  payload: CreateTenantUnitAssignmentRequest,
): Promise<TenantUnitAssignmentResponse> {
  const response = await propCareApi.post<TenantUnitAssignmentResponse>(
    '/api/admin/users/tenant-assignments',
    payload,
  )
  return response.data
}

export async function endTenantUnitAssignment(
  assignmentId: string,
  payload: EndTenantUnitAssignmentRequest,
): Promise<void> {
  await propCareApi.patch(
    `/api/admin/users/tenant-assignments/${assignmentId}/end`,
    payload,
  )
}

export async function getAvailableUnits(): Promise<AvailableUnitResponse[]> {
  const response = await propCareApi.get<AvailableUnitResponse[]>(
    '/api/admin/users/available-units',
  )
  return response.data
}

export { apiBaseUrl }
