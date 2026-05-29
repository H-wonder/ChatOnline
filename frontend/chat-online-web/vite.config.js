import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  build: {
    // 直接输出到后端 wwwroot，build 后 dotnet run 即可同时提供前端和 API
    outDir: '../../backend/ChatOnline.Api/wwwroot',
    emptyOutDir: true,
  },
  // 开发服务器配置（仅 npx vite 开发时使用）
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5000', ws: true },
      '/uploads': { target: 'http://localhost:5000', changeOrigin: true }
    }
  }
})
