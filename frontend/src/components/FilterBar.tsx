import type { ReactNode } from 'react'
import { Button } from './Button'
import { FilterX } from 'lucide-react'

interface FilterBarProps {
  /** The filter controls (Input and Select components), laid out in a row that wraps. */
  children: ReactNode
  /** How many rows pass the filters. */
  count: number
  /** Whether any filter is set; "Filtreleri temizle" shows only then. */
  active: boolean
  onClear: () => void
}

/**
 * The bar above a list with filters: the controls, the result count ("12 kayıt") and a button that clears all
 * filters. The controls are passed in, so every list uses its own fields with the same look and behavior.
 */
export function FilterBar({ children, count, active, onClear }: FilterBarProps) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-3 shadow-sm">
      <div className="grid flex-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">{children}</div>
      <div className="flex items-center gap-3 pb-1.5">
        <span className="text-sm tabular-nums text-slate-700" aria-live="polite">
          {count} kayıt
        </span>
        {active ? (
          <Button variant="ghost" icon={FilterX} onClick={onClear}>
            Filtreleri temizle
          </Button>
        ) : null}
      </div>
    </div>
  )
}
