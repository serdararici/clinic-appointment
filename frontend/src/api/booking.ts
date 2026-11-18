import { api } from './client'
import type {
  AppointmentDetail,
  BookedAppointment,
  BranchSummary,
  CreateAppointmentRequest,
  Department,
  PricePreview,
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

export const getPricePreview = (serviceId: string, signal?: AbortSignal) =>
  api.get<PricePreview>(`/api/services/${serviceId}/price-preview`, undefined, signal)

export const createAppointment = (request: CreateAppointmentRequest) => api.post<BookedAppointment>('/api/appointments', request)

export const listPatientAppointments = (patientId: string, signal?: AbortSignal) =>
  api.get<AppointmentDetail[]>(`/api/patients/${patientId}/appointments`, undefined, signal)

export const cancelAppointment = (appointmentId: string) => api.post<void>(`/api/appointments/${appointmentId}/cancel`)
