import { Clock } from 'lucide-react'
import { useCallback, useState } from 'react'
import { getWorkingHours } from '../../api/admin'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { useAsync } from '../../utils/useAsync'
import { WorkingHoursModal } from '../shared/WorkingHoursModal'
import { hourGroups } from '../shared/working-hours-utils'

/** "Çalışma saatleri" on the manager home: the weekly summary of the manager's branch and the editor (the same one the admin uses). */
export function WorkingHoursCard({ branchId, branchName }: { branchId: string; branchName: string }) {
  const state = useAsync(useCallback((signal: AbortSignal) => getWorkingHours(branchId, signal), [branchId]))
  const [editing, setEditing] = useState(false)

  return (
    <Card
      icon={Clock}
      title="Çalışma saatleri"
      actions={
        <Button variant="secondary" onClick={() => setEditing(true)} disabled={!branchId}>
          Düzenle
        </Button>
      }
    >
      {state.loading ? (
        <Spinner label="Yükleniyor..." />
      ) : state.error ? (
        <div className="space-y-3">
          <Alert tone="error">{state.error}</Alert>
          <Button variant="secondary" onClick={state.reload}>
            Yeniden dene
          </Button>
        </div>
      ) : state.data && state.data.length === 0 ? (
        <p className="text-sm text-slate-600">Çalışma saati tanımlı değil: şubede randevu alınamaz. “Düzenle” ile hafta içi saatlerini ekleyin.</p>
      ) : state.data ? (
        <dl className="space-y-1.5 text-sm">
          {hourGroups(state.data, true).map((group) => (
            <div key={group.days} className="flex gap-4">
              <dt className="w-20 shrink-0 text-slate-600">{group.days}</dt>
              <dd className={group.ranges === 'Kapalı' ? 'text-slate-500' : 'font-medium tabular-nums text-slate-900'}>{group.ranges}</dd>
            </div>
          ))}
        </dl>
      ) : null}

      <WorkingHoursModal open={editing} branchId={branchId} branchName={branchName} onClose={() => setEditing(false)} onChanged={state.reload} />
    </Card>
  )
}
