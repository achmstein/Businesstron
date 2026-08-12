import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Aspire's WithReference("businesstron-server") injects per-endpoint env vars of the
// form services__<name>__<scheme>__<index>. Prefer plain HTTP because Vite's proxy
// doesn't speak h2 (the .NET https endpoint negotiates HTTP/2).
const proxyTarget =
  process.env['services__businesstron-server__http__0'] ||
  process.env.SERVER_HTTP ||
  process.env['services__businesstron-server__https__0'] ||
  process.env.SERVER_HTTPS ||
  'http://localhost:5080'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    proxy: {
      '/api': { target: proxyTarget, changeOrigin: true, secure: false },
      '/health': { target: proxyTarget, changeOrigin: true, secure: false },
      '/hangfire': { target: proxyTarget, changeOrigin: true, secure: false },
    },
  },
})
