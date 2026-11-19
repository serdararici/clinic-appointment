import { Building2, Layers, UserPlus, Users } from 'lucide-react'
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

interface DepartmentStaff {
  id: string
  name: string
  doctors: number
  technicians: number
}

interface Summary {
  organizationName: string
  branchName: string
  departmentCount: number
  staffCount: number
  doctorCount: number
  activeDoctorCount: number
  technicianCount: number
  activeStaffCount: number
  byDepartment: DepartmentStaff[]
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <dt className="text-sm text-slate-600">{label}</dt>
      <dd className="text-2xl font-semibold tabular-nums text-slate-900">{value}</dd>
    </div>
  )
}

/** Doctors and technicians per department as thin proportional bars (plain CSS, real counts only). */
function StaffByDepartment({ rows }: { rows: readonly DepartmentStaff[] }) {
  const largest = Math.max(1, ...rows.map((row) => row.doctors + row.technicians))
  return (
    <div className="space-y-4">
      <ul className="space-y-3">
        {rows.map((row) => {
          const total = row.doctors + row.technicians
          return (
            <li key={row.id} className="space-y-1.5">
              <div className="flex flex-wrap items-baseline justify-between gap-2 text-sm">
                <span className="font-medium text-slate-900">{row.name}</span>
                <span className="tabular-nums text-slate-600">
                  {row.doctors} doktor · {row.technicians} teknisyen
                </span>
              </div>
              <div className="flex h-1.5 overflow-hidden rounded-full bg-slate-100" role="img" aria-label={`${row.name}: ${row.doctors} doktor, ${row.technicians} sağlık teknisyeni`}>
                <div className="bg-teal-600" style={{ width: `${(row.doctors / largest) * 100}%` }} />
                <div className="bg-slate-400" style={{ width: `${(row.technicians / largest) * 100}%` }} />
              </div>
              {total === 0 ? <p className="text-xs text-slate-500">Bu bölümde henüz personel yok.</p> : null}
            </li>
          )
        })}
      </ul>
      <div className="flex flex-wrap gap-x-5 gap-y-1 text-xs text-slate-600" aria-hidden="true">
        <span className="flex items-center gap-1.5">
          <span className="h-1.5 w-3 rounded-full bg-teal-600" /> Doktor
        </span>
        <span className="flex items-center gap-1.5">
          <span className="h-1.5 w-3 rounded-full bg-slate-400" /> Sağlık teknisyeni
        </span>
      </div>
    </div>
  )
}

/**
 * The manager's home: their branch with the organization, staff counters, doctors and technicians per department,
 * and quick actions. There is no endpoint that lists a branch's appointments, so no appointment figures are shown.
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
        const technicians = staff.filter((member) => member.role === 'HealthTechnician')
        return {
          organizationName: branch?.organizationName ?? '',
          branchName: branch?.name ?? '',
          departmentCount: departments.length,
          staffCount: staff.length,
          doctorCount: doctors.length,
          activeDoctorCount: doctors.filter((doctor) => doctor.isActive).length,
          technicianCount: technicians.length,
          activeStaffCount: staff.filter((member) => member.isActive).length,
          byDepartment: departments.map((department) => ({
            id: department.id,
            name: department.name,
            doctors: doctors.filter((member) => member.departmentId === department.id).length,
            technicians: technicians.filter((member) => member.departmentId === department.id).length,
          })),
        }
      },
      [branchId],
    ),
  )

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Merhaba, {user?.name}</h1>

      <div className="flex flex-wrap gap-3">
        <LinkButton to="/yonetici/personel?ekle=1" icon={UserPlus}>
          Personel ekle
        </LinkButton>
        <LinkButton to="/yonetici/bolumler?ekle=1" variant="secondary" icon={Layers}>
          Bölüm ekle
        </LinkButton>
        <LinkButton to="/yonetici/personel" variant="secondary" icon={Users}>
          Personel
        </LinkButton>
      </div>

      <Card icon={Building2} title={state.data ? `${state.data.organizationName} · ${state.data.branchName}` : 'Şubeniz'}>
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
            <Stat label="Bölüm" value={state.data.departmentCount} />
            <Stat label="Personel" value={state.data.staffCount} />
            <Stat label="Doktor" value={state.data.doctorCount} />
            <Stat label="Aktif doktor" value={state.data.activeDoctorCount} />
            <Stat label="Sağlık teknisyeni" value={state.data.technicianCount} />
            <Stat label="Aktif personel" value={state.data.activeStaffCount} />
          </dl>
        ) : null}
      </Card>

      {state.data && state.data.byDepartment.length > 0 ? (
        <Card icon={Layers} title="Bölümlere göre personel">
          <StaffByDepartment rows={state.data.byDepartment} />
        </Card>
      ) : null}
    </div>
  )
}
