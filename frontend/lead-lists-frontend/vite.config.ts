import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env  = loadEnv(mode, process.cwd(), '')
  const api  = env.VITE_API || 'http://localhost:5267'
  const Port = Number(env.VITE_PORT ?? 5173) || 5173

  return {
    plugins: [react()],
    server: {
      port: Port,
      proxy: {
        '/lead-lists': {
          target: api,
          changeOrigin: true,
          secure: false,
        },
        '/health': {
          target: api,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
