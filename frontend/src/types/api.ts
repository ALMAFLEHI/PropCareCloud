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
