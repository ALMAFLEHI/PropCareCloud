import { CircleCheck, Cloud } from 'lucide-react'

function Topbar() {
  return (
    <header className="border-b border-slate-200 bg-white px-4 py-4 sm:px-6 lg:px-8">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-slate-900 text-white">
            <Cloud className="size-5" aria-hidden="true" />
          </div>
          <div>
            <h1 className="text-lg font-semibold text-slate-950">
              PropCare Cloud
            </h1>
            <p className="text-sm text-slate-500">Sprint 8 CRUD Integration</p>
          </div>
        </div>

        <div className="inline-flex w-fit items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-800">
          <CircleCheck className="size-4" aria-hidden="true" />
          <span>Local Development</span>
        </div>
      </div>
    </header>
  )
}

export default Topbar
