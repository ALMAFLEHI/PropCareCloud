import { CheckCircle2, Circle, Clock3, XCircle } from 'lucide-react'
import type { ApiEnumValue } from '../types/api'

type StatusTimelineProps = {
  status: ApiEnumValue
  compact?: boolean
}

const steps = [
  { value: 0, label: 'Submitted' },
  { value: 1, label: 'Under Review' },
  { value: 2, label: 'Assigned' },
  { value: 3, label: 'In Progress' },
  { value: 4, label: 'Completed' },
]

const statusMap: Record<string, number> = {
  submitted: 0,
  underreview: 1,
  assigned: 2,
  inprogress: 3,
  completed: 4,
  cancelled: 5,
}

function getStatusNumber(status: ApiEnumValue) {
  if (typeof status === 'number') {
    return status
  }

  const parsed = Number(status)
  if (!Number.isNaN(parsed) && status.trim() !== '') {
    return parsed
  }

  return statusMap[status.replace(/\s+/g, '').toLowerCase()] ?? 0
}

function StatusTimeline({ status, compact = false }: StatusTimelineProps) {
  const currentStatus = getStatusNumber(status)
  const isCancelled = currentStatus === 5
  const activeIndex = isCancelled ? 0 : Math.min(currentStatus, steps.length - 1)

  if (isCancelled) {
    return (
      <div className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-900">
        <div className="flex items-center gap-2 font-semibold">
          <XCircle className="size-4" aria-hidden="true" />
          Request Cancelled
        </div>
        {!compact && (
          <p className="mt-2 text-rose-800">
            This request is no longer moving through the service workflow.
          </p>
        )}
      </div>
    )
  }

  return (
    <ol
      className={
        compact
          ? 'grid gap-2 sm:grid-cols-5'
          : 'grid gap-3 md:grid-cols-5'
      }
    >
      {steps.map((step, index) => {
        const isComplete = index < activeIndex
        const isCurrent = index === activeIndex
        const Icon = isComplete ? CheckCircle2 : isCurrent ? Clock3 : Circle

        return (
          <li
            key={step.value}
            className={[
              'rounded-lg border p-3',
              isComplete
                ? 'border-emerald-200 bg-emerald-50 text-emerald-900'
                : isCurrent
                  ? 'border-cyan-200 bg-cyan-50 text-cyan-900'
                  : 'border-slate-200 bg-white text-slate-500',
            ].join(' ')}
          >
            <div className="flex items-center gap-2">
              <Icon className="size-4 shrink-0" aria-hidden="true" />
              <span className="text-xs font-semibold sm:text-sm">
                {step.label}
              </span>
            </div>
          </li>
        )
      })}
    </ol>
  )
}

export default StatusTimeline
