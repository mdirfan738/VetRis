import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// VetRIS-Scaffold frontend build config
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true
  },
  preview: {
    port: 5173,
    host: true
  }
})
