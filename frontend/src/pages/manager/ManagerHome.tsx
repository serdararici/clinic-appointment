import { useCallback } from 'react'
import { listBranches, listDepartments } from '../../api/booking'
import { listStaff } from '../../api/manager'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { LinkButton } from '../../components/LinkButton'
import { Spinner } from '../../components/Spinner'
import { useAsync } from '../../utils/useAsync'
import { usePageTitle } from '../../utils/usePageTitle'

interface Summary {
  organizationName: string
  branchName: string
  departmentCount: number
  doctorCount: number
  activeDoctorCount: number
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <dt className="text-sm text-slate-600">{label}</dt>
      <dd className="text-2xl font-semibold tabular-nums text-slate-900">{value}</dd>
    </div>
  )
}

/**
 * The manager's home: their branch with the organization, how many departments and doctors it has, and links to the
 * management pages. There is no endpoint that lists a branch's appointments, so no appointment figures are shown.
 */
export function ManagerHome() {
  usePageTitle('Ana sayfa')
  const { user } = useAuth()
  const branchId = user?.branchId ?? ''

  const state = useAsync(
    useCallback(
      async (signal: AbortSignal): Promise<Summary> => {
        const [branches, departments, staff] = await Promise.all([listBranches(signal), listDepartments(branchId, signal), listStaff(branchId, signal)])
        const branch = branches.find((candidate) => candidate.id === branchId)
        const doctors = staff.filter((member) => member.role === 'Doctor')
        return {
          organizationName: branch?.organizationName ?? '',
          branchName: branch?.name ?? '',
          departmentCount: departments.length,
          doctorCount: doctors.length,
          activeDoctorCount: doctors.filter((doctor) => doctor.isActive).length,
        }
      },
      [branchId],
    ),
  )

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user?.name}</h1>

      <Card title={state.data ? `${state.data.organizationName} · ${state.data.branchName}` : 'Şubeniz'}>
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
          <dl className="grid gap-4 sm:grid-cols-3">
            <Stat label="Bölüm" value={state.data.departmentCount} />
            <Stat label="Doktor" value={state.data.doctorCount} />
            <Stat label="Aktif doktor" value={state.data.activeDoctorCount} />
          </dl>
        ) : null}
      </Card>

      <div className="flex flex-wrap gap-3">
        <LinkButton to="/yonetici/bolumler">Bölümler ve hizmetler</LinkButton>
        <LinkButton to="/yonetici/doktorlar" variant="secondary">
          Doktorlar
        </LinkButton>
      </div>
    </div>
  )
}
