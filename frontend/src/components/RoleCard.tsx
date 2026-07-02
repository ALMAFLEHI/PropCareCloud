import type { LucideIcon } from 'lucide-react'

type RoleCardProps = {
  title: string
  description: string
  responsibilities: string[]
  icon: LucideIcon
}

function RoleCard({
  title,
  description,
  responsibilities,
  icon: Icon,
}: RoleCardProps) {
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-md bg-indigo-50 text-indigo-700">
          <Icon className="size-5" aria-hidden="true" />
        </div>
        <h3 className="text-base font-semibold text-slate-950">{title}</h3>
      </div>
      <p className="mt-4 text-sm text-slate-600">{description}</p>
      <ul className="mt-4 space-y-2 text-sm text-slate-500">
        {responsibilities.map((item) => (
          <li key={item} className="flex gap-2">
            <span className="mt-2 size-1.5 shrink-0 rounded-full bg-cyan-700" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </article>
  )
}

export default RoleCard
