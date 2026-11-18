import { useCallback, useState } from 'react'
import { activateManager, deactivateManager, listManagers } from '../../api/admin'
import { listBranches } from '../../api/booking'
import type { BranchSummary, ClinicManagerAccount } from '../../api/types'
import { Badge } from '../../components/Badge'
import { Button } from '../../components/Button'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { Table, type Column } from '../../components/Table'
import { useAsync } from '../../utils/useAsync'
import { ManagerFormModal } from './ManagerFormModal'
import { usePageTitle } from '../../utils/usePageTitle'
import { useToast } from '../../components/useToast'
import { FilterBar } from '../../components/FilterBar'
import { Input } from '../../components/Input'
import { Select } from '../../components/Select'
import { matchesQuery } from '../../utils/search'

interface ManagersData {
  managers: ClinicManagerAccount[]
  branches: BranchSummary[]
}

type Dialog =
  | { kind: 'form'; manager: ClinicManagerAccount | null }
  | { kind: 'deactivate'; manager: ClinicManagerAccount }
  | { kind: 'activate'; manager: ClinicManagerAccount }

/** The clinic managers of every organization: add, edit, deactivate and activate. */
export function ManagersPage() {
  const toast = useToast()
  usePageTitle('Yöneticiler')
  const state = useAsync(
    useCallback(async (signal: AbortSignal): Promise<ManagersData> => {
      const [managers, branches] = await Promise.all([listManagers(signal), listBranches(signal)])
      return { managers, branches }
    }, []),
  )
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const close = () => setDialog(null)

  const all = state.data?.managers
  const managers = all?.filter(
    (manager) =>
      matchesQuery(search, [manager.fullName, manager.email, manager.branchName, manager.organizationName]) &&
      (!statusFilter || (statusFilter === 'active') === manager.isActive),
  )
  const filtersActive = Boolean(search.trim() || statusFilter)
  const noMatch = (all?.length ?? 0) > 0 && managers?.length === 0
  const clearFilters = () => {
    setSearch('')
    setStatusFilter('')
  }

  const columns: Column<ClinicManagerAccount>[] = [
    { key: 'name', header: 'Ad soyad', primary: true, cell: (row) => <span className="font-medium">{row.fullName}</span> },
    { key: 'email', header: 'E-posta', cell: (row) => row.email },
    { key: 'organization', header: 'Kurum', cell: (row) => row.organizationName },
    { key: 'branch', header: 'Şube', cell: (row) => row.branchName },
    { key: 'status', header: 'Durum', cell: (row) => <Badge tone={row.isActive ? 'green' : 'slate'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" onClick={() => setDialog({ kind: 'form', manager: row })}>
            Düzenle
          </Button>
          <Button variant="ghost" onClick={() => setDialog({ kind: row.isActive ? 'deactivate' : 'activate', manager: row })}>
            {row.isActive ? 'Pasifleştir' : 'Aktifleştir'}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Yöneticiler</h1>
        <Button onClick={() => setDialog({ kind: 'form', manager: null })} disabled={!state.data || state.data.branches.length === 0}>
          Yönetici ekle
        </Button>
      </div>

      {all && all.length > 0 ? (
        <FilterBar count={managers?.length ?? 0} active={filtersActive} onClear={clearFilters}>
          <Input label="Ara" type="search" placeholder="Ad, e-posta, kurum veya şube" value={search} onChange={(event) => setSearch(event.target.value)} />
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
        rows={managers}
        rowKey={(row) => row.id}
        loading={state.loading}
        error={state.error}
        onRetry={state.reload}
        emptyTitle="Henüz yönetici yok"
        emptyDescription="Önce bir şube oluşturun, sonra “Yönetici ekle” ile yönetici hesabı tanımlayın."
        filtersHideAll={noMatch}
        onClearFilters={clearFilters}
      />

      <ManagerFormModal
        open={dialog?.kind === 'form'}
        branches={state.data?.branches ?? []}
        manager={dialog?.kind === 'form' ? dialog.manager : null}
        onClose={close}
        onSaved={() => {
          toast.success(dialog?.kind === 'form' && dialog.manager ? 'Yönetici güncellendi.' : 'Yönetici eklendi.')
          close()
          state.reload()
        }}
      />
      <ConfirmDialog
        open={dialog?.kind === 'deactivate'}
        title="Yöneticiyi pasifleştir"
        confirmLabel="Pasifleştir"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'deactivate') return
          await deactivateManager(dialog.manager.id)
          toast.success('Yönetici pasifleştirildi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'deactivate' ? (
          <p>
            <span className="font-medium">{dialog.manager.fullName}</span> pasifleştirilecek: bir sonraki girişten itibaren oturum açamaz. Hesap ve kayıtları silinmez.
          </p>
        ) : null}
      </ConfirmDialog>
      <ConfirmDialog
        open={dialog?.kind === 'activate'}
        title="Yöneticiyi aktifleştir"
        confirmLabel="Aktifleştir"
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'activate') return
          await activateManager(dialog.manager.id)
          toast.success('Yönetici aktifleştirildi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'activate' ? (
          <p>
            <span className="font-medium">{dialog.manager.fullName}</span> yeniden aktifleştirilecek.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
