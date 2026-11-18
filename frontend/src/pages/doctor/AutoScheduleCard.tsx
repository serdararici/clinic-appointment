import { useCallback, useState } from 'react'
import { getAutoSchedule, setAutoSchedule } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { BOOKING_WINDOW_DAYS } from '../../utils/booking-window'
import { cx } from '../../utils/cx'
import { useAsync } from '../../utils/useAsync'
import { useToast } from '../../components/useToast'
import { Repeat } from 'lucide-react'

/**
 * The switch of the automatic work schedule. While it is on, the API keeps the next 14 days filled with the default
 * hours (weekdays 09:00-12:00 and 13:00-16:00). Turning it on fills the window right away, so the list is reloaded.
 */
export function AutoScheduleCard({ doctorId, onChanged }: { doctorId: string; onChanged: () => void }) {
  const state = useAsync(useCallback((signal: AbortSignal) => getAutoSchedule(doctorId, signal), [doctorId]))
  const toast = useToast()
  const [changed, setChanged] = useState<boolean | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const enabled = changed ?? state.data?.enabled

  const toggle = async () => {
    if (enabled === undefined) return
    setSaving(true)
    setError(null)
    try {
      const result = await setAutoSchedule(doctorId, !enabled)
      setChanged(result.enabled)
      toast.success(result.enabled ? 'Otomatik çalışma takvimi açıldı.' : 'Otomatik çalışma takvimi kapatıldı.')
      onChanged()
    } catch (caught) {
      setError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Otomatik çalışma takvimi" icon={Repeat}>
      <div className="space-y-4">
        {enabled === undefined ? (
          state.error ? (
            <div className="space-y-3">
              <Alert tone="error">{state.error}</Alert>
              <Button variant="secondary" onClick={state.reload}>
                Yeniden dene
              </Button>
            </div>
          ) : (
            <Spinner label="Yükleniyor..." />
          )
        ) : (
          <div className="flex items-center gap-3">
            <button
              type="button"
              role="switch"
              aria-checked={enabled}
              aria-label="Otomatik çalışma takvimi"
              disabled={saving}
              onClick={toggle}
              className={cx(
                'relative h-7 w-12 shrink-0 rounded-full transition-colors disabled:opacity-50',
                enabled ? 'bg-teal-700' : 'bg-slate-300',
              )}
            >
              <span className={cx('absolute top-0.5 h-6 w-6 rounded-full bg-white transition-all', enabled ? 'left-[1.375rem]' : 'left-0.5')} />
            </button>
            <span className="text-sm font-medium text-slate-900">{enabled ? 'Açık' : 'Kapalı'}</span>
            {saving ? <Spinner label="" /> : null}
          </div>
        )}

        <div className="space-y-2 text-sm text-slate-700">
          <p>
            Açıkken önümüzdeki {BOOKING_WINDOW_DAYS} gün için hafta içi 09:00-12:00 ve 13:00-16:00 saatleri otomatik olarak uygunluk olarak eklenir
            (12:00-13:00 öğle arasıdır).
          </p>
          <p>Bir günü izin yapmak için o günün uygunluklarını aşağıdaki listeden silmeniz yeterlidir.</p>
          <p>Kendi çalışma takviminizi oluşturmak veya uzun süreli izne çıkmak için bu ayarı kapatın.</p>
          <p className="text-slate-600">Açıkken son günlerin uygunluklarını silerseniz, bu günler otomatik olarak yeniden eklenir.</p>
        </div>

        {error ? <Alert tone="error">{error}</Alert> : null}
      </div>
    </Card>
  )
}
