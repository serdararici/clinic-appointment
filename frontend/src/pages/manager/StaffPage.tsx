import { Microscope, Stethoscope } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { listDepartments } from '../../api/booking'
import { activateStaff, deactivateStaff, listStaff } from '../../api/manager'
import type { Department, StaffMember } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Badge } from '../../components/Badge'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { FilterBar } from '../../components/FilterBar'
import { Icon } from '../../components/Icon'
import { Input } from '../../components/Input'
import { Select } from '../../components/Select'
import { Table, type Column } from '../../components/Table'
import { useToast } from '../../components/useToast'
import { matchesQuery } from '../../utils/search'
import { useAsync } from '../../utils/useAsync'
import { usePageTitle } from '../../utils/usePageTitle'
import { DoctorFormModal } from './DoctorFormModal'
import { DoctorServicesModal } from './DoctorServicesModal'
import { StaffTypeModal, type StaffKind } from './StaffTypeModal'
import { TechnicianFormModal } from './TechnicianFormModal'

interface StaffData {
  staff: StaffMember[]
  departments: Department[]
}

type Dialog =
  | { kind: 'type' }
  | { kind: 'doctor-form'; doctor: StaffMember | null }
  | { kind: 'technician-form'; technician: StaffMember | null }
  | { kind: 'services'; doctor: StaffMember }
  | { kind: 'deactivate'; member: StaffMember }
  | { kind: 'activate'; member: StaffMember }

const isDoctor = (member: StaffMember) => member.role === 'Doctor'
const typeLabel = (member: StaffMember) => (isDoctor(member) ? 'Doktor' : 'Sağlık teknisyeni')

/** The branch's doctors and health technicians in one list: add, edit, offered services (doctors), deactivate and activate. */
export function StaffPage() {
  usePageTitle('Personel')
  const { user } = useAuth()
  const toast = useToast()
  const branchId = user?.branchId ?? ''
  const [searchParams] = useSearchParams()

  const state = useAsync(
    useCallback(
      async (signal: AbortSignal): Promise<StaffData> => {
        const [staff, departments] = await Promise.all([listStaff(branchId, signal), listDepartments(branchId, signal)])
        return { staff, departments }
      },
      [branchId],
    ),
  )
  // "Personel ekle" on the manager home opens the add flow right away (?ekle=1).
  const [dialog, setDialog] = useState<Dialog | null>(() => (searchParams.get('ekle') ? { kind: 'type' } : null))
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const close = () => setDialog(null)
  const all = state.data?.staff
  const staff = all?.filter(
    (member) =>
      matchesQuery(search, [member.fullName, member.email]) &&
      (!typeFilter || member.role === typeFilter) &&
      (!departmentFilter || member.departmentId === departmentFilter) &&
      (!statusFilter || (statusFilter === 'active') === member.isActive),
  )
  const filtersActive = Boolean(search.trim() || typeFilter || departmentFilter || statusFilter)
  const noMatch = (all?.length ?? 0) > 0 && staff?.length === 0
  const clearFilters = () => {
    setSearch('')
    setTypeFilter('')
    setDepartmentFilter('')
    setStatusFilter('')
  }
  const departmentName = (id: string) => state.data?.departments.find((department) => department.id === id)?.name ?? '-'
  const saved = (message: string) => {
    toast.success(message)
    close()
    state.reload()
  }

  const columns: Column<StaffMember>[] = [
    { key: 'name', header: 'Ad soyad', primary: true, cell: (row) => <span className="font-medium">{row.fullName}</span> },
    {
      key: 'type',
      header: 'Tür',
      cell: (row) => (
        <Badge tone={isDoctor(row) ? 'teal' : 'slate'}>
          <Icon icon={isDoctor(row) ? Stethoscope : Microscope} size={14} />
          {typeLabel(row)}
        </Badge>
      ),
    },
    { key: 'department', header: 'Bölüm', cell: (row) => departmentName(row.departmentId) },
    { key: 'email', header: 'E-posta', cell: (row) => row.email },
    { key: 'phone', header: 'Telefon', cell: (row) => <span className="tabular-nums">{row.phoneNumber || '–'}</span> },
    // Only doctors have appointment slots.
    { key: 'slot', header: 'Randevu süresi', align: 'right', cell: (row) => (isDoctor(row) ? `${row.slotDurationMinutes} dk` : '–') },
    { key: 'status', header: 'Durum', cell: (row) => <Badge tone={row.isActive ? 'green' : 'slate'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <span className="inline-flex flex-wrap justify-end gap-2">
          <Button variant="secondary" onClick={() => setDialog(isDoctor(row) ? { kind: 'doctor-form', doctor: row } : { kind: 'technician-form', technician: row })}>
            Düzenle
          </Button>
          {isDoctor(row) ? (
            <Button variant="secondary" onClick={() => setDialog({ kind: 'services', doctor: row })}>
              Hizmetler
            </Button>
          ) : null}
          <Button variant="ghost" onClick={() => setDialog({ kind: row.isActive ? 'deactivate' : 'activate', member: row })}>
            {row.isActive ? 'Pasifleştir' : 'Aktifleştir'}
          </Button>
        </span>
      ),
    },
  ]

  const pickType = (kind: StaffKind) => setDialog(kind === 'doctor' ? { kind: 'doctor-form', doctor: null } : { kind: 'technician-form', technician: null })

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Personel</h1>
        <Button onClick={() => setDialog({ kind: 'type' })} disabled={!state.data || state.data.departments.length === 0}>
          Personel ekle
        </Button>
      </div>

      {!branchId ? <Alert tone="error">Hesabınıza bağlı bir şube bulunamadı.</Alert> : null}

      {all && all.length > 0 ? (
        <FilterBar count={staff?.length ?? 0} active={filtersActive} onClear={clearFilters}>
          <Input label="Ara" type="search" placeholder="Ad veya e-posta" value={search} onChange={(event) => setSearch(event.target.value)} />
          <Select
            label="Tür"
            placeholder="Tümü"
            value={typeFilter}
            onChange={(event) => setTypeFilter(event.target.value)}
            options={[
              { value: 'Doctor', label: 'Doktor' },
              { value: 'HealthTechnician', label: 'Sağlık teknisyeni' },
            ]}
          />
          <Select
            label="Bölüm"
            placeholder="Tümü"
            value={departmentFilter}
            onChange={(event) => setDepartmentFilter(event.target.value)}
            options={(state.data?.departments ?? []).map((department) => ({ value: department.id, label: department.name }))}
          />
          <Select
            label="Durum"
            placeholder="Tümü"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            options={[
              { value: 'active', label: 'Aktif' },
              { value: 'inactive', label: 'Pasif' },
            ]}
          />
        </FilterBar>
      ) : null}

      <Table
        columns={columns}
        rows={staff}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle="Henüz personel yok"
        emptyDescription="Önce bir bölüm oluşturun, sonra “Personel ekle” ile doktor veya sağlık teknisyeni tanımlayın."
        filtersHideAll={noMatch}
        onClearFilters={clearFilters}
      />

      <StaffTypeModal open={dialog?.kind === 'type'} onClose={close} onPick={pickType} />
      <DoctorFormModal
        open={dialog?.kind === 'doctor-form'}
        departments={state.data?.departments ?? []}
        doctor={dialog?.kind === 'doctor-form' ? dialog.doctor : null}
        onClose={close}
        onSaved={() => saved(dialog?.kind === 'doctor-form' && dialog.doctor ? 'Doktor güncellendi.' : 'Doktor eklendi.')}
      />
      <TechnicianFormModal
        open={dialog?.kind === 'technician-form'}
        departments={state.data?.departments ?? []}
        technician={dialog?.kind === 'technician-form' ? dialog.technician : null}
        onClose={close}
        onSaved={() => saved(dialog?.kind === 'technician-form' && dialog.technician ? 'Sağlık teknisyeni güncellendi.' : 'Sağlık teknisyeni eklendi.')}
      />
      <DoctorServicesModal open={dialog?.kind === 'services'} doctor={dialog?.kind === 'services' ? dialog.doctor : null} onClose={close} onSaved={() => saved('Hizmetler kaydedildi.')} />

      <ConfirmDialog
        open={dialog?.kind === 'deactivate'}
        title="Personeli pasifleştir"
        confirmLabel="Pasifleştir"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'deactivate') return
          await deactivateStaff(dialog.member.id)
          toast.success(`${typeLabel(dialog.member)} pasifleştirildi.`)
          state.reload()
        }}
      >
        {dialog?.kind === 'deactivate' ? (
          <p>
            <span className="font-medium">{dialog.member.fullName}</span>{' '}
            {isDoctor(dialog.member)
              ? 'pasifleştirilecek: giriş yapamaz ve yeni randevu alınamaz. Geçmiş kayıtları korunur. Yaklaşan randevusu varsa işlem yapılamaz.'
              : 'pasifleştirilecek. Kaydı listede kalır, geçmiş bilgileri korunur.'}
          </p>
        ) : null}
      </ConfirmDialog>
      <ConfirmDialog
        open={dialog?.kind === 'activate'}
        title="Personeli aktifleştir"
        confirmLabel="Aktifleştir"
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'activate') return
          await activateStaff(dialog.member.id)
          toast.success(`${typeLabel(dialog.member)} aktifleştirildi.`)
          state.reload()
        }}
      >
        {dialog?.kind === 'activate' ? (
          <p>
            <span className="font-medium">{dialog.member.fullName}</span> yeniden aktifleştirilecek.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
