<template>
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
</template>

<script setup>
// Pestaña "Backend": configuración y generación del backend del sistema. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  abrirBackendEntidad,
  backendEntities,
  backendPersistenceOptions,
  backendSystemAuthOptions,
  backendSystemConfig,
  generarBackend,
  guardarBackendConfig,
  headersBackend,
  route
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
