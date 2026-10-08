import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // Config de esta app. El layout y el plugin de Vuetify la leen por acá, así la fábrica
      // (frontend/) puede reutilizarlos con su propia config.
      '@config': fileURLToPath(new URL('./src/comun/config', import.meta.url))
    }
  }
})
