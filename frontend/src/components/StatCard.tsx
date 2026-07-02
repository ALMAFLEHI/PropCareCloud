import type { LucideIcon } from 'lucide-react'

type StatCardProps = {
  title: string
  value: string
  helperText: string
  icon: LucideIcon
}

function StatCard({ title, value, helperText, icon: Icon }: StatCardProps) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-slate-500">{title}</p>
          <p className="mt-3 text-3xl font-semibold text-slate-950">{value}</p>
        </div>
        <div className="flex size-11 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
          <Icon className="size-5" aria-hidden="true" />
        </div>
      </div>
      <p className="mt-4 text-sm text-slate-500">{helperText}</p>
    </article>
  )
}

export default StatCard
