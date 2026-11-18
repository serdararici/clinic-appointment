import { Container } from './Container'

/** A slim footer under every page of the app (not on login and register, which have their own frame). */
export function Footer() {
  return (
    <footer className="border-t border-slate-200 bg-white">
      <Container className="py-5">
        <p className="text-sm text-slate-500">Klinik Randevu · Nesneye Dayalı Programlama dönem projesi · 2026</p>
      </Container>
    </footer>
  )
}
