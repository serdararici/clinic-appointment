import { useCallback, useState } from 'react'
import { createBranch, createOrganization, deleteBranch, deleteOrganization, getWorkingHours, listOrganizations, updateBranch, updateOrganization } from '../../api/admin'
import { listBranches, listDepartments } from '../../api/booking'
import type { BranchSummary, Organization, WorkingHour } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { EmptyState } from '../../components/EmptyState'
import { Spinner } from '../../components/Spinner'
import { Table, type Column } from '../../components/Table'
import { useAsync } from '../../utils/useAsync'
import { summarizeHours } from '../shared/working-hours-utils'
import { NameFormModal } from './NameFormModal'
import { WorkingHoursModal } from '../shared/WorkingHoursModal'
import { usePageTitle } from '../../utils/usePageTitle'
import { useToast } from '../../components/useToast'
import { Building2 } from 'lucide-react'

interface BranchRow {
  branch: BranchSummary
  departmentCount: number
  hours: WorkingHour[]
}

interface OrganizationRow {
  organization: Organization
  branches: BranchRow[]
}

type Dialog =
  | { kind: 'organization'; organization: Organization | null }
  | { kind: 'branch'; organization: Organization; branch: BranchSummary | null }
  | { kind: 'hours'; branch: BranchSummary }
  | { kind: 'delete-organization'; organization: Organization }
  | { kind: 'delete-branch'; branch: BranchSummary }

/** Organizations with their branches (address, departments, working hours): add, edit and delete both. */
export function OrganizationsPage() {
  const toast = useToast()
  usePageTitle('Kurumlar')
  const state = useAsync(
    useCallback(async (signal: AbortSignal): Promise<OrganizationRow[]> => {
      const [organizations, branches] = await Promise.all([listOrganizations(signal), listBranches(signal)])
      // Departments and working hours of every branch, loaded side by side.
      const rows = await Promise.all(
        branches.map(async (branch): Promise<BranchRow> => {
          const [departments, hours] = await Promise.all([listDepartments(branch.id, signal), getWorkingHours(branch.id, signal)])
          return { branch, departmentCount: departments.length, hours }
        }),
      )
      return organizations.map((organization) => ({ organization, branches: rows.filter((row) => row.branch.organizationId === organization.id) }))
    }, []),
  )
  const [dialog, setDialog] = useState<Dialog | null>(null)

  const close = () => setDialog(null)
  const saved = async (action: () => Promise<unknown>, message: string) => {
    await action()
    close()
    state.reload()
    toast.success(message)
  }

  const branchColumns = (organization: Organization): Column<BranchRow>[] => [
    { key: 'name', header: 'Şube', primary: true, cell: (row) => <span className="font-medium">{row.branch.name}</span> },
    { key: 'address', header: 'Adres', cell: (row) => row.branch.address },
    { key: 'departments', header: 'Bölüm', align: 'right', cell: (row) => row.departmentCount },
    { key: 'hours', header: 'Çalışma saatleri', cell: (row) => <span className="tabular-nums">{summarizeHours(row.hours)}</span> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <span className="inline-flex flex-wrap justify-end gap-2">
          <Button variant="secondary" onClick={() => setDialog({ kind: 'hours', branch: row.branch })}>
            Saatler
          </Button>
          <Button variant="secondary" onClick={() => setDialog({ kind: 'branch', organization, branch: row.branch })}>
            Düzenle
          </Button>
          <Button variant="ghost" onClick={() => setDialog({ kind: 'delete-branch', branch: row.branch })}>
            Sil
          </Button>
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Kurumlar</h1>
        <Button onClick={() => setDialog({ kind: 'organization', organization: null })}>Kurum ekle</Button>
      </div>

      {state.loading ? (
        <div className="flex justify-center py-10">
          <Spinner label="Yükleniyor..." />
        </div>
      ) : state.error ? (
        <div className="space-y-3">
          <Alert tone="error">{state.error}</Alert>
          <Button variant="secondary" onClick={state.reload}>
            Yeniden dene
          </Button>
        </div>
      ) : state.data && state.data.length > 0 ? (
        state.data.map(({ organization, branches }) => (
          <Card
            key={organization.id}
            title={organization.name}
            icon={Building2}
            flush
            actions={
              <span className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => setDialog({ kind: 'branch', organization, branch: null })}>
                  Şube ekle
                </Button>
                <Button variant="secondary" onClick={() => setDialog({ kind: 'organization', organization })}>
                  Düzenle
                </Button>
                <Button variant="ghost" onClick={() => setDialog({ kind: 'delete-organization', organization })}>
                  Sil
                </Button>
              </span>
            }
          >
            <Table
              columns={branchColumns(organization)}
              rows={branches}
              rowKey={(row) => row.branch.id}
              emptyTitle="Bu kurumda şube yok"
              emptyDescription="“Şube ekle” ile ilk şubeyi oluşturun. Yeni şube hafta içi 08:00-17:00 çalışma saatleriyle başlar."
            />
          </Card>
        ))
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
          <EmptyState title="Henüz kurum yok" description="“Kurum ekle” ile ilk kurumu oluşturun." />
        </div>
      )}

      <NameFormModal
        open={dialog?.kind === 'organization'}
        title={dialog?.kind === 'organization' && dialog.organization ? 'Kurumu düzenle' : 'Kurum ekle'}
        nameLabel="Kurum adı"
        nameRequiredMessage="Kurum adı zorunludur."
        initialName={dialog?.kind === 'organization' ? (dialog.organization?.name ?? '') : ''}
        onClose={close}
        onSubmit={(name) =>
          saved(
            () => (dialog?.kind === 'organization' && dialog.organization ? updateOrganization(dialog.organization.id, name) : createOrganization(name)),
            dialog?.kind === 'organization' && dialog.organization ? 'Kurum güncellendi.' : 'Kurum eklendi.',
          )
        }
      />
      <NameFormModal
        open={dialog?.kind === 'branch'}
        title={dialog?.kind === 'branch' && dialog.branch ? 'Şubeyi düzenle' : 'Şube ekle'}
        nameLabel="Şube adı"
        nameRequiredMessage="Şube adı zorunludur."
        addressLabel="Adres"
        addressRequiredMessage="Şube adresi zorunludur."
        initialName={dialog?.kind === 'branch' ? (dialog.branch?.name ?? '') : ''}
        initialAddress={dialog?.kind === 'branch' ? (dialog.branch?.address ?? '') : ''}
        onClose={close}
        onSubmit={(name, address) =>
          saved(
            () => {
              if (dialog?.kind !== 'branch') return Promise.resolve()
              return dialog.branch ? updateBranch(dialog.branch.id, name, address) : createBranch(dialog.organization.id, name, address)
            },
            dialog?.kind === 'branch' && dialog.branch ? 'Şube güncellendi.' : 'Şube eklendi.',
          )
        }
      />
      <WorkingHoursModal
        open={dialog?.kind === 'hours'}
        branchId={dialog?.kind === 'hours' ? dialog.branch.id : ''}
        branchName={dialog?.kind === 'hours' ? dialog.branch.name : ''}
        onClose={close}
        onChanged={state.reload}
      />
      <ConfirmDialog
        open={dialog?.kind === 'delete-organization'}
        title="Kurumu sil"
        confirmLabel="Kurumu sil"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'delete-organization') return
          await deleteOrganization(dialog.organization.id)
          toast.success('Kurum silindi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'delete-organization' ? (
          <p>
            <span className="font-medium">{dialog.organization.name}</span> kurumu silinecek. Kurumun şubeleri varsa silinemez.
          </p>
        ) : null}
      </ConfirmDialog>
      <ConfirmDialog
        open={dialog?.kind === 'delete-branch'}
        title="Şubeyi sil"
        confirmLabel="Şubeyi sil"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'delete-branch') return
          await deleteBranch(dialog.branch.id)
          toast.success('Şube silindi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'delete-branch' ? (
          <p>
            <span className="font-medium">{dialog.branch.name}</span> şubesi ve çalışma saatleri silinecek. Şubede bölüm veya yönetici varsa silinemez.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
