import { ROLES, type Role } from '../api/types'

/** The logged-in user, as far as the UI needs to know it. */
export interface SessionUser {
  id: string
  name: string
  email: string
  role: Role
  /** Staff and clinic managers only. */
  branchId: string | null
  organizationId: string | null
  /** When the token expires, in epoch milliseconds. */
  expiresAt: number
}

// JWT segments are base64url encoded UTF-8 (names such as "Şahin" need real UTF-8 decoding, not just atob).
function decodeSegment(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(segment.length / 4) * 4, '=')
  const bytes = Uint8Array.from(atob(base64), (char) => char.charCodeAt(0))
  return new TextDecoder().decode(bytes)
}

interface Claims {
  sub?: string
  email?: string
  name?: string
  role?: string
  branch_id?: string
  organization_id?: string
  exp?: number
}

/**
 * Reads the user out of a token. This does NOT verify the signature: that is the API's job on every request.
 * Decoding is only a convenience so the UI can show the name and pick the right menu and pages.
 * Returns null for anything that is not a well-formed token with a known role.
 */
export function decodeToken(token: string): SessionUser | null {
  try {
    const payload = token.split('.')[1]
    if (!payload) return null

    const claims = JSON.parse(decodeSegment(payload)) as Claims
    const role = ROLES.find((known) => known === claims.role)
    if (!claims.sub || !role || typeof claims.exp !== 'number') return null

    return {
      id: claims.sub,
      name: claims.name ?? '',
      email: claims.email ?? '',
      role,
      branchId: claims.branch_id ?? null,
      organizationId: claims.organization_id ?? null,
      expiresAt: claims.exp * 1000, // "exp" is in seconds
    }
  } catch {
    return null
  }
}

export function isExpired(user: SessionUser, now: number = Date.now()): boolean {
  return user.expiresAt <= now
}
