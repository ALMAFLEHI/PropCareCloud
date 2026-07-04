import type { ApiEnumValue } from '../types/api'
import {
  formatEnumLabel,
  getPriorityLabel,
  getPropertyStatusLabel,
  getStatusLabel,
  getUnitStatusLabel,
} from '../utils/formatters'

type StatusBadgeProps = {
  value: ApiEnumValue
  kind?: 'maintenance' | 'priority' | 'property' | 'unit' | 'plain'
}

const badgeClasses: Record<string, string> = {
  neutral: 'bg-slate-100 text-slate-700 ring-slate-200',
  blue: 'bg-sky-50 text-sky-800 ring-sky-200',
  cyan: 'bg-cyan-50 text-cyan-800 ring-cyan-200',
  amber: 'bg-amber-50 text-amber-800 ring-amber-200',
  orange: 'bg-orange-50 text-orange-800 ring-orange-200',
  emerald: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  rose: 'bg-rose-50 text-rose-800 ring-rose-200',
  violet: 'bg-violet-50 text-violet-800 ring-violet-200',
}

function normalize(value: ApiEnumValue) {
  return typeof value === 'number' ? value : Number(value)
}

function getTone(value: ApiEnumValue, kind: StatusBadgeProps['kind']) {
  const numeric = normalize(value)

  if (kind === 'priority') {
    if (numeric === 3) return 'rose'
    if (numeric === 2) return 'orange'
    if (numeric === 1) return 'amber'
    return 'emerald'
  }

  if (kind === 'maintenance') {
    if (numeric === 4) return 'emerald'
    if (numeric === 5) return 'neutral'
    if (numeric === 3) return 'violet'
    if (numeric === 2) return 'blue'
    if (numeric === 1) return 'amber'
    return 'cyan'
  }

  if (kind === 'property' || kind === 'unit') {
    if (numeric === 2) return 'amber'
    if (numeric === 1) return kind === 'property' ? 'neutral' : 'emerald'
    return 'emerald'
  }

  return 'neutral'
}

function getLabel(value: ApiEnumValue, kind: StatusBadgeProps['kind']) {
  if (kind === 'priority') return getPriorityLabel(value)
  if (kind === 'maintenance') return getStatusLabel(value)
  if (kind === 'property') return getPropertyStatusLabel(value)
  if (kind === 'unit') return getUnitStatusLabel(value)
  return formatEnumLabel(value)
}

function StatusBadge({ value, kind = 'plain' }: StatusBadgeProps) {
  const tone = getTone(value, kind)

  return (
    <span
      className={`inline-flex w-fit items-center rounded-md px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${badgeClasses[tone]}`}
    >
      {getLabel(value, kind)}
    </span>
  )
}

export default StatusBadge
