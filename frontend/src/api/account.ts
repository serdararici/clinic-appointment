import { api } from './client'
import type { Account, ChangePasswordRequest, UpdateAccountRequest } from './types'

// "My account": the API always works on the user in the token, so no id is sent.

export const getAccount = (signal?: AbortSignal) => api.get<Account>('/api/account', undefined, signal)

export const updateAccount = (request: UpdateAccountRequest) => api.put<Account>('/api/account', request)

/** 400 if the current password is wrong or the new one is too short or too long. */
export const changePassword = (request: ChangePasswordRequest) => api.put<void>('/api/account/password', request)
