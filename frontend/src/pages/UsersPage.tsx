import axios from 'axios'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  Building2,
  KeyRound,
  Lock,
  RefreshCcw,
  ShieldCheck,
  ToggleLeft,
  UserCog,
  UserPlus,
  UsersRound,
} from 'lucide-react'
import {
  assignTenantToUnit,
  createInternalUser,
  endTenantUnitAssignment,
  getAdminUsers,
  getAvailableUnits,
  getTenantUnitAssignments,
  resetUserPassword,
  updateAccountStatus,
  updateUserProfile,
} from '../api/propCareApi'
import AccessDenied from '../components/AccessDenied'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import { useAuth } from '../context/AuthContext'
import type {
  AvailableUnitResponse,
  TenantUnitAssignmentResponse,
  UserAccountSummaryResponse,
  UserRole,
} from '../types/api'
import { formatDateTime } from '../utils/formatters'
import { getUserRoleKey } from '../utils/roles'

type ActiveTab = 'accounts' | 'create' | 'assignments' | 'rules'
type StatusFilter = '' | 'active' | 'disabled'

const tabs: Array<{ id: ActiveTab; label: string }> = [
  { id: 'accounts', label: 'Accounts' },
  { id: 'create', label: 'Create Internal User' },
  { id: 'assignments', label: 'Tenant Unit Assignments' },
  { id: 'rules', label: 'Access Rules' },
]

const roleFilterOptions: Array<{ value: UserRole | ''; label: string }> = [
  { value: '', label: 'All roles' },
  { value: 'AdminOwner', label: 'Admin / Owner' },
  { value: 'PropertyManager', label: 'Property Manager' },
  { value: 'Tenant', label: 'Tenant' },
  { value: 'MaintenanceStaff', label: 'Maintenance Staff' },
]

const internalRoleOptions: Array<{ value: UserRole; label: string }> = [
  { value: 'PropertyManager', label: 'Property Manager' },
  { value: 'MaintenanceStaff', label: 'Maintenance Staff' },
]

const initialCreateForm = {
  fullName: '',
  email: '',
  password: '',
  role: 'PropertyManager' as UserRole,
}

const initialAssignmentForm = {
  tenantProfileId: '',
  rentalUnitId: '',
}

function getErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined
    return data?.message ?? fallback
  }

  return fallback
}

function isRole(user: UserAccountSummaryResponse, role: string) {
  return getUserRoleKey(user.role) === role
}

function accountStatusClass(isActive: boolean) {
  return isActive
    ? 'bg-emerald-50 text-emerald-800 ring-emerald-200'
    : 'bg-slate-100 text-slate-700 ring-slate-200'
}

function UsersPage() {
  const { user, isAdminOwner } = useAuth()
  const [activeTab, setActiveTab] = useState<ActiveTab>('accounts')
  const [users, setUsers] = useState<UserAccountSummaryResponse[]>([])
  const [allUsers, setAllUsers] = useState<UserAccountSummaryResponse[]>([])
  const [assignments, setAssignments] = useState<TenantUnitAssignmentResponse[]>([])
  const [availableUnits, setAvailableUnits] = useState<AvailableUnitResponse[]>([])
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [createForm, setCreateForm] = useState(initialCreateForm)
  const [assignmentForm, setAssignmentForm] = useState(initialAssignmentForm)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [busyId, setBusyId] = useState('')
  const [profileUser, setProfileUser] = useState<UserAccountSummaryResponse | null>(null)
  const [profileName, setProfileName] = useState('')
  const [passwordUser, setPasswordUser] = useState<UserAccountSummaryResponse | null>(null)
  const [newPassword, setNewPassword] = useState('')

  const isActiveFilter =
    statusFilter === 'active' ? true : statusFilter === 'disabled' ? false : ''

  const summary = useMemo(() => {
    const activeAccounts = allUsers.filter((account) => account.isActive)
    const tenantProfiles = allUsers.filter((account) => isRole(account, 'Tenant'))
    const staffAccounts = allUsers.filter((account) =>
      isRole(account, 'MaintenanceStaff'),
    )

    return {
      totalUsers: allUsers.length,
      activeAccounts: activeAccounts.length,
      tenantProfiles: tenantProfiles.length,
      staffAccounts: staffAccounts.length,
    }
  }, [allUsers])

  const tenantOptions = useMemo(
    () => allUsers.filter((account) => isRole(account, 'Tenant') && account.isActive),
    [allUsers],
  )

  async function loadAccessData() {
    setIsLoading(true)
    setError('')

    try {
      const [filteredUsers, fullUserList, assignmentData, unitData] =
        await Promise.all([
          getAdminUsers({ role: roleFilter, isActive: isActiveFilter }),
          getAdminUsers(),
          getTenantUnitAssignments(),
          getAvailableUnits(),
        ])

      setUsers(filteredUsers)
      setAllUsers(fullUserList)
      setAssignments(assignmentData)
      setAvailableUnits(unitData)
      setAssignmentForm((current) => ({
        tenantProfileId: current.tenantProfileId || fullUserList.find((account) => isRole(account, 'Tenant'))?.userProfileId || '',
        rentalUnitId: current.rentalUnitId || unitData[0]?.rentalUnitId || '',
      }))
    } catch (loadError) {
      setError(
        getErrorMessage(
          loadError,
          'Access management data could not be loaded. Confirm the backend is running and the signed-in account is an Admin / Owner.',
        ),
      )
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    if (isAdminOwner) {
      void loadAccessData()
    }
  }, [isAdminOwner, roleFilter, statusFilter])

  if (!isAdminOwner) {
    return <AccessDenied />
  }

  function updateCreateForm(field: keyof typeof initialCreateForm, value: string) {
    setCreateForm((current) => ({ ...current, [field]: value }))
  }

  function updateAssignmentForm(
    field: keyof typeof initialAssignmentForm,
    value: string,
  ) {
    setAssignmentForm((current) => ({ ...current, [field]: value }))
  }

  function openProfileModal(account: UserAccountSummaryResponse) {
    setProfileUser(account)
    setProfileName(account.fullName)
  }

  function openPasswordModal(account: UserAccountSummaryResponse) {
    setPasswordUser(account)
    setNewPassword('')
  }

  async function handleCreateInternalUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await createInternalUser({
        fullName: createForm.fullName.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
        role: createForm.role,
      })
      setCreateForm(initialCreateForm)
      setSuccessMessage('Internal user account created.')
      setActiveTab('accounts')
      await loadAccessData()
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Internal user creation failed.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleProfileUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!profileUser) {
      return
    }

    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await updateUserProfile(profileUser.userProfileId, {
        fullName: profileName.trim(),
      })
      setProfileUser(null)
      setSuccessMessage('Profile name updated.')
      await loadAccessData()
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Profile update failed.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleStatusToggle(account: UserAccountSummaryResponse) {
    setBusyId(account.userProfileId)
    setError('')
    setSuccessMessage('')

    try {
      await updateAccountStatus(account.userProfileId, {
        isActive: !account.isActive,
      })
      setSuccessMessage(
        account.isActive ? 'Account disabled.' : 'Account reactivated.',
      )
      await loadAccessData()
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Account status update failed.'))
    } finally {
      setBusyId('')
    }
  }

  async function handlePasswordReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!passwordUser) {
      return
    }

    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await resetUserPassword(passwordUser.userProfileId, {
        newPassword,
      })
      setPasswordUser(null)
      setNewPassword('')
      setSuccessMessage('Password reset completed.')
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Password reset failed.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleAssignTenant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')
    setSuccessMessage('')

    try {
      await assignTenantToUnit({
        tenantProfileId: assignmentForm.tenantProfileId,
        rentalUnitId: assignmentForm.rentalUnitId,
      })
      setAssignmentForm(initialAssignmentForm)
      setSuccessMessage('Tenant unit assignment created.')
      await loadAccessData()
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Tenant unit assignment failed.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleEndAssignment(assignment: TenantUnitAssignmentResponse) {
    setBusyId(assignment.assignmentId)
    setError('')
    setSuccessMessage('')

    try {
      await endTenantUnitAssignment(assignment.assignmentId, {
        reason: 'Ended by admin access management.',
      })
      setSuccessMessage('Tenant unit assignment ended.')
      await loadAccessData()
    } catch (submitError) {
      setError(getErrorMessage(submitError, 'Ending assignment failed.'))
    } finally {
      setBusyId('')
    }
  }

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-3xl">
            <p className="text-sm font-semibold text-cyan-700">
              Access Management
            </p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950">
              Admin User & Access Management
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-600">
              Manage operational accounts, account availability, passwords, and tenant unit assignments from a protected admin console.
            </p>
          </div>
          <button
            type="button"
            onClick={() => {
              void loadAccessData()
            }}
            className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <RefreshCcw className="size-4" aria-hidden="true" />
            Refresh
          </button>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <SummaryCard title="Total users" value={summary.totalUsers} icon={UsersRound} />
        <SummaryCard title="Active accounts" value={summary.activeAccounts} icon={ShieldCheck} />
        <SummaryCard title="Tenant profiles" value={summary.tenantProfiles} icon={Building2} />
        <SummaryCard title="Staff accounts" value={summary.staffAccounts} icon={UserCog} />
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-2 shadow-sm">
        <div className="flex flex-wrap gap-2">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              className={`rounded-md px-3 py-2 text-sm font-semibold transition ${
                activeTab === tab.id
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
            void loadAccessData()
          }}
        />
      )}

      {isLoading && <LoadingState title="Loading access management" />}

      {!isLoading && activeTab === 'accounts' && (
        <section className="rounded-lg border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-col gap-4 border-b border-slate-200 px-5 py-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <h3 className="text-base font-semibold text-slate-950">
                User Accounts
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Account actions are enforced by the admin-only backend API.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <select
                value={roleFilter}
                onChange={(event) => setRoleFilter(event.target.value as UserRole | '')}
                className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              >
                {roleFilterOptions.map((option) => (
                  <option key={String(option.value)} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as StatusFilter)}
                className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              >
                <option value="">All statuses</option>
                <option value="active">Active</option>
                <option value="disabled">Disabled</option>
              </select>
            </div>
          </div>

          {users.length === 0 ? (
            <div className="p-5">
              <EmptyState
                title="No matching accounts"
                message="Adjust the role or status filter to view accounts."
              />
            </div>
          ) : (
            <div className="divide-y divide-slate-200">
              {users.map((account) => {
                const isCurrentUser = account.userProfileId === user?.userProfileId

                return (
                  <article
                    key={account.authUserAccountId}
                    className="grid gap-5 px-5 py-5 xl:grid-cols-[1fr_320px]"
                  >
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <h4 className="text-base font-semibold text-slate-950">
                          {account.fullName}
                        </h4>
                        <span
                          className={`inline-flex w-fit rounded-md px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${accountStatusClass(account.isActive)}`}
                        >
                          {account.isActive ? 'Active' : 'Disabled'}
                        </span>
                        <span className="inline-flex w-fit rounded-md bg-cyan-50 px-2.5 py-1 text-xs font-semibold text-cyan-800 ring-1 ring-inset ring-cyan-200">
                          {account.roleDisplayName}
                        </span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{account.email}</p>
                      <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
                        <InfoItem label="Created" value={formatDateTime(account.createdAtUtc)} />
                        <InfoItem label="Last login" value={formatDateTime(account.lastLoginAtUtc)} />
                        <InfoItem label="Active units" value={String(account.activeUnitCount)} />
                        <InfoItem label="Requests" value={String(account.requestCount)} />
                      </dl>
                    </div>
                    <div className="flex flex-col gap-2 rounded-lg bg-slate-50 p-4">
                      <button
                        type="button"
                        onClick={() => openProfileModal(account)}
                        className="inline-flex items-center justify-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
                      >
                        <UserCog className="size-4" aria-hidden="true" />
                        Edit profile
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          void handleStatusToggle(account)
                        }}
                        disabled={busyId === account.userProfileId || (isCurrentUser && account.isActive)}
                        className="inline-flex items-center justify-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400"
                      >
                        <ToggleLeft className="size-4" aria-hidden="true" />
                        {account.isActive ? 'Disable account' : 'Reactivate account'}
                      </button>
                      <button
                        type="button"
                        onClick={() => openPasswordModal(account)}
                        className="inline-flex items-center justify-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
                      >
                        <KeyRound className="size-4" aria-hidden="true" />
                        Reset password
                      </button>
                      {isCurrentUser && (
                        <p className="text-xs leading-5 text-slate-500">
                          Current admin account cannot be disabled here.
                        </p>
                      )}
                    </div>
                  </article>
                )
              })}
            </div>
          )}
        </section>
      )}

      {!isLoading && activeTab === 'create' && (
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <UserPlus className="size-5 text-cyan-700" aria-hidden="true" />
            <h3 className="text-base font-semibold text-slate-950">
              Create Internal User
            </h3>
          </div>
          <form className="mt-5 grid gap-4 lg:grid-cols-2" onSubmit={handleCreateInternalUser}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Full name</span>
              <input
                required
                maxLength={150}
                value={createForm.fullName}
                onChange={(event) => updateCreateForm('fullName', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Email</span>
              <input
                required
                type="email"
                maxLength={256}
                value={createForm.email}
                onChange={(event) => updateCreateForm('email', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Password</span>
              <input
                required
                type="password"
                minLength={8}
                maxLength={100}
                value={createForm.password}
                onChange={(event) => updateCreateForm('password', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Role</span>
              <select
                value={createForm.role}
                onChange={(event) => updateCreateForm('role', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              >
                {internalRoleOptions.map((option) => (
                  <option key={String(option.value)} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <div className="lg:col-span-2">
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex items-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                <UserPlus className="size-4" aria-hidden="true" />
                {isSubmitting ? 'Creating...' : 'Create account'}
              </button>
            </div>
          </form>
        </section>
      )}

      {!isLoading && activeTab === 'assignments' && (
        <section className="grid gap-5 xl:grid-cols-[380px_1fr]">
          <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center gap-2">
              <Building2 className="size-5 text-cyan-700" aria-hidden="true" />
              <h3 className="text-base font-semibold text-slate-950">
                Assign Tenant to Unit
              </h3>
            </div>
            <form className="mt-5 space-y-4" onSubmit={handleAssignTenant}>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Tenant</span>
                <select
                  required
                  value={assignmentForm.tenantProfileId}
                  onChange={(event) =>
                    updateAssignmentForm('tenantProfileId', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  {tenantOptions.map((tenant) => (
                    <option key={tenant.userProfileId} value={tenant.userProfileId}>
                      {tenant.fullName}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Available unit
                </span>
                <select
                  required
                  value={assignmentForm.rentalUnitId}
                  onChange={(event) =>
                    updateAssignmentForm('rentalUnitId', event.target.value)
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
              <button
                type="submit"
                disabled={
                  isSubmitting ||
                  tenantOptions.length === 0 ||
                  availableUnits.length === 0
                }
                className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                Assign unit
              </button>
              <p className="text-xs leading-5 text-slate-500">
                A rental unit can have only one active tenant assignment. Ending an active assignment makes that unit available again.
              </p>
            </form>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white shadow-sm">
            <div className="border-b border-slate-200 px-5 py-4">
              <h3 className="text-base font-semibold text-slate-950">
                Tenant Unit Assignments
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Active and historical unit relationships for tenant profiles.
              </p>
            </div>
            {assignments.length === 0 ? (
              <div className="p-5">
                <EmptyState
                  title="No tenant assignments"
                  message="Tenant unit assignments will appear here after they are created."
                />
              </div>
            ) : (
              <div className="divide-y divide-slate-200">
                {assignments.map((assignment) => (
                  <article
                    key={assignment.assignmentId}
                    className="grid gap-4 px-5 py-4 lg:grid-cols-[1fr_auto]"
                  >
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <h4 className="text-sm font-semibold text-slate-950">
                          {assignment.tenantName}
                        </h4>
                        <span
                          className={`inline-flex rounded-md px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${accountStatusClass(assignment.isActive)}`}
                        >
                          {assignment.isActive ? 'Active' : 'Ended'}
                        </span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">
                        {assignment.propertyName} - Unit {assignment.unitNumber}
                      </p>
                      <p className="mt-2 text-xs text-slate-500">
                        Lease start {formatDateTime(assignment.leaseStartDateUtc)}
                        {assignment.leaseEndDateUtc
                          ? ` - Ended ${formatDateTime(assignment.leaseEndDateUtc)}`
                          : ''}
                      </p>
                    </div>
                    {assignment.isActive && (
                      <button
                        type="button"
                        onClick={() => {
                          void handleEndAssignment(assignment)
                        }}
                        disabled={busyId === assignment.assignmentId}
                        className="inline-flex h-fit items-center justify-center gap-2 rounded-md border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:bg-slate-100"
                      >
                        End assignment
                      </button>
                    )}
                  </article>
                ))}
              </div>
            )}
          </div>
        </section>
      )}

      {!isLoading && activeTab === 'rules' && (
        <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <AccessRuleCard
            title="Admin / Owner"
            description="Manages accounts, access status, passwords, properties, requests, and tenant unit assignments."
            icon={ShieldCheck}
          />
          <AccessRuleCard
            title="Property Manager"
            description="Handles property operations, request triage, staff assignment, and workflow status updates."
            icon={Building2}
          />
          <AccessRuleCard
            title="Tenant"
            description="Views only their own requests and creates requests only for active assigned units."
            icon={UsersRound}
          />
          <AccessRuleCard
            title="Maintenance Staff"
            description="Views assigned jobs only and updates assigned work to in progress or completed."
            icon={Lock}
          />
        </section>
      )}

      {profileUser && (
        <Modal
          title="Edit Profile"
          description={profileUser.email}
          onClose={() => setProfileUser(null)}
        >
          <form className="space-y-4" onSubmit={handleProfileUpdate}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Full name</span>
              <input
                required
                maxLength={150}
                value={profileName}
                onChange={(event) => setProfileName(event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setProfileUser(null)}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex justify-center rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {isSubmitting ? 'Saving...' : 'Save changes'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {passwordUser && (
        <Modal
          title="Reset Password"
          description={`${passwordUser.fullName} - ${passwordUser.email}`}
          onClose={() => setPasswordUser(null)}
        >
          <form className="space-y-4" onSubmit={handlePasswordReset}>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                New password
              </span>
              <input
                required
                type="password"
                minLength={8}
                maxLength={100}
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => setPasswordUser(null)}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex justify-center rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {isSubmitting ? 'Saving...' : 'Reset password'}
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
}: {
  title: string
  value: number
  icon: typeof UsersRound
}) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-slate-500">{title}</p>
        <div className="flex size-10 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
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

function AccessRuleCard({
  title,
  description,
  icon: Icon,
}: {
  title: string
  description: string
  icon: typeof ShieldCheck
}) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex size-10 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
        <Icon className="size-5" aria-hidden="true" />
      </div>
      <h3 className="mt-4 text-base font-semibold text-slate-950">{title}</h3>
      <p className="mt-3 text-sm leading-6 text-slate-600">{description}</p>
    </article>
  )
}

export default UsersPage
