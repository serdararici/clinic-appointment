import type { ReactNode } from 'react'
import { cx } from '../utils/cx'
import { Alert } from './Alert'
import { Button } from './Button'
import { EmptyState } from './EmptyState'
import { Spinner } from './Spinner'

export interface Column<T> {
  key: string
  header: string
  /** Renders the cell for one row. */
  cell: (row: T) => ReactNode
  /** Right-align numbers, prices and times. */
  align?: 'left' | 'right'
  /** On phone width the row becomes a card; the primary column is its heading. */
  primary?: boolean
  /** Leave this column out of the phone card. */
  hideOnMobile?: boolean
}

interface TableProps<T> {
  columns: ReadonlyArray<Column<T>>
  /** undefined while nothing has loaded yet. */
  rows: readonly T[] | undefined
  rowKey: (row: T) => string
  loading?: boolean
  /** A readable (Turkish) error message; shows the error state with a retry button. */
  error?: string | null
  onRetry?: () => void
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

/**
 * Dense data table for staff and admin screens. It owns the three list states in Turkish:
 * loading, error (with "Yeniden dene") and empty. On phone width the table turns into a list of cards.
 */
export function Table<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  error,
  onRetry,
  emptyTitle = 'Kayıt bulunamadı',
  emptyDescription,
  emptyAction,
}: TableProps<T>) {
  const frame = 'overflow-hidden rounded-lg border border-slate-200 bg-white'

  if (loading) {
    return (
      <div className={cx(frame, 'flex justify-center px-4 py-10')}>
        <Spinner label="Yükleniyor..." />
      </div>
    )
  }

  if (error) {
    return (
      <div className={cx(frame, 'space-y-3 p-4')}>
        <Alert tone="error">{error}</Alert>
        {onRetry ? (
          <Button variant="secondary" onClick={onRetry}>
            Yeniden dene
          </Button>
        ) : null}
      </div>
    )
  }

  if (!rows || rows.length === 0) {
    return (
      <div className={frame}>
        <EmptyState title={emptyTitle} description={emptyDescription} action={emptyAction} />
      </div>
    )
  }

  const primary = columns.find((column) => column.primary) ?? columns[0]
  const cardColumns = columns.filter((column) => column !== primary && !column.hideOnMobile)

  return (
    <div className={frame}>
      {/* Tablet and desktop: a real table. */}
      <table className="hidden w-full text-left text-sm md:table">
        <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-600">
          <tr>
            {columns.map((column) => (
              <th key={column.key} scope="col" className={cx('px-4 py-2.5', column.align === 'right' && 'text-right')}>
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200">
          {rows.map((row) => (
            <tr key={rowKey(row)} className="hover:bg-slate-50">
              {columns.map((column) => (
                <td key={column.key} className={cx('px-4 py-3 align-middle text-slate-900', column.align === 'right' && 'text-right tabular-nums')}>
                  {column.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>

      {/* Phone: one card per row, label and value pairs. */}
      <ul className="divide-y divide-slate-200 md:hidden">
        {rows.map((row) => (
          <li key={rowKey(row)} className="space-y-2 px-4 py-3">
            <div className="font-semibold text-slate-900">{primary.cell(row)}</div>
            <dl className="space-y-1 text-sm">
              {cardColumns.map((column) => (
                <div key={column.key} className="flex justify-between gap-4">
                  <dt className="text-slate-600">{column.header}</dt>
                  <dd className={cx('text-right text-slate-900', column.align === 'right' && 'tabular-nums')}>{column.cell(row)}</dd>
                </div>
              ))}
            </dl>
          </li>
        ))}
      </ul>
    </div>
  )
}
