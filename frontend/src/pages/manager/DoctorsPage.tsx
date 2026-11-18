import { useCallback, useState } from 'react'
import { listDepartments } from '../../api/booking'
import { activateStaff, deactivateStaff, listStaff } from '../../api/manager'
import type { Department, StaffMember } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Badge } from '../../components/Badge'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { Table, type Column } from '../../components/Table'
import { useAsync } from '../../utils/useAsync'
import { DoctorFormModal } from './DoctorFormModal'
import { usePageTitle } from '../../utils/usePageTitle'
import { useToast } from '../../components/useToast'
import { FilterBar } from '../../components/FilterBar'
import { Input } from '../../components/Input'
import { Select } from '../../components/Select'
import { matchesQuery } from '../../utils/search'

interface DoctorsData {
  doctors: StaffMember[]
  departments: Department[]
}

type Dialog = { kind: 'form'; doctor: StaffMember | null } | { kind: 'deactivate'; doctor: StaffMember } | { kind: 'activate'; doctor: StaffMember }

/** The branch's doctors: add, edit, deactivate and activate. Availability is not shown: the doctor manages it. */
export function DoctorsPage() {
  usePageTitle('Doktorlar')
  const { user } = useAuth()
  const toast = useToast()
  const branchId = user?.branchId ?? ''

  const state = useAsync(
    useCallback(
      async (signal: AbortSignal): Promise<DoctorsData> => {
        const [staff, departments] = await Promise.all([listStaff(branchId, signal), listDepartments(branchId, signal)])
        // The staff list also holds technicians; this page is about doctors.
        return { doctors: staff.filter((member) => member.role === 'Doctor'), departments }
      },
      [branchId],
    ),
  )
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [search, setSearch] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const close = () => setDialog(null)
  const all = state.data?.doctors
  const doctors = all?.filter(
    (doctor) =>
      matchesQuery(search, [doctor.fullName, doctor.email]) &&
      (!departmentFilter || doctor.departmentId === departmentFilter) &&
      (!statusFilter || (statusFilter === 'active') === doctor.isActive),
  )
  const filtersActive = Boolean(search.trim() || departmentFilter || statusFilter)
  const noMatch = (all?.length ?? 0) > 0 && doctors?.length === 0
  const clearFilters = () => {
    setSearch('')
    setDepartmentFilter('')
    setStatusFilter('')
  }
  const departmentName = (id: string) => state.data?.departments.find((department) => department.id === id)?.name ?? '-'

  const columns: Column<StaffMember>[] = [
    { key: 'name', header: 'Ad soyad', primary: true, cell: (row) => <span className="font-medium">{row.fullName}</span> },
    { key: 'department', header: 'Bölüm', cell: (row) => departmentName(row.departmentId) },
    { key: 'email', header: 'E-posta', cell: (row) => row.email },
    { key: 'slot', header: 'Randevu süresi', align: 'right', cell: (row) => `${row.slotDurationMinutes} dk` },
    { key: 'status', header: 'Durum', cell: (row) => <Badge tone={row.isActive ? 'green' : 'slate'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" onClick={() => setDialog({ kind: 'form', doctor: row })}>
            Düzenle
          </Button>
          <Button variant="ghost" onClick={() => setDialog({ kind: row.isActive ? 'deactivate' : 'activate', doctor: row })}>
            {row.isActive ? 'Pasifleştir' : 'Aktifleştir'}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Doktorlar</h1>
        <Button onClick={() => setDialog({ kind: 'form', doctor: null })} disabled={!state.data || state.data.departments.length === 0}>
          Doktor ekle
        </Button>
      </div>

      {!branchId ? <Alert tone="error">Hesabınıza bağlı bir şube bulunamadı.</Alert> : null}

      {all && all.length > 0 ? (
        <FilterBar count={doctors?.length ?? 0} active={filtersActive} onClear={clearFilters}>
          <Input label="Ara" type="search" placeholder="Ad veya e-posta" value={search} onChange={(event) => setSearch(event.target.value)} />
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
        rows={doctors}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle="Henüz doktor yok"
        emptyDescription="Önce bir bölüm oluşturun, sonra “Doktor ekle” ile doktoru tanımlayın."
        filtersHideAll={noMatch}
        onClearFilters={clearFilters}
      />

      <DoctorFormModal
        open={dialog?.kind === 'form'}
        departments={state.data?.departments ?? []}
        doctor={dialog?.kind === 'form' ? dialog.doctor : null}
        onClose={close}
        onSaved={() => {
          toast.success(dialog?.kind === 'form' && dialog.doctor ? 'Doktor güncellendi.' : 'Doktor eklendi.')
          close()
          state.reload()
        }}
      />
      <ConfirmDialog
        open={dialog?.kind === 'deactivate'}
        title="Doktoru pasifleştir"
        confirmLabel="Pasifleştir"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'deactivate') return
          await deactivateStaff(dialog.doctor.id)
          toast.success('Doktor pasifleştirildi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'deactivate' ? (
          <p>
            <span className="font-medium">{dialog.doctor.fullName}</span> pasifleştirilecek: giriş yapamaz ve yeni randevu alınamaz. Geçmiş kayıtları korunur.
            Yaklaşan randevusu varsa işlem yapılamaz.
          </p>
        ) : null}
      </ConfirmDialog>
      <ConfirmDialog
        open={dialog?.kind === 'activate'}
        title="Doktoru aktifleştir"
        confirmLabel="Aktifleştir"
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'activate') return
          await activateStaff(dialog.doctor.id)
          toast.success('Doktor aktifleştirildi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'activate' ? (
          <p>
            <span className="font-medium">{dialog.doctor.fullName}</span> yeniden aktifleştirilecek.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
