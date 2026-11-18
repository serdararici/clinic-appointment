import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
// Tailwind CSS v4 runs as a Vite plugin: no tailwind.config file, the theme lives in src/index.css.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // The API allows exactly this origin through CORS (Cors:AllowedOrigins), so the port must not drift.
    port: 5173,
    strictPort: true,
  },
})
