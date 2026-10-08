import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const carpetaRuntime = fileURLToPath(new URL('../frontend-runtime/src', import.meta.url))

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // Layout, Vuetify y estilos se comparten con los sistemas generados: viven una sola vez
      // en frontend-runtime/ (la plantilla que se copia a cada sistema) y la fábrica los importa
      // desde '@runtime/...'. Ver docs/estructura.md.
      '@runtime': carpetaRuntime,
      '@config': fileURLToPath(new URL('./src/comun/config', import.meta.url))
    },
    // Lo que importan los archivos de frontend-runtime se resuelve con los node_modules de la fábrica
    dedupe: ['vue', 'vue-router', 'vuetify', '@mdi/font']
  },
  server: {
    fs: { allow: ['.', carpetaRuntime] }
  }
})
