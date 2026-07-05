import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  ClipboardCheck,
  Plus,
  RefreshCcw,
  Send,
  UserCheck,
  Wrench,
} from 'lucide-react'
import {
  assignMaintenanceRequest,
  createMaintenanceRequest,
  getMaintenanceRequests,
  getMaintenanceStaff,
  getMyAssignedUnits,
  getTenants,
  updateMaintenanceRequestStatus,
} from '../api/propCareApi'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import StatusBadge from '../components/StatusBadge'
import StatusTimeline from '../components/StatusTimeline'
import { useAuth } from '../context/AuthContext'
import { Link } from 'react-router-dom'
import type {
  ApiEnumValue,
  AssignedUnitResponse,
  MaintenanceRequestResponse,
  UserProfileSummaryResponse,
} from '../types/api'
import { formatDateTime, getCategoryLabel } from '../utils/formatters'
import {
  canAssignRequests,
  canCreateMaintenanceRequest,
  canUpdateRequestStatus,
  getAllowedStatusValues,
  isAdminOrManager,
  isMaintenanceStaff,
  isTenant,
  type UserRoleKey,
} from '../utils/roles'

const requestSteps = [
  {
    title: 'Submit Request',
    description: 'Residents submit maintenance issues for their assigned units.',
    icon: Send,
  },
  {
    title: 'Review & Prioritize',
    description: 'Managers review incoming work and coordinate the next step.',
    icon: UserCheck,
  },
  {
    title: 'Assign Staff',
    description: 'Maintenance teams receive clear ownership for each job.',
    icon: Wrench,
  },
  {
    title: 'Track Completion',
    description: 'Everyone can follow progress through to completion.',
    icon: ClipboardCheck,
  },
]

const statusOptions = [
  { value: '0', label: 'Submitted' },
  { value: '1', label: 'Under review' },
  { value: '2', label: 'Assigned' },
  { value: '3', label: 'In progress' },
  { value: '4', label: 'Completed' },
  { value: '5', label: 'Cancelled' },
]

const categoryOptions = [
  { value: '0', label: 'Plumbing' },
  { value: '1', label: 'Electrical' },
  { value: '2', label: 'HVAC' },
  { value: '3', label: 'Cleaning' },
  { value: '4', label: 'Security' },
  { value: '5', label: 'Structural' },
  { value: '6', label: 'Other' },
]

const priorityOptions = [
  { value: '0', label: 'Low' },
  { value: '1', label: 'Medium' },
  { value: '2', label: 'High' },
  { value: '3', label: 'Emergency' },
]

const statusValueByName: Record<string, string> = {
  submitted: '0',
  underreview: '1',
  assigned: '2',
  inprogress: '3',
  completed: '4',
  cancelled: '5',
}

const initialRequestForm = {
  rentalUnitId: '',
  tenantProfileId: '',
  title: '',
  description: '',
  category: '6',
  priority: '1',
}

const requestPageCopy: Record<UserRoleKey, { title: string; description: string }> = {
  AdminOwner: {
    title: 'Portfolio Request Oversight',
    description:
      'Monitor maintenance activity across all managed properties.',
  },
  PropertyManager: {
    title: 'Maintenance Request Management',
    description:
      'Review, assign, and coordinate maintenance work.',
  },
  Tenant: {
    title: 'My Maintenance Requests',
    description:
      'Submit issues and monitor repair progress.',
  },
  MaintenanceStaff: {
    title: 'My Assigned Work',
    description:
      'Update job progress and record maintenance activity.',
  },
}

function getStatusValue(status: ApiEnumValue) {
  if (typeof status === 'number') {
    return String(status)
  }

  const parsed = Number(status)
  if (!Number.isNaN(parsed) && status.trim() !== '') {
    return String(parsed)
  }

  return statusValueByName[status.replace(/\s+/g, '').toLowerCase()] ?? '0'
}

function RequestsPage() {
  const { user, userRoleKey } = useAuth()
  const [requests, setRequests] = useState<MaintenanceRequestResponse[]>([])
  const [assignedUnits, setAssignedUnits] = useState<AssignedUnitResponse[]>([])
  const [maintenanceStaff, setMaintenanceStaff] = useState<UserProfileSummaryResponse[]>([])
  const [tenants, setTenants] = useState<UserProfileSummaryResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [updatingRequestId, setUpdatingRequestId] = useState('')
  const [assigningRequestId, setAssigningRequestId] = useState('')
  const [form, setForm] = useState(initialRequestForm)

  const copy = requestPageCopy[userRoleKey ?? 'Tenant']
  const tenantRole = isTenant(user)
  const staffRole = isMaintenanceStaff(user)
  const adminOrManager = isAdminOrManager(user)
  const canCreate = canCreateMaintenanceRequest(user)
  const canAssign = canAssignRequests(user)
  const allowedStatusValues = getAllowedStatusValues(user)
  const defaultRequest = requests[0]

  const openRequests = useMemo(
    () =>
      requests.filter((request) => {
        const status = getStatusValue(request.status)
        return status !== '4' && status !== '5'
      }).length,
    [requests],
  )

  async function loadRequests() {
    setIsLoading(true)
    setError('')

    try {
      const [requestData, assignedUnitData, staffData, tenantData] =
        await Promise.all([
          getMaintenanceRequests(),
          tenantRole ? getMyAssignedUnits() : Promise.resolve([]),
          adminOrManager ? getMaintenanceStaff() : Promise.resolve([]),
          adminOrManager ? getTenants() : Promise.resolve([]),
        ])

      setRequests(requestData)
      setAssignedUnits(assignedUnitData)
      setMaintenanceStaff(staffData)
      setTenants(tenantData)

      const firstRequest = requestData[0]
      const firstAssignedUnit = assignedUnitData[0]
      const firstTenant = tenantData[0]
      setForm((current) => ({
        ...current,
        rentalUnitId:
          current.rentalUnitId ||
          firstAssignedUnit?.rentalUnitId ||
          firstRequest?.rentalUnitId ||
          '',
        tenantProfileId:
          current.tenantProfileId ||
          (tenantRole ? user?.userProfileId : firstTenant?.id) ||
          firstRequest?.tenantProfileId ||
          '',
      }))
    } catch {
      setError('Maintenance requests could not be loaded. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadRequests()
  }, [adminOrManager, tenantRole, user?.userProfileId])

  function updateForm(field: keyof typeof initialRequestForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function openCreateModal() {
    setForm((current) => ({
      ...current,
      rentalUnitId:
        current.rentalUnitId ||
        assignedUnits[0]?.rentalUnitId ||
        defaultRequest?.rentalUnitId ||
        '',
      tenantProfileId:
        current.tenantProfileId ||
        (tenantRole ? user?.userProfileId : tenants[0]?.id) ||
        defaultRequest?.tenantProfileId ||
        '',
    }))
    setIsModalOpen(true)
  }

  async function handleStatusChange(requestId: string, value: string) {
    setUpdatingRequestId(requestId)
    setError('')

    try {
      await updateMaintenanceRequestStatus(requestId, { status: Number(value) })
      await loadRequests()
    } catch {
      setError('Status update failed. Confirm the request is assigned to your role.')
    } finally {
      setUpdatingRequestId('')
    }
  }

  async function handleAssignRequest(requestId: string, staffProfileId: string) {
    if (!staffProfileId) {
      return
    }

    setAssigningRequestId(requestId)
    setError('')

    try {
      await assignMaintenanceRequest(requestId, {
        assignedStaffProfileId: staffProfileId,
      })
      await loadRequests()
    } catch {
      setError('Staff assignment failed. Confirm the selected user has the Maintenance Staff role.')
    } finally {
      setAssigningRequestId('')
    }
  }

  async function handleCreateRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')

    try {
      await createMaintenanceRequest({
        rentalUnitId: form.rentalUnitId.trim(),
        tenantProfileId: tenantRole
          ? user?.userProfileId ?? '00000000-0000-0000-0000-000000000000'
          : form.tenantProfileId.trim(),
        title: form.title.trim(),
        description: form.description.trim(),
        category: Number(form.category),
        priority: Number(form.priority),
      })

      setForm({
        ...initialRequestForm,
        rentalUnitId: form.rentalUnitId,
        tenantProfileId: form.tenantProfileId,
      })
      setIsModalOpen(false)
      await loadRequests()
    } catch {
      setError(
        tenantRole
          ? 'Request creation failed. Tenants can submit only for active assigned units.'
          : 'Request creation failed. Confirm the unit and tenant profile are valid.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="premium-hero-card p-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-3xl">
            <p className="text-sm font-semibold text-cyan-700">
              Maintenance Requests
            </p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950">
              {copy.title}
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-600">
              {copy.description}
            </p>
          </div>
          {canCreate && (
            <button
              type="button"
              onClick={openCreateModal}
              disabled={tenantRole && assignedUnits.length === 0}
              className="inline-flex w-fit items-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            >
              <Plus className="size-4" aria-hidden="true" />
              Add Request
            </button>
          )}
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {requestSteps.map((step) => {
          const Icon = step.icon

          return (
            <article
              key={step.title}
              className="premium-card p-5"
            >
              <div className="flex size-10 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
                <Icon className="size-5" aria-hidden="true" />
              </div>
              <h3 className="mt-4 text-base font-semibold text-slate-950">
                {step.title}
              </h3>
              <p className="mt-3 text-sm leading-6 text-slate-600">
                {step.description}
              </p>
            </article>
          )
        })}
      </section>

      <section className="grid gap-4 md:grid-cols-3">
        <article className="premium-card p-5">
          <p className="text-sm font-medium text-slate-500">
            {staffRole ? 'Assigned Requests' : 'Visible Requests'}
          </p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {requests.length}
          </p>
        </article>
        <article className="premium-card p-5">
          <p className="text-sm font-medium text-slate-500">Open Requests</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {openRequests}
          </p>
        </article>
        <article className="premium-card p-5">
          <p className="text-sm font-medium text-slate-500">
            {tenantRole ? 'Assigned Units' : 'Assignment Options'}
          </p>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            {tenantRole
              ? `${assignedUnits.length} active unit assignment${assignedUnits.length === 1 ? '' : 's'} available.`
              : adminOrManager
                ? `${maintenanceStaff.length} staff member${maintenanceStaff.length === 1 ? '' : 's'} available.`
                : 'Status updates are limited to your assigned jobs.'}
          </p>
        </article>
      </section>

      {isLoading && <LoadingState title="Loading maintenance requests" />}

      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            void loadRequests()
          }}
        />
      )}

      {!isLoading && !error && requests.length === 0 && (
        <EmptyState
          title="No maintenance requests found"
          message="No requests are available for this account yet."
        />
      )}

      {!isLoading && requests.length > 0 && (
        <section className="premium-panel overflow-hidden">
          <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">
                Service Requests
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Requests and actions are matched to your account permissions.
              </p>
            </div>
            <button
              type="button"
              onClick={() => {
                void loadRequests()
              }}
              className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              <RefreshCcw className="size-4" aria-hidden="true" />
              Refresh
            </button>
          </div>

          <div className="divide-y divide-slate-200">
            {requests.map((request) => (
              <article key={request.id} className="grid gap-5 px-5 py-5 xl:grid-cols-[1fr_260px]">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h4 className="text-base font-semibold text-slate-950">
                      {request.title}
                    </h4>
                    <StatusBadge value={request.status} kind="maintenance" />
                    <StatusBadge value={request.priority} kind="priority" />
                  </div>
                  <p className="mt-3 max-w-4xl text-sm leading-6 text-slate-600">
                    {request.description}
                  </p>
                  <div className="mt-4">
                    <StatusTimeline status={request.status} compact />
                  </div>
                  <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
                    <div>
                      <dt className="font-medium text-slate-500">Tenant</dt>
                      <dd className="mt-1 text-slate-900">{request.tenantName}</dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Unit</dt>
                      <dd className="mt-1 text-slate-900">
                        {request.propertyName ? `${request.propertyName} - ` : ''}
                        Unit {request.unitNumber}
                      </dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Category</dt>
                      <dd className="mt-1 text-slate-900">
                        {getCategoryLabel(request.category)}
                      </dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Created</dt>
                      <dd className="mt-1 text-slate-900">
                        {formatDateTime(request.createdAtUtc)}
                      </dd>
                    </div>
                  </dl>
                </div>

                <div className="space-y-4 rounded-lg border border-slate-200 bg-slate-50/80 p-4">
                  <Link
                    to={`/requests/${request.id}`}
                    className="inline-flex w-full items-center justify-center rounded-md border border-cyan-200 bg-white px-3 py-2 text-sm font-semibold text-cyan-700 transition hover:-translate-y-0.5 hover:bg-cyan-50 hover:shadow-sm"
                  >
                    {staffRole ? 'View details and add work note' : 'View details'}
                  </Link>

                  {canAssign && (
                    <label className="block">
                      <span className="text-sm font-medium text-slate-700">
                        Assigned staff
                      </span>
                      <select
                        value={request.assignedStaffProfileId ?? ''}
                        disabled={assigningRequestId === request.id}
                        onChange={(event) => {
                          void handleAssignRequest(request.id, event.target.value)
                        }}
                        className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                      >
                        <option value="">Unassigned</option>
                        {maintenanceStaff.map((staff) => (
                          <option key={staff.id} value={staff.id}>
                            {staff.fullName}
                          </option>
                        ))}
                      </select>
                    </label>
                  )}

                  {canUpdateRequestStatus(user) && allowedStatusValues.length > 0 ? (
                    <label className="block">
                      <span className="text-sm font-medium text-slate-700">
                        Update status
                      </span>
                      <select
                        value={getStatusValue(request.status)}
                        disabled={updatingRequestId === request.id}
                        onChange={(event) => {
                          void handleStatusChange(request.id, event.target.value)
                        }}
                        className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100 disabled:cursor-not-allowed disabled:bg-slate-100"
                      >
                        {statusOptions
                          .filter((option) => allowedStatusValues.includes(option.value))
                          .map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                      </select>
                    </label>
                  ) : (
                    <div>
                      <p className="text-sm font-medium text-slate-700">
                        Status tracking
                      </p>
                      <p className="mt-2 text-sm leading-6 text-slate-600">
                        You can follow progress and add notes from request details.
                      </p>
                    </div>
                  )}

                  <p className="text-xs leading-5 text-slate-500">
                    {updatingRequestId === request.id || assigningRequestId === request.id
                      ? 'Saving changes...'
                      : request.assignedStaffName
                        ? `Assigned to ${request.assignedStaffName}.`
                        : 'No staff member assigned yet.'}
                  </p>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      {isModalOpen && (
        <Modal
          title="Add Maintenance Request"
          description={
            tenantRole
              ? 'Create a service request for one of your active assigned units.'
              : 'Create a maintenance request for a selected tenant and rental unit.'
          }
          onClose={() => setIsModalOpen(false)}
        >
          <form className="space-y-4" onSubmit={handleCreateRequest}>
            {tenantRole ? (
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Assigned unit
                </span>
                <select
                  required
                  value={form.rentalUnitId}
                  onChange={(event) => updateForm('rentalUnitId', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  {assignedUnits.map((unit) => (
                    <option key={unit.id} value={unit.rentalUnitId}>
                      {unit.propertyName} - Unit {unit.unitNumber}
                    </option>
                  ))}
                </select>
              </label>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">
                    Rental unit reference
                  </span>
                  <input
                    required
                    value={form.rentalUnitId}
                    onChange={(event) => updateForm('rentalUnitId', event.target.value)}
                    className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                  />
                </label>
                <label className="block">
                  <span className="text-sm font-medium text-slate-700">
                    Tenant profile
                  </span>
                  <select
                    required
                    value={form.tenantProfileId}
                    onChange={(event) =>
                      updateForm('tenantProfileId', event.target.value)
                    }
                    className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                  >
                    {tenants.map((tenant) => (
                      <option key={tenant.id} value={tenant.id}>
                        {tenant.fullName}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            )}
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Title</span>
              <input
                required
                maxLength={200}
                value={form.title}
                onChange={(event) => updateForm('title', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Description
              </span>
              <textarea
                required
                rows={4}
                maxLength={2000}
                value={form.description}
                onChange={(event) => updateForm('description', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Category</span>
                <select
                  value={form.category}
                  onChange={(event) => updateForm('category', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  {categoryOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Priority</span>
                <select
                  value={form.priority}
                  onChange={(event) => updateForm('priority', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  {priorityOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex justify-center rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {isSubmitting ? 'Saving...' : 'Create Request'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}

export default RequestsPage
