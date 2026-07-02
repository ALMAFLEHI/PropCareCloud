import { Building2, DoorOpen, MapPinned } from 'lucide-react'

function PropertiesPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-sm font-semibold text-cyan-700">
          Properties and Units
        </p>
        <h2 className="mt-3 text-3xl font-semibold text-slate-950">
          Property management foundation
        </h2>
        <p className="mt-4 max-w-3xl text-base leading-7 text-slate-600">
          Future database-backed screens will manage properties, units,
          occupancy, tenant links, and maintenance history per location.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-3">
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <Building2 className="size-6 text-cyan-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Property records
          </h3>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Store building details, ownership context, and management status.
          </p>
        </article>
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <DoorOpen className="size-6 text-indigo-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Unit tracking
          </h3>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Track rental units, linked tenants, and request history.
          </p>
        </article>
        <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <MapPinned className="size-6 text-emerald-700" aria-hidden="true" />
          <h3 className="mt-4 text-base font-semibold text-slate-950">
            Location context
          </h3>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Prepare for property grouping, service areas, and cloud reporting.
          </p>
        </article>
      </section>
    </div>
  )
}

export default PropertiesPage
