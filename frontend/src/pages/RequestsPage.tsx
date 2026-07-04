import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  ClipboardCheck,
  ListChecks,
  Plus,
  RefreshCcw,
  Send,
  Wrench,
} from 'lucide-react'
import {
  createMaintenanceRequest,
  getMaintenanceRequests,
  updateMaintenanceRequestStatus,
} from '../api/propCareApi'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../context/AuthContext'
import type { ApiEnumValue, MaintenanceRequestResponse } from '../types/api'
import { formatDateTime, getCategoryLabel } from '../utils/formatters'
import type { UserRoleKey } from '../utils/roles'

const requestSteps = [
  {
    title: 'Tenant request creation',
    description: 'Create maintenance records with seeded tenant and unit IDs.',
    icon: Send,
  },
  {
    title: 'Manager review',
    description: 'Review live records returned by the backend API.',
    icon: ListChecks,
  },
  {
    title: 'Status update',
    description: 'Move requests through submitted, assigned, progress, and closed states.',
    icon: Wrench,
  },
  {
    title: 'Completion tracking',
    description: 'Retain request timestamps, comments, and future attachment metadata.',
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
    title: 'Portfolio request oversight',
    description:
      'Review all maintenance records across the demo portfolio and monitor request workload.',
  },
  PropertyManager: {
    title: 'Maintenance request management',
    description:
      'Review tenant requests, track priorities, and update request status for local workflow validation.',
  },
  Tenant: {
    title: 'Submit and track requests',
    description:
      'Create a maintenance request with seeded tenant/unit IDs and follow request progress.',
  },
  MaintenanceStaff: {
    title: 'Assigned work queue',
    description:
      'Review maintenance jobs and update progress as work moves through the demo workflow.',
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
  const { userRoleKey } = useAuth()
  const [requests, setRequests] = useState<MaintenanceRequestResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [updatingRequestId, setUpdatingRequestId] = useState('')
  const [form, setForm] = useState(initialRequestForm)
  const copy = requestPageCopy[userRoleKey ?? 'Tenant']

  const seededRequest = requests[0]

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
      const data = await getMaintenanceRequests()
      setRequests(data)
      const firstRequest = data[0]
      if (firstRequest) {
        setForm((current) => ({
          ...current,
          rentalUnitId: current.rentalUnitId || firstRequest.rentalUnitId,
          tenantProfileId: current.tenantProfileId || firstRequest.tenantProfileId,
        }))
      }
    } catch {
      setError(
        'Maintenance requests could not be loaded. Confirm the backend is running on http://localhost:5015.',
      )
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadRequests()
  }, [])

  function updateForm(field: keyof typeof initialRequestForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function openCreateModal() {
    setForm((current) => ({
      ...current,
      rentalUnitId: current.rentalUnitId || seededRequest?.rentalUnitId || '',
      tenantProfileId: current.tenantProfileId || seededRequest?.tenantProfileId || '',
    }))
    setIsModalOpen(true)
  }

  async function handleStatusChange(requestId: string, value: string) {
    setUpdatingRequestId(requestId)

    try {
      await updateMaintenanceRequestStatus(requestId, { status: Number(value) })
      await loadRequests()
    } finally {
      setUpdatingRequestId('')
    }
  }

  async function handleCreateRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)

    try {
      await createMaintenanceRequest({
        rentalUnitId: form.rentalUnitId.trim(),
        tenantProfileId: form.tenantProfileId.trim(),
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
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
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
          {userRoleKey !== 'MaintenanceStaff' && (
            <button
              type="button"
              onClick={openCreateModal}
              className="inline-flex w-fit items-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800"
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
              className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
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
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm font-medium text-slate-500">Total Requests</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {requests.length}
          </p>
        </article>
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm font-medium text-slate-500">Open Requests</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">
            {openRequests}
          </p>
        </article>
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm font-medium text-slate-500">Seed Helper</p>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            {seededRequest
              ? `Unit ${seededRequest.unitNumber} and ${seededRequest.tenantName} are available for demo create tests.`
              : 'Seeded IDs appear here after request data loads.'}
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
          message="Run the seed endpoint or create a new request with valid local IDs."
        />
      )}

      {!isLoading && !error && requests.length > 0 && (
        <section className="rounded-lg border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">
                Request Records
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Status changes call PATCH `/api/maintenance-requests/{'{id}'}/status`.
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
              <article key={request.id} className="grid gap-5 px-5 py-5 xl:grid-cols-[1fr_240px]">
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
                  <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
                    <div>
                      <dt className="font-medium text-slate-500">Tenant</dt>
                      <dd className="mt-1 text-slate-900">{request.tenantName}</dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Unit</dt>
                      <dd className="mt-1 text-slate-900">{request.unitNumber}</dd>
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

                <div className="rounded-lg bg-slate-50 p-4">
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
                      {statusOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <p className="mt-3 text-xs leading-5 text-slate-500">
                    {updatingRequestId === request.id
                      ? 'Saving status update...'
                      : 'Changes are persisted to the local backend.'}
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
          description="Creates a demo request using existing seeded tenant and rental unit IDs."
          onClose={() => setIsModalOpen(false)}
        >
          <form className="space-y-4" onSubmit={handleCreateRequest}>
            <div className="rounded-md bg-slate-50 p-4 text-sm leading-6 text-slate-600">
              Use the seeded IDs below for local validation until authentication
              and tenant lookup screens are added.
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Rental unit ID
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
                  Tenant profile ID
                </span>
                <input
                  required
                  value={form.tenantProfileId}
                  onChange={(event) =>
                    updateForm('tenantProfileId', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
            </div>
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
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
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
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
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
