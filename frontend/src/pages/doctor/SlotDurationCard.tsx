import { useState } from 'react'
import { setSlotDuration } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Select } from '../../components/Select'
import { SLOT_DURATIONS } from './availability-utils'
import { useToast } from '../../components/useToast'
import { Timer } from 'lucide-react'

interface SlotDurationCardProps {
  doctorId: string
  /** The saved duration. */
  current: number
  onSaved: (minutes: number) => void
}

/**
 * The doctor's appointment length. The API cuts each availability range into slots of this length on the fly, so
 * a change applies to every availability range at once; appointments that already exist keep their own times.
 */
export function SlotDurationCard({ doctorId, current, onSaved }: SlotDurationCardProps) {
  const toast = useToast()
  const [value, setValue] = useState(String(current))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const save = async () => {
    setSaving(true)
    setError(null)
    try {
      await setSlotDuration(doctorId, Number(value))
      toast.success('Randevu süresi kaydedildi.')
      onSaved(Number(value))
    } catch (caught) {
      setError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Randevu süresi" icon={Timer}>
      <div className="space-y-4">
        <p className="text-sm text-slate-700">
          Şu anki randevu süreniz: <span className="font-semibold tabular-nums text-slate-900">{current} dakika</span>
        </p>
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-48">
            <Select
              label="Yeni süre"
              value={value}
              onChange={(event) => {
                setValue(event.target.value)
              }}
              options={SLOT_DURATIONS.map((minutes) => ({ value: String(minutes), label: `${minutes} dakika` }))}
            />
          </div>
          <Button loading={saving} disabled={Number(value) === current} onClick={save}>
            Kaydet
          </Button>
        </div>
        <p className="text-sm text-slate-600">
          Yeni süre, tüm uygunluk aralıklarınızdaki randevu saatlerine hemen uygulanır. Mevcut randevular değişmez; yeni saat düzenine
          oturmasalar bile kendi saatlerinde geçerli kalır ve o saatler dolu görünür.
        </p>
        {error ? <Alert tone="error">{error}</Alert> : null}
      </div>
    </Card>
  )
}
