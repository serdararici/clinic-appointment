/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the Clinic API (no trailing slash needed). Defaults to http://localhost:5268. */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
