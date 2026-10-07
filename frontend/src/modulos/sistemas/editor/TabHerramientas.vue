<template>
  <v-row class="mt-2">
    <v-col cols="12">
      <v-card elevation="2" class="card">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-tools</v-icon>
            <span class="text-h6 font-weight-medium">Herramientas</span>
          </div>
          <div class="d-flex align-center ga-2">
            <v-chip size="small" :color="backendHealthColor" variant="tonal">
              Backend: {{ backendHealthLabel }}
            </v-chip>
            <v-btn size="x-small" variant="text" color="primary" @click="checkBackendHealth">
              <v-icon left size="16">mdi-refresh</v-icon>
              Actualizar
            </v-btn>
            <v-btn
              v-if="backendHealth.status !== 'online'"
              color="blue"
              size="small"
              @click="iniciarBackend"
            >
              <v-icon left>mdi-play</v-icon>
              Iniciar backend
            </v-btn>
            <v-btn
              v-if="backendHealth.status === 'online'"
              color="red"
              size="small"
              @click="detenerBackend"
            >
              <v-icon left>mdi-stop</v-icon>
              Detener backend
            </v-btn>
            <v-btn color="orange" size="small" @click="reiniciarBackend">
              <v-icon left>mdi-restart</v-icon>
              Reiniciar backend
            </v-btn>
          </div>
        </v-card-title>

        <v-divider />

        <v-card-text>
          <v-dialog v-model="restartDialog.open" max-width="420">
            <v-card>
              <v-card-title>Backend</v-card-title>
              <v-card-text>
                <div class="mb-2">{{ restartDialog.message }}</div>
                <v-progress-linear
                  v-if="restartDialog.status === 'restarting' || restartDialog.status === 'waiting'"
                  indeterminate
                  color="primary"
                />
                <v-alert v-if="restartDialog.status === 'online'" type="success" variant="tonal" class="mt-3">
                  Backend listo.
                </v-alert>
                <v-alert v-if="restartDialog.status === 'error' || restartDialog.status === 'timeout'" type="error" variant="tonal" class="mt-3">
                  No pudimos confirmar el reinicio.
                </v-alert>
              </v-card-text>
              <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="restartDialog.open = false">Cerrar</v-btn>
              </v-card-actions>
            </v-card>
          </v-dialog>

          <v-alert type="info" variant="tonal" class="mb-4">
            Para que el reinicio funcione, ejecuta el backend con
            <span class="mono">dotnet watch run</span>.
          </v-alert>

          <v-alert type="info" variant="tonal" class="mb-4">
            <div class="text-body-2 font-weight-medium mb-1">Como probar una API</div>
            <div class="text-body-2">1. Elige una ruta en "APIs generadas".</div>
            <div class="text-body-2">2. Revisa el Path y usa el ejemplo si es POST/PUT.</div>
            <div class="text-body-2">3. Presiona Enviar y mira el response.</div>
          </v-alert>

          <div class="text-subtitle-2 mb-2">Puertos</div>
          <v-row class="mb-4">
            <v-col cols="12" md="6">
              <v-card elevation="1" class="port-card">
                <v-card-title class="d-flex align-center justify-space-between">
                  <div class="d-flex align-center">
                    <v-icon class="mr-2" color="primary">mdi-server</v-icon>
                    <span class="text-body-2 font-weight-medium">SystemBase backend</span>
                  </div>
                  <v-btn
                    size="x-small"
                    variant="text"
                    color="primary"
                    @click="copiarTexto(systembaseBaseUrl, 'URL de SystemBase')"
                  >
                    <v-icon left size="16">mdi-content-copy</v-icon>
                    Copiar
                  </v-btn>
                </v-card-title>
                <v-card-text class="pt-0">
                  <div class="mono port-value">{{ systembaseBaseUrl }}</div>
                </v-card-text>
              </v-card>
            </v-col>
            <v-col cols="12" md="6">
              <v-card elevation="1" class="port-card">
                <v-card-title class="d-flex align-center justify-space-between">
                  <div class="d-flex align-center">
                    <v-icon class="mr-2" color="primary">mdi-server-network</v-icon>
                    <span class="text-body-2 font-weight-medium">Backend del sistema</span>
                  </div>
                  <v-btn
                    size="x-small"
                    variant="text"
                    color="primary"
                    @click="copiarTexto(backendBaseUrl, 'URL del backend')"
                  >
                    <v-icon left size="16">mdi-content-copy</v-icon>
                    Copiar
                  </v-btn>
                </v-card-title>
                <v-card-text class="pt-0">
                  <div class="mono port-value">{{ backendBaseUrl }}</div>
                </v-card-text>
              </v-card>
            </v-col>
            <v-col cols="12" md="6">
              <v-card elevation="1" class="port-card">
                <v-card-title class="d-flex align-center justify-space-between">
                  <div class="d-flex align-center">
                    <v-icon class="mr-2" color="primary">mdi-file-document-outline</v-icon>
                    <span class="text-body-2 font-weight-medium">Registro de puertos</span>
                  </div>
                  <v-btn
                    size="x-small"
                    variant="text"
                    color="primary"
                    @click="copiarTexto(portsFilePath, 'Ruta de ports.json')"
                  >
                    <v-icon left size="16">mdi-content-copy</v-icon>
                    Copiar
                  </v-btn>
                </v-card-title>
                <v-card-text class="pt-0">
                  <div class="mono port-value">{{ portsFilePath }}</div>
                </v-card-text>
              </v-card>
            </v-col>
          </v-row>

          <v-card elevation="1" class="mb-4">
            <v-card-title class="d-flex align-center justify-space-between">
              <div class="d-flex align-center">
                <v-icon class="mr-2" color="primary">mdi-console-line</v-icon>
                <span class="text-subtitle-2 font-weight-medium">Consola backend</span>
              </div>
              <div class="d-flex align-center ga-3">
                <v-switch
                  v-model="backendLogs.autoScroll"
                  color="green"
                  :base-color="'grey'"
                  density="compact"
                  hide-details
                  label="Auto-scroll"
                />
                <v-btn size="x-small" variant="text" color="primary" @click="cargarBackendLogs(true)">
                  <v-icon left size="16">mdi-refresh</v-icon>
                  Actualizar
                </v-btn>
                <v-btn size="x-small" variant="text" color="primary" @click="limpiarBackendLogs">
                  Limpiar
                </v-btn>
              </div>
            </v-card-title>
            <v-divider />
            <v-card-text>
              <div ref="backendLogRef" class="backend-console mono">
                <div v-if="!backendLogs.entries.length" class="text-caption text-medium-emphasis">
                  Aún no hay logs del backend.
                </div>
                <div
                  v-for="entry in backendLogs.entries"
                  :key="entry.id"
                  :class="[
                    'backend-log-line',
                    entry.level === 'stderr' ? 'backend-log-error' : '',
                    entry.level === 'stdout' ? 'backend-log-stdout' : ''
                  ]"
                >
                  <span class="backend-log-time">{{ formatLogTime(entry.timestamp) }}</span>
                  <span class="backend-log-level">[{{ entry.level }}]</span>
                  <span class="backend-log-message">{{ entry.message }}</span>
                </div>
              </div>
            </v-card-text>
          </v-card>

          <div class="text-subtitle-2 mb-2">APIs generadas</div>
          <v-row class="mb-2">
            <v-col cols="12" md="5">
              <v-text-field
                v-model="apiRouteFilterText"
                label="Buscar por path o nombre"
                density="compact"
                prepend-inner-icon="mdi-magnify"
                clearable
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="apiRouteFilterMethod"
                :items="apiMethodOptions"
                item-title="title"
                item-value="value"
                label="Metodo"
                density="compact"
                clearable
              />
            </v-col>
            <v-col cols="12" md="3" class="d-flex align-center text-caption text-medium-emphasis">
              Mostrando {{ filteredApiRoutes.length }} de {{ apiRouteOptions.length }}
            </v-col>
          </v-row>
          <v-data-table
            :headers="headersApiRoutes"
            :items="filteredApiRoutes"
            class="table mb-4"
            density="compact"
            hover
          >
            <template #item.method="{ item }">
              <v-chip size="small" :color="apiMethodColor(item.method)" variant="tonal" class="text-uppercase">
                {{ item.method }}
              </v-chip>
            </template>
            <template #item.path="{ item }">
              <span class="mono">{{ item.path }}</span>
            </template>
            <template #item.actions="{ item }">
              <v-btn size="small" variant="text" color="primary" @click="usarApiRoute(item)">
                Usar en consola
              </v-btn>
              <v-btn size="small" variant="text" color="secondary" @click="configurarApi(item)">
                Configurar
              </v-btn>
            </template>
          </v-data-table>

          <v-card elevation="1" class="mb-4">
            <v-card-title class="d-flex align-center justify-space-between">
              <div class="d-flex align-center">
                <v-icon class="mr-2" color="primary">mdi-api</v-icon>
                <span class="text-subtitle-2 font-weight-medium">Consola API</span>
              </div>
              <v-btn color="primary" size="small" @click="enviarApiConsole">
                <v-icon left>mdi-send</v-icon>
                Enviar
              </v-btn>
            </v-card-title>
            <v-divider />
            <v-card-text>
              <div class="text-caption text-medium-emphasis mb-2">Request</div>
              <v-row>
                <v-col cols="12" md="4">
                  <v-text-field v-model="apiConsole.baseUrl" label="Base URL" density="compact" />
                </v-col>
                <v-col cols="12" md="4">
                  <v-select
                    v-model="apiRouteSeleccionada"
                    :items="apiRouteOptions"
                    item-title="title"
                    :return-object="true"
                    label="Ruta sugerida"
                    density="compact"
                  />
                </v-col>
                <v-col cols="12" md="2">
                  <v-select
                    v-model="apiConsole.method"
                    :items="['GET','POST','PUT','DELETE']"
                    label="Metodo"
                    density="compact"
                  />
                </v-col>
                <v-col cols="12" md="2">
                  <v-tooltip text="Usa el token guardado en localStorage">
                    <template #activator="{ props }">
                      <div v-bind="props">
                        <v-checkbox v-model="apiConsole.sendToken" label="Enviar token" density="compact" />
                      </div>
                    </template>
                  </v-tooltip>
                </v-col>
              </v-row>

              <v-row class="mt-2">
                <v-col cols="12">
                  <v-text-field v-model="apiConsole.path" label="Path" density="compact" />
                </v-col>
              </v-row>

              <v-row>
                <v-col cols="12">
                  <v-textarea v-model="apiConsole.body" label="Body (JSON)" rows="4" density="compact" class="api-textarea" />
                </v-col>
              </v-row>

              <v-row class="mt-2">
                <v-col cols="12" md="6">
                  <v-textarea
                    :model-value="apiExampleRequestText"
                    label="Ejemplo request"
                    rows="6"
                    density="compact"
                    readonly
                    class="api-textarea"
                  />
                  <v-btn
                    class="mt-2"
                    size="small"
                    color="primary"
                    variant="text"
                    :disabled="!apiExampleRequestText"
                    @click="usarEjemploRequest"
                  >
                    Usar ejemplo en Body
                  </v-btn>
                </v-col>
                <v-col cols="12" md="6">
                  <v-textarea
                    :model-value="apiExampleResponseText"
                    label="Ejemplo response"
                    rows="6"
                    density="compact"
                    readonly
                    class="api-textarea"
                  />
                </v-col>
              </v-row>

              <v-divider class="my-4" />

              <div class="text-caption text-medium-emphasis mb-2">Response</div>
              <v-row>
                <v-col cols="12" md="4">
                  <v-text-field v-model="apiConsole.responseStatus" label="Status" readonly density="compact" />
                </v-col>
                <v-col cols="12" md="4">
                  <v-text-field v-model="apiConsole.responseTime" label="Tiempo" readonly density="compact" />
                </v-col>
              </v-row>

              <v-row>
                <v-col cols="12" md="6">
                  <v-textarea v-model="apiConsole.responseHeaders" label="Headers" rows="6" readonly density="compact" class="api-textarea" />
                </v-col>
                <v-col cols="12" md="6">
                  <v-textarea v-model="apiConsole.responseBody" label="Response" rows="6" readonly density="compact" class="api-textarea" />
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>
        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup>
// Pestaña "Herramientas": procesos del backend generado, APIs y consola API. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  apiConsole,
  apiExampleRequestText,
  apiExampleResponseText,
  apiMethodColor,
  apiMethodOptions,
  apiRouteFilterMethod,
  apiRouteFilterText,
  apiRouteOptions,
  apiRouteSeleccionada,
  backendBaseUrl,
  backendHealth,
  backendHealthColor,
  backendHealthLabel,
  backendLogRef,
  backendLogs,
  cargarBackendLogs,
  checkBackendHealth,
  configurarApi,
  copiarTexto,
  detenerBackend,
  enviarApiConsole,
  filteredApiRoutes,
  formatLogTime,
  headersApiRoutes,
  iniciarBackend,
  limpiarBackendLogs,
  portsFilePath,
  reiniciarBackend,
  restartDialog,
  sistema,
  systembaseBaseUrl,
  usarApiRoute,
  usarEjemploRequest
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
