import type { ReactNode } from 'react'
import { ROLE_LABELS } from '../auth/roles'
import { useAuth } from '../auth/useAuth'
import { Badge } from '../components/Badge'
import { Card } from '../components/Card'

// The home pages of the roles. They are placeholders for now: they greet the user with what the login
// already knows (name, role) and say plainly that the real screens are still to come. No made-up data.

function HomePlaceholder({ heading, children }: { heading: string; children: ReactNode }) {
  const { user } = useAuth()

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{heading}</h1>
        {user ? <Badge tone="teal">{ROLE_LABELS[user.role]}</Badge> : null}
      </div>

      <Card title={user ? `Hoş geldiniz, ${user.name}` : 'Hoş geldiniz'}>
        <p className="text-slate-700">{children}</p>
        <p className="mt-3 text-sm text-slate-600">Bu bölüm henüz hazırlanıyor.</p>
      </Card>
    </div>
  )
}

export function TechnicianHome() {
  return <HomePlaceholder heading="Personel paneli">Bu rol için henüz bir ekran tanımlanmadı.</HomePlaceholder>
}
