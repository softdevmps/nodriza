<template>
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
</template>

<script setup>
// Pestaña "Datos": entidades, campos, relaciones, publicar y consola SQL. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  campos,
  editarCampo,
  editarEntidad,
  editarRelacion,
  ejecutarSqlConsole,
  eliminarEntidad,
  entidadNombre,
  entidadSeleccionada,
  entidades,
  headersCampos,
  headersEntidades,
  headersRelaciones,
  limpiarSqlConsole,
  nuevaEntidad,
  nuevaRelacion,
  nuevoCampo,
  publicarSistema,
  relaciones,
  seleccionarEntidad,
  sistema,
  sqlConsole,
  sqlScriptTemplate,
  sqlTargetSchema,
  verDatos
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
