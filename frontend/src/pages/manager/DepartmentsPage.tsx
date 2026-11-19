import { useCallback, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { listDepartments, listServices } from '../../api/booking'
import { deleteDepartment, deleteService } from '../../api/manager'
import type { Department, Service } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { Alert } from '../../components/Alert'
import { Badge } from '../../components/Badge'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { EmptyState } from '../../components/EmptyState'
import { Spinner } from '../../components/Spinner'
import { Table, type Column } from '../../components/Table'
import { formatCurrency } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { SERVICE_TYPE_LABELS } from '../patient/labels'
import { DepartmentFormModal } from './DepartmentFormModal'
import { ServiceFormModal } from './ServiceFormModal'
import { usePageTitle } from '../../utils/usePageTitle'
import { useToast } from '../../components/useToast'
import { Hospital } from 'lucide-react'

interface DepartmentWithServices {
  department: Department
  services: Service[]
}

/** What the open modal is about: a form for a department or a service, or a delete confirmation. */
type Dialog =
  | { kind: 'department'; department: Department | null }
  | { kind: 'service'; department: Department; service: Service | null }
  | { kind: 'delete-department'; department: Department }
  | { kind: 'delete-service'; service: Service }

/** The branch's departments with their services: add, edit and delete both. */
export function DepartmentsPage() {
  usePageTitle('Bölümler')
  const { user } = useAuth()
  const toast = useToast()
  const branchId = user?.branchId ?? ''

  const state = useAsync(
    useCallback(
      async (signal: AbortSignal): Promise<DepartmentWithServices[]> => {
        const departments = await listDepartments(branchId, signal)
        return Promise.all(departments.map(async (department) => ({ department, services: await listServices(department.id, signal) })))
      },
      [branchId],
    ),
  )
  const [searchParams] = useSearchParams()
  // "Bölüm ekle" on the manager home opens the form right away (?ekle=1).
  const [dialog, setDialog] = useState<Dialog | null>(() => (searchParams.get('ekle') ? { kind: 'department', department: null } : null))

  const close = () => setDialog(null)
  const saved = (message: string) => {
    close()
    state.reload()
    toast.success(message)
  }

  const serviceColumns = (department: Department): Column<Service>[] => [
    { key: 'name', header: 'Hizmet', primary: true, cell: (row) => <span className="font-medium">{row.name}</span> },
    { key: 'type', header: 'Tür', cell: (row) => <Badge tone={row.serviceType === 'Examination' ? 'teal' : 'slate'}>{SERVICE_TYPE_LABELS[row.serviceType]}</Badge> },
    {
      key: 'duration',
      header: 'Süre',
      // An examination lasts as long as the doctor's slot duration, so no fixed number is shown for it.
      cell: (row) => (row.serviceType === 'Diagnostic' ? <span className="tabular-nums">{row.durationMinutes} dk</span> : <span className="text-slate-600">Doktora göre</span>),
    },
    { key: 'price', header: 'Ücret', align: 'right', cell: (row) => formatCurrency(row.basePrice) },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" onClick={() => setDialog({ kind: 'service', department, service: row })}>
            Düzenle
          </Button>
          <Button variant="ghost" onClick={() => setDialog({ kind: 'delete-service', service: row })}>
            Sil
          </Button>
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Bölümler</h1>
        <Button onClick={() => setDialog({ kind: 'department', department: null })}>Bölüm ekle</Button>
      </div>

      {!branchId ? <Alert tone="error">Hesabınıza bağlı bir şube bulunamadı.</Alert> : null}

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
        state.data.map(({ department, services }) => (
          <Card
            key={department.id}
            title={department.name}
            icon={Hospital}
            flush
            actions={
              <span className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => setDialog({ kind: 'service', department, service: null })}>
                  Hizmet ekle
                </Button>
                <Button variant="secondary" onClick={() => setDialog({ kind: 'department', department })}>
                  Düzenle
                </Button>
                <Button variant="ghost" onClick={() => setDialog({ kind: 'delete-department', department })}>
                  Sil
                </Button>
              </span>
            }
          >
            <Table
              columns={serviceColumns(department)}
              rows={services}
              rowKey={(row) => row.id}
              emptyTitle="Bu bölümde hizmet yok"
              emptyDescription="“Hizmet ekle” ile ilk hizmeti tanımlayın."
            />
          </Card>
        ))
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
          <EmptyState title="Henüz bölüm yok" description="“Bölüm ekle” ile ilk bölümü oluşturun." />
        </div>
      )}

      <DepartmentFormModal
        open={dialog?.kind === 'department'}
        branchId={branchId}
        department={dialog?.kind === 'department' ? dialog.department : null}
        onClose={close}
        onSaved={() => saved(dialog?.kind === 'department' && dialog.department ? 'Bölüm güncellendi.' : 'Bölüm eklendi.')}
      />
      <ServiceFormModal
        open={dialog?.kind === 'service'}
        departmentId={dialog?.kind === 'service' ? dialog.department.id : ''}
        service={dialog?.kind === 'service' ? dialog.service : null}
        onClose={close}
        onSaved={() => saved(dialog?.kind === 'service' && dialog.service ? 'Hizmet güncellendi.' : 'Hizmet eklendi.')}
      />
      <ConfirmDialog
        open={dialog?.kind === 'delete-department'}
        title="Bölümü sil"
        confirmLabel="Bölümü sil"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'delete-department') return
          await deleteDepartment(dialog.department.id)
          toast.success('Bölüm silindi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'delete-department' ? (
          <p>
            <span className="font-medium">{dialog.department.name}</span> bölümü silinecek. Bölümde hizmet veya personel varsa silinemez.
          </p>
        ) : null}
      </ConfirmDialog>
      <ConfirmDialog
        open={dialog?.kind === 'delete-service'}
        title="Hizmeti sil"
        confirmLabel="Hizmeti sil"
        danger
        onClose={close}
        onConfirm={async () => {
          if (dialog?.kind !== 'delete-service') return
          await deleteService(dialog.service.id)
          toast.success('Hizmet silindi.')
          state.reload()
        }}
      >
        {dialog?.kind === 'delete-service' ? (
          <p>
            <span className="font-medium">{dialog.service.name}</span> hizmeti silinecek. Hizmete ait randevular varsa veya doktorlar bu hizmeti veriyorsa silinemez.
          </p>
        ) : null}
      </ConfirmDialog>
    </div>
  )
}
