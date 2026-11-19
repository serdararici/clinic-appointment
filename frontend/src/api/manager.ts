import { api } from './client'
import type { CreateDoctorRequest, CreateTechnicianRequest, Department, SaveServiceRequest, Service, StaffMember, UpdateStaffRequest } from './types'

// The clinic manager's calls. The API limits a manager to their own organization (403 for anything else); the
// screens work on the branch in the manager's token. Reads of departments and services are public and live in
// ./booking (listDepartments, listServices).

export const createDepartment = (branchId: string, name: string) =>
  api.post<Department>(`/api/branches/${branchId}/departments`, { name })

export const updateDepartment = (departmentId: string, name: string) => api.put<Department>(`/api/departments/${departmentId}`, { name })

/** 409 while the department still has services or staff. */
export const deleteDepartment = (departmentId: string) => api.delete(`/api/departments/${departmentId}`)

export const createService = (departmentId: string, request: SaveServiceRequest) =>
  api.post<Service>(`/api/departments/${departmentId}/services`, request)

export const updateService = (serviceId: string, request: SaveServiceRequest) => api.put<Service>(`/api/services/${serviceId}`, request)

/** 409 while appointments exist for the service or doctors still offer it. */
export const deleteService = (serviceId: string) => api.delete(`/api/services/${serviceId}`)

/** All staff of a branch (doctors and technicians, inactive ones included). */
export const listStaff = (branchId: string, signal?: AbortSignal) => api.get<StaffMember[]>('/api/staff', { branchId }, signal)

export const createDoctor = (departmentId: string, request: CreateDoctorRequest) =>
  api.post<StaffMember>(`/api/departments/${departmentId}/doctors`, request)

export const updateStaff = (staffId: string, request: UpdateStaffRequest) => api.put<StaffMember>(`/api/staff/${staffId}`, request)

/** 409 while the staff member has upcoming appointments. */
export const deactivateStaff = (staffId: string) => api.post<StaffMember>(`/api/staff/${staffId}/deactivate`)

export const activateStaff = (staffId: string) => api.post<StaffMember>(`/api/staff/${staffId}/activate`)

/** A health technician is a staff record only: no password, no login. */
export const createTechnician = (departmentId: string, request: CreateTechnicianRequest) =>
  api.post<StaffMember>(`/api/departments/${departmentId}/technicians`, request)

/** Replaces the examination services a doctor offers (the current ones are the doctor's serviceIds). */
export const setStaffServices = (staffId: string, serviceIds: string[]) => api.put<StaffMember>(`/api/staff/${staffId}/services`, { serviceIds })
