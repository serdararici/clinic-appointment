import { useState, type FormEvent } from 'react'
import { addBulkAvailability } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import type { BulkAvailabilityResult, WeekdayName } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { isoDateOf, todayIsoDate } from '../../utils/format'
import { DEFAULT_WEEKDAYS, WEEKDAYS, minutesOf, previewBulk, type TimeWindow } from './availability-utils'

interface BulkAvailabilityCardProps {
  doctorId: string
  slotDurationMinutes: number
  onAdded: () => void
}

/** What the card shows after saving: the counts of every request it sent, merged. */
interface Summary {
  /** Distinct days that got at least one range. */
  createdDays: number
  skippedDays: number
  pastDays: number
}

function merge(results: BulkAvailabilityResult[]): Summary {
  const created = new Set(results.flatMap((result) => result.created.map((range) => isoDateOf(range.startUtc))))
  const skipped = new Set(results.flatMap((result) => result.skippedDates))
  const past = new Set(results.flatMap((result) => result.pastDates))
  return { createdDays: created.size, skippedDays: skipped.size, pastDays: past.size }
}

/**
 * Adds the same daily window on many days at once: a date range, the weekdays and a start/end time (Turkey time).
 * With the lunch break on, the day is two windows (before and after the break), so the card sends two bulk
 * requests; the API itself knows nothing about lunch. The API skips days that overlap an existing range and days
 * that have already started, and reports them.
 */
export function BulkAvailabilityCard({ doctorId, slotDurationMinutes, onAdded }: BulkAvailabilityCardProps) {
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [weekdays, setWeekdays] = useState<WeekdayName[]>([...DEFAULT_WEEKDAYS])
  const [start, setStart] = useState('09:00')
  const [end, setEnd] = useState('17:00')
  const [lunch, setLunch] = useState(true)
  const [lunchStart, setLunchStart] = useState('12:00')
  const [lunchEnd, setLunchEnd] = useState('13:00')
  const [errors, setErrors] = useState<{ from?: string; to?: string; weekdays?: string; end?: string; lunch?: string }>({})
  const [saving, setSaving] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)
  const [summary, setSummary] = useState<Summary | null>(null)
  const [now] = useState(() => Date.now())

  const windows: TimeWindow[] = lunch
    ? [
        { start, end: lunchStart },
        { start: lunchEnd, end },
      ]
    : [{ start, end }]

  const preview = previewBulk(fromDate, toDate, weekdays, windows, slotDurationMinutes, now)
  const showPreview = fromDate && toDate && toDate >= fromDate && weekdays.length > 0 && preview.days + preview.slots > 0

  const toggle = (name: WeekdayName) =>
    setWeekdays((current) => (current.includes(name) ? current.filter((day) => day !== name) : [...current, name]))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSummary(null)
    setApiError(null)

    const found: typeof errors = {}
    if (!fromDate) found.from = 'Başlangıç tarihini seçin.'
    else if (fromDate < todayIsoDate()) found.from = 'Geçmiş bir tarih seçilemez.'
    if (!toDate) found.to = 'Bitiş tarihini seçin.'
    else if (fromDate && toDate < fromDate) found.to = 'Bitiş tarihi başlangıçtan önce olamaz.'
    if (weekdays.length === 0) found.weekdays = 'En az bir gün seçin.'
    if (!start || !end) found.end = 'Başlangıç ve bitiş saatlerini girin.'
    else if (minutesOf(end) <= minutesOf(start)) found.end = 'Bitiş saati başlangıçtan sonra olmalıdır.'
    if (lunch && !found.end) {
      if (!lunchStart || !lunchEnd) found.lunch = 'Öğle arasının başlangıç ve bitiş saatlerini girin.'
      else if (minutesOf(lunchEnd) <= minutesOf(lunchStart)) found.lunch = 'Öğle arası bitişi başlangıçtan sonra olmalıdır.'
      else if (minutesOf(lunchStart) <= minutesOf(start) || minutesOf(lunchEnd) >= minutesOf(end)) {
        found.lunch = 'Öğle arası günlük saat aralığının içinde olmalıdır.'
      }
    }
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    const results: BulkAvailabilityResult[] = []
    try {
      // One request per window, one after the other: first before the lunch break, then after it.
      for (const window of windows) {
        results.push(await addBulkAvailability(doctorId, { fromDate, toDate, weekdays, startTime: window.start, endTime: window.end }))
      }
    } catch (caught) {
      // If the second request fails the first one has already saved its ranges: say so and show what was added.
      setApiError(readableMessage(caught))
    } finally {
      if (results.length > 0) {
        setSummary(merge(results))
        onAdded()
      }
      setSaving(false)
    }
  }

  return (
    <Card title="Toplu uygunluk ekle">
      <form onSubmit={submit} noValidate className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Input label="Başlangıç tarihi" type="date" min={todayIsoDate()} value={fromDate} onChange={(e) => setFromDate(e.target.value)} error={errors.from} />
          <Input label="Bitiş tarihi" type="date" min={fromDate || todayIsoDate()} value={toDate} onChange={(e) => setToDate(e.target.value)} error={errors.to} />
        </div>

        <fieldset>
          <legend className="mb-1.5 text-sm font-medium text-slate-900">Günler</legend>
          <div className="flex flex-wrap gap-x-5 gap-y-2">
            {WEEKDAYS.map((day) => (
              <label key={day.name} className="inline-flex min-h-8 items-center gap-2 text-sm text-slate-900">
                <input type="checkbox" checked={weekdays.includes(day.name)} onChange={() => toggle(day.name)} className="h-4 w-4 accent-teal-700" />
                {day.label}
              </label>
            ))}
          </div>
          {errors.weekdays ? <p className="mt-1.5 text-sm font-medium text-red-700">{errors.weekdays}</p> : null}
        </fieldset>

        <div className="grid gap-4 sm:grid-cols-2">
          <Input label="Günlük başlangıç saati" type="time" value={start} onChange={(e) => setStart(e.target.value)} />
          <Input label="Günlük bitiş saati" type="time" value={end} onChange={(e) => setEnd(e.target.value)} error={errors.end} />
        </div>

        <fieldset className="space-y-3">
          <label className="inline-flex min-h-8 items-center gap-2 text-sm font-medium text-slate-900">
            <input type="checkbox" checked={lunch} onChange={(e) => setLunch(e.target.checked)} className="h-4 w-4 accent-teal-700" />
            Öğle arası
          </label>
          {lunch ? (
            <div className="grid gap-4 sm:grid-cols-2">
              <Input label="Öğle arası başlangıcı" type="time" value={lunchStart} onChange={(e) => setLunchStart(e.target.value)} error={errors.lunch} />
              <Input label="Öğle arası bitişi" type="time" value={lunchEnd} onChange={(e) => setLunchEnd(e.target.value)} />
            </div>
          ) : null}
        </fieldset>
        <p className="text-sm text-slate-600">Saatler Türkiye saatidir.</p>

        {showPreview ? (
          <p className="rounded-md bg-slate-100 px-3 py-2 text-sm text-slate-800" aria-live="polite">
            <span className="font-medium tabular-nums">
              {preview.days} gün, {preview.slots} randevu aralığı
            </span>
            {lunch ? ' (öğle arası hariç)' : ''} (mevcut uygunluklarla çakışan günler eklenmez)
          </p>
        ) : null}

        {summary ? (
          <Alert tone={summary.createdDays > 0 ? 'success' : 'info'}>
            <p className="font-medium">{summary.createdDays} gün için uygunluk eklendi.</p>
            {summary.skippedDays > 0 ? <p>{summary.skippedDays} gün mevcut bir uygunlukla çakıştığı için atlandı.</p> : null}
            {summary.pastDays > 0 ? <p>{summary.pastDays} gün başlamış olduğu için atlandı.</p> : null}
          </Alert>
        ) : null}
        {apiError ? (
          <Alert tone="error">
            {summary ? 'İşlem yarım kaldı: öğle arasından önceki aralıklar eklendi, sonrakiler eklenemedi. ' : ''}
            {apiError}
          </Alert>
        ) : null}

        <Button type="submit" loading={saving}>
          Toplu ekle
        </Button>
      </form>
    </Card>
  )
}
