import { Inbox } from 'lucide-react'

type EmptyStateProps = {
  title: string
  message: string
}

function EmptyState({ title, message }: EmptyStateProps) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center">
      <div className="mx-auto flex size-11 items-center justify-center rounded-md bg-slate-100 text-slate-600">
        <Inbox className="size-5" aria-hidden="true" />
      </div>
      <p className="mt-4 text-sm font-semibold text-slate-950">{title}</p>
      <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-500">
        {message}
      </p>
    </div>
  )
}

export default EmptyState
