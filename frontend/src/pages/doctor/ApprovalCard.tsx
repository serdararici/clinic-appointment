import { ClipboardCheck } from 'lucide-react'
import { useState } from 'react'
import { setAutoConfirm } from '../../api/doctor'
import { readableMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { Switch } from '../../components/Switch'
import { useToast } from '../../components/useToast'

interface ApprovalCardProps {
  doctorId: string
  /** The saved setting, or undefined while the profile loads. */
  enabled: boolean | undefined
  /** An error from loading the profile, if any. */
  loadError: string | null
  onChanged: (enabled: boolean) => void
}

/**
 * "Randevu onayı": the doctor's own switch for approving appointments. On (the default): new appointments are
 * confirmed at once. Off: they wait as Pending in the "Bekleyen" tab. Turning it on never changes appointments that
 * are already waiting.
 */
export function ApprovalCard({ doctorId, enabled, loadError, onChanged }: ApprovalCardProps) {
  const toast = useToast()
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const toggle = async () => {
    if (enabled === undefined) return
    setSaving(true)
    setError(null)
    try {
      const profile = await setAutoConfirm(doctorId, !enabled)
      onChanged(profile.autoConfirmAppointments)
      toast.success(profile.autoConfirmAppointments ? 'Yeni randevular otomatik onaylanacak.' : 'Yeni randevular onayınızı bekleyecek.')
    } catch (caught) {
      setError(readableMessage(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card title="Randevu onayı" icon={ClipboardCheck}>
      <div className="space-y-3">
        <div className="flex flex-wrap items-center gap-3">
          {enabled === undefined ? (
            loadError ? <Alert tone="error">{loadError}</Alert> : <Spinner label="Yükleniyor..." />
          ) : (
            <>
              <Switch checked={enabled} onChange={toggle} label="Randevuları otomatik onayla" disabled={saving} />
              <span className="text-sm font-medium text-slate-900">Randevuları otomatik onayla</span>
              <span className="text-sm text-slate-600">{enabled ? 'Açık' : 'Kapalı'}</span>
              {saving ? <Spinner label="" /> : null}
            </>
          )}
        </div>
        <p className="text-sm text-slate-600">
          Kapalıyken yeni randevular “Bekleyen” sekmesinde onayınızı bekler. Açmak, zaten bekleyen randevuları değiştirmez.
        </p>
        {error ? <Alert tone="error">{error}</Alert> : null}
      </div>
    </Card>
  )
}
