<template>
  <v-container fluid class="runtime-container">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-monitor</v-icon>
          </div>
          <div>
            <h2 class="mb-1">{{ sistema?.name || 'Frontend' }}</h2>
            <span class="sb-page-subtitle text-body-2">
              /frontend/{{ slug }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2 align-center">
        <v-chip size="small" :color="statusColor" variant="tonal">
          {{ statusLabel }}
        </v-chip>
        <v-btn variant="tonal" color="primary" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
        <v-btn variant="text" color="primary" @click="refrescarIframe" :disabled="!frontendUrl">
          <v-icon left>mdi-refresh</v-icon>
          Refrescar
        </v-btn>
        <v-btn color="primary" :disabled="!frontendUrl" @click="abrirExterno">
          <v-icon left>mdi-open-in-new</v-icon>
          Abrir en nueva pestaña
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-else-if="online === false" type="warning" variant="tonal" class="mb-4">
      El frontend no está online. Podés iniciarlo desde el diseñador.
    </v-alert>

    <div class="frontend-frame">
      <iframe
        v-if="frontendUrl"
        :key="iframeKey"
        :src="frontendUrl"
        title="Frontend generado"
        frameborder="0"
      />
      <div v-else class="pa-6 text-medium-emphasis">
        Cargando frontend...
      </div>
    </div>
  </v-container>
</template>

<script setup>
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'

const route = useRoute()
const router = useRouter()

const slug = String(route.params.slug || '')
const sistema = ref(null)
const frontendUrl = ref('')
const online = ref(null)
const error = ref('')
const iframeKey = ref(0)

const baseFrontendPort = 5173

const statusLabel = computed(() => {
  if (online.value === true) return 'Online'
  if (online.value === false) return 'Offline'
  return 'Verificando...'
})

const statusColor = computed(() => {
  if (online.value === true) return 'green'
  if (online.value === false) return 'red'
  return 'orange'
})

function volver() {
  router.back()
}

function refrescarIframe() {
  iframeKey.value += 1
}

function abrirExterno() {
  if (!frontendUrl.value) return
  window.open(frontendUrl.value, '_blank')
}

async function checkOnline(systemId) {
  try {
    const { data } = await sistemaService.pingFrontend(systemId)
    online.value = !!data?.online
  } catch {
    online.value = false
  }
}

async function cargarSistema() {
  try {
    const { data } = await sistemaService.getBySlug(slug)
    sistema.value = data
    const systemId = Number(data?.id)
    if (!systemId) throw new Error('Sistema no encontrado.')
    const port = baseFrontendPort + systemId
    frontendUrl.value = `http://localhost:${port}`
    await checkOnline(systemId)
  } catch (err) {
    error.value = err?.response?.data?.message || err?.message || 'No se pudo cargar el frontend.'
  }
}

onMounted(() => {
  cargarSistema()
})
</script>

<style scoped>
.frontend-frame {
  width: 100%;
  height: calc(100vh - 220px);
  min-height: 520px;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.35);
  background: #fff;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.06);
}

.frontend-frame iframe {
  width: 100%;
  height: 100%;
  border: 0;
}
</style>
