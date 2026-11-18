import { api } from './client'
import type {
  AddWorkingHourRequest,
  Branch,
  ClinicManagerAccount,
  CreateManagerRequest,
  Organization,
  StaffMember,
  UpdateManagerRequest,
  WorkingHour,
} from './types'

// The system administrator's calls. Reading every branch with its organization name is listBranches in ./booking;
// departments of a branch are listDepartments there too.

export const listOrganizations = (signal?: AbortSignal) => api.get<Organization[]>('/api/organizations', undefined, signal)

export const createOrganization = (name: string) => api.post<Organization>('/api/organizations', { name })

export const updateOrganization = (organizationId: string, name: string) => api.put<Organization>(`/api/organizations/${organizationId}`, { name })

/** 409 while the organization still has branches. */
export const deleteOrganization = (organizationId: string) => api.delete(`/api/organizations/${organizationId}`)

/** A new branch is created with Monday-Friday 08:00-17:00 working hours by the API. */
export const createBranch = (organizationId: string, name: string, address: string) =>
  api.post<Branch>(`/api/organizations/${organizationId}/branches`, { name, address })

export const updateBranch = (branchId: string, name: string, address: string) => api.put<Branch>(`/api/branches/${branchId}`, { name, address })

/** 409 while the branch still has departments or clinic managers. */
export const deleteBranch = (branchId: string) => api.delete(`/api/branches/${branchId}`)

export const getWorkingHours = (branchId: string, signal?: AbortSignal) =>
  api.get<WorkingHour[]>(`/api/branches/${branchId}/working-hours`, undefined, signal)

export const addWorkingHour = (branchId: string, request: AddWorkingHourRequest) =>
  api.post<WorkingHour>(`/api/branches/${branchId}/working-hours`, request)

export const removeWorkingHour = (branchId: string, workingHourId: string) => api.delete(`/api/branches/${branchId}/working-hours/${workingHourId}`)

export const listManagers = (signal?: AbortSignal) => api.get<ClinicManagerAccount[]>('/api/managers', undefined, signal)

export const createManager = (branchId: string, request: CreateManagerRequest) => api.post<unknown>(`/api/branches/${branchId}/managers`, request)

export const updateManager = (managerId: string, request: UpdateManagerRequest) => api.put<ClinicManagerAccount>(`/api/managers/${managerId}`, request)

export const deactivateManager = (managerId: string) => api.post<ClinicManagerAccount>(`/api/managers/${managerId}/deactivate`)

export const activateManager = (managerId: string) => api.post<ClinicManagerAccount>(`/api/managers/${managerId}/activate`)

/** Every staff member of every organization (a system administrator is not limited to one). */
export const listAllStaff = (signal?: AbortSignal) => api.get<StaffMember[]>('/api/staff', undefined, signal)
