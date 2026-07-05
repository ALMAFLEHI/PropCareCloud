import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Building2, DoorOpen, Home, Plus, RefreshCcw, Wrench } from 'lucide-react'
import { createProperty, getProperties, getUnitsByProperty } from '../api/propCareApi'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import Modal from '../components/Modal'
import StatusBadge from '../components/StatusBadge'
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

function PropertiesPage() {
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
            <div className="border-b border-slate-200 px-5 py-4">
              <h3 className="text-base font-semibold text-slate-950">
                {selectedProperty?.name ?? 'Rental Units'}
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Unit details for the selected property.
              </p>
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
                <EmptyState
                  title="No units found"
                  message="The selected property has no rental units yet."
                />
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
                        <StatusBadge value={unit.status} kind="unit" />
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
    </div>
  )
}

export default PropertiesPage
