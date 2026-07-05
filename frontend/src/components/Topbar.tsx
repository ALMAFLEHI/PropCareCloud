import { CircleCheck, Cloud, LogOut, UserRound } from 'lucide-react'
import { useAuth } from '../context/AuthContext'

function Topbar() {
  const { logout, user } = useAuth()

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
            <p className="text-sm text-slate-500">
              Property Maintenance Portal
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="inline-flex w-fit items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm font-medium text-emerald-800">
            <CircleCheck className="size-4" aria-hidden="true" />
            <span>Secure Portal</span>
          </div>
          {user && (
            <div className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-medium text-slate-700">
              <UserRound className="size-4" aria-hidden="true" />
              <span>{user.fullName}</span>
              <span className="rounded bg-white px-2 py-0.5 text-xs font-semibold text-cyan-700">
                {user.roleDisplayName}
              </span>
            </div>
          )}
          <button
            type="button"
            onClick={logout}
            className="inline-flex w-fit items-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <LogOut className="size-4" aria-hidden="true" />
            Logout
          </button>
        </div>
      </div>
    </header>
  )
}

export default Topbar
