import { useCallback, useEffect, useState } from 'react'
import { readableMessage } from '../api/errors'

interface Result<T> {
  /** The loader and reload counter this result belongs to; a different pair means it is out of date. */
  load: unknown
  nonce: number
  data?: T
  error?: string
}

export interface AsyncState<T> {
  data: T | undefined
  /** A readable (Turkish) message, or null. */
  error: string | null
  loading: boolean
  reload: () => void
}

/**
 * Runs `load` when the component mounts and whenever `load` changes (wrap it in useCallback with its inputs
 * as dependencies), and again on `reload()`. Late answers of an outdated request are ignored.
 * `loading` is derived rather than set, so the effect never has to call setState synchronously.
 */
export function useAsync<T>(load: (signal: AbortSignal) => Promise<T>): AsyncState<T> {
  const [nonce, setNonce] = useState(0)
  const [result, setResult] = useState<Result<T> | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    load(controller.signal).then(
      (data) => setResult({ load, nonce, data }),
      (error: unknown) => {
        if (controller.signal.aborted) return
        setResult({ load, nonce, error: readableMessage(error) })
      },
    )
    return () => controller.abort()
  }, [load, nonce])

  const reload = useCallback(() => setNonce((value) => value + 1), [])
  const current = result !== null && result.load === load && result.nonce === nonce

  return {
    data: current ? result.data : undefined,
    error: current ? (result.error ?? null) : null,
    loading: !current,
    reload,
  }
}
