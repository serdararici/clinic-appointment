import { useState, type FormEvent } from 'react'
import { addAvailability } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { istanbulToUtcIso, todayIsoDate } from '../../utils/format'
import { minutesOf } from './availability-utils'

/** Adds one availability range on one day. Times are Turkey time and are sent to the API as UTC. */
export function SingleAvailabilityCard({ doctorId, onAdded }: { doctorId: string; onAdded: () => void }) {
  const [date, setDate] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [errors, setErrors] = useState<{ date?: string; start?: string; end?: string }>({})
  const [saving, setSaving] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)
  const [added, setAdded] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setAdded(false)
    setApiError(null)

    const found: typeof errors = {}
    if (!date) found.date = 'Tarih seçin.'
    else if (date < todayIsoDate()) found.date = 'Geçmiş bir tarih seçilemez.'
    if (!start) found.start = 'Başlangıç saati girin.'
    if (!end) found.end = 'Bitiş saati girin.'
    if (start && end && minutesOf(end) <= minutesOf(start)) found.end = 'Bitiş saati başlangıçtan sonra olmalıdır.'
    if (!found.date && !found.start && date && start && new Date(istanbulToUtcIso(date, start)).getTime() < Date.now()) {
      found.start = 'Başlangıç saati geçmişte kalamaz.'
    }
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      await addAvailability(doctorId, { startUtc: istanbulToUtcIso(date, start), endUtc: istanbulToUtcIso(date, end) })
      setAdded(true)
      setStart('')
      setEnd('')
      onAdded()
    } catch (caught) {
      setApiError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Tek aralık ekle">
      <form onSubmit={submit} noValidate className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-3">
          <Input label="Tarih" type="date" min={todayIsoDate()} value={date} onChange={(e) => setDate(e.target.value)} error={errors.date} />
          <Input label="Başlangıç saati" type="time" value={start} onChange={(e) => setStart(e.target.value)} error={errors.start} />
          <Input label="Bitiş saati" type="time" value={end} onChange={(e) => setEnd(e.target.value)} error={errors.end} />
        </div>
        <p className="text-sm text-slate-600">Saatler Türkiye saatidir.</p>
        {added ? <Alert tone="success">Uygunluk aralığı eklendi.</Alert> : null}
        {apiError ? <Alert tone="error">{apiError}</Alert> : null}
        <Button type="submit" loading={saving}>
          Aralık ekle
        </Button>
      </form>
    </Card>
  )
}
