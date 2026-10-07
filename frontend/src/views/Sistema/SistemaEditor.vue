<template>
  <v-container fluid>
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-vector-square</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Diseñador</h2>
            <div class="d-flex align-center flex-wrap ga-2">
              <span class="sb-page-subtitle text-body-2">
                {{ sistema?.name || 'Sistema' }}
              </span>
              <v-chip size="x-small" color="primary" variant="tonal">
                {{ sistema?.slug || '-' }}
              </v-chip>
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
      </v-col>
    </v-row>

    <v-tabs v-model="tab" class="mb-4 sb-tabs">
      <v-tab value="datos">
        <v-icon class="mr-2" size="18">mdi-database</v-icon>
        Datos
      </v-tab>
      <v-tab value="backend">
        <v-icon class="mr-2" size="18">mdi-cogs</v-icon>
        Backend
      </v-tab>
      <v-tab value="herramientas">
        <v-icon class="mr-2" size="18">mdi-tools</v-icon>
        Herramientas
      </v-tab>
      <v-tab value="frontend">
        <v-icon class="mr-2" size="18">mdi-monitor</v-icon>
        Frontend
      </v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="datos">
        <v-row class="data-grid">
          <v-col cols="12" md="6" class="d-flex">
            <v-card elevation="2" class="card data-card">
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon class="mr-2" color="primary">mdi-table</v-icon>
                  <span class="text-h6 font-weight-medium">Entidades</span>
                </div>
                <div class="d-flex ga-2">
                  <v-btn color="primary" size="small" @click="nuevaEntidad">
                    <v-icon left>mdi-plus</v-icon>
                    Nueva entidad
                  </v-btn>
                  <v-tooltip text="Crea/actualiza las tablas del sistema">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" color="green" size="small" @click="publicarSistema">
                        <v-icon left>mdi-rocket-launch</v-icon>
                        Publicar DB
                      </v-btn>
                    </template>
                  </v-tooltip>
                </div>
              </v-card-title>

              <v-divider />

              <v-card-text class="data-card-body">
                <v-data-table
                  :headers="headersEntidades"
                  :items="entidades"
                  :items-per-page="5"
                  :items-per-page-options="[5, 10, 25]"
                  class="table data-table"
                  density="compact"
                  hover
                >
                  <template #item.isActive="{ item }">
                    <v-chip size="small" :color="item.isActive ? 'green' : 'grey'">
                      {{ item.isActive ? 'Activo' : 'Inactivo' }}
                    </v-chip>
                  </template>

                <template #item.actions="{ item }">
                  <div class="table-actions">
                    <v-tooltip text="Seleccionar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="secondary" variant="text"
                          @click="seleccionarEntidad(item)">
                          <v-icon>mdi-database-search</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>

                    <v-tooltip text="Datos">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="teal"
                          variant="text"
                          @click="verDatos(item)"
                        >
                          <v-icon>mdi-table</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>

                    <v-tooltip text="Editar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                          @click="editarEntidad(item)">
                          <v-icon>mdi-pencil</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>

                    <v-tooltip text="Eliminar">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="red"
                          variant="text"
                          @click="eliminarEntidad(item)"
                        >
                          <v-icon>mdi-delete</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                  </div>
                </template>
                </v-data-table>
              </v-card-text>
            </v-card>
          </v-col>

          <v-col cols="12" md="6" class="d-flex">
            <v-card elevation="2" class="card data-card">
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon class="mr-2" color="primary">mdi-form-textbox</v-icon>
                  <span class="text-h6 font-weight-medium">Campos</span>
                </div>
                <v-btn color="primary" size="small" :disabled="!entidadSeleccionada" @click="nuevoCampo">
                  <v-icon left>mdi-plus</v-icon>
                  Nuevo campo
                </v-btn>
              </v-card-title>

              <v-divider />

              <v-card-text class="data-card-body">
                <div v-if="!entidadSeleccionada" class="empty-state data-empty">
                  Selecciona una entidad para ver sus campos.
                </div>

                <v-data-table
                  v-else
                  :headers="headersCampos"
                  :items="campos"
                  :items-per-page="5"
                  :items-per-page-options="[5, 10, 25]"
                  class="table data-table"
                  density="compact"
                  hover
                >
                  <template #item.required="{ item }">
                    <v-chip size="small" :color="item.required ? 'green' : 'grey'">
                      {{ item.required ? 'Si' : 'No' }}
                    </v-chip>
                  </template>

                  <template #item.isPrimaryKey="{ item }">
                    <v-chip size="small" :color="item.isPrimaryKey ? 'primary' : 'grey'">
                      {{ item.isPrimaryKey ? 'PK' : '-' }}
                    </v-chip>
                  </template>

                  <template #item.actions="{ item }">
                    <v-tooltip text="Editar">
                      <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                          @click="editarCampo(item)">
                          <v-icon>mdi-pencil</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                  </template>
                </v-data-table>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>

        <v-row class="mt-4">
          <v-col cols="12">
            <v-card elevation="2" class="card">
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon class="mr-2" color="primary">mdi-link-variant</v-icon>
                  <span class="text-h6 font-weight-medium">Relaciones</span>
                </div>
                <v-btn color="primary" size="small" @click="nuevaRelacion">
                  <v-icon left>mdi-plus</v-icon>
                  Nueva relacion
                </v-btn>
              </v-card-title>

              <v-divider />

              <v-data-table :headers="headersRelaciones" :items="relaciones" class="table" density="compact" hover>
                <template #item.sourceEntityId="{ item }">
                  {{ entidadNombre(item.sourceEntityId) }}
                </template>

                <template #item.targetEntityId="{ item }">
                  {{ entidadNombre(item.targetEntityId) }}
                </template>

                <template #item.cascadeDelete="{ item }">
                  <v-chip size="small" :color="item.cascadeDelete ? 'red' : 'grey'">
                    {{ item.cascadeDelete ? 'Si' : 'No' }}
                  </v-chip>
                </template>

                <template #item.actions="{ item }">
                  <v-tooltip text="Editar">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                        @click="editarRelacion(item)">
                        <v-icon>mdi-pencil</v-icon>
                      </v-btn>
                    </template>
                  </v-tooltip>
                </template>
              </v-data-table>
            </v-card>
          </v-col>
        </v-row>

        <v-row class="mt-4">
          <v-col cols="12">
            <v-card elevation="2" class="card">
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon class="mr-2" color="primary">mdi-console</v-icon>
                  <span class="text-h6 font-weight-medium">Consola SQL (bootstrap)</span>
                </div>
                <div class="d-flex ga-2">
                  <v-btn
                    variant="text"
                    color="secondary"
                    size="small"
                    @click="sqlConsole.script = sqlScriptTemplate"
                  >
                    Cargar plantilla
                  </v-btn>
                  <v-btn variant="text" color="secondary" size="small" @click="limpiarSqlConsole">
                    Limpiar
                  </v-btn>
                  <v-btn
                    color="deep-orange"
                    size="small"
                    :loading="sqlConsole.running"
                    @click="ejecutarSqlConsole"
                  >
                    <v-icon left>mdi-play</v-icon>
                    Ejecutar SQL
                  </v-btn>
                </div>
              </v-card-title>

              <v-divider />

              <v-card-text>
                <v-alert type="warning" variant="tonal" class="mb-4">
                  Solo disponible en <span class="mono">Development</span> y para <span class="mono">admin</span>.
                  El script debe operar sobre <span class="mono">{{ sqlTargetSchema }}</span>.
                </v-alert>

                <v-textarea
                  v-model="sqlConsole.script"
                  label="Script SQL"
                  rows="10"
                  density="compact"
                  class="api-textarea"
                  hint="Soporta multiples sentencias y separador GO."
                  persistent-hint
                />

                <v-alert
                  v-if="sqlConsole.resultMessage"
                  :type="sqlConsole.resultType"
                  variant="tonal"
                  class="mt-4"
                >
                  {{ sqlConsole.resultMessage }}
                </v-alert>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>

      </v-window-item>

      <v-window-item value="herramientas">
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
      </v-window-item>

      <v-window-item value="frontend">
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

                <v-divider class="my-4" />

                <v-row>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="frontendConfig.system.seedAdminUser"
                      label="Seed admin usuario"
                      density="compact"
                      hint="Usuario por defecto en export"
                      persistent-hint
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="frontendConfig.system.seedAdminPassword"
                      label="Seed admin contraseña"
                      type="password"
                      density="compact"
                      hint="Password por defecto en export"
                      persistent-hint
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="frontendConfig.system.seedAdminEmail"
                      label="Seed admin email"
                      density="compact"
                      hint="Email por defecto en export"
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
                  <v-icon class="mr-2" color="primary">mdi-microphone-outline</v-icon>
                  <span class="text-h6 font-weight-medium">Audio y pipeline</span>
                </div>
              </v-card-title>

              <v-divider />

              <v-card-text>
                <v-alert type="info" variant="tonal" class="mb-4">
                  Configura transcodificacion, retencion y storage para el procesamiento de audios.
                </v-alert>
                <v-row>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="backendSystemConfig.audioStorageProvider"
                      :items="backendAudioProviderOptions"
                      label="Storage provider"
                      item-title="title"
                      item-value="value"
                      hint="Local o S3/MinIO (adapter)"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-switch
                      v-model="backendSystemConfig.audioTranscodeEnabled"
                      color="green"
                      :base-color="'grey'"
                      inset
                      label="Transcodificacion"
                      hint="Convierte a formato liviano (ej: opus)"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="backendSystemConfig.audioTranscodeFormat"
                      :items="backendAudioFormatOptions"
                      label="Formato"
                      hint="Formato objetivo"
                      persistent-hint
                      density="compact"
                      :disabled="!backendSystemConfig.audioTranscodeEnabled"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="backendSystemConfig.audioTranscodeBitrate"
                      label="Bitrate"
                      hint="Ej: 32k"
                      persistent-hint
                      density="compact"
                      :disabled="!backendSystemConfig.audioTranscodeEnabled"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-switch
                      v-model="backendSystemConfig.audioTranscodeDeleteOriginal"
                      color="green"
                      :base-color="'grey'"
                      inset
                      label="Borrar original"
                      hint="Elimina el archivo original luego de transcodificar"
                      persistent-hint
                      density="compact"
                      :disabled="!backendSystemConfig.audioTranscodeEnabled"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="backendSystemConfig.audioRetentionSoftDays"
                      label="Retencion soft (dias)"
                      type="number"
                      hint="Marca como borrado logico"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="backendSystemConfig.audioRetentionPurgeDays"
                      label="Retencion purge (dias)"
                      type="number"
                      hint="Borra archivo y registro"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="backendSystemConfig.audioRetentionRunMinutes"
                      label="Frecuencia limpieza (min)"
                      type="number"
                      hint="Cada cuanto corre el worker de limpieza"
                      persistent-hint
                      density="compact"
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
      </v-window-item>

      <v-window-item value="backend">
        <v-row class="mt-2">
          <v-col cols="12">
            <v-card elevation="2" class="card">
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon class="mr-2" color="primary">mdi-cogs</v-icon>
                  <span class="text-h6 font-weight-medium">Configuracion general</span>
                </div>
                <div class="d-flex ga-2">
                  <v-btn color="primary" size="small" @click="guardarBackendConfig">
                    <v-icon left>mdi-content-save</v-icon>
                    Guardar
                  </v-btn>
                  <v-btn color="teal" size="small" @click="generarBackend">
                    <v-icon left>mdi-code-tags</v-icon>
                    Generar backend
                  </v-btn>
                </div>
              </v-card-title>

              <v-divider />

              <v-card-text>
                <v-alert type="info" variant="tonal" class="mb-4">
                  Aqui defines como se genera el backend: rutas, seguridad, persistencia y paginacion.
                </v-alert>
                <v-row>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="backendSystemConfig.apiBase"
                      label="API Base"
                      hint="Prefijo de rutas. Ej: api/v1"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="backendSystemConfig.requireAuth"
                      :items="backendSystemAuthOptions"
                      label="Auth global"
                      item-title="title"
                      item-value="value"
                      hint="Define si todas las rutas requieren token"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="backendSystemConfig.schemaPrefix"
                      label="Prefijo schema"
                      hint="Se usa para el schema SQL: sys_slug"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="backendSystemConfig.persistence"
                      :items="backendPersistenceOptions"
                      label="Persistencia"
                      hint="SQL directo o EF Core (solo SQL por ahora)"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="backendSystemConfig.defaultPageSize"
                      label="Page size default"
                      type="number"
                      hint="Cantidad por pagina cuando hay paginacion"
                      persistent-hint
                      density="compact"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="backendSystemConfig.maxPageSize"
                      label="Page size max"
                      type="number"
                      hint="Limite maximo de registros por request"
                      persistent-hint
                      density="compact"
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
                  <v-icon class="mr-2" color="primary">mdi-database</v-icon>
                  <span class="text-h6 font-weight-medium">Entidades</span>
                </div>
              </v-card-title>

              <v-divider />

              <v-card-text class="pt-4">
                <div class="text-body-2">
                  Activa o desactiva el CRUD por entidad y ajusta su configuracion.
                </div>
              </v-card-text>

              <v-data-table :headers="headersBackend" :items="backendEntities" class="table" density="compact" hover>
                <template #item.name="{ item }">
                  {{ item.displayName || item.name }}
                </template>

                <template #item.isEnabled="{ item }">
                  <v-tooltip text="Genera controller, gestor y rutas para esta entidad">
                    <template #activator="{ props }">
                      <div v-bind="props">
                        <v-switch
                          v-model="item.isEnabled"
                          color="green"
                          :base-color="'grey'"
                          density="compact"
                          hide-details
                        />
                      </div>
                    </template>
                  </v-tooltip>
                </template>

                <template #item.route="{ item }">
                  <span class="mono">{{ item.route }}</span>
                </template>

                <template #item.actions="{ item }">
                  <v-btn size="small" variant="text" color="primary" @click="abrirBackendEntidad(item)">
                    Configurar
                  </v-btn>
                </template>
              </v-data-table>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>

    <v-dialog v-model="mostrarBackendDialog" max-width="1100">
      <v-card>
        <v-card-title class="d-flex align-center justify-space-between">
          <div>
            Configurar backend -
            {{ backendEntidadActual?.displayName || backendEntidadActual?.name || '' }}
          </div>
          <v-btn icon variant="text" @click="mostrarBackendDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>

        <v-divider />

        <v-card-text v-if="backendEntidadActual">
          <v-alert v-if="endpointOnlyMode" type="info" variant="tonal" class="mb-4">
            <div class="d-flex align-center justify-space-between">
              <div>Configurando API: {{ endpointOnlyTitle }}</div>
              <v-btn size="small" variant="text" color="primary" @click="endpointOnlyMode = false; endpointOnlyKey = null; endpointPanel = []">
                Ver configuracion completa
              </v-btn>
            </div>
          </v-alert>
          <v-alert v-if="!endpointOnlyMode" type="info" variant="tonal" class="mb-4">
            Configura la ruta, seguridad, paginacion y campos expuestos para esta entidad.
          </v-alert>
          <v-row v-if="!endpointOnlyMode">
            <v-col cols="12" md="4">
              <v-text-field
                v-model="backendEntidadActual.route"
                label="Ruta base"
                hint="Segmento de URL sin espacios"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="backendEntidadActual.requireAuth"
                :items="backendAuthOptions"
                label="Auth"
                item-title="title"
                item-value="value"
                hint="Override del auth global"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-tooltip text="En vez de borrar, marca activo/inactivo">
                <template #activator="{ props }">
                  <div v-bind="props">
                    <v-switch
                      v-model="backendEntidadActual.softDelete"
                      label="Soft delete"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </div>
                </template>
              </v-tooltip>
              <div class="text-caption text-grey">En vez de borrar, marca activo/inactivo</div>
            </v-col>
            <v-col cols="12" md="6">
              <v-select
                v-model="backendEntidadActual.softDeleteFieldId"
                :items="backendEntidadActual.fields"
                item-title="name"
                item-value="fieldId"
                label="Campo soft delete"
                hint="Campo booleano/activo usado para borrar logico"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-tooltip text="Habilita take/skip en la lista">
                <template #activator="{ props }">
                  <div v-bind="props">
                    <v-switch
                      v-model="backendEntidadActual.pagination"
                      label="Paginacion"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </div>
                </template>
              </v-tooltip>
              <div class="text-caption text-grey">Habilita take/skip en la lista</div>
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="backendEntidadActual.defaultPageSize"
                label="Page size"
                type="number"
                hint="Tamanio por defecto"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="backendEntidadActual.maxPageSize"
                label="Max page size"
                type="number"
                hint="Limite de registros por request"
                persistent-hint
                density="compact"
              />
            </v-col>
          </v-row>

          <v-divider class="my-4" v-if="!endpointOnlyMode" />

          <div class="text-subtitle-2 mb-1">Endpoints</div>
          <div class="text-caption text-grey mb-2">
            Activa o desactiva las operaciones que se generan para esta entidad.
          </div>
          <v-expansion-panels v-model="endpointPanel" multiple>
            <v-expansion-panel v-if="shouldShowEndpointPanel('list')">
              <v-expansion-panel-title>Listar (GET)</v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-row>
                  <v-col cols="12" md="3">
                    <v-switch
                      v-model="backendEntidadActual.endpoints.list"
                      label="Activo"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="getEndpointConfig('list').requireAuth"
                      :items="backendAuthOptions"
                      label="Auth"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del auth global"
                      persistent-hint
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>

            <v-expansion-panel v-if="shouldShowEndpointPanel('get')">
              <v-expansion-panel-title>Obtener (GET /:id)</v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-row>
                  <v-col cols="12" md="3">
                    <v-switch
                      v-model="backendEntidadActual.endpoints.get"
                      label="Activo"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="getEndpointConfig('get').requireAuth"
                      :items="backendAuthOptions"
                      label="Auth"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del auth global"
                      persistent-hint
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>

            <v-expansion-panel v-if="shouldShowEndpointPanel('create')">
              <v-expansion-panel-title>Crear (POST)</v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-row>
                  <v-col cols="12" md="3">
                    <v-switch
                      v-model="backendEntidadActual.endpoints.create"
                      label="Activo"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="getEndpointConfig('create').requireAuth"
                      :items="backendAuthOptions"
                      label="Auth"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del auth global"
                      persistent-hint
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>

            <v-expansion-panel v-if="shouldShowEndpointPanel('update')">
              <v-expansion-panel-title>Editar (PUT)</v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-row>
                  <v-col cols="12" md="3">
                    <v-switch
                      v-model="backendEntidadActual.endpoints.update"
                      label="Activo"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="getEndpointConfig('update').requireAuth"
                      :items="backendAuthOptions"
                      label="Auth"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del auth global"
                      persistent-hint
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>

            <v-expansion-panel v-if="shouldShowEndpointPanel('delete')">
              <v-expansion-panel-title>Eliminar (DELETE)</v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-row>
                  <v-col cols="12" md="3">
                    <v-switch
                      v-model="backendEntidadActual.endpoints.delete"
                      label="Activo"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="getEndpointConfig('delete').requireAuth"
                      :items="backendAuthOptions"
                      label="Auth"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del auth global"
                      persistent-hint
                    />
                  </v-col>
                  <v-col cols="12" md="3">
                    <v-select
                      v-model="getEndpointConfig('delete').useSoftDelete"
                      :items="endpointSoftDeleteOptions"
                      label="Soft delete"
                      item-title="title"
                      item-value="value"
                      density="compact"
                      hint="Override del modo de borrado"
                      persistent-hint
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="backendEntidadActual.softDeleteFieldId"
                      :items="backendEntidadActual.fields"
                      item-title="name"
                      item-value="fieldId"
                      label="Campo soft delete"
                      density="compact"
                      hint="Campo booleano/activo"
                      persistent-hint
                    />
                  </v-col>
                </v-row>
              </v-expansion-panel-text>
            </v-expansion-panel>
          </v-expansion-panels>

          <v-divider class="my-4" v-if="!endpointOnlyMode" />

          <div class="text-subtitle-2 mb-2" v-if="!endpointOnlyMode">Filtros y orden</div>
          <v-row v-if="!endpointOnlyMode">
            <v-col cols="12" md="6">
              <v-select
                v-model="backendEntidadActual.filterFieldIds"
                :items="backendEntidadActual.fields"
                item-title="name"
                item-value="fieldId"
                label="Campos filtrables"
                multiple
                hint="Campos tipo texto usados en search"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="backendEntidadActual.defaultSortFieldId"
                :items="backendEntidadActual.fields"
                item-title="name"
                item-value="fieldId"
                label="Orden por defecto"
                hint="Campo usado para ordenar"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="backendEntidadActual.defaultSortDirection"
                :items="backendSortOptions"
                label="Direccion"
                hint="asc o desc"
                persistent-hint
                density="compact"
              />
            </v-col>
          </v-row>

          <v-divider class="my-4" v-if="!endpointOnlyMode" />

          <div class="text-subtitle-2 mb-2" v-if="!endpointOnlyMode">Campos</div>
          <div class="d-flex flex-wrap ga-2 text-caption mb-2" v-if="!endpointOnlyMode">
            <v-chip size="x-small" variant="tonal" color="primary">Expose: sale en respuesta</v-chip>
            <v-chip size="x-small" variant="tonal" color="primary">ReadOnly: no se envia</v-chip>
            <v-chip size="x-small" variant="tonal" color="primary">Required/Max/Unique: validaciones</v-chip>
            <v-chip size="x-small" variant="tonal" color="primary">Default: valor sugerido</v-chip>
            <v-chip size="x-small" variant="tonal" color="primary">Display: alias UI</v-chip>
          </div>
          <v-data-table
            v-if="!endpointOnlyMode"
            :headers="headersBackendFields"
            :items="backendEntidadActual.fields"
            class="table"
            density="compact"
            hover
          >
            <template #item.expose="{ item }">
              <v-tooltip text="Si, aparece en la respuesta">
                <template #activator="{ props }">
                  <div v-bind="props">
                    <v-switch
                      v-model="item.expose"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </div>
                </template>
              </v-tooltip>
            </template>
            <template #item.readOnly="{ item }">
              <v-tooltip text="No se envia en create/update">
                <template #activator="{ props }">
                  <div v-bind="props">
                    <v-switch
                      v-model="item.readOnly"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </div>
                </template>
              </v-tooltip>
            </template>
            <template #item.required="{ item }">
              <v-select
                v-model="item.required"
                :items="backendRequiredOptions"
                item-title="title"
                item-value="value"
                density="compact"
                hide-details
              />
            </template>
            <template #item.maxLength="{ item }">
              <v-text-field v-model.number="item.maxLength" type="number" density="compact" hide-details />
            </template>
            <template #item.unique="{ item }">
              <v-tooltip text="Valida que el valor no se repita">
                <template #activator="{ props }">
                  <div v-bind="props">
                    <v-switch
                      v-model="item.unique"
                      color="green"
                      :base-color="'grey'"
                      density="compact"
                      hide-details
                    />
                  </div>
                </template>
              </v-tooltip>
            </template>
            <template #item.defaultValue="{ item }">
              <v-text-field v-model="item.defaultValue" density="compact" hide-details />
            </template>
            <template #item.displayAs="{ item }">
              <v-text-field v-model="item.displayAs" density="compact" hide-details />
            </template>
          </v-data-table>
        </v-card-text>

        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="mostrarBackendDialog = false">Cerrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="mostrarFrontendDialog" max-width="1000">
      <v-card>
        <v-card-title class="d-flex align-center justify-space-between">
          <div>
            Configurar frontend -
            {{ frontendEntidadActual?.displayName || frontendEntidadActual?.name || '' }}
          </div>
          <v-btn icon variant="text" @click="mostrarFrontendDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>

        <v-divider />

        <v-card-text v-if="frontendEntidadActual">
          <v-row>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendEntidadActual.displayName"
                label="Titulo en vista"
                hint="Se usa en el header y listados"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendEntidadActual.menuLabel"
                label="Etiqueta en menu"
                hint="Texto mostrado en la lista de entidades"
                persistent-hint
                density="compact"
              />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.showInMenu"
                label="Mostrar en menu"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.menuIcon"
                label="Icono menu (mdi-*)"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.routeSlug"
                label="Ruta personalizada"
                hint="Ej: productos"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="frontendEntidadActual.formLayout"
                :items="frontendFormLayoutOptions"
                label="Layout del formulario"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="frontendEntidadActual.defaultSortFieldId"
                :items="frontendEntidadActual.fields"
                item-title="name"
                item-value="fieldId"
                label="Orden por defecto"
                density="compact"
                clearable
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="frontendEntidadActual.defaultSortDirection"
                :items="frontendSortOptions"
                label="Direccion"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.listStickyHeader"
                label="Header fijo"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.listShowTotals"
                label="Mostrar totales"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.enableDuplicate"
                label="Permitir duplicar"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.confirmSave"
                label="Confirmar guardado"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendEntidadActual.confirmDelete"
                label="Confirmar borrado"
                color="green"
                :base-color="'grey'"
                density="compact"
                hide-details
              />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.messages.empty"
                label="Mensaje vacio"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.messages.error"
                label="Mensaje error"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.messages.successCreate"
                label="Mensaje crear"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.messages.successUpdate"
                label="Mensaje editar"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="frontendEntidadActual.messages.successDelete"
                label="Mensaje borrar"
                density="compact"
              />
            </v-col>
          </v-row>

          <v-divider class="my-4" />

          <div class="text-subtitle-2 mb-2">Campos</div>
          <div class="text-caption text-medium-emphasis mb-2">Arrastra para ordenar los campos.</div>
          <v-table density="compact" class="table frontend-fields-table">
            <thead>
              <tr>
                <th class="text-caption">Orden</th>
                <th class="text-caption">Campo</th>
                <th class="text-caption">Label</th>
                <th class="text-caption">Listar</th>
                <th class="text-caption">Form</th>
                <th class="text-caption">Filtro</th>
                <th class="text-caption">Config</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(item, index) in frontendEntidadActual.fields"
                :key="item.fieldId || item.id || index"
                class="frontend-field-row"
                :class="{ 'is-drag-over': index === frontendFieldDragOver }"
                @dragover.prevent="onFrontendFieldDragOver(index)"
                @drop.prevent="onFrontendFieldDrop(index)"
              >
                <td class="drag-cell">
                  <v-icon
                    class="drag-handle"
                    size="16"
                    draggable="true"
                    @dragstart="onFrontendFieldDragStart(index)"
                    @dragend="onFrontendFieldDragEnd"
                  >
                    mdi-drag
                  </v-icon>
                </td>
                <td>{{ item.name || item.columnName }}</td>
                <td>
                  <v-text-field v-model="item.label" density="compact" hide-details />
                </td>
                <td>
                  <v-switch
                    v-model="item.showInList"
                    color="green"
                    :base-color="'grey'"
                    density="compact"
                    hide-details
                  />
                </td>
                <td>
                  <v-switch
                    v-model="item.showInForm"
                    color="green"
                    :base-color="'grey'"
                    density="compact"
                    hide-details
                  />
                </td>
                <td>
                  <v-switch
                    v-model="item.showInFilter"
                    color="green"
                    :base-color="'grey'"
                    density="compact"
                    hide-details
                  />
                </td>
                <td>
                  <v-btn size="x-small" variant="text" color="primary" @click="abrirFrontendField(item)">
                    Configurar
                  </v-btn>
                </td>
              </tr>
            </tbody>
          </v-table>
        </v-card-text>

        <v-card-actions class="pa-4">
          <v-spacer />
          <v-btn variant="text" @click="mostrarFrontendDialog = false">Cerrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="mostrarFrontendFieldDialog" max-width="720">
      <v-card>
        <v-card-title class="d-flex align-center justify-space-between">
          <div>
            Configurar campo -
            {{ frontendFieldActual?.label || frontendFieldActual?.name || '' }}
          </div>
          <v-btn icon variant="text" @click="mostrarFrontendFieldDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>

        <v-divider />

        <v-card-text v-if="frontendFieldActual">
          <v-row>
            <v-col cols="12" md="6">
              <v-select
                v-model="frontendFieldActual.inputType"
                :items="frontendFieldInputOptions"
                label="Tipo de input"
                density="compact"
                clearable
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-select
                v-model="frontendFieldActual.format"
                :items="frontendFieldFormatOptions"
                label="Formato en listado"
                density="compact"
                clearable
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendFieldActual.placeholder"
                label="Placeholder"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendFieldActual.helpText"
                label="Texto de ayuda"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="frontendFieldActual.section"
                label="Seccion"
                hint="Se usa para agrupar el formulario"
                persistent-hint
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="frontendFieldActual.min"
                label="Min"
                type="number"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="frontendFieldActual.max"
                label="Max"
                type="number"
                density="compact"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model="frontendFieldActual.pattern"
                label="Regex"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch
                v-model="frontendFieldActual.quickToggle"
                color="green"
                :base-color="'grey'"
                density="compact"
                label="Accion rapida"
                hide-details
              />
            </v-col>
          </v-row>
        </v-card-text>

        <v-card-actions class="pa-4">
          <v-spacer />
          <v-btn variant="text" @click="mostrarFrontendFieldDialog = false">Cerrar</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <EntidadDialog v-model="mostrarEntidadDialog" :entidad="entidadSeleccionadaEdicion" :system-id="systemId"
      @guardado="cargarEntidades" />

    <CampoDialog v-model="mostrarCampoDialog" :campo="campoSeleccionado" :system-id="systemId"
      :entity-id="entidadSeleccionada?.id" @guardado="cargarCampos" />

    <RelacionDialog v-model="mostrarRelacionDialog" :relacion="relacionSeleccionada" :entidades="entidades"
      :system-id="systemId" @guardado="cargarRelaciones" />
  </v-container>
</template>

<script setup>
import { onMounted, onBeforeUnmount, ref, computed, watch, reactive, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import frontendConfigService from '../../api/frontendConfig.service.js'
import entidadService from '../../api/entidad.service.js'
import campoService from '../../api/campo.service.js'
import relacionService from '../../api/relacion.service.js'
import backendConfigService from '../../api/backend-config.service.js'
import EntidadDialog from '../../components/sistemas/EntidadDialog.vue'
import CampoDialog from '../../components/sistemas/CampoDialog.vue'
import RelacionDialog from '../../components/sistemas/RelacionDialog.vue'
import { toKebab } from '../../utils/slug.js'
import { useMenuStore } from '../../store/menu.store.js'

const route = useRoute()
const router = useRouter()
const systemId = Number(route.params.id)
const { cargarMenuTree } = useMenuStore()

const sistema = ref(null)
const entidades = ref([])
const campos = ref([])
const entidadSeleccionada = ref(null)
const relaciones = ref([])
const backendSystemConfig = ref({
  apiBase: 'api/v1',
  requireAuth: true,
  schemaPrefix: 'sys',
  persistence: 'sql',
  defaultPageSize: 50,
  maxPageSize: 200,
  audioStorageProvider: 'local',
  audioTranscodeEnabled: false,
  audioTranscodeFormat: 'opus',
  audioTranscodeBitrate: '32k',
  audioTranscodeDeleteOriginal: true,
  audioRetentionSoftDays: 0,
  audioRetentionPurgeDays: 0,
  audioRetentionRunMinutes: 60
})
const backendEntities = ref([])
const tab = ref(localStorage.getItem('systemEditorTab') || 'datos')

const mostrarEntidadDialog = ref(false)
const entidadSeleccionadaEdicion = ref(null)

const mostrarCampoDialog = ref(false)
const campoSeleccionado = ref(null)

const mostrarRelacionDialog = ref(false)
const relacionSeleccionada = ref(null)
const mostrarBackendDialog = ref(false)
const backendEntidadActual = ref(null)
const mostrarFrontendDialog = ref(false)
const frontendEntidadActual = ref(null)
const mostrarFrontendFieldDialog = ref(false)
const frontendFieldActual = ref(null)
const frontendFieldDragIndex = ref(null)
const frontendFieldDragOver = ref(null)
const frontendEntityDragIndex = ref(null)
const frontendEntityDragOver = ref(null)
const apiConsole = ref({
  baseUrl: '',
  method: 'GET',
  path: '',
  body: '',
  sendToken: true,
  responseStatus: '',
  responseTime: '',
  responseBody: '',
  responseHeaders: ''
})
const sqlConsole = ref({
  script: '',
  running: false,
  resultType: 'info',
  resultMessage: ''
})
const apiRouteSeleccionada = ref(null)
const apiRouteFilterText = ref('')
const apiRouteFilterMethod = ref('')
const endpointPanel = ref([])
const endpointOnlyMode = ref(false)
const endpointOnlyKey = ref(null)
const restartDialog = reactive({
  open: false,
  status: 'idle',
  message: ''
})
const frontendDialog = reactive({
  open: false,
  status: 'idle',
  message: ''
})

const frontendConfig = ref({
  system: {
    appTitle: 'SystemBase',
    showSearch: true,
    showFilters: true,
    defaultItemsPerPage: 10,
    itemsPerPageOptions: [10, 20, 50, 100],
    primaryColor: '#2563eb',
    secondaryColor: '#0ea5e9',
    density: 'comfortable',
    fontFamily: 'Inter, system-ui, -apple-system, Segoe UI, sans-serif',
    uiMode: 'enterprise',
    locale: 'es-AR',
    currency: 'ARS',
    authMode: 'local',
    authBaseUrl: 'http://localhost:5032/api/v1',
    seedAdminUser: 'admin',
    seedAdminPassword: 'admin',
    seedAdminEmail: 'admin@local'
  },
  entities: []
})

const backendHealth = reactive({
  status: 'unknown',
  lastChecked: null
})
const frontendHealth = reactive({
  status: 'unknown',
  lastChecked: null
})
const healthIntervalId = ref(null)
const backendLogs = reactive({
  entries: [],
  lastId: 0,
  status: 'idle',
  autoScroll: true
})
const backendLogRef = ref(null)

const headersEntidades = [
  { title: 'Nombre', key: 'name' },
  { title: 'TableName', key: 'tableName' },
  { title: 'Activo', key: 'isActive' },
  { title: 'Acciones', key: 'actions', sortable: false, width: 180 }
]

const headersCampos = [
  { title: 'Nombre', key: 'name' },
  { title: 'ColumnName', key: 'columnName' },
  { title: 'DataType', key: 'dataType' },
  { title: 'Required', key: 'required' },
  { title: 'PK', key: 'isPrimaryKey' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const headersRelaciones = [
  { title: 'Origen', key: 'sourceEntityId' },
  { title: 'FK', key: 'foreignKey' },
  { title: 'Destino', key: 'targetEntityId' },
  { title: 'Tipo', key: 'relationType' },
  { title: 'Cascade', key: 'cascadeDelete' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const headersBackend = [
  { title: 'Entidad', key: 'name' },
  { title: 'Ruta', key: 'route' },
  { title: 'Generar CRUD', key: 'isEnabled', sortable: false },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const headersBackendFields = [
  { title: 'Campo', key: 'name' },
  { title: 'Expose', key: 'expose', sortable: false },
  { title: 'ReadOnly', key: 'readOnly', sortable: false },
  { title: 'Required', key: 'required', sortable: false },
  { title: 'Max', key: 'maxLength', sortable: false },
  { title: 'Unique', key: 'unique', sortable: false },
  { title: 'Default', key: 'defaultValue', sortable: false },
  { title: 'Display', key: 'displayAs', sortable: false }
]

const headersApiRoutes = [
  { title: 'Metodo', key: 'method' },
  { title: 'Path', key: 'path' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const backendAuthOptions = [
  { title: 'Heredar', value: null },
  { title: 'Requerir', value: true },
  { title: 'No requerir', value: false }
]

const backendSystemAuthOptions = [
  { title: 'Requerir', value: true },
  { title: 'No requerir', value: false }
]

const backendPersistenceOptions = [
  'sql',
  'efcore'
]

const backendAudioProviderOptions = [
  { title: 'Local', value: 'local' }
]

const backendAudioFormatOptions = [
  'opus',
  'mp3',
  'aac',
  'wav'
]

const backendRequiredOptions = [
  { title: 'Heredar', value: null },
  { title: 'Si', value: true },
  { title: 'No', value: false }
]

const endpointSoftDeleteOptions = [
  { title: 'Heredar', value: null },
  { title: 'Soft delete', value: true },
  { title: 'Hard delete', value: false }
]

const backendSortOptions = [
  'asc',
  'desc'
]

const frontendDensityOptions = [
  'comfortable',
  'compact',
  'default'
]

const frontendUiModeOptions = [
  'enterprise',
  'minimal'
]

const frontendAuthModeOptions = [
  { title: 'Local (backend del sistema)', value: 'local' },
  { title: 'Central (SystemBase)', value: 'central' }
]

const frontendFieldInputOptions = [
  'auto',
  'text',
  'textarea',
  'number',
  'date',
  'datetime',
  'select',
  'checkbox',
  'switch'
]

const frontendFieldFormatOptions = [
  'text',
  'uppercase',
  'lowercase',
  'date',
  'datetime',
  'money',
  'badge',
  'boolean'
]

const frontendFormLayoutOptions = [
  'single',
  'sections',
  'tabs'
]

const frontendSortOptions = [
  'asc',
  'desc'
]

const baseBackendPort = 5032
const backendPort = computed(() => baseBackendPort + (Number.isFinite(systemId) ? systemId : 0))
const backendBaseUrl = computed(() => `http://localhost:${backendPort.value}`)
const systembaseBaseUrl = computed(() => `http://localhost:${baseBackendPort}`)
const portsFilePath = computed(() => 'systems/ports.json')
const baseFrontendPort = 5173
const frontendPort = computed(() => baseFrontendPort + (Number.isFinite(systemId) ? systemId : 0))
const frontendBaseUrl = computed(() => `http://localhost:${frontendPort.value}`)
const sqlTargetSchema = computed(() => {
  const slug = (sistema.value?.slug || '').trim().toLowerCase()
  return slug ? `sys_${slug}` : 'sys_<slug>'
})
const sqlScriptTemplate = computed(() => {
  const schema = sqlTargetSchema.value
  return `IF OBJECT_ID('[${schema}].[Recurso]', 'U') IS NULL
BEGIN
    CREATE TABLE [${schema}].[Recurso] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Codigo] NVARCHAR(80) NOT NULL,
        [Descripcion] NVARCHAR(200) NULL,
        [Activo] BIT NOT NULL CONSTRAINT [DF_${schema}_Recurso_Activo] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_${schema}_Recurso_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_${schema}_Recurso] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_${schema}_Recurso_Codigo'
      AND object_id = OBJECT_ID('[${schema}].[Recurso]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_${schema}_Recurso_Codigo]
    ON [${schema}].[Recurso] ([Codigo]);
END`
})

const backendHealthLabel = computed(() => {
  if (backendHealth.status === 'online') return 'Online'
  if (backendHealth.status === 'offline') return 'Offline'
  if (backendHealth.status === 'checking') return 'Verificando...'
  return 'Sin estado'
})

const backendHealthColor = computed(() => {
  if (backendHealth.status === 'online') return 'green'
  if (backendHealth.status === 'offline') return 'red'
  if (backendHealth.status === 'checking') return 'orange'
  return 'grey'
})

const frontendHealthLabel = computed(() => {
  if (frontendHealth.status === 'online') return 'Online'
  if (frontendHealth.status === 'offline') return 'Offline'
  if (frontendHealth.status === 'checking') return 'Verificando...'
  return 'Sin estado'
})

const frontendHealthColor = computed(() => {
  if (frontendHealth.status === 'online') return 'green'
  if (frontendHealth.status === 'offline') return 'red'
  if (frontendHealth.status === 'checking') return 'orange'
  return 'grey'
})

const apiRouteOptions = computed(() => {
  const base = backendSystemConfig.value.apiBase || 'api/v1'
  const basePath = `/${String(base).replace(/^\/+|\/+$/g, '')}`
  const routes = []

  backendEntities.value.forEach(entity => {
    const route = entity.route || toKebab(entity.name || '')
    const entityId = entity.entityId ?? entity.id
    const fullBase = `${basePath}/${route}`

    if (entity.endpoints?.list !== false) {
      routes.push({ title: `GET ${fullBase}`, method: 'GET', path: fullBase, entityId })
    }
    if (entity.endpoints?.get !== false) {
      routes.push({ title: `GET ${fullBase}/{id}`, method: 'GET', path: `${fullBase}/:id`, entityId })
    }
    if (entity.endpoints?.create !== false) {
      routes.push({ title: `POST ${fullBase}`, method: 'POST', path: fullBase, entityId })
    }
    if (entity.endpoints?.update !== false) {
      routes.push({ title: `PUT ${fullBase}/{id}`, method: 'PUT', path: `${fullBase}/:id`, entityId })
    }
    if (entity.endpoints?.delete !== false) {
      routes.push({ title: `DELETE ${fullBase}/{id}`, method: 'DELETE', path: `${fullBase}/:id`, entityId })
    }
  })

  return routes
})

const filteredApiRoutes = computed(() => {
  const text = apiRouteFilterText.value.trim().toLowerCase()
  const methodFilter = apiRouteFilterMethod.value
  return apiRouteOptions.value.filter(item => {
    if (methodFilter && methodFilter !== item.method) return false
    if (!text) return true
    return (
      item.path.toLowerCase().includes(text) ||
      item.title.toLowerCase().includes(text)
    )
  })
})

const apiMethodOptions = [
  { title: 'Todos', value: '' },
  { title: 'GET', value: 'GET' },
  { title: 'POST', value: 'POST' },
  { title: 'PUT', value: 'PUT' },
  { title: 'DELETE', value: 'DELETE' }
]

const frontendItemsPerPageText = computed({
  get() {
    const options = frontendConfig.value?.system?.itemsPerPageOptions || []
    return options.join(', ')
  },
  set(value) {
    const numbers = String(value)
      .split(',')
      .map(v => parseInt(v.trim(), 10))
      .filter(n => Number.isFinite(n) && n > 0)
    if (numbers.length) {
      frontendConfig.value.system.itemsPerPageOptions = numbers
    }
  }
})

function apiMethodColor(method) {
  if (method === 'GET') return 'green'
  if (method === 'POST') return 'blue'
  if (method === 'PUT') return 'orange'
  if (method === 'DELETE') return 'red'
  return 'grey'
}

const endpointConfigKeyMap = {
  list: 'listConfig',
  get: 'getConfig',
  create: 'createConfig',
  update: 'updateConfig',
  delete: 'deleteConfig'
}

const endpointTitleMap = {
  list: 'Listar (GET)',
  get: 'Obtener (GET /:id)',
  create: 'Crear (POST)',
  update: 'Editar (PUT)',
  delete: 'Eliminar (DELETE)'
}

function getEndpointConfig(key) {
  const entity = backendEntidadActual.value
  if (!entity) return {}
  if (!entity.endpoints) entity.endpoints = {}
  const prop = endpointConfigKeyMap[key]
  if (!prop) return {}
  if (!entity.endpoints[prop]) {
    entity.endpoints[prop] = {
      requireAuth: null,
      useSoftDelete: null
    }
  }
  return entity.endpoints[prop]
}

function endpointKeyFromRoute(item) {
  if (!item?.method) return null
  const method = item.method.toUpperCase()
  if (method === 'GET') return item.path.includes(':id') ? 'get' : 'list'
  if (method === 'POST') return 'create'
  if (method === 'PUT') return 'update'
  if (method === 'DELETE') return 'delete'
  return null
}

const endpointOnlyTitle = computed(() => {
  if (!endpointOnlyKey.value) return ''
  return endpointTitleMap[endpointOnlyKey.value] || ''
})

function shouldShowEndpointPanel(key) {
  return !endpointOnlyMode.value || endpointOnlyKey.value === key
}

function backendBasePath() {
  const base = backendSystemConfig.value.apiBase || 'api/v1'
  return `/${String(base).replace(/^\/+|\/+$/g, '')}`
}

const apiSelectedEntity = computed(() => {
  const entityId = apiRouteSeleccionada.value?.entityId
  if (!entityId) return null
  return backendEntities.value.find(entity => (entity.entityId ?? entity.id) === entityId) || null
})

function toPascalCase(value) {
  if (!value) return ''
  return String(value)
    .replace(/[_\-\s]+(.)?/g, (_, chr) => (chr ? chr.toUpperCase() : ''))
    .replace(/^(.)/, chr => chr.toUpperCase())
}

function fieldKey(field) {
  return toPascalCase(field.columnName || field.name || '')
}

function sampleValueForField(field) {
  const type = String(field.dataType || '').toLowerCase()
  if (type.includes('uniqueidentifier') || type.includes('uuid')) return '00000000-0000-0000-0000-000000000000'
  if (type.includes('int')) return 1
  if (type.includes('decimal') || type.includes('numeric') || type.includes('money')) return 10.5
  if (type.includes('float') || type.includes('real')) return 10.5
  if (type.includes('bit') || type.includes('bool')) return true
  if (type.includes('date') || type.includes('time')) return '2024-01-01T00:00:00Z'
  if (type.includes('char') || type.includes('text')) return 'string'
  return 'valor'
}

function buildExampleObject(fields) {
  const result = {}
  fields.forEach(field => {
    const key = fieldKey(field)
    if (!key) return
    result[key] = sampleValueForField(field)
  })
  return result
}

function getCreateFields(entity) {
  return (entity.fields || []).filter(field => field.expose && !field.readOnly && !field.isIdentity)
}

function getUpdateFields(entity) {
  return (entity.fields || []).filter(
    field => field.expose && !field.readOnly && !field.isPrimaryKey && !field.isIdentity
  )
}

function getResponseFields(entity) {
  return (entity.fields || []).filter(field => field.expose)
}

const apiExampleRequestText = computed(() => {
  const route = apiRouteSeleccionada.value
  const entity = apiSelectedEntity.value
  if (!route || !entity) return ''
  if (route.method === 'POST') return JSON.stringify(buildExampleObject(getCreateFields(entity)), null, 2)
  if (route.method === 'PUT') return JSON.stringify(buildExampleObject(getUpdateFields(entity)), null, 2)
  return ''
})

const apiExampleResponseText = computed(() => {
  const route = apiRouteSeleccionada.value
  const entity = apiSelectedEntity.value
  if (!route || !entity) return ''
  const payload = buildExampleObject(getResponseFields(entity))
  if (route.method === 'GET' && route.path.includes(':id')) return JSON.stringify(payload, null, 2)
  if (route.method === 'GET') return JSON.stringify([payload], null, 2)
  if (route.method === 'POST' || route.method === 'PUT') return JSON.stringify(payload, null, 2)
  return ''
})

function usarEjemploRequest() {
  if (!apiExampleRequestText.value) return
  apiConsole.value.body = apiExampleRequestText.value
}

async function cargarSistema() {
  const { data } = await sistemaService.getById(systemId)
  sistema.value = data
}

async function cargarEntidades() {
  const { data } = await entidadService.getBySystem(systemId)
  entidades.value = data
  if (entidadSeleccionada.value) {
    const match = data.find(e => e.id === entidadSeleccionada.value.id)
    entidadSeleccionada.value = match ?? null
    if (entidadSeleccionada.value) await cargarCampos()
  }
}

async function cargarCampos() {
  if (!entidadSeleccionada.value) {
    campos.value = []
    return
  }

  const { data } = await campoService.getByEntity(systemId, entidadSeleccionada.value.id)
  campos.value = data
}

async function cargarRelaciones() {
  const { data } = await relacionService.getBySystem(systemId)
  relaciones.value = data
}

async function cargarBackendConfig() {
  const { data } = await backendConfigService.getBySystem(systemId)
  backendSystemConfig.value = data?.system || backendSystemConfig.value
  ensureBackendSystemConfig(backendSystemConfig.value)
  backendEntities.value = data?.entities || []
}

function seleccionarEntidad(item) {
  entidadSeleccionada.value = item
  cargarCampos()
}

function nuevaEntidad() {
  entidadSeleccionadaEdicion.value = null
  mostrarEntidadDialog.value = true
}

function editarEntidad(item) {
  entidadSeleccionadaEdicion.value = item
  mostrarEntidadDialog.value = true
}

async function eliminarEntidad(item) {
  const entityLabel = item?.displayName || item?.name || item?.tableName || `#${item?.id}`
  const ok = window.confirm(`Eliminar entidad "${entityLabel}"?`)
  if (!ok) return

  const tableLabel = item?.tableName || item?.name || 'tabla_runtime'
  const dropTable = window.confirm(
    `Tambien eliminar la tabla fisica [${sqlTargetSchema.value}].[${tableLabel}]?\n\nAceptar = si, Cancelar = no`
  )

  try {
    const { data } = await entidadService.eliminar(systemId, item.id, dropTable)
    const message = data?.message || data?.Message || 'Entidad eliminada.'
    window.alert(message)
    if (entidadSeleccionada.value?.id === item.id) {
      entidadSeleccionada.value = null
      campos.value = []
    }
    await cargarEntidades()
    await cargarRelaciones()
  } catch (error) {
    const raw = error?.response?.data
    const rawText =
      typeof raw === 'string'
        ? raw
        : raw && typeof raw === 'object'
          ? (raw.message || raw.Message || '')
          : ''
    const message =
      rawText ||
      (error?.response?.status ? `HTTP ${error.response.status}` : '') ||
      'Error al eliminar entidad.'
    window.alert(message)
  }
}

function verDatos(item) {
  if (!sistema.value?.slug) {
    window.alert('Sistema sin slug.')
    return
  }
  router.push(`/s/${sistema.value.slug}/${toKebab(item.name)}`)
}

function nuevoCampo() {
  campoSeleccionado.value = null
  mostrarCampoDialog.value = true
}

function editarCampo(item) {
  campoSeleccionado.value = item
  mostrarCampoDialog.value = true
}

function nuevaRelacion() {
  relacionSeleccionada.value = null
  mostrarRelacionDialog.value = true
}

function editarRelacion(item) {
  relacionSeleccionada.value = item
  mostrarRelacionDialog.value = true
}

function volver() {
  router.push('/sistemas')
}

async function publicarSistema() {
  const ok = window.confirm('Publicar sistema?')
  if (!ok) return

  try {
    await sistemaService.publicar(systemId)
    await cargarSistema()
    await cargarMenuTree()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al publicar el sistema.'
    window.alert(message)
  }
}

function limpiarSqlConsole() {
  sqlConsole.value.script = ''
  sqlConsole.value.resultMessage = ''
  sqlConsole.value.resultType = 'info'
}

async function ejecutarSqlConsole() {
  const script = sqlConsole.value.script?.trim()
  if (!script) {
    sqlConsole.value.resultType = 'warning'
    sqlConsole.value.resultMessage = 'Pega un script SQL antes de ejecutar.'
    return
  }

  const ok = window.confirm(
    `Ejecutar script SQL en ${sqlTargetSchema.value}?\n\nSolo usar para bootstrap controlado.`
  )
  if (!ok) return

  sqlConsole.value.running = true
  sqlConsole.value.resultMessage = ''

  try {
    const { data } = await sistemaService.ejecutarSql(systemId, script)
    const message = data?.message || data?.Message || 'Script SQL ejecutado.'
    sqlConsole.value.resultType = 'success'
    sqlConsole.value.resultMessage = message
    await cargarEntidades()
    await cargarRelaciones()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error ejecutando script SQL.'
    sqlConsole.value.resultType = 'error'
    sqlConsole.value.resultMessage = message
  } finally {
    sqlConsole.value.running = false
  }
}

async function guardarBackendConfig() {
  try {
    const payload = {
      system: backendSystemConfig.value,
      entities: backendEntities.value
    }
    await backendConfigService.guardar(systemId, payload)
    window.alert('Configuracion de backend guardada.')
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al guardar configuracion de backend.'
    window.alert(message)
  }
}

function abrirBackendEntidad(item) {
  backendEntidadActual.value = item
  mostrarBackendDialog.value = true
  endpointPanel.value = []
  endpointOnlyMode.value = false
  endpointOnlyKey.value = null
}

function usarApiRoute(item) {
  apiRouteSeleccionada.value = item
}

function configurarApi(item) {
  const entityId = item?.entityId
  if (!entityId) return
  const entity = backendEntities.value.find(e => (e.entityId ?? e.id) === entityId)
  if (!entity) return

  backendEntidadActual.value = entity
  mostrarBackendDialog.value = true

  const key = endpointKeyFromRoute(item)
  if (key) {
    endpointOnlyMode.value = true
    endpointOnlyKey.value = key
    const indexMap = { list: 0, get: 1, create: 2, update: 3, delete: 4 }
    const index = indexMap[key]
    if (index !== undefined) endpointPanel.value = [index]
  } else {
    endpointOnlyMode.value = false
    endpointOnlyKey.value = null
  }
}

async function copiarTexto(value, label = 'Texto') {
  if (!value) return
  try {
    await navigator.clipboard.writeText(value)
    window.alert(`${label} copiado.`)
  } catch {
    window.prompt('Copia esto:', value)
  }
}

watch(
  () => apiConsole.value.baseUrl,
  value => {
    if (value) localStorage.setItem('backendConsoleBaseUrl', value)
  }
)

watch(apiRouteSeleccionada, value => {
  if (!value) return
  apiConsole.value.method = value.method
  apiConsole.value.path = value.path
  apiConsole.value.body = ''
  apiConsole.value.responseStatus = ''
  apiConsole.value.responseTime = ''
  apiConsole.value.responseBody = ''
  apiConsole.value.responseHeaders = ''
})

watch(
  tab,
  value => {
    if (value) localStorage.setItem('systemEditorTab', value)
    if (value === 'herramientas') {
      startHealthPolling()
      startLogsPolling()
    } else {
      stopHealthPolling()
      stopLogsPolling()
    }
  }
)

async function enviarApiConsole() {
  apiConsole.value.responseStatus = ''
  apiConsole.value.responseTime = ''
  apiConsole.value.responseBody = ''
  apiConsole.value.responseHeaders = ''

  const baseUrl = apiConsole.value.baseUrl.replace(/\/+$/, '')
  const path = apiConsole.value.path.startsWith('/') ? apiConsole.value.path : `/${apiConsole.value.path}`
  const url = `${baseUrl}${path}`.replace('/:id', '/1')
  const method = apiConsole.value.method.toUpperCase()

  const headers = {
    'Content-Type': 'application/json'
  }

  if (apiConsole.value.sendToken) {
    const token = localStorage.getItem('token')
    if (token) headers.Authorization = `Bearer ${token}`
  }

  let body = undefined
  if (method !== 'GET' && method !== 'DELETE') {
    if (apiConsole.value.body) {
      try {
        body = JSON.stringify(JSON.parse(apiConsole.value.body))
      } catch {
        body = apiConsole.value.body
      }
    }
  }

  const start = performance.now()
  try {
    const response = await fetch(url, { method, headers, body })
    const time = Math.round(performance.now() - start)
    apiConsole.value.responseStatus = `${response.status} ${response.statusText}`
    apiConsole.value.responseTime = `${time} ms`
    apiConsole.value.responseHeaders = [...response.headers.entries()]
      .map(([k, v]) => `${k}: ${v}`)
      .join('\n')

    const text = await response.text()
    try {
      apiConsole.value.responseBody = JSON.stringify(JSON.parse(text), null, 2)
    } catch {
      apiConsole.value.responseBody = text
    }
  } catch (error) {
    apiConsole.value.responseStatus = 'Error'
    apiConsole.value.responseBody = error?.message || 'Error al llamar la API.'
  }
}

async function reiniciarBackend() {
  const ok = window.confirm('Reiniciar backend? (requiere dotnet watch en modo dev)')
  if (!ok) return

  try {
    restartDialog.open = true
    restartDialog.status = 'restarting'
    restartDialog.message = 'Reiniciando backend...'

    const token = localStorage.getItem('token')
    const basePath = backendBasePath()
    const url = `${backendBaseUrl.value}${basePath}/dev/restart`
    const headers = token ? { Authorization: `Bearer ${token}` } : undefined
    const response = await fetch(url, { method: 'POST', headers })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || 'Error al reiniciar backend.')
    }

    restartDialog.status = 'waiting'
    restartDialog.message = 'Esperando que el backend vuelva...'

    const online = await esperarBackendOnline()
    if (online) {
      restartDialog.status = 'online'
      restartDialog.message = 'Backend reiniciado.'
      backendHealth.status = 'online'
      await cargarBackendLogs()
      setTimeout(() => {
        restartDialog.open = false
      }, 1200)
    } else {
      restartDialog.status = 'timeout'
      restartDialog.message = 'Timeout esperando el backend.'
    }
  } catch (error) {
    restartDialog.status = 'error'
    restartDialog.message = error?.message || 'Error al reiniciar backend.'
  }
}

async function iniciarBackend() {
  const ok = window.confirm('Iniciar backend? (modo dev)')
  if (!ok) return

  try {
    restartDialog.open = true
    restartDialog.status = 'restarting'
    restartDialog.message = 'Iniciando backend...'

    await cargarBackendLogs(true)
    await sistemaService.iniciarBackend(systemId)

    restartDialog.status = 'waiting'
    restartDialog.message = 'Esperando que el backend este online...'

    const online = await esperarBackendOnline()
    if (online) {
      restartDialog.status = 'online'
      restartDialog.message = 'Backend iniciado.'
      backendHealth.status = 'online'
      await cargarBackendLogs(true)
      setTimeout(() => {
        restartDialog.open = false
      }, 1200)
    } else {
      restartDialog.status = 'timeout'
      restartDialog.message = 'Timeout esperando el backend.'
    }
  } catch (error) {
    restartDialog.status = 'error'
    restartDialog.message = error?.response?.data?.message || error?.message || 'Error al iniciar backend.'
  }
}

async function detenerBackend() {
  const ok = window.confirm('Detener backend?')
  if (!ok) return

  try {
    restartDialog.open = true
    restartDialog.status = 'restarting'
    restartDialog.message = 'Deteniendo backend...'

    await sistemaService.detenerBackend(systemId)

    backendHealth.status = 'offline'
    restartDialog.status = 'online'
    restartDialog.message = 'Backend detenido.'
    setTimeout(() => {
      restartDialog.open = false
    }, 1200)
  } catch (error) {
    restartDialog.status = 'error'
    restartDialog.message = error?.response?.data?.message || error?.message || 'Error al detener backend.'
  }
}

async function esperarBackendOnline() {
  const maxAttempts = 20
  for (let i = 0; i < maxAttempts; i += 1) {
    try {
      const { data } = await sistemaService.pingBackend(systemId)
      if (data?.online) return true
    } catch {
      // ignore
    }
    await new Promise(resolve => setTimeout(resolve, 800))
  }

  return false
}

async function iniciarFrontend() {
  const ok = window.confirm('Iniciar frontend? (modo dev)')
  if (!ok) return

  try {
    frontendDialog.open = true
    frontendDialog.status = 'restarting'
    frontendDialog.message = 'Iniciando frontend...'

    await sistemaService.iniciarFrontend(systemId)

    frontendDialog.status = 'waiting'
    frontendDialog.message = 'Esperando que el frontend este online...'

    const online = await esperarFrontendOnline()
    if (online) {
      frontendDialog.status = 'online'
      frontendDialog.message = 'Frontend iniciado.'
      frontendHealth.status = 'online'
      setTimeout(() => {
        frontendDialog.open = false
      }, 1200)
    } else {
      frontendDialog.status = 'timeout'
      frontendDialog.message = 'Timeout esperando el frontend.'
    }
  } catch (error) {
    frontendDialog.status = 'error'
    frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al iniciar frontend.'
  }
}

async function detenerFrontend() {
  const ok = window.confirm('Detener frontend?')
  if (!ok) return

  try {
    frontendDialog.open = true
    frontendDialog.status = 'restarting'
    frontendDialog.message = 'Deteniendo frontend...'

    await sistemaService.detenerFrontend(systemId)

    frontendHealth.status = 'offline'
    frontendDialog.status = 'online'
    frontendDialog.message = 'Frontend detenido.'
    setTimeout(() => {
      frontendDialog.open = false
    }, 1200)
  } catch (error) {
    frontendDialog.status = 'error'
    frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al detener frontend.'
  }
}

async function reiniciarFrontend() {
  const ok = window.confirm('Reiniciar frontend?')
  if (!ok) return

  try {
    frontendDialog.open = true
    frontendDialog.status = 'restarting'
    frontendDialog.message = 'Reiniciando frontend...'

    await sistemaService.detenerFrontend(systemId)
    await sistemaService.iniciarFrontend(systemId)

    frontendDialog.status = 'waiting'
    frontendDialog.message = 'Esperando que el frontend vuelva...'

    const online = await esperarFrontendOnline()
    if (online) {
      frontendDialog.status = 'online'
      frontendDialog.message = 'Frontend reiniciado.'
      frontendHealth.status = 'online'
      setTimeout(() => {
        frontendDialog.open = false
      }, 1200)
    } else {
      frontendDialog.status = 'timeout'
      frontendDialog.message = 'Timeout esperando el frontend.'
    }
  } catch (error) {
    frontendDialog.status = 'error'
    frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al reiniciar frontend.'
  }
}

async function esperarFrontendOnline() {
  const maxAttempts = 20
  for (let i = 0; i < maxAttempts; i += 1) {
    try {
      const { data } = await sistemaService.pingFrontend(systemId)
      if (data?.online) return true
    } catch {
      // ignore
    }
    await new Promise(resolve => setTimeout(resolve, 800))
  }

  return false
}

async function checkFrontendHealth() {
  frontendHealth.status = 'checking'

  try {
    const { data } = await sistemaService.pingFrontend(systemId)
    frontendHealth.status = data?.online ? 'online' : 'offline'
  } catch {
    frontendHealth.status = 'offline'
  } finally {
    frontendHealth.lastChecked = new Date().toISOString()
  }
}

async function checkBackendHealth() {
  backendHealth.status = 'checking'

  try {
    const { data } = await sistemaService.pingBackend(systemId)
    backendHealth.status = data?.online ? 'online' : 'offline'
  } catch {
    backendHealth.status = 'offline'
  } finally {
    backendHealth.lastChecked = new Date().toISOString()
  }
}

function startHealthPolling() {
  if (healthIntervalId.value) return
  checkBackendHealth()
}

function stopHealthPolling() {
  if (!healthIntervalId.value) return
  clearInterval(healthIntervalId.value)
  healthIntervalId.value = null
}

function formatLogTime(value) {
  if (!value) return ''
  try {
    return new Date(value).toLocaleTimeString()
  } catch {
    return ''
  }
}

function scrollBackendLogs() {
  if (!backendLogs.autoScroll) return
  nextTick(() => {
    const el = backendLogRef.value
    if (!el) return
    el.scrollTop = el.scrollHeight
  })
}

function limpiarBackendLogs() {
  backendLogs.entries.splice(0, backendLogs.entries.length)
}

async function cargarBackendLogs(forceReset = false) {
  if (forceReset) {
    backendLogs.entries.splice(0, backendLogs.entries.length)
    backendLogs.lastId = 0
  }
  try {
    const { data } = await sistemaService.logsBackend(systemId, backendLogs.lastId, 200)
    const items = data?.items || data?.Items || []
    if (items.length) {
      backendLogs.entries.push(...items)
      const lastItem = items[items.length - 1]
      backendLogs.lastId = lastItem?.id ?? lastItem?.Id ?? data?.lastId ?? backendLogs.lastId
      if (backendLogs.entries.length > 500) {
        backendLogs.entries.splice(0, backendLogs.entries.length - 500)
      }
      scrollBackendLogs()
    } else if (data?.lastId) {
      backendLogs.lastId = data.lastId
    }
    backendLogs.status = 'ok'
  } catch {
    backendLogs.status = 'error'
  }
}

function startLogsPolling() {
  cargarBackendLogs(true)
}

function stopLogsPolling() {
  // no-op (polling disabled)
}

async function cargarFrontendConfig() {
  if (!systemId) return
  try {
    const { data } = await frontendConfigService.getBySystem(systemId)
    frontendConfig.value = data
    ensureFrontendSystemConfig(frontendConfig.value?.system)
    if (frontendConfig.value?.entities?.length) {
      frontendConfig.value.entities.forEach(entity => ensureFrontendEntityConfig(entity))
    }
  } catch {
    // keep defaults
  }
}

async function guardarFrontendConfig() {
  if (!systemId) return
  try {
    await frontendConfigService.guardar(systemId, frontendConfig.value)
    window.alert('Configuracion de frontend guardada.')
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al guardar configuracion de frontend.'
    window.alert(message)
  }
}

function frontendEntityLabel(entity) {
  return entity?.displayName || entity?.name || ''
}

function ensureBackendSystemConfig(systemConfig) {
  if (!systemConfig) return
  if (!systemConfig.audioStorageProvider) systemConfig.audioStorageProvider = 'local'
  if (systemConfig.audioTranscodeEnabled === undefined) systemConfig.audioTranscodeEnabled = false
  if (!systemConfig.audioTranscodeFormat) systemConfig.audioTranscodeFormat = 'opus'
  if (!systemConfig.audioTranscodeBitrate) systemConfig.audioTranscodeBitrate = '32k'
  if (systemConfig.audioTranscodeDeleteOriginal === undefined) systemConfig.audioTranscodeDeleteOriginal = true
  if (systemConfig.audioRetentionSoftDays === undefined || systemConfig.audioRetentionSoftDays === null) systemConfig.audioRetentionSoftDays = 0
  if (systemConfig.audioRetentionPurgeDays === undefined || systemConfig.audioRetentionPurgeDays === null) systemConfig.audioRetentionPurgeDays = 0
  if (systemConfig.audioRetentionRunMinutes === undefined || systemConfig.audioRetentionRunMinutes === null) systemConfig.audioRetentionRunMinutes = 60
}

function ensureFrontendSystemConfig(systemConfig) {
  if (!systemConfig) return
  if (!systemConfig.authMode) systemConfig.authMode = 'local'
  if (!systemConfig.authBaseUrl) systemConfig.authBaseUrl = 'http://localhost:5032/api/v1'
  if (!systemConfig.seedAdminUser) systemConfig.seedAdminUser = 'admin'
  if (systemConfig.seedAdminPassword === undefined || systemConfig.seedAdminPassword === null || systemConfig.seedAdminPassword === '') {
    systemConfig.seedAdminPassword = 'admin'
  }
  if (!systemConfig.seedAdminEmail) {
    const user = systemConfig.seedAdminUser || 'admin'
    systemConfig.seedAdminEmail = `${user}@local`
  }
}

function ensureFrontendEntityConfig(entityConfig) {
  if (!entityConfig) return
  if (entityConfig.showInMenu === undefined) entityConfig.showInMenu = true
  if (!entityConfig.menuIcon) entityConfig.menuIcon = 'mdi-table'
  if (entityConfig.routeSlug === undefined) entityConfig.routeSlug = ''
  if (entityConfig.listStickyHeader === undefined) entityConfig.listStickyHeader = false
  if (entityConfig.listShowTotals === undefined) entityConfig.listShowTotals = true
  if (!entityConfig.defaultSortDirection) entityConfig.defaultSortDirection = 'asc'
  if (!entityConfig.formLayout) entityConfig.formLayout = 'single'
  if (entityConfig.confirmSave === undefined) entityConfig.confirmSave = true
  if (entityConfig.confirmDelete === undefined) entityConfig.confirmDelete = true
  if (entityConfig.enableDuplicate === undefined) entityConfig.enableDuplicate = true
  if (!entityConfig.messages) {
    entityConfig.messages = {
      empty: 'No hay registros todavia.',
      error: 'Ocurrio un error al procesar la solicitud.',
      successCreate: 'Registro creado.',
      successUpdate: 'Registro actualizado.',
      successDelete: 'Registro eliminado.'
    }
  }
}

function ensureFrontendFieldConfigs(entityConfig, fields) {
  if (!entityConfig.fields) entityConfig.fields = []
  const map = new Map(entityConfig.fields.map(f => [f.fieldId, f]))
  fields.forEach(field => {
    if (map.has(field.id)) {
      const existing = map.get(field.id)
      if (!existing.dataType && field.dataType) existing.dataType = field.dataType
      if (existing.isPrimaryKey === undefined) existing.isPrimaryKey = field.isPrimaryKey
      if (existing.isIdentity === undefined) existing.isIdentity = field.isIdentity
      if (existing.required === undefined) existing.required = field.required
      if (existing.maxLength === undefined || existing.maxLength === null) existing.maxLength = field.maxLength
      return
    }
    entityConfig.fields.push({
      fieldId: field.id,
      name: field.name,
      columnName: field.columnName,
      dataType: field.dataType,
      isPrimaryKey: field.isPrimaryKey,
      isIdentity: field.isIdentity,
      required: field.required,
      maxLength: field.maxLength,
      label: field.name,
      showInList: true,
      showInForm: !field.isIdentity,
      showInFilter: true,
      placeholder: '',
      helpText: '',
      inputType: '',
      section: 'General',
      format: '',
      min: null,
      max: null,
      pattern: '',
      quickToggle: false
    })
  })
}

function onFrontendFieldDragStart(index) {
  frontendFieldDragIndex.value = index
}

function onFrontendFieldDragOver(index) {
  frontendFieldDragOver.value = index
}

function onFrontendFieldDrop(index) {
  const from = frontendFieldDragIndex.value
  if (from === null || from === undefined) return
  if (!frontendEntidadActual.value?.fields?.length) return
  if (from === index) {
    frontendFieldDragIndex.value = null
    frontendFieldDragOver.value = null
    return
  }

  const list = frontendEntidadActual.value.fields
  const [moved] = list.splice(from, 1)
  list.splice(index, 0, moved)
  frontendFieldDragIndex.value = null
  frontendFieldDragOver.value = null
}

function onFrontendFieldDragEnd() {
  frontendFieldDragIndex.value = null
  frontendFieldDragOver.value = null
}

function abrirFrontendField(field) {
  frontendFieldActual.value = field
  mostrarFrontendFieldDialog.value = true
}

function onFrontendEntityDragStart(index) {
  frontendEntityDragIndex.value = index
}

function onFrontendEntityDragOver(index) {
  frontendEntityDragOver.value = index
}

function onFrontendEntityDrop(index) {
  const from = frontendEntityDragIndex.value
  if (from === null || from === undefined) return
  if (!frontendConfig.value?.entities?.length) return
  if (from === index) {
    frontendEntityDragIndex.value = null
    frontendEntityDragOver.value = null
    return
  }

  const list = frontendConfig.value.entities
  const [moved] = list.splice(from, 1)
  list.splice(index, 0, moved)
  frontendEntityDragIndex.value = null
  frontendEntityDragOver.value = null
}

function onFrontendEntityDragEnd() {
  frontendEntityDragIndex.value = null
  frontendEntityDragOver.value = null
}

async function abrirFrontendEntidad(entity) {
  frontendEntidadActual.value = entity
  ensureFrontendEntityConfig(frontendEntidadActual.value)
  const fields = await campoService.getByEntity(systemId, entity.entityId || entity.id)
  ensureFrontendFieldConfigs(frontendEntidadActual.value, fields.data || [])
  mostrarFrontendDialog.value = true
}

async function generarBackend() {
  const nombre = sistema.value?.name || 'sistema'
  const ok = window.confirm(`Generar backend para ${nombre}?`)
  if (!ok) return

  try {
    const { data } = await sistemaService.generarBackend(systemId, false)
    const outputPath = data?.outputPath || data?.OutputPath
    const restoreOk = data?.restoreOk ?? data?.RestoreOk
    const restoreError = data?.restoreError || data?.RestoreError
    window.alert(`Backend generado en:\n${outputPath}`)
    if (restoreOk === false) {
      window.alert(`dotnet restore fallo:\n${restoreError || 'Revisa la consola del backend.'}`)
    }
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al generar backend.'

    if (message.includes('overwrite=true')) {
      const overwrite = window.confirm(`${message}\n\nDeseas reemplazarlo?`)
      if (overwrite) {
        try {
          const { data } = await sistemaService.generarBackend(systemId, true)
          const outputPath = data?.outputPath || data?.OutputPath
          window.alert(`Backend generado en:\n${outputPath}`)
          return
        } catch (innerError) {
          const innerMessage =
            innerError?.response?.data?.message ||
            innerError?.response?.data?.Message ||
            'Error al reemplazar el backend.'
          window.alert(innerMessage)
          return
        }
      }
    }

    window.alert(message)
  }
}

async function generarFrontend() {
  const nombre = sistema.value?.name || 'sistema'
  const ok = window.confirm(`Generar frontend para ${nombre}?`)
  if (!ok) return

  try {
    const { data } = await sistemaService.generarFrontend(systemId, false)
    const outputPath = data?.outputPath || data?.OutputPath
    await cargarMenuTree()
    window.alert(`Frontend generado en:\n${outputPath}`)
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al generar frontend.'

    if (message.includes('overwrite=true')) {
      const overwrite = window.confirm(`${message}\n\nDeseas reemplazarlo?`)
      if (overwrite) {
        try {
          const { data } = await sistemaService.generarFrontend(systemId, true)
          const outputPath = data?.outputPath || data?.OutputPath
          await cargarMenuTree()
          window.alert(`Frontend generado en:\n${outputPath}`)
          return
        } catch (innerError) {
          const innerMessage =
            innerError?.response?.data?.message ||
            innerError?.response?.data?.Message ||
            'Error al reemplazar el frontend.'
          window.alert(innerMessage)
          return
        }
      }
    }

    window.alert(message)
  }
}

onMounted(async () => {
  const savedBase = localStorage.getItem('backendConsoleBaseUrl')
  apiConsole.value.baseUrl = savedBase || backendBaseUrl.value
  await cargarSistema()
  sqlConsole.value.script = sqlScriptTemplate.value
  await cargarEntidades()
  await cargarRelaciones()
  await cargarBackendConfig()
  await cargarFrontendConfig()
  await checkFrontendHealth()
  if (tab.value === 'herramientas') {
    startHealthPolling()
    startLogsPolling()
  }
})

onBeforeUnmount(() => {
  stopHealthPolling()
  stopLogsPolling()
})

function entidadNombre(id) {
  const entidad = entidades.value.find(e => e.id === id)
  return entidad?.displayName || entidad?.name || `#${id}`
}
</script>

<style scoped>
.empty-state {
  padding: 24px;
  color: #6b7280;
}

.mono {
  font-family: monospace;
  font-size: 0.85rem;
}

.backend-console {
  background: #0b1020;
  color: #e2e8f0;
  border-radius: 8px;
  padding: 12px;
  height: 220px;
  overflow-y: auto;
  font-size: 0.8rem;
}

.backend-log-line {
  white-space: pre-wrap;
  line-height: 1.35;
  margin-bottom: 4px;
}

.backend-log-time {
  color: #94a3b8;
  margin-right: 6px;
}

.backend-log-level {
  color: #60a5fa;
  margin-right: 6px;
  text-transform: uppercase;
  font-size: 0.7rem;
}

.backend-log-error {
  color: #fca5a5;
}

.backend-log-stdout {
  color: #e2e8f0;
}

.data-grid {
  align-items: stretch;
}

.data-card {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.data-card-body {
  flex: 1 1 auto;
  min-height: clamp(260px, 36vh, 420px);
  display: flex;
  flex-direction: column;
}

.data-card-body :deep(.v-data-table) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.data-card-body :deep(.v-table__wrapper) {
  flex: 1 1 auto;
  overflow-y: auto;
}

.data-card-body :deep(.v-data-table-footer) {
  flex: 0 0 auto;
}

.data-empty {
  flex: 1 1 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
}

.table-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-wrap: nowrap;
}

.table-actions :deep(.v-btn) {
  min-width: 32px;
}

.api-textarea :deep(textarea) {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}

.port-card {
  border-radius: 12px;
}

.port-value {
  font-size: 0.95rem;
  color: #1f2937;
}

.frontend-fields-table .drag-handle,
.frontend-entities-table .drag-handle {
  cursor: grab;
  color: #64748b;
}

.frontend-field-row.is-drag-over {
  background: #eef2ff;
}

.frontend-fields-table .drag-cell,
.frontend-entities-table .drag-cell {
  width: 36px;
}
</style>
