<template>
  <v-row class="mt-2">
    <v-col cols="12">
      <v-card elevation="2" class="card">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-monitor</v-icon>
            <span class="text-h6 font-weight-medium">Frontend</span>
          </div>
          <div class="d-flex ga-2">
            <v-btn color="primary" size="small" @click="guardarFrontendConfig">
              <v-icon left>mdi-content-save</v-icon>
              Guardar
            </v-btn>
            <v-btn color="teal" size="small" @click="generarFrontend">
              <v-icon left>mdi-code-tags</v-icon>
              Generar frontend
            </v-btn>
          </div>
        </v-card-title>

        <v-divider />

        <v-card-text>
          <v-alert type="info" variant="tonal" class="mb-4">
            Genera el frontend runtime del sistema en <span class="mono">systems/{{ sistema?.slug || 'slug' }}/frontend</span>.
            Incluye rutas runtime y elimina pantallas administrativas.
          </v-alert>

          <v-card elevation="1" class="mb-4 card">
            <v-card-title class="d-flex align-center justify-space-between">
              <div class="d-flex align-center">
                <v-icon class="mr-2" color="primary">mdi-monitor-play</v-icon>
                <span class="text-subtitle-1 font-weight-medium">Runtime frontend</span>
              </div>
              <div class="d-flex align-center ga-2 flex-wrap">
                <v-chip size="small" :color="frontendHealthColor" variant="tonal">
                  Frontend: {{ frontendHealthLabel }}
                </v-chip>
                <v-btn size="x-small" variant="text" color="primary" @click="checkFrontendHealth">
                  <v-icon left size="16">mdi-refresh</v-icon>
                  Actualizar
                </v-btn>
                <v-btn
                  v-if="frontendHealth.status !== 'online'"
                  color="blue"
                  size="small"
                  @click="iniciarFrontend"
                >
                  <v-icon left>mdi-play</v-icon>
                  Iniciar frontend
                </v-btn>
                <v-btn
                  v-if="frontendHealth.status === 'online'"
                  color="red"
                  size="small"
                  @click="detenerFrontend"
                >
                  <v-icon left>mdi-stop</v-icon>
                  Detener frontend
                </v-btn>
                <v-btn color="orange" size="small" @click="reiniciarFrontend">
                  <v-icon left>mdi-restart</v-icon>
                  Reiniciar frontend
                </v-btn>
                <v-btn
                  size="small"
                  variant="outlined"
                  color="primary"
                  :href="frontendBaseUrl"
                  target="_blank"
                >
                  <v-icon left>mdi-open-in-new</v-icon>
                  Abrir
                </v-btn>
              </div>
            </v-card-title>

            <v-divider />

            <v-card-text>
              <v-dialog v-model="frontendDialog.open" max-width="420">
                <v-card>
                  <v-card-title>Frontend</v-card-title>
                  <v-card-text>
                    <div class="mb-2">{{ frontendDialog.message }}</div>
                    <v-progress-linear
                      v-if="frontendDialog.status === 'restarting' || frontendDialog.status === 'waiting'"
                      indeterminate
                      color="primary"
                    />
                    <v-alert v-if="frontendDialog.status === 'online'" type="success" variant="tonal" class="mt-3">
                      Frontend listo.
                    </v-alert>
                    <v-alert v-if="frontendDialog.status === 'error' || frontendDialog.status === 'timeout'" type="error" variant="tonal" class="mt-3">
                      No pudimos confirmar el frontend.
                    </v-alert>
                  </v-card-text>
                  <v-card-actions>
                    <v-spacer />
                    <v-btn variant="text" @click="frontendDialog.open = false">Cerrar</v-btn>
                  </v-card-actions>
                </v-card>
              </v-dialog>

              <v-row>
                <v-col cols="12" md="6">
                  <v-card elevation="1" class="port-card">
                    <v-card-title class="d-flex align-center justify-space-between">
                      <div class="d-flex align-center">
                        <v-icon class="mr-2" color="primary">mdi-laptop</v-icon>
                        <span class="text-body-2 font-weight-medium">Frontend del sistema</span>
                      </div>
                      <v-btn
                        size="x-small"
                        variant="text"
                        color="primary"
                        @click="copiarTexto(frontendBaseUrl, 'URL del frontend')"
                      >
                        <v-icon left size="16">mdi-content-copy</v-icon>
                        Copiar
                      </v-btn>
                    </v-card-title>
                    <v-card-text class="pt-0">
                      <div class="mono port-value">{{ frontendBaseUrl }}</div>
                    </v-card-text>
                  </v-card>
                </v-col>
                <v-col cols="12" md="6" class="d-flex align-center">
                  <div class="text-body-2 text-medium-emphasis">
                    Si es la primera vez, el boton iniciar corre <span class="mono">npm install</span> antes de levantar.
                  </div>
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>

          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendConfig.system.appTitle"
                label="Titulo de la app"
                density="compact"
                hint="Se usa en el header del runtime"
                persistent-hint
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model.number="frontendConfig.system.defaultItemsPerPage"
                label="Items por pagina"
                type="number"
                density="compact"
                hint="Valor por defecto en listados"
                persistent-hint
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendItemsPerPageText"
                label="Opciones de paginacion"
                density="compact"
                hint="Ej: 10, 20, 50, 100"
                persistent-hint
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-switch
                v-model="frontendConfig.system.showSearch"
                label="Mostrar busqueda"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-switch
                v-model="frontendConfig.system.showFilters"
                label="Mostrar filtros"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
          </v-row>

          <v-divider class="my-4" />

          <v-row>
            <v-col cols="12" md="3">
              <v-text-field
                v-model="frontendConfig.system.primaryColor"
                label="Color primario"
                type="color"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model="frontendConfig.system.secondaryColor"
                label="Color secundario"
                type="color"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="frontendConfig.system.density"
                :items="frontendDensityOptions"
                label="Densidad"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="frontendConfig.system.uiMode"
                :items="frontendUiModeOptions"
                label="Modo UI"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendConfig.system.fontFamily"
                label="Tipografia (font-family)"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model="frontendConfig.system.locale"
                label="Locale"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model="frontendConfig.system.currency"
                label="Moneda"
                density="compact"
              />
            </v-col>
          </v-row>

          <v-divider class="my-4" />

          <v-row>
            <v-col cols="12" md="4">
              <v-select
                v-model="frontendConfig.system.authMode"
                :items="frontendAuthModeOptions"
                item-title="title"
                item-value="value"
                label="Auth"
                density="compact"
                hint="Local = backend del sistema. Central = SystemBase."
                persistent-hint
              />
            </v-col>
            <v-col cols="12" md="8">
              <v-text-field
                v-model="frontendConfig.system.authBaseUrl"
                label="Auth base URL"
                density="compact"
                :disabled="frontendConfig.system.authMode !== 'central'"
                hint="Solo aplica si Auth = Central. Ej: http://localhost:5032/api/v1"
                persistent-hint
              />
            </v-col>
          </v-row>

        </v-card-text>
      </v-card>
    </v-col>
  </v-row>


  <v-row class="mt-4">
    <v-col cols="12">
      <v-card elevation="2" class="card">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-format-list-bulleted</v-icon>
            <span class="text-h6 font-weight-medium">Entidades UI</span>
          </div>
        </v-card-title>

        <v-divider />

        <div class="text-caption text-medium-emphasis mb-2">Arrastra para ordenar las entidades.</div>
        <v-table density="compact" class="table frontend-entities-table">
          <thead>
            <tr>
              <th class="text-caption">Orden</th>
              <th class="text-caption">Entidad</th>
              <th class="text-caption">Menu</th>
              <th class="text-caption">Visible</th>
              <th class="text-caption">Icono</th>
              <th class="text-caption">Ruta</th>
              <th class="text-caption">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(item, index) in frontendConfig.entities"
              :key="item.entityId || item.id || index"
              class="frontend-field-row"
              :class="{ 'is-drag-over': index === frontendEntityDragOver }"
              @dragover.prevent="onFrontendEntityDragOver(index)"
              @drop.prevent="onFrontendEntityDrop(index)"
            >
              <td class="drag-cell">
                <v-icon
                  class="drag-handle"
                  size="16"
                  draggable="true"
                  @dragstart="onFrontendEntityDragStart(index)"
                  @dragend="onFrontendEntityDragEnd"
                >
                  mdi-drag
                </v-icon>
              </td>
              <td>{{ frontendEntityLabel(item) }}</td>
              <td>{{ item.menuLabel || frontendEntityLabel(item) }}</td>
              <td>
                <v-switch
                  v-model="item.showInMenu"
                  color="green"
                  :base-color="'grey'"
                  density="compact"
                  hide-details
                />
              </td>
              <td>
                <v-text-field v-model="item.menuIcon" density="compact" hide-details />
              </td>
              <td>
                <v-text-field v-model="item.routeSlug" density="compact" hide-details />
              </td>
              <td>
                <v-btn size="small" variant="text" color="primary" @click="abrirFrontendEntidad(item)">
                  Configurar
                </v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
    </v-col>
  </v-row>
</template>

<script setup>
// Pestaña "Frontend": configuración y generación del frontend del sistema. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  abrirFrontendEntidad,
  checkFrontendHealth,
  copiarTexto,
  detenerFrontend,
  entidades,
  frontendAuthModeOptions,
  frontendBaseUrl,
  frontendConfig,
  frontendDensityOptions,
  frontendDialog,
  frontendEntityDragOver,
  frontendEntityLabel,
  frontendHealth,
  frontendHealthColor,
  frontendHealthLabel,
  frontendItemsPerPageText,
  frontendUiModeOptions,
  generarFrontend,
  guardarFrontendConfig,
  iniciarFrontend,
  onFrontendEntityDragEnd,
  onFrontendEntityDragOver,
  onFrontendEntityDragStart,
  onFrontendEntityDrop,
  reiniciarFrontend,
  sistema
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
