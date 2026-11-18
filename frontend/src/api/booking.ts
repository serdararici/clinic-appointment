import { api } from './client'
import type {
  AppointmentDetail,
  AppointmentStatus,
  BookedAppointment,
  BranchSummary,
  CreateAppointmentRequest,
  Department,
  DoctorSummary,
  Service,
  Slot,
} from './types'

// The calls of the patient's booking flow and appointment list. Every list here is public except the
// appointment calls, which need the patient's token (added by the client).

export const listBranches = (signal?: AbortSignal) => api.get<BranchSummary[]>('/api/branches', undefined, signal)

export const listDepartments = (branchId: string, signal?: AbortSignal) =>
  api.get<Department[]>(`/api/branches/${branchId}/departments`, undefined, signal)

export const listServices = (departmentId: string, signal?: AbortSignal) =>
  api.get<Service[]>(`/api/departments/${departmentId}/services`, undefined, signal)

export const listDoctors = (departmentId: string, signal?: AbortSignal) =>
  api.get<DoctorSummary[]>(`/api/departments/${departmentId}/doctors`, undefined, signal)

/** Slots of a doctor for one local day ("YYYY-MM-DD"). Examination services. */
export const listDoctorSlots = (doctorId: string, date: string, signal?: AbortSignal) =>
  api.get<Slot[]>(`/api/doctors/${doctorId}/slots`, { date }, signal)

/** Slots of a Diagnostic service for one local day. */
export const listServiceSlots = (serviceId: string, date: string, signal?: AbortSignal) =>
  api.get<Slot[]>(`/api/services/${serviceId}/slots`, { date }, signal)

// The booking response carries the status as a number (the enum is not written as a name there), while
// the list sends names. Both are mapped to the same names so the screens only deal with one form.
const STATUS_BY_NUMBER: readonly AppointmentStatus[] = ['Pending', 'Confirmed', 'Rejected', 'Cancelled']

function normalizeStatus(status: unknown): AppointmentStatus {
  if (typeof status === 'number') return STATUS_BY_NUMBER[status] ?? 'Pending'
  return status as AppointmentStatus
}

export async function createAppointment(request: CreateAppointmentRequest): Promise<BookedAppointment> {
  const created = await api.post<BookedAppointment>('/api/appointments', request)
  return { ...created, status: normalizeStatus(created.status) }
}

export const listPatientAppointments = (patientId: string, signal?: AbortSignal) =>
  api.get<AppointmentDetail[]>(`/api/patients/${patientId}/appointments`, undefined, signal)

export const cancelAppointment = (appointmentId: string) => api.post<void>(`/api/appointments/${appointmentId}/cancel`)
