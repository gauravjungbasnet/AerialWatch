import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// In development, /api and /health are proxied to the .NET API (backend launch profile "http": http://localhost:5080).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Same aliases as tsconfig.app.json "paths".
  resolve: {
    alias: {
      '@app': '/src/app',
      '@shared': '/src/shared',
      '@modules': '/src/modules',
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/health': 'http://localhost:5080',
    },
  },
});
