import { useEffect, useState } from 'react'
import { AlertTriangle, CheckCircle2, Loader2, Server } from 'lucide-react'
import { getHealth, getSystemInfo } from '../api/propCareApi'
import type { HealthResponse, SystemInfoResponse } from '../types/api'

type ApiState =
  | { status: 'loading' }
  | {
      status: 'connected'
      health: HealthResponse
      systemInfo: SystemInfoResponse
    }
  | { status: 'error'; message: string }

function ApiStatusCard() {
  const [apiState, setApiState] = useState<ApiState>({ status: 'loading' })

  useEffect(() => {
    let isMounted = true

    async function loadApiStatus() {
      try {
        const [health, systemInfo] = await Promise.all([
          getHealth(),
          getSystemInfo(),
        ])

        if (isMounted) {
          setApiState({ status: 'connected', health, systemInfo })
        }
      } catch {
        if (isMounted) {
          setApiState({
            status: 'error',
            message:
              'Service status could not be confirmed. Please try again shortly.',
          })
        }
      }
    }

    void loadApiStatus()

    return () => {
      isMounted = false
    }
  }, [])

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-950">
            <Server className="size-4 text-cyan-700" aria-hidden="true" />
            System Health
          </div>
          <p className="mt-2 text-sm text-slate-500">
            Service availability and application status.
          </p>
        </div>
        {apiState.status === 'loading' && (
          <span className="inline-flex w-fit items-center gap-2 rounded-md bg-slate-100 px-3 py-2 text-sm font-medium text-slate-700">
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
            Checking
          </span>
        )}
        {apiState.status === 'connected' && (
          <span className="inline-flex w-fit items-center gap-2 rounded-md bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-800">
            <CheckCircle2 className="size-4" aria-hidden="true" />
            Connected
          </span>
        )}
        {apiState.status === 'error' && (
          <span className="inline-flex w-fit items-center gap-2 rounded-md bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
            <AlertTriangle className="size-4" aria-hidden="true" />
            Offline
          </span>
        )}
      </div>

      <div className="mt-5 border-t border-slate-200 pt-5">
        {apiState.status === 'loading' && (
          <p className="text-sm text-slate-500">
            Checking service availability.
          </p>
        )}

        {apiState.status === 'connected' && (
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <p className="text-xs font-semibold uppercase text-slate-500">
                Health
              </p>
              <p className="mt-2 text-sm font-medium text-slate-950">
                {apiState.health.status} - {apiState.health.service}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                Last checked: {apiState.health.timestampUtc}
              </p>
            </div>
            <div>
              <p className="text-xs font-semibold uppercase text-slate-500">
                Application
              </p>
              <p className="mt-2 text-sm font-medium text-slate-950">
                {apiState.systemInfo.applicationName}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                {apiState.systemInfo.module}
              </p>
            </div>
          </div>
        )}

        {apiState.status === 'error' && (
          <div>
            <p className="text-sm font-medium text-slate-950">
              Service connection failed.
            </p>
            <p className="mt-2 text-sm text-slate-500">{apiState.message}</p>
          </div>
        )}
      </div>
    </section>
  )
}

export default ApiStatusCard
