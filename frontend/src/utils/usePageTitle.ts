import { useEffect } from 'react'

const APP_NAME = 'Klinik Randevu'

/** Sets the browser tab title to "<title> · Klinik Randevu" while the page is shown (and for screen readers on navigation). */
export function usePageTitle(title: string): void {
  useEffect(() => {
    document.title = `${title} · ${APP_NAME}`
  }, [title])
}
