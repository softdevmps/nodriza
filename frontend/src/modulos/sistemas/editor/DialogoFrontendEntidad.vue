<template>
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
</template>

<script setup>
// Diálogo de configuración del frontend de una entidad. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  abrirFrontendField,
  campos,
  entidades,
  frontendEntidadActual,
  frontendFieldDragOver,
  frontendFormLayoutOptions,
  frontendSortOptions,
  mostrarFrontendDialog,
  onFrontendFieldDragEnd,
  onFrontendFieldDragOver,
  onFrontendFieldDragStart,
  onFrontendFieldDrop
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
