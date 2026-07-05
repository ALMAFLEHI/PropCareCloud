import { Loader2 } from 'lucide-react'

type LoadingStateProps = {
  title?: string
  message?: string
}

function LoadingState({
  title = 'Loading data',
  message = 'Fetching the latest records.',
}: LoadingStateProps) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
      <div className="flex items-center gap-3">
        <Loader2 className="size-5 animate-spin text-cyan-700" aria-hidden="true" />
        <div>
          <p className="text-sm font-semibold text-slate-950">{title}</p>
          <p className="mt-1 text-sm text-slate-500">{message}</p>
        </div>
      </div>
    </div>
  )
}

export default LoadingState
