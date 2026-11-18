import { useCallback, useState } from 'react'
import { getAccount } from '../../api/account'
import type { Account } from '../../api/types'
import { ROLE_LABELS } from '../../auth/roles'
import { Alert } from '../../components/Alert'
import { Badge } from '../../components/Badge'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { useAsync } from '../../utils/useAsync'
import { PasswordCard } from './PasswordCard'
import { ProfileCard } from './ProfileCard'
import { usePageTitle } from '../../utils/usePageTitle'

/** "Hesabım": the same page for every role: personal information and changing the password. */
export function AccountPage() {
  usePageTitle('Hesabım')
  const state = useAsync(useCallback((signal: AbortSignal) => getAccount(signal), []))
  // After a save the cards keep showing what was saved without reloading (a reload would blank the forms).
  const [saved, setSaved] = useState<Account | null>(null)
  const account = saved ?? state.data

  return (
    <div className="mx-auto max-w-3xl space-y-5">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Hesabım</h1>
        {account ? <Badge tone="teal">{ROLE_LABELS[account.role]}</Badge> : null}
      </div>

      {account ? (
        <>
          <ProfileCard account={account} onSaved={setSaved} />
          <PasswordCard />
        </>
      ) : state.error ? (
        <div className="space-y-3">
          <Alert tone="error">{state.error}</Alert>
          <Button variant="secondary" onClick={state.reload}>
            Yeniden dene
          </Button>
        </div>
      ) : (
        <div className="flex justify-center py-10">
          <Spinner label="Yükleniyor..." />
        </div>
      )}
    </div>
  )
}
