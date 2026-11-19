import { useCallback, useEffect } from 'react'
import { listServices } from '../../api/booking'
import type { Service, ServiceType } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { formatCurrency, formatDuration } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'

interface ServiceChecklistProps {
  departmentId: string
  /** Examination for a doctor, Diagnostic for a technician: only services of that kind are listed. */
  kind: ServiceType
  selected: ReadonlySet<string>
  onSelectedChange: (next: Set<string>) => void
  /** Called with the ids of the listed services each time a list has loaded (the add forms check them all). Keep it stable (useCallback). */
  onLoaded?: (ids: string[]) => void
  /** The length shown for an examination: the doctor's appointment length (an examination lasts as long as the doctor's slot). */
  examinationMinutes?: number
  error?: string
}

/**
 * The services of a department of one kind as checkboxes with name, price and duration, plus "Tümünü seç" and
 * "Temizle". Used by the add-doctor and add-technician forms and by the "Hizmetler" dialog.
 */
export function ServiceChecklist({ departmentId, kind, selected, onSelectedChange, onLoaded, examinationMinutes, error }: ServiceChecklistProps) {
  const state = useAsync(
    useCallback(async (signal: AbortSignal): Promise<Service[]> => (await listServices(departmentId, signal)).filter((service) => service.serviceType === kind), [departmentId, kind]),
  )

  const services = state.data
  useEffect(() => {
    if (services) onLoaded?.(services.map((service) => service.id))
  }, [services, onLoaded])

  if (state.loading) {
    return (
      <div className="flex justify-center py-4">
        <Spinner label="Hizmetler yükleniyor..." />
      </div>
    )
  }
  if (state.error) {
    return (
      <div className="space-y-3">
        <Alert tone="error">{state.error}</Alert>
        <Button variant="secondary" onClick={state.reload}>
          Yeniden dene
        </Button>
      </div>
    )
  }
  if (!services || services.length === 0) {
    return <p className="rounded-lg border border-slate-200 px-3 py-3 text-sm text-slate-600">Bu bölümde {kind === 'Examination' ? 'muayene' : 'tetkik'} hizmeti yok.</p>
  }

  const toggle = (id: string) => {
    const next = new Set(selected)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    onSelectedChange(next)
  }

  return (
    <fieldset className="space-y-2">
      <div className="flex items-center justify-between gap-3">
        <legend className="text-sm font-medium text-slate-900">{kind === 'Examination' ? 'Verdiği muayene hizmetleri' : 'Bağlı tetkik hizmetleri'}</legend>
        <span className="flex gap-1">
          <button
            type="button"
            onClick={() => onSelectedChange(new Set(services.map((service) => service.id)))}
            className="min-h-9 rounded-md px-2 text-sm font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800"
          >
            Tümünü seç
          </button>
          <button
            type="button"
            onClick={() => onSelectedChange(new Set())}
            className="min-h-9 rounded-md px-2 text-sm font-medium text-teal-700 underline underline-offset-2 hover:text-teal-800"
          >
            Temizle
          </button>
        </span>
      </div>
      <ul className="divide-y divide-slate-200 rounded-lg border border-slate-200">
        {services.map((service) => (
          <li key={service.id}>
            <label className="flex min-h-11 cursor-pointer items-center gap-3 px-3 py-2">
              <input type="checkbox" checked={selected.has(service.id)} onChange={() => toggle(service.id)} className="h-4 w-4 shrink-0 accent-teal-700" />
              <span className="min-w-0 flex-1 text-sm font-medium text-slate-900">{service.name}</span>
              <span className="text-sm tabular-nums text-slate-600">{formatDuration(kind === 'Examination' ? (examinationMinutes ?? service.durationMinutes) : service.durationMinutes)}</span>
              <span className="w-20 text-right text-sm tabular-nums text-slate-900">{formatCurrency(service.basePrice)}</span>
            </label>
          </li>
        ))}
      </ul>
      {error ? <p className="text-sm font-medium text-red-700">{error}</p> : null}
    </fieldset>
  )
}
