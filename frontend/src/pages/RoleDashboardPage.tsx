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
  UsersRound,
  Wrench,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import {
  getAdminUsers,
  getMaintenanceRequests,
  getMaintenanceStaff,
  getMyAssignedUnits,
  getProperties,
} from '../api/propCareApi'
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
  UserAccountSummaryResponse,
  UserProfileSummaryResponse,
} from '../types/api'
import { formatDateTime, getPriorityLabel } from '../utils/formatters'
import type { UserRoleKey } from '../utils/roles'

type DashboardData = {
  properties: PropertyResponse[]
  requests: MaintenanceRequestResponse[]
  assignedUnits: AssignedUnitResponse[]
  staff: UserProfileSummaryResponse[]
  users: UserAccountSummaryResponse[]
}

type DashboardCopy = {
  eyebrow: string
  title: string
  description: string
  actionLabel: string
  actionTo: string
  actions: Array<{ label: string; to: string }>
}

const dashboardCopy: Record<UserRoleKey, DashboardCopy> = {
  AdminOwner: {
    eyebrow: 'Owner Workspace',
    title: 'Portfolio Operations Dashboard',
    description:
      'Monitor properties, service requests, user access, and maintenance activity.',
    actionLabel: 'Review service requests',
    actionTo: '/requests',
    actions: [
      { label: 'Manage access', to: '/users' },
      { label: 'Review service requests', to: '/requests' },
      { label: 'Review property portfolio', to: '/properties' },
    ],
  },
  PropertyManager: {
    eyebrow: 'Manager Workspace',
    title: 'Property Operations Dashboard',
    description:
      'Coordinate tenant requests, staff assignments, and property maintenance.',
    actionLabel: 'Review urgent requests',
    actionTo: '/requests',
    actions: [
      { label: 'Review urgent requests', to: '/requests' },
      { label: 'Assign maintenance staff', to: '/requests' },
      { label: 'Review property units', to: '/properties' },
    ],
  },
  Tenant: {
    eyebrow: 'Resident Workspace',
    title: 'My Home Service Portal',
    description:
      'Submit maintenance issues and track repair progress for your assigned unit(s).',
    actionLabel: 'Submit maintenance request',
    actionTo: '/requests',
    actions: [
      { label: 'Submit maintenance request', to: '/requests' },
      { label: 'Track repair progress', to: '/requests' },
    ],
  },
  MaintenanceStaff: {
    eyebrow: 'Maintenance Workspace',
    title: 'Assigned Work Dashboard',
    description:
      'Review assigned jobs, update progress, and close completed work.',
    actionLabel: 'View assigned work',
    actionTo: '/requests',
    actions: [
      { label: 'View assigned work', to: '/requests' },
      { label: 'Update progress', to: '/requests' },
      { label: 'Add work note', to: '/requests' },
    ],
  },
}

function getEnumNumber(value: ApiEnumValue) {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number(value)
  return Number.isNaN(parsed) ? null : parsed
}

function getStatusText(status: ApiEnumValue) {
  return String(status).toLowerCase().replace(/\s+/g, '')
}

function isOpenRequest(status: ApiEnumValue) {
  const numericStatus = getEnumNumber(status)
  if (numericStatus !== null) {
    return numericStatus !== 4 && numericStatus !== 5
  }

  const textStatus = getStatusText(status)
  return !textStatus.includes('completed') && !textStatus.includes('cancelled')
}

function isInProgress(status: ApiEnumValue) {
  const numericStatus = getEnumNumber(status)
  return numericStatus === 3 || getStatusText(status).includes('inprogress')
}

function isCompleted(status: ApiEnumValue) {
  const numericStatus = getEnumNumber(status)
  return numericStatus === 4 || getStatusText(status).includes('completed')
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
  const {
    user,
    userRoleKey,
    isAdminOwner,
    isPropertyManager,
    isTenant,
    isMaintenanceStaff,
  } = useAuth()
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const roleKey = userRoleKey ?? 'Tenant'
  const copy = dashboardCopy[roleKey]

  async function loadDashboardData() {
    setIsLoading(true)
    setError('')

    try {
      const [properties, requests, assignedUnits, staff, users] =
        await Promise.all([
          isAdminOwner || isPropertyManager ? getProperties() : Promise.resolve([]),
          getMaintenanceRequests(),
          isTenant ? getMyAssignedUnits() : Promise.resolve([]),
          isAdminOwner || isPropertyManager
            ? getMaintenanceStaff()
            : Promise.resolve([]),
          isAdminOwner ? getAdminUsers() : Promise.resolve([]),
        ])

      setDashboardData({ properties, requests, assignedUnits, staff, users })
    } catch {
      setError('Dashboard data could not be loaded. Please try again.')
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
    const staff = dashboardData?.staff ?? []
    const users = dashboardData?.users ?? []
    const openRequests = requests.filter((request) =>
      isOpenRequest(request.status),
    )
    const highPriorityRequests = requests.filter((request) =>
      isHighPriority(request.priority),
    )
    const inProgressRequests = requests.filter((request) =>
      isInProgress(request.status),
    )
    const completedRequests = requests.filter((request) =>
      isCompleted(request.status),
    )
    const activeUsers = users.filter((account) => account.isActive)

    return {
      properties,
      requests,
      assignedUnits,
      staff,
      openRequests,
      highPriorityRequests,
      inProgressRequests,
      completedRequests,
      activeUsers,
      recentRequests: requests.slice(0, 5),
    }
  }, [dashboardData])

  const cards = useMemo(() => {
    if (isAdminOwner) {
      return [
        {
          title: 'Open requests',
          value: summary.openRequests.length,
          helperText: 'Requests awaiting completion.',
          icon: Wrench,
        },
        {
          title: 'Properties',
          value: summary.properties.length,
          helperText: 'Managed portfolio records.',
          icon: Building2,
        },
        {
          title: 'Active users',
          value: summary.activeUsers.length,
          helperText: 'Operational accounts currently active.',
          icon: UsersRound,
        },
        {
          title: 'High priority requests',
          value: summary.highPriorityRequests.length,
          helperText: 'High and emergency priority work.',
          icon: AlertTriangle,
        },
      ]
    }

    if (isPropertyManager) {
      return [
        {
          title: 'Open requests',
          value: summary.openRequests.length,
          helperText: 'Requests awaiting coordination.',
          icon: Wrench,
        },
        {
          title: 'Properties',
          value: summary.properties.length,
          helperText: 'Properties under management.',
          icon: Building2,
        },
        {
          title: 'High priority',
          value: summary.highPriorityRequests.length,
          helperText: 'Requests needing quick attention.',
          icon: AlertTriangle,
        },
        {
          title: 'Assigned staff',
          value: summary.staff.length,
          helperText: 'Available maintenance staff.',
          icon: Hammer,
        },
      ]
    }

    if (isMaintenanceStaff) {
      return [
        {
          title: 'Assigned jobs',
          value: summary.requests.length,
          helperText: 'Jobs assigned to your queue.',
          icon: ClipboardList,
        },
        {
          title: 'In progress',
          value: summary.inProgressRequests.length,
          helperText: 'Work currently underway.',
          icon: Hammer,
        },
        {
          title: 'Completed',
          value: summary.completedRequests.length,
          helperText: 'Jobs marked complete.',
          icon: ShieldCheck,
        },
        {
          title: 'High priority',
          value: summary.highPriorityRequests.length,
          helperText: 'Priority jobs in your queue.',
          icon: AlertTriangle,
        },
      ]
    }

    return [
      {
        title: 'My requests',
        value: summary.requests.length,
        helperText: 'Requests linked to your account.',
        icon: ClipboardList,
      },
      {
        title: 'Assigned units',
        value: summary.assignedUnits.length,
        helperText: 'Units available for service requests.',
        icon: Home,
      },
      {
        title: 'Open requests',
        value: summary.openRequests.length,
        helperText: 'Requests currently active.',
        icon: Wrench,
      },
      {
        title: 'Completed requests',
        value: summary.completedRequests.length,
        helperText: 'Requests marked complete.',
        icon: ShieldCheck,
      },
    ]
  }, [
    isAdminOwner,
    isMaintenanceStaff,
    isPropertyManager,
    summary,
  ])

  return (
    <div className="space-y-6">
      <section className="premium-hero-card p-6">
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
            {cards.map((card) => (
              <StatCard
                key={card.title}
                title={card.title}
                value={String(card.value)}
                helperText={card.helperText}
                icon={card.icon}
              />
            ))}
          </section>

          <section
            className={
              isAdminOwner
                ? 'grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]'
                : 'grid gap-4'
            }
          >
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
              <div className="mt-5 grid gap-3 md:grid-cols-3">
                {copy.actions.map((action) => (
                  <Link
                    key={action.label}
                    to={action.to}
                    className="premium-lift rounded-lg border border-slate-200 bg-white/70 p-4 text-sm font-semibold text-slate-950 hover:bg-cyan-50"
                  >
                    {action.label}
                  </Link>
                ))}
              </div>
            </article>

            {isAdminOwner && <ApiStatusCard />}
          </section>

          {isTenant && (
            <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-slate-950">
                    Assigned Units
                  </h3>
                  <p className="mt-1 text-sm text-slate-500">
                    Active units available for service requests.
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
                  Latest service activity available to your role.
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
                      {request.propertyName ? `${request.propertyName} - ` : ''}
                      Unit {request.unitNumber} - {request.tenantName} -{' '}
                      {getPriorityLabel(request.priority)}
                    </p>
                  </div>
                  <div className="flex flex-col gap-2 text-sm text-slate-500 lg:items-end">
                    <span>{formatDateTime(request.createdAtUtc)}</span>
                    <Link
                      to={`/requests/${request.id}`}
                      className="font-semibold text-cyan-700 hover:text-cyan-800"
                    >
                      View details
                    </Link>
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
