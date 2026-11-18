import { EmptyState } from '../components/EmptyState'
import { LinkButton } from '../components/LinkButton'

/** Shown for any address that no route matches. */
export function NotFoundPage() {
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <EmptyState
        title="Sayfa bulunamadı"
        description="Aradığınız sayfa mevcut değil veya taşınmış olabilir."
        action={<LinkButton to="/">Ana sayfaya dön</LinkButton>}
      />
    </div>
  )
}
