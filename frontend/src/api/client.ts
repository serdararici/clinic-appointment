import { getToken } from '../auth/session'
import { ApiError, createApiError, parseErrorBody } from './errors'

// The API address. Override with VITE_API_URL (see .env.example); the default matches `dotnet run` (http profile).
const BASE_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5268').replace(/\/+$/, '')

type QueryValue = string | number | boolean | null | undefined

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  /** Sent as JSON. */
  body?: unknown
  /** Appended as ?a=1&b=2; null and undefined values are left out. */
  query?: Record<string, QueryValue>
  /** Do not send the bearer token (login and register are public). */
  skipAuth?: boolean
  signal?: AbortSignal
}

// Called when the API answers 401 to a request that carried a token, i.e. the session is no longer valid.
// The AuthProvider registers it: it clears the session and goes to the login page.
let unauthorizedHandler: (() => void) | null = null

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(`${BASE_URL}${path}`)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== null && value !== undefined) url.searchParams.set(key, String(value))
  }
  return url.toString()
}

/**
 * Calls the API and returns the parsed JSON (undefined for an empty response such as 204).
 * Adds the bearer token, turns error responses into {@link ApiError} with a Turkish message,
 * and reports a 401 on an authenticated request to the unauthorized handler.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, query, skipAuth = false, signal } = options

  const token = skipAuth ? null : getToken()
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  let response: Response
  try {
    response = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    })
  } catch (error) {
    // A cancelled request is not a failure the user needs to hear about.
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    // fetch rejects only when the server cannot be reached at all (API stopped, CORS blocked, offline).
    throw new ApiError(0, 'Sunucuya ulaşılamıyor. Bağlantınızı kontrol edip tekrar deneyin.', String(error))
  }

  const text = await response.text()

  if (!response.ok) {
    const serverMessage = parseErrorBody(text, response.headers.get('content-type'))
    // A 401 with a token means it was rejected (expired or invalid); without a token it is just "not logged in".
    if (response.status === 401 && token) unauthorizedHandler?.()
    throw createApiError(response.status, serverMessage)
  }

  return (text ? JSON.parse(text) : undefined) as T
}

/** Short helpers for the common verbs. */
export const api = {
  get: <T>(path: string, query?: Record<string, QueryValue>, signal?: AbortSignal) =>
    apiFetch<T>(path, { query, signal }),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    apiFetch<T>(path, { ...options, method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: 'PUT', body }),
  delete: <T = void>(path: string) => apiFetch<T>(path, { method: 'DELETE' }),
}
