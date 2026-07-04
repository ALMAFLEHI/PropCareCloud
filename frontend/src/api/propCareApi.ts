import axios from 'axios'
import type {
  HealthResponse,
  MaintenanceRequestCommentResponse,
  MaintenanceRequestCreateRequest,
  MaintenanceRequestResponse,
  MaintenanceRequestStatusUpdateRequest,
  PropertyCreateRequest,
  PropertyResponse,
  RentalUnitResponse,
  SystemInfoResponse,
} from '../types/api'

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL?.trim() || 'http://localhost:5015'

const propCareApi = axios.create({
  baseURL: apiBaseUrl,
  timeout: 5000,
})

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

export async function getMaintenanceRequestComments(
  id: string,
): Promise<MaintenanceRequestCommentResponse[]> {
  const response = await propCareApi.get<MaintenanceRequestCommentResponse[]>(
    `/api/maintenance-requests/${id}/comments`,
  )
  return response.data
}

export { apiBaseUrl }
