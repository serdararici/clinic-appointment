import { useCallback, useState, type ReactNode } from 'react'
import { createAppointment, getPricePreview, listBranches } from '../../api/booking'
import { ApiError, readableMessage } from '../../api/errors'
import type { BranchSummary, Department, DoctorSummary, Service, Slot } from '../../api/types'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { Card } from '../../components/Card'
import { LinkButton } from '../../components/LinkButton'
import { StatusBadge } from '../../components/StatusBadge'
import { StepIndicator } from '../../components/StepIndicator'
import { formatCurrency, formatLongDate, formatTime } from '../../utils/format'
import { useAsync } from '../../utils/useAsync'
import { CHOICE_ORDER, EMPTY_SELECTION, type BookingResult, type Selection, type Step } from './booking-types'
import { BranchStep, DepartmentStep, DoctorStep, OrganizationStep, ServiceStep, SlotStep } from './BookingSteps'
import { SERVICE_TYPE_LABELS } from './labels'

const HEADINGS: Record<Step, string> = {
  organization: 'Kurum seçin',
  branch: 'Şube seçin',
  department: 'Bölüm seçin',
  service: 'Hizmet seçin',
  doctor: 'Doktor seçin',
  slot: 'Tarih ve saat seçin',
  summary: 'Randevu özeti',
  done: 'Randevunuz alındı',
}

/** The step a chosen service leads to: examinations pick a doctor, diagnostic services go straight to the time. */
function stepAfterService(service: Service): Step {
  return service.serviceType === 'Examination' ? 'doctor' : 'slot'
}

/**
 * The patient's step-by-step booking flow. `stack` is the path actually walked (skipped steps never enter it),
 * so "Geri" always returns to a screen the patient has seen.
 */
export function BookingPage() {
  const branches = useAsync(useCallback((signal: AbortSignal) => listBranches(signal), []))

  const [stack, setStack] = useState<Step[]>(['organization'])
  const [selection, setSelection] = useState<Selection>(EMPTY_SELECTION)
  const [submitting, setSubmitting] = useState(false)
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [result, setResult] = useState<BookingResult | null>(null)

  const step = stack[stack.length - 1]

  const push = (next: Step) => setStack((current) => [...current, next])
  const choose = (patch: Partial<Selection>) => setSelection((current) => ({ ...current, ...patch }))

  const goBack = () => {
    if (stack.length <= 1) return
    const remaining = stack.slice(0, -1)
    const top = remaining[remaining.length - 1]
    // Forget what was chosen after the screen we return to (including choices made by skipped steps).
    const topIndex = CHOICE_ORDER.findIndex(([name]) => name === top)
    const cleared: Partial<Selection> = {}
    for (const [, field] of CHOICE_ORDER.slice(topIndex + 1)) Object.assign(cleared, { [field]: null })
    setStack(remaining)
    choose(cleared)
    setBookingError(null)
  }

  const restart = () => {
    setStack(['organization'])
    setSelection(EMPTY_SELECTION)
    setResult(null)
    setBookingError(null)
  }

  const pickOrganization = (organizationId: string) => {
    const ofOrganization = (branches.data ?? []).filter((branch) => branch.organizationId === organizationId)
    // A single-branch organization has nothing to choose at the branch step.
    if (ofOrganization.length === 1) {
      choose({ organizationId, branch: ofOrganization[0] })
      push('department')
    } else {
      choose({ organizationId })
      push('branch')
    }
  }

  const pickBranch = (branch: BranchSummary) => {
    choose({ branch })
    push('department')
  }

  const pickDepartment = (department: Department) => {
    choose({ department })
    push('service')
  }

  const pickService = (service: Service) => {
    choose({ service })
    push(stepAfterService(service))
  }

  // A department with one service: the service step is replaced instead of added to the path.
  const autoPickService = useCallback((service: Service) => {
    setSelection((current) => ({ ...current, service }))
    setStack((current) => [...current.slice(0, -1), stepAfterService(service)])
  }, [])

  const pickDoctor = (doctor: DoctorSummary) => {
    choose({ doctor })
    push('slot')
  }

  const pickSlot = (slot: Slot) => {
    choose({ slot })
    setBookingError(null)
  }

  const book = async () => {
    const { service, doctor, slot } = selection
    if (!service || !slot) return
    setSubmitting(true)
    setBookingError(null)
    try {
      // The slot's start is already UTC; the patient comes from the token, so no patientId is sent.
      const appointment = await createAppointment({
        serviceId: service.id,
        startUtc: slot.startUtc,
        doctorId: service.serviceType === 'Examination' ? doctor?.id : undefined,
      })
      setResult({ appointment, selection })
      push('done')
    } catch (error) {
      setBookingError(readableMessage(error))
      // 409 / 422: the slot was taken or is no longer bookable. Go back to the slot screen, which loads fresh slots.
      if (error instanceof ApiError && (error.status === 409 || error.status === 422)) {
        setStack((current) => current.slice(0, -1))
        choose({ slot: null })
      }
    } finally {
      setSubmitting(false)
    }
  }

  // The indicator: doctor appears only for examinations (and while the service is still undecided).
  const labels = ['Şube', 'Bölüm', 'Hizmet', ...(selection.service?.serviceType === 'Diagnostic' ? [] : ['Doktor']), 'Tarih ve saat', 'Özet']
  const indicatorStep: Record<Step, string> = {
    organization: 'Şube',
    branch: 'Şube',
    department: 'Bölüm',
    service: 'Hizmet',
    doctor: 'Doktor',
    slot: 'Tarih ve saat',
    summary: 'Özet',
    done: 'Özet',
  }
  const currentIndex = Math.max(0, labels.indexOf(indicatorStep[step]))

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Randevu al</h1>

      {step !== 'done' ? <StepIndicator labels={labels} current={currentIndex} /> : null}

      <div className="space-y-4">
        <h2 className="text-xl font-semibold text-slate-900">{HEADINGS[step]}</h2>

        {step !== 'done' && selection.branch ? (
          <p className="text-sm text-slate-600">
            {selection.branch.organizationName} · {selection.branch.name}
            {selection.department ? ` · ${selection.department.name}` : ''}
          </p>
        ) : null}

        {step === 'organization' ? <OrganizationStep branches={branches} onSelect={pickOrganization} /> : null}
        {step === 'branch' && branches.data && selection.organizationId ? (
          <BranchStep branches={branches.data} organizationId={selection.organizationId} onSelect={pickBranch} />
        ) : null}
        {step === 'department' && selection.branch ? <DepartmentStep branchId={selection.branch.id} onSelect={pickDepartment} /> : null}
        {step === 'service' && selection.department ? (
          <ServiceStep departmentId={selection.department.id} onSelect={pickService} onAutoSelect={autoPickService} />
        ) : null}
        {step === 'doctor' && selection.department && selection.service ? (
          <DoctorStep departmentId={selection.department.id} serviceId={selection.service.id} onSelect={pickDoctor} />
        ) : null}
        {step === 'slot' && selection.service ? (
          <SlotStep
            service={selection.service}
            doctor={selection.doctor}
            selected={selection.slot}
            notice={bookingError}
            onSelect={pickSlot}
            onContinue={() => push('summary')}
          />
        ) : null}
        {step === 'summary' ? <Summary selection={selection} error={bookingError} submitting={submitting} onConfirm={book} /> : null}
        {step === 'done' && result ? <Done result={result} onRestart={restart} /> : null}
      </div>

      {stack.length > 1 && step !== 'done' ? (
        <Button variant="secondary" size="lg" onClick={goBack} disabled={submitting}>
          Geri
        </Button>
      ) : null}
    </div>
  )
}

function SummaryRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex justify-between gap-4 py-2.5">
      <dt className="text-slate-600">{label}</dt>
      <dd className="text-right font-medium text-slate-900">{children}</dd>
    </div>
  )
}

function SummaryDetails({ selection, startUtc, priceLabel, price }: { selection: Selection; startUtc: string; priceLabel: string; price: number }) {
  const { branch, department, service, doctor } = selection
  return (
    <dl className="divide-y divide-slate-200 text-base">
      <SummaryRow label="Kurum">{branch?.organizationName}</SummaryRow>
      <SummaryRow label="Şube">{branch?.name}</SummaryRow>
      <SummaryRow label="Bölüm">{department?.name}</SummaryRow>
      <SummaryRow label="Hizmet">
        {service?.name}
        {service ? <span className="block text-sm font-normal text-slate-600">{SERVICE_TYPE_LABELS[service.serviceType]}</span> : null}
      </SummaryRow>
      {doctor ? <SummaryRow label="Doktor">{doctor.fullName}</SummaryRow> : null}
      <SummaryRow label="Tarih ve saat">
        <span className="tabular-nums">
          {formatLongDate(startUtc)}, {formatTime(startUtc)}
        </span>
      </SummaryRow>
      <SummaryRow label={priceLabel}>
        <span className="tabular-nums">{formatCurrency(price)}</span>
      </SummaryRow>
    </dl>
  )
}

function Summary({ selection, error, submitting, onConfirm }: { selection: Selection; error: string | null; submitting: boolean; onConfirm: () => void }) {
  const serviceId = selection.service?.id ?? ''
  // The amount comes from the API, which prices it with the same strategy the booking uses.
  const preview = useAsync(useCallback((signal: AbortSignal) => getPricePreview(serviceId, signal), [serviceId]))

  return (
    <div className="space-y-4">
      <Card>
        {selection.slot && selection.service ? (
          <SummaryDetails selection={selection} startUtc={selection.slot.startUtc} priceLabel="Hizmet bedeli" price={selection.service.basePrice} />
        ) : null}
        <div className="mt-3 border-t border-slate-200 pt-3 text-base">
          {preview.loading ? (
            <Spinner label="Ödeyeceğiniz tutar hesaplanıyor..." />
          ) : preview.error ? (
            <div className="space-y-3">
              <Alert tone="error">{preview.error}</Alert>
              <Button variant="secondary" onClick={preview.reload}>
                Yeniden dene
              </Button>
            </div>
          ) : preview.data ? (
            <p className="text-slate-900">
              {preview.data.hasInsurance ? 'Sigortanıza göre ödeyeceğiniz tutar: ' : 'Sigortanız bulunmuyor. Ödeyeceğiniz tutar: '}
              <span className="font-semibold tabular-nums">{formatCurrency(preview.data.price)}</span>
            </p>
          ) : null}
        </div>
      </Card>
      {error ? <Alert tone="error">{error}</Alert> : null}
      <Button size="lg" loading={submitting} disabled={!preview.data} onClick={onConfirm}>
        Randevuyu onayla
      </Button>
    </div>
  )
}

function Done({ result, onRestart }: { result: BookingResult; onRestart: () => void }) {
  const { appointment, selection } = result
  return (
    <div className="space-y-4">
      <Alert tone="success">Randevu talebiniz kaydedildi.</Alert>
      <Card>
        <div className="mb-3 flex flex-wrap items-center gap-3">
          <span className="text-sm text-slate-600">Durum</span>
          <StatusBadge status={appointment.status} />
        </div>
        <SummaryDetails selection={selection} startUtc={appointment.scheduledStartUtc} priceLabel="Ücret" price={appointment.price} />
      </Card>
      <div className="flex flex-wrap gap-3">
        <LinkButton to="/hasta/randevularim" size="lg">
          Randevularım
        </LinkButton>
        <Button variant="secondary" size="lg" onClick={onRestart}>
          Başka randevu al
        </Button>
      </div>
    </div>
  )
}
