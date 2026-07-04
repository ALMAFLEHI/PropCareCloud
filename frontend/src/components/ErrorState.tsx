import { AlertTriangle, RotateCcw } from 'lucide-react'

type ErrorStateProps = {
  title?: string
  message: string
  onRetry?: () => void
}

function ErrorState({
  title = 'Unable to load data',
  message,
  onRetry,
}: ErrorStateProps) {
  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50 p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex gap-3">
          <AlertTriangle className="mt-0.5 size-5 text-amber-700" aria-hidden="true" />
          <div>
            <p className="text-sm font-semibold text-amber-950">{title}</p>
            <p className="mt-1 text-sm text-amber-800">{message}</p>
          </div>
        </div>
        {onRetry && (
          <button
            type="button"
            onClick={onRetry}
            className="inline-flex w-fit items-center gap-2 rounded-md border border-amber-300 bg-white px-3 py-2 text-sm font-medium text-amber-900 hover:bg-amber-100"
          >
            <RotateCcw className="size-4" aria-hidden="true" />
            Retry
          </button>
        )}
      </div>
    </div>
  )
}

export default ErrorState
