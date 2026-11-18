import { api } from './client'
import type {
  AddAvailabilityRequest,
  AppointmentDetail,
  AutoSchedule,
  Availability,
  BulkAvailabilityRequest,
  BulkAvailabilityResult,
  DoctorProfile,
} from './types'

// The doctor's own calls (Doctor role; the API refuses any other doctor's id). Cancelling an appointment
// uses cancelAppointment from ./booking: it is the same endpoint the patient uses.

export const getDoctorProfile = (doctorId: string, signal?: AbortSignal) =>
  api.get<DoctorProfile>(`/api/doctors/${doctorId}/profile`, undefined, signal)

export const listDoctorAppointments = (doctorId: string, signal?: AbortSignal) =>
  api.get<AppointmentDetail[]>(`/api/doctors/${doctorId}/appointments`, undefined, signal)

export const listAvailability = (doctorId: string, signal?: AbortSignal) =>
  api.get<Availability[]>(`/api/doctors/${doctorId}/availability`, undefined, signal)

export const addAvailability = (doctorId: string, request: AddAvailabilityRequest) =>
  api.post<Availability>(`/api/doctors/${doctorId}/availability`, request)

export const addBulkAvailability = (doctorId: string, request: BulkAvailabilityRequest) =>
  api.post<BulkAvailabilityResult>(`/api/doctors/${doctorId}/availability/bulk`, request)

export const deleteAvailability = (doctorId: string, availabilityId: string) =>
  api.delete(`/api/doctors/${doctorId}/availability/${availabilityId}`)

/** The doctor changes their own slot duration (10, 15, 20, 30 or 60 minutes). */
export const setSlotDuration = (doctorId: string, slotDurationMinutes: number) =>
  api.put<{ staffId: string; slotDurationMinutes: number }>(`/api/staff/${doctorId}/slot-duration`, { slotDurationMinutes })

export const getAutoSchedule = (doctorId: string, signal?: AbortSignal) =>
  api.get<AutoSchedule>(`/api/doctors/${doctorId}/auto-schedule`, undefined, signal)

/** Turning it on fills the booking window with the default hours right away. */
export const setAutoSchedule = (doctorId: string, enabled: boolean) =>
  api.put<AutoSchedule>(`/api/doctors/${doctorId}/auto-schedule`, { enabled })

/** The doctor chooses whether new appointments are confirmed at once or wait for their approval. */
export const setAutoConfirm = (doctorId: string, enabled: boolean) =>
  api.put<DoctorProfile>(`/api/doctors/${doctorId}/auto-confirm`, { enabled })

/** Only a Pending appointment can be confirmed or rejected (409 otherwise). */
export const confirmAppointment = (appointmentId: string) => api.post<void>(`/api/appointments/${appointmentId}/confirm`)

export const rejectAppointment = (appointmentId: string) => api.post<void>(`/api/appointments/${appointmentId}/reject`)
