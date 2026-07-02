import { ClipboardCheck, ListChecks, Send, Wrench } from 'lucide-react'

const requestSteps = [
  {
    title: 'Tenant request creation',
    description: 'Tenants will submit maintenance issues with category, priority, and notes.',
    icon: Send,
  },
  {
    title: 'Manager assignment',
    description: 'Property managers will review requests and assign maintenance staff.',
    icon: ListChecks,
  },
  {
    title: 'Staff progress update',
    description: 'Maintenance staff will update job status during repair work.',
    icon: Wrench,
  },
  {
    title: 'Completion tracking',
    description: 'Completed work will keep a clear record for tenants and managers.',
    icon: ClipboardCheck,
  },
]

function RequestsPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-sm font-semibold text-cyan-700">
          Maintenance Requests
        </p>
        <h2 className="mt-3 text-3xl font-semibold text-slate-950">
          Request workflow foundation
        </h2>
        <p className="mt-4 max-w-3xl text-base leading-7 text-slate-600">
          This page reserves the future workflow for tenant maintenance request
          creation, manager assignment, staff updates, and completion tracking.
        </p>
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
    </div>
  )
}

export default RequestsPage
