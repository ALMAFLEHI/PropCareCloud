function AccessDenied() {
  return (
    <div className="min-h-screen bg-slate-100 px-4 py-10 text-slate-950">
      <div className="mx-auto max-w-2xl rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-sm font-semibold text-rose-700">Access denied</p>
        <h1 className="mt-3 text-2xl font-semibold text-slate-950">
          This page is not available for your role
        </h1>
        <p className="mt-3 text-sm leading-6 text-slate-600">
          PropCare Cloud protects role-specific sections in both the frontend
          and backend API. Use the sidebar to open the areas available to the
          signed-in demo account.
        </p>
      </div>
    </div>
  )
}

export default AccessDenied
