import { api } from './client'
import type { AppointmentDetail, DoctorProfile } from './types'

// The doctor's own calls (Doctor role; the API refuses any other doctor's id). Cancelling an appointment
// uses cancelAppointment from ./booking: it is the same endpoint the patient uses.

export const getDoctorProfile = (doctorId: string, signal?: AbortSignal) =>
  api.get<DoctorProfile>(`/api/doctors/${doctorId}/profile`, undefined, signal)

export const listDoctorAppointments = (doctorId: string, signal?: AbortSignal) =>
  api.get<AppointmentDetail[]>(`/api/doctors/${doctorId}/appointments`, undefined, signal)
