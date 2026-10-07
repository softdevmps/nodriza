<template>
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
</template>

<script setup>
// Diálogo de configuración del backend de una entidad. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  backendAuthOptions,
  backendEntidadActual,
  backendRequiredOptions,
  backendSortOptions,
  campos,
  endpointOnlyKey,
  endpointOnlyMode,
  endpointOnlyTitle,
  endpointPanel,
  endpointSoftDeleteOptions,
  getEndpointConfig,
  headersBackendFields,
  mostrarBackendDialog,
  route,
  shouldShowEndpointPanel
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
