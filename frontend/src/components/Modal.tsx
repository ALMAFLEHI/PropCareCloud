import { X } from 'lucide-react'
import type { ReactNode } from 'react'

type ModalProps = {
  title: string
  description?: string
  children: ReactNode
  onClose: () => void
}

function Modal({ title, description, children, onClose }: ModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4 py-6">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white shadow-xl">
        <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-950">{title}</h2>
            {description && (
              <p className="mt-1 text-sm text-slate-500">{description}</p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="inline-flex size-9 items-center justify-center rounded-md text-slate-500 hover:bg-slate-100 hover:text-slate-900"
            aria-label="Close modal"
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        </div>
        <div className="px-6 py-5">{children}</div>
      </div>
    </div>
  )
}

export default Modal
