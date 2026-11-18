import { Outlet } from 'react-router-dom'
import { Container } from './Container'
import { Footer } from './Footer'
import { Navbar } from './Navbar'

/**
 * The frame of every page except login and register: the navbar, the routed page and the footer, all in the same
 * container width. A flex column of at least the screen height keeps the footer at the bottom of short pages
 * (it is not fixed: on long pages it comes after the content).
 */
export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <main className="flex-1">
        <Container className="py-6 sm:py-8">
          <Outlet />
        </Container>
      </main>
      <Footer />
    </div>
  )
}
