import { useEffect, useMemo, useState } from 'react'
import {
  Building2,
  ClipboardList,
  Hammer,
  Home,
  ShieldCheck,
  UserRound,
  UsersRound,
  Wrench,
} from 'lucide-react'
import { getMaintenanceRequests, getProperties } from '../api/propCareApi'
import ApiStatusCard from '../components/ApiStatusCard'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import RoleCard from '../components/RoleCard'
import StatCard from '../components/StatCard'
import StatusBadge from '../components/StatusBadge'
import type { ApiEnumValue, MaintenanceRequestResponse, PropertyResponse } from '../types/api'
import { formatDateTime, getPriorityLabel } from '../utils/formatters'

type DashboardData = {
  properties: PropertyResponse[]
  requests: MaintenanceRequestResponse[]
}

const roleCards = [
  {
    title: 'Admin / Owner',
    description: 'Oversees the platform, property portfolio, and cloud progress.',
    responsibilities: ['Manage ownership view', 'Review reports', 'Track service quality'],
    icon: ShieldCheck,
  },
  {
    title: 'Property Manager',
    description: 'Coordinates tenant requests and maintenance work assignments.',
    responsibilities: ['Review requests', 'Assign staff', 'Monitor outcomes'],
    icon: ClipboardList,
  },
  {
    title: 'Tenant',
    description: 'Submits maintenance issues and follows request progress.',
    responsibilities: ['Create requests', 'Provide details', 'View updates'],
    icon: UserRound,
  },
  {
    title: 'Maintenance Staff',
    description: 'Receives assigned work and records repair progress.',
    responsibilities: ['Accept jobs', 'Update status', 'Record completion notes'],
    icon: Hammer,
  },
]

function getEnumNumber(value: ApiEnumValue) {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number(value)
  return Number.isNaN(parsed) ? null : parsed
}

function isOpenRequest(status: ApiEnumValue) {
  const numericStatus = getEnumNumber(status)
  if (numericStatus !== null) {
    return numericStatus !== 4 && numericStatus !== 5
  }

  const textStatus = String(status).toLowerCase()
  return !textStatus.includes('completed') && !textStatus.includes('cancelled')
}

function DashboardPage() {
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  async function loadDashboardData() {
    setIsLoading(true)
    setError('')

    try {
      const [properties, requests] = await Promise.all([
        getProperties(),
        getMaintenanceRequests(),
      ])

      setDashboardData({ properties, requests })
    } catch {
      setError(
        'Dashboard data could not be loaded. Start the backend on http://localhost:5015 and try again.',
      )
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadDashboardData()
  }, [])

  const summary = useMemo(() => {
    const properties = dashboardData?.properties ?? []
    const requests = dashboardData?.requests ?? []
    const openRequests = requests.filter((request) =>
      isOpenRequest(request.status),
    ).length
    const activeTenantIds = new Set(requests.map((request) => request.tenantProfileId))
    const staffIds = new Set(
      requests
        .map((request) => request.assignedStaffProfileId)
        .filter((staffId): staffId is string => Boolean(staffId)),
    )

    return {
      openRequests,
      properties: properties.length,
      activeTenants: activeTenantIds.size,
      staffMembers: staffIds.size,
      recentRequests: requests.slice(0, 4),
    }
  }, [dashboardData])

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <div className="max-w-4xl">
          <p className="text-sm font-semibold text-cyan-700">
            Sprint 8 Frontend CRUD Integration
          </p>
          <h2 className="mt-3 text-3xl font-semibold text-slate-950 sm:text-4xl">
            Property maintenance and tenant service portal
          </h2>
          <p className="mt-4 text-base leading-7 text-slate-600">
            PropCare Cloud now reads local PostgreSQL-backed property and
            maintenance request records through the ASP.NET Core Web API while
            keeping authentication, AWS deployment, and full production workflow
            features deferred to later sprints.
          </p>
        </div>
      </section>

      {isLoading && <LoadingState title="Loading dashboard" />}

      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            void loadDashboardData()
          }}
        />
      )}

      {!isLoading && !error && (
        <>
          <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard
              title="Open Requests"
              value={String(summary.openRequests)}
              helperText="Live count excluding completed and cancelled requests."
              icon={Wrench}
            />
            <StatCard
              title="Properties"
              value={String(summary.properties)}
              helperText="Loaded from the Sprint 7 properties API."
              icon={Building2}
            />
            <StatCard
              title="Active Tenants"
              value={String(summary.activeTenants)}
              helperText="Distinct seeded tenants from maintenance requests."
              icon={UsersRound}
            />
            <StatCard
              title="Staff Members"
              value={String(summary.staffMembers)}
              helperText="Assigned maintenance staff represented in request data."
              icon={Hammer}
            />
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-lg font-semibold text-slate-950">
                  Recent Maintenance Requests
                </h2>
                <p className="mt-1 text-sm text-slate-500">
                  Latest records returned by the local CRUD API.
                </p>
              </div>
              <span className="text-sm font-medium text-slate-500">
                {summary.recentRequests.length} shown
              </span>
            </div>

            <div className="mt-5 divide-y divide-slate-200">
              {summary.recentRequests.map((request) => (
                <article
                  key={request.id}
                  className="grid gap-4 py-4 lg:grid-cols-[1fr_auto]"
                >
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-base font-semibold text-slate-950">
                        {request.title}
                      </h3>
                      <StatusBadge value={request.status} kind="maintenance" />
                      <StatusBadge value={request.priority} kind="priority" />
                    </div>
                    <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
                      {request.description}
                    </p>
                    <p className="mt-3 text-sm text-slate-500">
                      Unit {request.unitNumber} - {request.tenantName} -{' '}
                      {getPriorityLabel(request.priority)}
                    </p>
                  </div>
                  <div className="text-sm text-slate-500 lg:text-right">
                    {formatDateTime(request.createdAtUtc)}
                  </div>
                </article>
              ))}
            </div>
          </section>
        </>
      )}

      <ApiStatusCard />

      <section>
        <div className="mb-4 flex items-center gap-2">
          <UsersRound className="size-5 text-cyan-700" aria-hidden="true" />
          <h2 className="text-xl font-semibold text-slate-950">
            Planned User Roles
          </h2>
        </div>
        <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
          {roleCards.map((role) => (
            <RoleCard key={role.title} {...role} />
          ))}
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-2">
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <Home className="size-5 text-cyan-700" aria-hidden="true" />
            <h2 className="text-lg font-semibold text-slate-950">
              Cloud Architecture Summary
            </h2>
          </div>
          <p className="mt-4 rounded-md bg-slate-50 p-4 text-sm font-medium text-slate-700">
            React Frontend -&gt; ASP.NET Core Web API -&gt; Amazon RDS PostgreSQL
          </p>
        </article>

        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <Building2 className="size-5 text-indigo-700" aria-hidden="true" />
            <h2 className="text-lg font-semibold text-slate-950">
              Future AWS Services
            </h2>
          </div>
          <p className="mt-4 text-sm leading-6 text-slate-600">
            Planned cloud extensions include S3, API Gateway, Lambda, SNS/SQS,
            and CloudWatch/X-Ray for storage, integration, messaging, and
            observability.
          </p>
        </article>
      </section>
    </div>
  )
}

export default DashboardPage
