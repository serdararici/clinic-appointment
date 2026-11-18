import { useState, type FormEvent } from 'react'
import { addBulkAvailability } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import type { BulkAvailabilityResult, WeekdayName } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { todayIsoDate } from '../../utils/format'
import { DEFAULT_WEEKDAYS, WEEKDAYS, minutesOf, previewBulk } from './availability-utils'

interface BulkAvailabilityCardProps {
  doctorId: string
  slotDurationMinutes: number
  onAdded: () => void
}

/**
 * Adds the same daily window on many days at once: a date range, the weekdays and a start/end time (Turkey time).
 * The API skips days that overlap an existing range and days that have already started, and reports them.
 * The API has no lunch-break option, so a break means adding the morning and the afternoon as two requests.
 */
export function BulkAvailabilityCard({ doctorId, slotDurationMinutes, onAdded }: BulkAvailabilityCardProps) {
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [weekdays, setWeekdays] = useState<WeekdayName[]>([...DEFAULT_WEEKDAYS])
  const [start, setStart] = useState('09:00')
  const [end, setEnd] = useState('17:00')
  const [errors, setErrors] = useState<{ from?: string; to?: string; weekdays?: string; end?: string }>({})
  const [saving, setSaving] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)
  const [result, setResult] = useState<BulkAvailabilityResult | null>(null)
  const [now] = useState(() => Date.now())

  const preview = previewBulk(fromDate, toDate, weekdays, start, end, slotDurationMinutes, now)
  const showPreview = fromDate && toDate && toDate >= fromDate && start && end && minutesOf(end) > minutesOf(start) && weekdays.length > 0

  const toggle = (name: WeekdayName) =>
    setWeekdays((current) => (current.includes(name) ? current.filter((day) => day !== name) : [...current, name]))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setResult(null)
    setApiError(null)

    const found: typeof errors = {}
    if (!fromDate) found.from = 'Başlangıç tarihini seçin.'
    else if (fromDate < todayIsoDate()) found.from = 'Geçmiş bir tarih seçilemez.'
    if (!toDate) found.to = 'Bitiş tarihini seçin.'
    else if (fromDate && toDate < fromDate) found.to = 'Bitiş tarihi başlangıçtan önce olamaz.'
    if (weekdays.length === 0) found.weekdays = 'En az bir gün seçin.'
    if (!start || !end) found.end = 'Başlangıç ve bitiş saatlerini girin.'
    else if (minutesOf(end) <= minutesOf(start)) found.end = 'Bitiş saati başlangıçtan sonra olmalıdır.'
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      const created = await addBulkAvailability(doctorId, { fromDate, toDate, weekdays, startTime: start, endTime: end })
      setResult(created)
      onAdded()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
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
        <p className="text-sm text-slate-600">
          Saatler Türkiye saatidir. Öğle arası için sabah ve öğleden sonra aralıklarını ayrı ayrı ekleyin.
        </p>

        {showPreview ? (
          <p className="rounded-md bg-slate-100 px-3 py-2 text-sm text-slate-800" aria-live="polite">
            <span className="font-medium tabular-nums">
              {preview.days} gün, {preview.slots} randevu aralığı
            </span>
            {' '}(mevcut uygunluklarla çakışan günler eklenmez)
          </p>
        ) : null}

        {result ? <BulkResult result={result} /> : null}
        {apiError ? <Alert tone="error">{apiError}</Alert> : null}

        <Button type="submit" loading={saving}>
          Toplu ekle
        </Button>
      </form>
    </Card>
  )
}

function BulkResult({ result }: { result: BulkAvailabilityResult }) {
  return (
    <Alert tone={result.created.length > 0 ? 'success' : 'info'}>
      <p className="font-medium">{result.created.length} gün için uygunluk eklendi.</p>
      {result.skippedDates.length > 0 ? <p>{result.skippedDates.length} gün mevcut bir uygunlukla çakıştığı için atlandı.</p> : null}
      {result.pastDates.length > 0 ? <p>{result.pastDates.length} gün başlamış olduğu için atlandı.</p> : null}
    </Alert>
  )
}
