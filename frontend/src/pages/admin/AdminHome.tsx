import { useCallback } from 'react'
import { listAllStaff, listManagers, listOrganizations } from '../../api/admin'
import { listBranches } from '../../api/booking'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { LinkButton } from '../../components/LinkButton'
import { Spinner } from '../../components/Spinner'
import { useAsync } from '../../utils/useAsync'

interface Counts {
  organizations: number
  branches: number
  managers: number
  activeManagers: number
  doctors: number
  technicians: number
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <dt className="text-sm text-slate-600">{label}</dt>
      <dd className="text-2xl font-semibold tabular-nums text-slate-900">{value}</dd>
    </div>
  )
}

/** The administrator's home: how big the system is (organizations, branches, managers, staff) and links to manage it. */
export function AdminHome() {
  const { user } = useAuth()

  const state = useAsync(
    useCallback(async (signal: AbortSignal): Promise<Counts> => {
      const [organizations, branches, managers, staff] = await Promise.all([listOrganizations(signal), listBranches(signal), listManagers(signal), listAllStaff(signal)])
      return {
        organizations: organizations.length,
        branches: branches.length,
        managers: managers.length,
        activeManagers: managers.filter((manager) => manager.isActive).length,
        doctors: staff.filter((member) => member.role === 'Doctor').length,
        technicians: staff.filter((member) => member.role === 'HealthTechnician').length,
      }
    }, []),
  )

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user?.name}</h1>

      <Card title="Sistem özeti">
        {state.loading ? (
          <Spinner label="Yükleniyor..." />
        ) : state.error ? (
          <div className="space-y-3">
            <Alert tone="error">{state.error}</Alert>
            <Button variant="secondary" onClick={state.reload}>
              Yeniden dene
            </Button>
          </div>
        ) : state.data ? (
          <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
            <Stat label="Kurum" value={state.data.organizations} />
            <Stat label="Şube" value={state.data.branches} />
            <Stat label="Yönetici" value={state.data.managers} />
            <Stat label="Aktif yönetici" value={state.data.activeManagers} />
            <Stat label="Doktor" value={state.data.doctors} />
            <Stat label="Teknisyen" value={state.data.technicians} />
          </dl>
        ) : null}
      </Card>

      <div className="flex flex-wrap gap-3">
        <LinkButton to="/sistem/kurumlar">Kurumlar ve şubeler</LinkButton>
        <LinkButton to="/sistem/yoneticiler" variant="secondary">
          Yöneticiler
        </LinkButton>
      </div>
    </div>
  )
}
