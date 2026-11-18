import { ROLE_LABELS } from '../auth/roles'
import { Card } from '../components/Card'
import { DEMO_ACCOUNTS, DEMO_PASSWORD } from './demo-accounts-data'

/** A hint on the login page: pick a demo account to fill in the form. */
export function DemoAccounts({ onPick }: { onPick: (email: string, password: string) => void }) {
  return (
    <Card title="Demo hesapları">
      <p className="mb-3 text-sm text-slate-600">
        Denemek için bir hesap seçin. Tüm hesapların şifresi{' '}
        <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-slate-900">{DEMO_PASSWORD}</code> şeklindedir.
      </p>
      <ul className="divide-y divide-slate-200 rounded-md border border-slate-200">
        {DEMO_ACCOUNTS.map((account) => (
          <li key={account.email}>
            <button
              type="button"
              onClick={() => onPick(account.email, DEMO_PASSWORD)}
              className="flex min-h-11 w-full flex-col items-start gap-0.5 px-3 py-2 text-left transition-colors hover:bg-slate-50 sm:flex-row sm:items-center sm:justify-between sm:gap-4"
            >
              <span className="text-sm font-medium text-slate-900">
                {account.name}
                <span className="ml-2 font-normal text-slate-600">
                  {ROLE_LABELS[account.role]}
                  {account.workplace ? `, ${account.workplace}` : ''}
                </span>
              </span>
              <span className="text-sm text-slate-600">{account.email}</span>
            </button>
          </li>
        ))}
      </ul>
    </Card>
  )
}
