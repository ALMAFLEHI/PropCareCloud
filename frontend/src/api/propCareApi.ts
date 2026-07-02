import axios from 'axios'
import type { HealthResponse, SystemInfoResponse } from '../types/api'

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

export { apiBaseUrl }
