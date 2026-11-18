import { Outlet } from 'react-router-dom'
import { Navbar } from './Navbar'

/** The frame of every page: the navbar on top and the routed page below it. */
export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6 sm:py-8">
        <Outlet />
      </main>
    </div>
  )
}
