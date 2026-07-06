import axios from 'axios'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  CheckCircle2,
  Clock3,
  KeyRound,
  RefreshCcw,
  UserCheck,
  XCircle,
} from 'lucide-react'
import {
  approveTenantRegistration,
  getTenantRegistrationAvailableUnits,
  getTenantRegistrations,
  rejectTenantRegistration,
} from '../api/propCareApi'
import AccessDenied from '../components/AccessDenied'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import { useAuth } from '../context/AuthContext'
import type {
  AvailableUnitResponse,
  TenantRegistrationResponse,
  TenantRegistrationStatus,
} from '../types/api'
import { formatDateTime } from '../utils/formatters'

type StatusFilter = 'Pending' | 'Approved' | 'Rejected' | 'All'

const statusTabs: Array<{ value: StatusFilter; label: string }> = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'All', label: 'All' },
]

const initialApproveForm = {
  rentalUnitId: '',
  temporaryPassword: '',
  reviewNote: '',
}

function getErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined
    return data?.message ?? fallback
  }

  return fallback
}

function getStatusKey(status: TenantRegistrationStatus): StatusFilter {
  if (status === 'Pending' || status === 0) return 'Pending'
  if (status === 'Approved' || status === 1) return 'Approved'
  if (status === 'Rejected' || status === 2) return 'Rejected'

  return 'All'
}

function statusBadgeClass(status: TenantRegistrationStatus) {
  const key = getStatusKey(status)

  if (key === 'Approved') {
    return 'bg-emerald-50 text-emerald-800 ring-emerald-200'
  }

  if (key === 'Rejected') {
    return 'bg-rose-50 text-rose-800 ring-rose-200'
  }

  return 'bg-amber-50 text-amber-800 ring-amber-200'
}

function TenantRegistrationsPage() {
  const { isAdminOwner, isPropertyManager } = useAuth()
  const [activeFilter, setActiveFilter] = useState<StatusFilter>('Pending')
  const [registrations, setRegistrations] = useState<TenantRegistrationResponse[]>(
    [],
  )
  const [availableUnits, setAvailableUnits] = useState<AvailableUnitResponse[]>(
    [],
  )
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [approveTarget, setApproveTarget] =
    useState<TenantRegistrationResponse | null>(null)
  const [rejectTarget, setRejectTarget] =
    useState<TenantRegistrationResponse | null>(null)
  const [approveForm, setApproveForm] = useState(initialApproveForm)
  const [rejectNote, setRejectNote] = useState('')

  const canReview = isAdminOwner || isPropertyManager

  const summary = useMemo(() => {
    return {
      pending: registrations.filter(
        (registration) => getStatusKey(registration.status) === 'Pending',
      ).length,
      approved: registrations.filter(
        (registration) => getStatusKey(registration.status) === 'Approved',
      ).length,
      rejected: registrations.filter(
        (registration) => getStatusKey(registration.status) === 'Rejected',
      ).length,
    }
  }, [registrations])

  const visibleRegistrations = useMemo(() => {
    if (activeFilter === 'All') {
      return registrations
    }

    return registrations.filter(
      (registration) => getStatusKey(registration.status) === activeFilter,
    )
  }, [activeFilter, registrations])

  async function loadRegistrations() {
    setIsLoading(true)
    setError('')

    try {
      const [registrationData, unitData] = await Promise.all([
        getTenantRegistrations('All'),
        getTenantRegistrationAvailableUnits(),
      ])
      setRegistrations(registrationData)
      setAvailableUnits(unitData)
    } catch (loadError) {
      setError(
        getErrorMessage(
          loadError,
          'Tenant registration requests could not be loaded. Confirm the signed-in account has Admin / Owner or Property Manager access.',
        ),
      )
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    if (canReview) {
      void loadRegistrations()
    }
  }, [canReview])

  if (!canReview) {
    return <AccessDenied />
  }

  function openApproveModal(registration: TenantRegistrationResponse) {
    setApproveTarget(registration)
    setApproveForm({
      ...initialApproveForm,
      rentalUnitId: availableUnits[0]?.rentalUnitId ?? '',
    })
  }

  function openRejectModal(registration: TenantRegistrationResponse) {
    setRejectTarget(registration)
    setRejectNote('')
  }

  function updateApproveForm(
    field: keyof typeof initialApproveForm,
    value: string,
  ) {
    setApproveForm((current) => ({ ...current, [field]: value }))
  }

  async function handleApprove(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!approveTarget) {
      return
    }

    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await approveTenantRegistration(approveTarget.id, {
        rentalUnitId: approveForm.rentalUnitId,
        temporaryPassword: approveForm.temporaryPassword,
        reviewNote: approveForm.reviewNote.trim() || null,
      })
      setApproveTarget(null)
      setApproveForm(initialApproveForm)
      setSuccessMessage('Tenant registration approved and assigned to a unit.')
      await loadRegistrations()
      setActiveFilter('Approved')
    } catch (approveError) {
      setError(
        getErrorMessage(
          approveError,
          'Tenant registration approval failed. Check the unit and password, then try again.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleReject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!rejectTarget) {
      return
    }

    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await rejectTenantRegistration(rejectTarget.id, {
        reviewNote: rejectNote.trim() || null,
      })
      setRejectTarget(null)
      setRejectNote('')
      setSuccessMessage('Tenant registration rejected.')
      await loadRegistrations()
      setActiveFilter('Rejected')
    } catch (rejectError) {
      setError(
        getErrorMessage(
          rejectError,
          'Tenant registration rejection failed. Please try again.',
        ),
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
              Tenant Registration
            </p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950">
              Tenant Registration & Approval Workflow
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-600">
              Review public tenant access requests, approve verified applicants,
              assign available rental units, or reject requests that cannot be
              verified.
            </p>
          </div>
          <button
            type="button"
            onClick={() => {
              void loadRegistrations()
            }}
            className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <RefreshCcw className="size-4" aria-hidden="true" />
            Refresh
          </button>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-3">
        <SummaryCard
          title="Pending requests"
          value={summary.pending}
          icon={Clock3}
          tone="amber"
        />
        <SummaryCard
          title="Approved requests"
          value={summary.approved}
          icon={CheckCircle2}
          tone="emerald"
        />
        <SummaryCard
          title="Rejected requests"
          value={summary.rejected}
          icon={XCircle}
          tone="rose"
        />
      </section>

      <section className="premium-panel p-2">
        <div className="flex flex-wrap gap-2">
          {statusTabs.map((tab) => (
            <button
              key={tab.value}
              type="button"
              onClick={() => setActiveFilter(tab.value)}
              className={`rounded-md px-3 py-2 text-sm font-semibold transition ${
                activeFilter === tab.value
                  ? 'bg-cyan-700 text-white'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-950'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </section>

      {successMessage && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-medium text-emerald-900">
          {successMessage}
        </div>
      )}

      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            void loadRegistrations()
          }}
        />
      )}

      {isLoading && <LoadingState title="Loading tenant registrations" />}

      {!isLoading && !error && visibleRegistrations.length === 0 && (
        <EmptyState
          title="No registration requests"
          message="Tenant registration requests will appear here after visitors submit public portal access requests."
        />
      )}

      {!isLoading && !error && visibleRegistrations.length > 0 && (
        <section className="premium-panel overflow-hidden">
          <div className="border-b border-slate-200 px-5 py-4">
            <h3 className="text-base font-semibold text-slate-950">
              Registration Requests
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Filter, review, and action tenant portal access requests.
            </p>
          </div>

          <div className="divide-y divide-slate-200">
            {visibleRegistrations.map((registration) => {
              const isPending = getStatusKey(registration.status) === 'Pending'

              return (
                <article
                  key={registration.id}
                  className="grid gap-5 px-5 py-5 xl:grid-cols-[1fr_260px]"
                >
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h4 className="text-base font-semibold text-slate-950">
                        {registration.fullName}
                      </h4>
                      <span
                        className={`inline-flex w-fit rounded-md px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${statusBadgeClass(registration.status)}`}
                      >
                        {registration.statusDisplayName}
                      </span>
                    </div>
                    <p className="mt-2 text-sm text-slate-600">
                      {registration.email}
                      {registration.phoneNumber
                        ? ` - ${registration.phoneNumber}`
                        : ''}
                    </p>
                    <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
                      <InfoItem
                        label="Requested"
                        value={registration.requestedPropertyOrUnit ?? 'Not specified'}
                      />
                      <InfoItem
                        label="Submitted"
                        value={formatDateTime(registration.submittedAtUtc)}
                      />
                      <InfoItem
                        label="Reviewed"
                        value={formatDateTime(registration.reviewedAtUtc)}
                      />
                      <InfoItem
                        label="Assigned unit"
                        value={
                          registration.approvedPropertyName &&
                          registration.approvedUnitNumber
                            ? `${registration.approvedPropertyName} - Unit ${registration.approvedUnitNumber}`
                            : 'Not assigned'
                        }
                      />
                    </dl>
                    {registration.note && (
                      <p className="mt-4 rounded-lg bg-slate-50 p-3 text-sm leading-6 text-slate-600">
                        {registration.note}
                      </p>
                    )}
                    {registration.reviewNote && (
                      <p className="mt-3 text-xs leading-5 text-slate-500">
                        Review note: {registration.reviewNote}
                      </p>
                    )}
                  </div>

                  <div className="flex flex-col gap-2 rounded-lg bg-slate-50 p-4">
                    {isPending ? (
                      <>
                        <button
                          type="button"
                          onClick={() => openApproveModal(registration)}
                          className="inline-flex items-center justify-center gap-2 rounded-md bg-cyan-700 px-3 py-2 text-sm font-semibold text-white hover:bg-cyan-800"
                        >
                          <UserCheck className="size-4" aria-hidden="true" />
                          Approve
                        </button>
                        <button
                          type="button"
                          onClick={() => openRejectModal(registration)}
                          className="inline-flex items-center justify-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
                        >
                          <XCircle className="size-4" aria-hidden="true" />
                          Reject
                        </button>
                      </>
                    ) : (
                      <div className="rounded-md border border-slate-200 bg-white p-3 text-sm text-slate-600">
                        Reviewed by {registration.reviewedByName ?? 'portal user'}.
                      </div>
                    )}
                    <p className="text-xs leading-5 text-slate-500">
                      Approval creates tenant access only after a unit is selected.
                    </p>
                  </div>
                </article>
              )
            })}
          </div>
        </section>
      )}

      {approveTarget && (
        <Modal
          title="Approve Tenant Registration"
          description={`${approveTarget.fullName} - ${approveTarget.email}`}
          onClose={() => setApproveTarget(null)}
        >
          <form className="space-y-4" onSubmit={handleApprove}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Available unit
              </span>
              <select
                required
                value={approveForm.rentalUnitId}
                onChange={(event) =>
                  updateApproveForm('rentalUnitId', event.target.value)
                }
                className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              >
                {availableUnits.length === 0 && (
                  <option value="">No available units</option>
                )}
                {availableUnits.map((unit) => (
                  <option key={unit.rentalUnitId} value={unit.rentalUnitId}>
                    {unit.propertyName} - Unit {unit.unitNumber}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Temporary password
              </span>
              <input
                required
                type="password"
                minLength={8}
                maxLength={100}
                value={approveForm.temporaryPassword}
                onChange={(event) =>
                  updateApproveForm('temporaryPassword', event.target.value)
                }
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Review note
              </span>
              <textarea
                rows={3}
                maxLength={1000}
                value={approveForm.reviewNote}
                onChange={(event) =>
                  updateApproveForm('reviewNote', event.target.value)
                }
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>

            <div className="rounded-lg border border-cyan-100 bg-cyan-50 p-3 text-xs leading-5 text-cyan-900">
              The password is hashed by the backend and is not returned in the
              API response.
            </div>

            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setApproveTarget(null)}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={
                  isSubmitting ||
                  availableUnits.length === 0 ||
                  !approveForm.rentalUnitId
                }
                className="inline-flex items-center justify-center gap-2 rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                <KeyRound className="size-4" aria-hidden="true" />
                {isSubmitting ? 'Approving...' : 'Confirm approve'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {rejectTarget && (
        <Modal
          title="Reject Tenant Registration"
          description={`${rejectTarget.fullName} - ${rejectTarget.email}`}
          onClose={() => setRejectTarget(null)}
        >
          <form className="space-y-4" onSubmit={handleReject}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Review or rejection note
              </span>
              <textarea
                rows={4}
                maxLength={1000}
                value={rejectNote}
                onChange={(event) => setRejectNote(event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setRejectTarget(null)}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex justify-center rounded-md bg-rose-700 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {isSubmitting ? 'Rejecting...' : 'Confirm reject'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}

function SummaryCard({
  title,
  value,
  icon: Icon,
  tone,
}: {
  title: string
  value: number
  icon: typeof Clock3
  tone: 'amber' | 'emerald' | 'rose'
}) {
  const toneClasses = {
    amber: 'bg-amber-50 text-amber-700',
    emerald: 'bg-emerald-50 text-emerald-700',
    rose: 'bg-rose-50 text-rose-700',
  }

  return (
    <article className="premium-card p-5">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-slate-500">{title}</p>
        <div
          className={`flex size-10 items-center justify-center rounded-md ${toneClasses[tone]}`}
        >
          <Icon className="size-5" aria-hidden="true" />
        </div>
      </div>
      <p className="mt-3 text-3xl font-semibold text-slate-950">{value}</p>
    </article>
  )
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 text-slate-900">{value}</dd>
    </div>
  )
}

export default TenantRegistrationsPage
