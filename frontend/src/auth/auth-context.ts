import { createContext } from 'react'
import type { SessionUser } from './jwt'

export interface AuthContextValue {
  /** The logged-in user, or null when nobody is logged in. */
  user: SessionUser | null
  /** Logs in with the API; rejects with an ApiError (Turkish message) if the credentials are wrong. */
  login: (email: string, password: string) => Promise<SessionUser>
  /** Clears the session. */
  logout: () => void
  /** Shows a new name everywhere (navbar, greetings) after the user changed it in "Hesabım". */
  updateName: (name: string) => void
}

// Kept apart from the provider component so each file exports either a component or plain values
// (the react-refresh lint rule requires that).
export const AuthContext = createContext<AuthContextValue | null>(null)
