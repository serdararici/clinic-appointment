import type { ReactNode } from 'react'

const FEATURES = ['Hastalar birkaç adımda randevu alır.', 'Doktorlar kendi çalışma takvimini yönetir.', 'Klinikler tek yerden yönetilir.']
const TAGLINE = 'Randevu alın, takviminizi ve kliniğinizi tek yerden yönetin.'

function Check() {
  return (
    <svg viewBox="0 0 20 20" className="mt-0.5 h-5 w-5 shrink-0 text-teal-300" fill="none" stroke="currentColor" strokeWidth="2.2" aria-hidden="true">
      <path d="M4 10.5 8 14.5 16 6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

// A plain geometric mark (a cross in a rounded square); it stands for no real organization.
function Mark() {
  return (
    <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-teal-700 text-white" aria-hidden="true">
      <svg viewBox="0 0 20 20" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
        <path d="M10 4v12M4 10h12" />
      </svg>
    </span>
  )
}

interface AuthLayoutProps {
  title: string
  subtitle: string
  /** The form (usually a Card). */
  children: ReactNode
  /** Small text under the form, such as the link to the other page. */
  footer?: ReactNode
  /** Extra content below the first screen (the demo accounts); a hint at the bottom of the first screen points to it. */
  demo?: ReactNode
}

/**
 * The frame of the login and register pages. From lg up: a brand panel on the left (it stays in place while the
 * right side scrolls) and the form vertically centered on the right. On a phone: a compact brand header and the form.
 * The first screen fills the viewport; anything passed as `demo` sits below the fold.
 */
export function AuthLayout({ title, subtitle, children, footer, demo }: AuthLayoutProps) {
  return (
    <div className="bg-slate-50 lg:grid lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)]">
      {/* Brand panel, desktop only. Simple shapes and a slate-to-teal gradient: no photos, no figures, no real brand. */}
      <aside className="relative hidden overflow-hidden bg-slate-900 text-white lg:sticky lg:top-0 lg:flex lg:h-dvh lg:flex-col lg:justify-between lg:p-12">
        <div className="absolute inset-0 bg-gradient-to-br from-slate-900 via-slate-900 to-teal-800" aria-hidden="true" />
        <div className="absolute -right-24 -top-24 h-96 w-96 rounded-full border border-teal-300/20" aria-hidden="true" />
        <div className="absolute -right-10 -top-10 h-64 w-64 rounded-full border border-teal-300/20" aria-hidden="true" />
        <div className="absolute -bottom-28 -left-20 h-80 w-80 rounded-full bg-teal-700/25" aria-hidden="true" />

        <div className="relative flex items-center gap-3">
          <Mark />
          <span className="text-lg font-semibold tracking-tight">Klinik Randevu</span>
        </div>

        <div className="relative max-w-md space-y-8">
          <h2 className="text-3xl font-semibold leading-tight tracking-tight">{TAGLINE}</h2>
          <ul className="space-y-3 text-slate-200">
            {FEATURES.map((feature) => (
              <li key={feature} className="flex gap-3">
                <Check />
                <span>{feature}</span>
              </li>
            ))}
          </ul>
        </div>

        <span aria-hidden="true" />
      </aside>

      <div className="min-w-0">
        {/* First screen: exactly the viewport high. */}
        <div className="flex min-h-dvh flex-col">
          {/* Phone: compact brand header. */}
          <header className="relative overflow-hidden bg-slate-900 px-4 py-5 text-white lg:hidden">
            <div className="absolute inset-0 bg-gradient-to-br from-slate-900 via-slate-900 to-teal-800" aria-hidden="true" />
            <div className="relative flex items-center gap-3">
              <Mark />
              <div>
                <p className="text-base font-semibold leading-tight tracking-tight">Klinik Randevu</p>
                <p className="text-sm text-slate-300">{TAGLINE}</p>
              </div>
            </div>
          </header>

          <main className="flex flex-1 items-center justify-center px-4 py-8 sm:px-8">
            <div className="w-full max-w-md space-y-5">
              <div>
                <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
                <p className="mt-1 text-slate-600">{subtitle}</p>
              </div>
              {children}
              {footer}
            </div>
          </main>

          {demo ? (
            <a href="#demo-hesaplari" className="flex items-center justify-center gap-1.5 pb-6 text-sm text-slate-600 hover:text-slate-900">
              Demo hesapları için aşağı kaydırın
              <svg viewBox="0 0 20 20" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <path d="m5 8 5 5 5-5" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </a>
          ) : null}
        </div>

        {demo ? (
          <section id="demo-hesaplari" aria-label="Demo hesapları" className="border-t border-slate-200 bg-white px-4 py-10 sm:px-8">
            <div className="mx-auto max-w-md">{demo}</div>
          </section>
        ) : null}
      </div>
    </div>
  )
}
