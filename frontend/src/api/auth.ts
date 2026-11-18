import { api } from './client'
import type { LoginRequest, LoginResponse, RegisteredUser, RegisterRequest } from './types'

// The auth endpoints are public, so no bearer token is sent (skipAuth): a stale token in storage must not
// get in the way of logging in again.

export function loginRequest(credentials: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>('/api/auth/login', credentials, { skipAuth: true })
}

export function registerRequest(details: RegisterRequest): Promise<RegisteredUser> {
  return api.post<RegisteredUser>('/api/auth/register', details, { skipAuth: true })
}
