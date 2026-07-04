import { useEffect, useMemo, useState } from 'react'
import {
  AlertTriangle,
  Building2,
  ClipboardList,
  Hammer,
  Home,
  Plus,
  ShieldCheck,
  UserRound,
  Wrench,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { getMaintenanceRequests, getMyAssignedUnits, getProperties } from '../api/propCareApi'
import ApiStatusCard from '../components/ApiStatusCard'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import StatCard from '../components/StatCard'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../context/AuthContext'
import type {
  ApiEnumValue,
  AssignedUnitResponse,
  MaintenanceRequestResponse,
  PropertyResponse,
} from '../types/api'
import { formatDateTime, getPriorityLabel } from '../utils/formatters'

type DashboardData = {
  properties: PropertyResponse[]
  requests: MaintenanceRequestResponse[]
  assignedUnits: AssignedUnitResponse[]
}

type DashboardCopy = {
  eyebrow: string
  title: string
  description: string
  actionLabel: string
  actionTo: string
}

const dashboardCopy: Record<string, DashboardCopy> = {
  AdminOwner: {
    eyebrow: 'Admin / Owner Dashboard',
    title: 'Portfolio oversight',
    description:
      'Monitor the property portfolio, maintenance workload, and role-based demo access from one operational view.',
    actionLabel: 'Review properties',
    actionTo: '/properties',
  },
  PropertyManager: {
    eyebrow: 'Property Manager Dashboard',
    title: 'Request triage and property operations',
    description:
      'Review open work, track property context, and coordinate maintenance activity across the local demo data.',
    actionLabel: 'Manage requests',
    actionTo: '/requests',
  },
  Tenant: {
    eyebrow: 'Tenant Dashboard',
    title: 'My home service portal',
    description:
      'Submit and follow maintenance requests for units assigned to your tenant profile.',
    actionLabel: 'Submit request',
    actionTo: '/requests',
  },
  MaintenanceStaff: {
    eyebrow: 'Maintenance Staff Dashboard',
    title: 'My assigned work queue',
    description:
      'Review assigned and in-progress maintenance work, then update job status from the requests area.',
    actionLabel: 'View work queue',
    actionTo: '/requests',
  },
}

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

function isHighPriority(priority: ApiEnumValue) {
  const numericPriority = getEnumNumber(priority)
  if (numericPriority !== null) {
    return numericPriority >= 2
  }

  const textPriority = String(priority).toLowerCase()
  return textPriority.includes('high') || textPriority.includes('emergency')
}

function RoleDashboardPage() {
  const { user, userRoleKey, isAdminOwner, isPropertyManager, isTenant, isMaintenanceStaff } = useAuth()
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const copy = dashboardCopy[userRoleKey ?? 'Tenant']

  async function loadDashboardData() {
    setIsLoading(true)
    setError('')

    try {
      const [properties, requests, assignedUnits] = await Promise.all([
        isAdminOwner || isPropertyManager ? getProperties() : Promise.resolve([]),
        getMaintenanceRequests(),
        isTenant ? getMyAssignedUnits() : Promise.resolve([]),
      ])
      setDashboardData({ properties, requests, assignedUnits })
    } catch {
      setError(
        'Dashboard data could not be loaded. Confirm the backend is running and the signed-in role has valid demo data.',
      )
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadDashboardData()
  }, [isAdminOwner, isPropertyManager, isTenant])

  const summary = useMemo(() => {
    const properties = dashboardData?.properties ?? []
    const requests = dashboardData?.requests ?? []
    const assignedUnits = dashboardData?.assignedUnits ?? []
    const openRequests = requests.filter((request) =>
      isOpenRequest(request.status),
    )
    const highPriorityRequests = requests.filter((request) =>
      isHighPriority(request.priority),
    )
    const assignedRequests = requests.filter((request) =>
      Boolean(request.assignedStaffProfileId),
    )
    const completedRequests = requests.filter((request) => {
      const status = getEnumNumber(request.status)
      return status === 4 || String(request.status).toLowerCase().includes('completed')
    })

    return {
      properties,
      requests,
      assignedUnits,
      openRequests,
      highPriorityRequests,
      assignedRequests,
      completedRequests,
      recentRequests: requests.slice(0, 5),
    }
  }, [dashboardData])

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-4xl">
            <p className="text-sm font-semibold text-cyan-700">{copy.eyebrow}</p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950 sm:text-4xl">
              {copy.title}
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-600">
              {copy.description}
            </p>
            <p className="mt-4 text-sm font-medium text-slate-500">
              Signed in as {user?.fullName} - {user?.roleDisplayName}
            </p>
          </div>
          <Link
            to={copy.actionTo}
            className="inline-flex w-fit items-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800"
          >
            <Plus className="size-4" aria-hidden="true" />
            {copy.actionLabel}
          </Link>
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
              title={isTenant ? 'Tracked Requests' : 'Open Requests'}
              value={String(isTenant ? summary.requests.length : summary.openRequests.length)}
              helperText={
                isTenant
                  ? 'Demo tenant view over current request records.'
                  : 'Requests not completed or cancelled.'
              }
              icon={Wrench}
            />
            <StatCard
              title={
                isMaintenanceStaff
                  ? 'Assigned Work'
                  : isTenant
                    ? 'Assigned Units'
                    : 'Properties'
              }
              value={String(
                isMaintenanceStaff
                  ? summary.assignedRequests.length
                  : isTenant
                    ? summary.assignedUnits.length
                  : summary.properties.length,
              )}
              helperText={
                isMaintenanceStaff
                  ? 'Requests with assigned maintenance staff.'
                  : isTenant
                    ? 'Active unit assignments for this tenant profile.'
                  : 'Property records loaded from the backend.'
              }
              icon={Building2}
            />
            <StatCard
              title="High Priority"
              value={String(summary.highPriorityRequests.length)}
              helperText="High and emergency priority request records."
              icon={AlertTriangle}
            />
            <StatCard
              title={isMaintenanceStaff ? 'Completed' : 'Total Requests'}
              value={String(
                isMaintenanceStaff
                  ? summary.completedRequests.length
                  : summary.requests.length,
              )}
              helperText={
                isMaintenanceStaff
                  ? 'Completed work in the current request data.'
                  : 'All maintenance request records available locally.'
              }
              icon={ClipboardList}
            />
          </section>

          <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
            <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-center gap-2">
                {isAdminOwner && <ShieldCheck className="size-5 text-cyan-700" aria-hidden="true" />}
                {isPropertyManager && <Home className="size-5 text-cyan-700" aria-hidden="true" />}
                {isTenant && <UserRound className="size-5 text-cyan-700" aria-hidden="true" />}
                {isMaintenanceStaff && <Hammer className="size-5 text-cyan-700" aria-hidden="true" />}
                <h3 className="text-lg font-semibold text-slate-950">
                  Suggested Actions
                </h3>
              </div>
              <div className="mt-5 grid gap-3 md:grid-cols-2">
                {(isAdminOwner || isPropertyManager) && (
                  <>
                    <Link
                      to="/properties"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Review properties and unit counts
                    </Link>
                    <Link
                      to="/requests"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Review urgent maintenance requests
                    </Link>
                    {isPropertyManager && (
                      <Link
                        to="/requests"
                        className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                      >
                        Assign maintenance staff
                      </Link>
                    )}
                  </>
                )}
                {isTenant && (
                  <>
                    <Link
                      to="/requests"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Submit a maintenance issue
                    </Link>
                    <Link
                      to="/requests"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Track repair progress
                    </Link>
                  </>
                )}
                {isMaintenanceStaff && (
                  <>
                    <Link
                      to="/requests"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Start assigned work
                    </Link>
                    <Link
                      to="/requests"
                      className="rounded-lg border border-slate-200 p-4 text-sm font-semibold text-slate-950 hover:border-cyan-300 hover:bg-cyan-50"
                    >
                      Complete and update job status
                    </Link>
                  </>
                )}
              </div>
            </article>

            <ApiStatusCard />
          </section>

          {isTenant && (
            <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-slate-950">
                    Assigned Units
                  </h3>
                  <p className="mt-1 text-sm text-slate-500">
                    Active units available for tenant request creation.
                  </p>
                </div>
                <span className="text-sm font-medium text-slate-500">
                  {summary.assignedUnits.length} active
                </span>
              </div>
              <div className="mt-5 grid gap-3 md:grid-cols-2">
                {summary.assignedUnits.map((unit) => (
                  <article
                    key={unit.id}
                    className="rounded-lg border border-slate-200 p-4"
                  >
                    <p className="text-sm font-semibold text-slate-950">
                      {unit.propertyName}
                    </p>
                    <p className="mt-2 text-sm text-slate-600">
                      Unit {unit.unitNumber}
                      {unit.floor ? ` - Floor ${unit.floor}` : ''}
                    </p>
                  </article>
                ))}
              </div>
            </section>
          )}

          <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 className="text-lg font-semibold text-slate-950">
                  Recent Requests
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  Current local records shown through the role-based dashboard.
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
                      <h4 className="text-base font-semibold text-slate-950">
                        {request.title}
                      </h4>
                      <StatusBadge value={request.status} kind="maintenance" />
                      <StatusBadge value={request.priority} kind="priority" />
                    </div>
                    <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
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
    </div>
  )
}

export default RoleDashboardPage
