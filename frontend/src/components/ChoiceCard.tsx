import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Icon } from './Icon'

interface ChoiceCardProps {
  title: string
  /** Secondary lines under the title (address, specialty, ...). */
  description?: ReactNode
  /** Right side: price, duration. */
  meta?: ReactNode
  /** A small icon for the kind of choice (organization, branch, department, service); drawn plain, no tile. */
  icon?: LucideIcon
  onSelect: () => void
}

/** A large, full-width option button for the step-by-step patient flow (touch target well above 44px). */
export function ChoiceCard({ title, description, meta, icon, onSelect }: ChoiceCardProps) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className="flex min-h-16 w-full items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white px-4 py-3 text-left shadow-sm transition-colors duration-150 hover:border-teal-700 hover:bg-teal-50/60"
    >
      <span className="flex min-w-0 items-start gap-3">
        {icon ? <Icon icon={icon} size={20} className="mt-0.5 text-teal-700" /> : null}
        <span className="min-w-0">
          <span className="block text-base font-semibold text-slate-900">{title}</span>
          {description ? <span className="mt-0.5 block text-sm text-slate-600">{description}</span> : null}
        </span>
      </span>
      {meta ? <span className="shrink-0 text-right text-sm tabular-nums text-slate-700">{meta}</span> : null}
    </button>
  )
}
