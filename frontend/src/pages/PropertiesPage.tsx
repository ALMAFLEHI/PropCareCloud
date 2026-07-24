import axios from 'axios'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  Building2,
  DoorOpen,
  Home,
  Pencil,
  Plus,
  RefreshCcw,
  Wrench,
} from 'lucide-react'
import {
  createProperty,
  createRentalUnit,
  getProperties,
  getUnitsByProperty,
  updateRentalUnit,
} from '../api/propCareApi'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import StatusBadge from '../components/StatusBadge'
import { useAuth } from '../context/AuthContext'
import type { PropertyResponse, RentalUnitResponse } from '../types/api'
import { formatDateTime } from '../utils/formatters'

const initialPropertyForm = {
  name: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  country: '',
  status: '0',
}

const initialUnitForm = {
  unitNumber: '',
  floor: '',
  bedrooms: '',
  status: '0',
}

function getRequestErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | {
          message?: string
          errors?: Record<string, string[]>
        }
      | undefined
    const validationMessage = data?.errors
      ? Object.values(data.errors).flat()[0]
      : undefined

    return data?.message ?? validationMessage ?? fallback
  }

  return fallback
}

function getUnitStatusFormValue(status: RentalUnitResponse['status']) {
  const normalized = String(status).toLowerCase()
  if (status === 1 || normalized.includes('occupied')) return '1'
  if (status === 2 || normalized.includes('maintenance')) return '2'
  return '0'
}

function PropertiesPage() {
  const { isAdminOwner, isPropertyManager } = useAuth()
  const [properties, setProperties] = useState<PropertyResponse[]>([])
  const [selectedPropertyId, setSelectedPropertyId] = useState('')
  const [units, setUnits] = useState<RentalUnitResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingUnits, setIsLoadingUnits] = useState(false)
  const [error, setError] = useState('')
  const [unitError, setUnitError] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [form, setForm] = useState(initialPropertyForm)
  const [unitModalMode, setUnitModalMode] = useState<'create' | 'edit' | null>(
    null,
  )
  const [editingUnit, setEditingUnit] = useState<RentalUnitResponse | null>(null)
  const [unitForm, setUnitForm] = useState(initialUnitForm)
  const [unitFormError, setUnitFormError] = useState('')
  const [isSavingUnit, setIsSavingUnit] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')

  const selectedProperty = useMemo(
    () => properties.find((property) => property.id === selectedPropertyId),
    [properties, selectedPropertyId],
  )
  const totalUnits = useMemo(
    () => properties.reduce((total, property) => total + property.unitCount, 0),
    [properties],
  )
  const occupiedUnits = useMemo(
    () =>
      units.filter((unit) => String(unit.status).toLowerCase() === '1' || String(unit.status).toLowerCase().includes('occupied')).length,
    [units],
  )
  const canManageUnits = isAdminOwner || isPropertyManager
  const unitsUnderMaintenance = useMemo(
    () =>
      units.filter((unit) => String(unit.status).toLowerCase() === '2' || String(unit.status).toLowerCase().includes('maintenance')).length,
    [units],
  )

  async function loadProperties() {
    setIsLoading(true)
    setError('')

    try {
      const data = await getProperties()
      setProperties(data)
      setSelectedPropertyId((currentId) => {
        if (currentId && data.some((property) => property.id === currentId)) {
          return currentId
        }

        return data[0]?.id ?? ''
      })
    } catch {
      setError('Properties could not be loaded. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  async function loadUnits(propertyId: string) {
    if (!propertyId) {
      setUnits([])
      return
    }

    setIsLoadingUnits(true)
    setUnitError('')

    try {
      const data = await getUnitsByProperty(propertyId)
      setUnits(data)
    } catch {
      setUnitError('Rental units could not be loaded for this property.')
    } finally {
      setIsLoadingUnits(false)
    }
  }

  useEffect(() => {
    void loadProperties()
  }, [])

  useEffect(() => {
    void loadUnits(selectedPropertyId)
  }, [selectedPropertyId])

  function updateForm(field: keyof typeof initialPropertyForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function updateUnitForm(
    field: keyof typeof initialUnitForm,
    value: string,
  ) {
    setUnitForm((current) => ({ ...current, [field]: value }))
  }

  function closeUnitModal() {
    setUnitModalMode(null)
    setEditingUnit(null)
    setUnitForm(initialUnitForm)
    setUnitFormError('')
  }

  function openCreateUnitModal() {
    if (!canManageUnits || !selectedProperty) return

    setSuccessMessage('')
    setEditingUnit(null)
    setUnitForm(initialUnitForm)
    setUnitFormError('')
    setUnitModalMode('create')
  }

  function openEditUnitModal(unit: RentalUnitResponse) {
    if (!canManageUnits) return

    setSuccessMessage('')
    setEditingUnit(unit)
    setUnitForm({
      unitNumber: unit.unitNumber,
      floor: unit.floor ?? '',
      bedrooms: unit.bedrooms === null || unit.bedrooms === undefined
        ? ''
        : String(unit.bedrooms),
      status: getUnitStatusFormValue(unit.status),
    })
    setUnitFormError('')
    setUnitModalMode('edit')
  }

  async function handleCreateProperty(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)

    try {
      const created = await createProperty({
        name: form.name.trim(),
        addressLine1: form.addressLine1.trim(),
        addressLine2: form.addressLine2.trim() || null,
        city: form.city.trim(),
        country: form.country.trim(),
        status: Number(form.status),
      })

      setForm(initialPropertyForm)
      setIsModalOpen(false)
      await loadProperties()
      setSelectedPropertyId(created.id)
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleSaveUnit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedPropertyId || !canManageUnits) return

    const unitNumber = unitForm.unitNumber.trim()
    if (!unitNumber) {
      setUnitFormError('Unit number is required.')
      return
    }

    if (unitNumber.length > 50 || unitForm.floor.trim().length > 50) {
      setUnitFormError('Unit number and floor must be 50 characters or fewer.')
      return
    }

    const bedrooms =
      unitForm.bedrooms.trim() === '' ? null : Number(unitForm.bedrooms)
    if (
      bedrooms !== null &&
      (!Number.isInteger(bedrooms) || bedrooms < 0 || bedrooms > 20)
    ) {
      setUnitFormError('Bedrooms must be a whole number between 0 and 20.')
      return
    }

    setIsSavingUnit(true)
    setUnitFormError('')

    try {
      const payload = {
        unitNumber,
        floor: unitForm.floor.trim() || null,
        bedrooms,
        status: Number(unitForm.status),
      }

      if (unitModalMode === 'edit' && editingUnit) {
        await updateRentalUnit(selectedPropertyId, editingUnit.id, payload)
      } else {
        await createRentalUnit(selectedPropertyId, payload)
      }

      const [propertyData, unitData] = await Promise.all([
        getProperties(),
        getUnitsByProperty(selectedPropertyId),
      ])
      setProperties(propertyData)
      setUnits(unitData)
      setSuccessMessage(
        unitModalMode === 'edit'
          ? 'Rental unit updated successfully.'
          : 'Rental unit created successfully.',
      )
      closeUnitModal()
    } catch (saveError) {
      setUnitFormError(
        getRequestErrorMessage(
          saveError,
          unitModalMode === 'edit'
            ? 'Rental unit could not be updated. Please try again.'
            : 'Rental unit could not be created. Please try again.',
        ),
      )
    } finally {
      setIsSavingUnit(false)
    }
  }

  return (
    <div className="space-y-6">
      <section className="premium-hero-card p-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="max-w-3xl">
            <p className="text-sm font-semibold text-cyan-700">
              Properties and Units
            </p>
            <h2 className="mt-3 text-3xl font-semibold text-slate-950">
              Property Portfolio
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-600">
              Review managed properties, rental units, and occupancy status.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="inline-flex w-fit items-center gap-2 rounded-md bg-cyan-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-cyan-800"
          >
            <Plus className="size-4" aria-hidden="true" />
            Add Property
          </button>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <article className="premium-card p-5">
          <Building2 className="size-6 text-cyan-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Properties
          </h3>
          <p className="mt-3 text-2xl font-semibold text-slate-950">
            {properties.length}
          </p>
        </article>
        <article className="premium-card p-5">
          <DoorOpen className="size-6 text-indigo-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Units
          </h3>
          <p className="mt-3 text-2xl font-semibold text-slate-950">
            {totalUnits}
          </p>
        </article>
        <article className="premium-card p-5">
          <Home className="size-6 text-emerald-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Occupied units
          </h3>
          <p className="mt-3 text-2xl font-semibold text-slate-950">
            {occupiedUnits}
          </p>
        </article>
        <article className="premium-card p-5">
          <Wrench className="size-6 text-amber-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Under maintenance
          </h3>
          <p className="mt-3 text-2xl font-semibold text-slate-950">
            {unitsUnderMaintenance}
          </p>
        </article>
      </section>

      {successMessage && (
        <div
          role="status"
          className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800"
        >
          {successMessage}
        </div>
      )}

      {isLoading && <LoadingState title="Loading properties" />}

      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            void loadProperties()
          }}
        />
      )}

      {!isLoading && !error && properties.length === 0 && (
        <EmptyState
          title="No properties found"
          message="Create a property to begin building the portfolio."
        />
      )}

      {!isLoading && !error && properties.length > 0 && (
        <section className="grid gap-5 xl:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
          <div className="premium-panel overflow-hidden">
            <div className="flex items-center justify-between gap-3 border-b border-slate-200 px-5 py-4">
              <div>
                <h3 className="text-base font-semibold text-slate-950">
                  Property Records
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  Select a property to view its rental units.
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  void loadProperties()
                }}
                className="inline-flex size-9 items-center justify-center rounded-md border border-slate-200 text-slate-600 hover:bg-slate-50"
                aria-label="Refresh properties"
              >
                <RefreshCcw className="size-4" aria-hidden="true" />
              </button>
            </div>
            <div className="divide-y divide-slate-200">
              {properties.map((property) => {
                const isSelected = property.id === selectedPropertyId

                return (
                  <button
                    key={property.id}
                    type="button"
                    onClick={() => setSelectedPropertyId(property.id)}
                    className={`block w-full px-5 py-4 text-left hover:bg-slate-50 ${
                      isSelected ? 'bg-cyan-50/70' : 'bg-white'
                    }`}
                  >
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <h4 className="text-sm font-semibold text-slate-950">
                          {property.name}
                        </h4>
                        <p className="mt-2 text-sm leading-6 text-slate-600">
                          {property.addressLine1}
                          {property.addressLine2 ? `, ${property.addressLine2}` : ''}
                        </p>
                        <p className="mt-1 text-sm text-slate-500">
                          {property.city}, {property.country}
                        </p>
                      </div>
                      <div className="flex flex-wrap gap-2 sm:justify-end">
                        <StatusBadge value={property.status} kind="property" />
                        <span className="inline-flex rounded-md bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700">
                          {property.unitCount} units
                        </span>
                      </div>
                    </div>
                  </button>
                )
              })}
            </div>
          </div>

          <div className="premium-panel overflow-hidden">
            <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 className="text-base font-semibold text-slate-950">
                  {selectedProperty?.name ?? 'Rental Units'}
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  Unit details for the selected property.
                </p>
              </div>
              {canManageUnits && selectedProperty && (
                <button
                  type="button"
                  onClick={openCreateUnitModal}
                  className="inline-flex w-fit items-center gap-2 rounded-md bg-cyan-700 px-3.5 py-2 text-sm font-semibold text-white hover:bg-cyan-800"
                >
                  <Plus className="size-4" aria-hidden="true" />
                  Add Unit
                </button>
              )}
            </div>

            <div className="p-5">
              {isLoadingUnits && (
                <LoadingState
                  title="Loading units"
                  message="Fetching rental units for the selected property."
                />
              )}

              {unitError && <ErrorState message={unitError} />}

              {!isLoadingUnits && !unitError && units.length === 0 && (
                <div className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center">
                  <div className="mx-auto flex size-11 items-center justify-center rounded-md bg-slate-100 text-slate-600">
                    <DoorOpen className="size-5" aria-hidden="true" />
                  </div>
                  <p className="mt-4 text-sm font-semibold text-slate-950">
                    No units found
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-500">
                    The selected property has no rental units yet.
                  </p>
                  {canManageUnits && (
                    <button
                      type="button"
                      onClick={openCreateUnitModal}
                      className="mt-5 inline-flex items-center gap-2 rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800"
                    >
                      <Plus className="size-4" aria-hidden="true" />
                      Add First Unit
                    </button>
                  )}
                </div>
              )}

              {!isLoadingUnits && !unitError && units.length > 0 && (
                <div className="grid gap-3 md:grid-cols-2">
                  {units.map((unit) => (
                    <article
                      key={unit.id}
                      className="rounded-lg border border-slate-200 p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <h4 className="text-sm font-semibold text-slate-950">
                            Unit {unit.unitNumber}
                          </h4>
                          <p className="mt-2 text-sm text-slate-500">
                            Floor {unit.floor ?? 'Not set'} - Bedrooms{' '}
                            {unit.bedrooms ?? 'Not set'}
                          </p>
                        </div>
                        <div className="flex items-center gap-2">
                          <StatusBadge value={unit.status} kind="unit" />
                          {canManageUnits && (
                            <button
                              type="button"
                              onClick={() => openEditUnitModal(unit)}
                              className="inline-flex size-8 items-center justify-center rounded-md border border-slate-200 text-slate-600 hover:border-cyan-200 hover:bg-cyan-50 hover:text-cyan-800"
                              aria-label={`Edit unit ${unit.unitNumber}`}
                              title="Edit unit"
                            >
                              <Pencil className="size-3.5" aria-hidden="true" />
                            </button>
                          )}
                        </div>
                      </div>
                      <p className="mt-4 text-xs text-slate-500">
                        Created {formatDateTime(unit.createdAtUtc)}
                      </p>
                    </article>
                  ))}
                </div>
              )}
            </div>
          </div>
        </section>
      )}

      {isModalOpen && (
        <Modal
          title="Add Property"
          description="Create a property record for the managed portfolio."
          onClose={() => setIsModalOpen(false)}
        >
          <form className="space-y-4" onSubmit={handleCreateProperty}>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Name</span>
                <input
                  required
                  value={form.name}
                  onChange={(event) => updateForm('name', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Status</span>
                <select
                  value={form.status}
                  onChange={(event) => updateForm('status', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  <option value="0">Active</option>
                  <option value="1">Inactive</option>
                  <option value="2">Under maintenance</option>
                </select>
              </label>
            </div>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Address line 1
              </span>
              <input
                required
                value={form.addressLine1}
                onChange={(event) => updateForm('addressLine1', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                Address line 2
              </span>
              <input
                value={form.addressLine2}
                onChange={(event) => updateForm('addressLine2', event.target.value)}
                className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
              />
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">City</span>
                <input
                  required
                  value={form.city}
                  onChange={(event) => updateForm('city', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">Country</span>
                <input
                  required
                  value={form.country}
                  onChange={(event) => updateForm('country', event.target.value)}
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
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
                {isSubmitting ? 'Saving...' : 'Create Property'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {unitModalMode && selectedProperty && (
        <Modal
          title={unitModalMode === 'edit' ? 'Edit Unit' : 'Add Unit'}
          description={`${unitModalMode === 'edit' ? 'Update' : 'Create'} a rental unit for ${selectedProperty.name}.`}
          onClose={closeUnitModal}
        >
          <form className="space-y-4" onSubmit={handleSaveUnit}>
            {unitFormError && (
              <div
                role="alert"
                className="rounded-md border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800"
              >
                {unitFormError}
              </div>
            )}
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Unit number
                </span>
                <input
                  required
                  maxLength={50}
                  value={unitForm.unitNumber}
                  onChange={(event) =>
                    updateUnitForm('unitNumber', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Floor
                </span>
                <input
                  maxLength={50}
                  value={unitForm.floor}
                  onChange={(event) =>
                    updateUnitForm('floor', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Bedrooms
                </span>
                <input
                  type="number"
                  min="0"
                  max="20"
                  step="1"
                  value={unitForm.bedrooms}
                  onChange={(event) =>
                    updateUnitForm('bedrooms', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                />
              </label>
              <label className="block">
                <span className="text-sm font-medium text-slate-700">
                  Status
                </span>
                <select
                  value={unitForm.status}
                  onChange={(event) =>
                    updateUnitForm('status', event.target.value)
                  }
                  className="mt-2 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-cyan-700 focus:ring-2 focus:ring-cyan-100"
                >
                  <option value="0">Available</option>
                  <option value="1">Occupied</option>
                  <option value="2">Under maintenance</option>
                </select>
              </label>
            </div>
            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={closeUnitModal}
                className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSavingUnit}
                className="inline-flex justify-center rounded-md bg-cyan-700 px-4 py-2 text-sm font-semibold text-white hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {isSavingUnit
                  ? 'Saving...'
                  : unitModalMode === 'edit'
                    ? 'Save Changes'
                    : 'Create Unit'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}

export default PropertiesPage
