import { formatCurrency, formatLongDate, formatTime } from '../../utils/format'
import { cx } from '../../utils/cx'
import type { Selection, Step } from './booking-types'

interface Item {
  label: string
  /** null while nothing has been chosen for it yet. */
  value: string | null
  /** The step a click goes back to; null for a choice that cannot be changed from here. */
  goTo: Step | null
}

interface SelectionPanelProps {
  selection: Selection
  /** The name of the chosen organization, known before a branch is chosen. */
  organizationName: string | undefined
  /** Which steps are on the walked path, to know where a click can go. */
  walked: readonly Step[]
  onGoTo: (step: Step) => void
  disabled: boolean
}

function buildItems(selection: Selection, organizationName: string | undefined, walked: readonly Step[]): Item[] {
  const { branch, department, service, doctor, slot } = selection
  const items: Item[] = [
    {
      label: 'Kurum · Şube',
      value: branch ? `${branch.organizationName} · ${branch.name}` : (organizationName ?? null),
      goTo: walked.includes('branch') ? 'branch' : 'organization',
    },
    { label: 'Bölüm', value: department?.name ?? null, goTo: 'department' },
    // An automatically chosen service (the only one of its department) has no step of its own: go to the department.
    { label: 'Hizmet', value: service?.name ?? null, goTo: walked.includes('service') ? 'service' : 'department' },
  ]
  if (service?.serviceType !== 'Diagnostic') items.push({ label: 'Doktor', value: doctor?.fullName ?? null, goTo: 'doctor' })
  items.push({ label: 'Tarih ve saat', value: slot ? `${formatLongDate(slot.startUtc)}, ${formatTime(slot.startUtc)}` : null, goTo: 'slot' })
  return items
}

function ItemList({ selection, organizationName, walked, onGoTo, disabled }: SelectionPanelProps) {
  const items = buildItems(selection, organizationName, walked)
  const price = selection.service ? formatCurrency(selection.service.basePrice) : null

  return (
    <div>
      <ul className="divide-y divide-slate-200">
        {items.map((item) => {
          const content = (
            <>
              <span className="block text-xs font-medium uppercase tracking-wide text-slate-600">{item.label}</span>
              <span className={cx('block text-sm', item.value ? 'font-medium text-slate-900' : 'text-slate-500')}>{item.value ?? 'Henüz seçilmedi'}</span>
            </>
          )
          return (
            <li key={item.label}>
              {item.value && item.goTo ? (
                <button
                  type="button"
                  disabled={disabled}
                  onClick={() => onGoTo(item.goTo!)}
                  className="block min-h-11 w-full px-1 py-2 text-left hover:bg-slate-50 disabled:cursor-not-allowed"
                  aria-label={`${item.label}: ${item.value}. Değiştirmek için seçin.`}
                >
                  {content}
                </button>
              ) : (
                <div className="min-h-11 px-1 py-2">{content}</div>
              )}
            </li>
          )
        })}
      </ul>
      {price ? (
        <p className="mt-2 flex items-baseline justify-between border-t border-slate-200 px-1 pt-3 text-sm">
          <span className="text-slate-600">Hizmet bedeli</span>
          <span className="font-semibold tabular-nums text-slate-900">{price}</span>
        </p>
      ) : null}
    </div>
  )
}

/** Desktop (lg and up): a sticky side panel "Seçimleriniz". A finished item is a button that goes back to its step. */
export function SelectionAside(props: SelectionPanelProps) {
  return (
    <aside aria-label="Seçimleriniz" className="hidden self-start rounded-xl border border-slate-200 bg-white p-4 shadow-sm lg:sticky lg:top-6 lg:block">
      <h2 className="mb-2 px-1 text-base font-semibold text-slate-900">Seçimleriniz</h2>
      <ItemList {...props} />
    </aside>
  )
}

/** Phone: the same list as a compact bar above the step that opens and closes (a native <details>). */
export function SelectionBar(props: SelectionPanelProps) {
  const chosen = buildItems(props.selection, props.organizationName, props.walked).filter((item) => item.value).length
  return (
    <details className="rounded-xl border border-slate-200 bg-white shadow-sm lg:hidden">
      <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-2 text-sm font-semibold text-slate-900">
        <span>Seçimleriniz</span>
        <span className="font-normal tabular-nums text-slate-600">{chosen} seçim</span>
      </summary>
      <div className="border-t border-slate-200 px-3 py-2">
        <ItemList {...props} />
      </div>
    </details>
  )
}
