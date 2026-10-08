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
        <TabDatos />
      </v-window-item>

      <v-window-item value="herramientas">
        <TabHerramientas />
      </v-window-item>

      <v-window-item value="frontend">
        <TabFrontend />
      </v-window-item>

      <v-window-item value="backend">
        <TabBackend />
      </v-window-item>
    </v-window>

    <DialogoBackendEntidad />

    <DialogoFrontendEntidad />

    <DialogoFrontendCampo />

    <EntidadDialog v-model="mostrarEntidadDialog" :entidad="entidadSeleccionadaEdicion" :system-id="systemId"
      @guardado="cargarEntidades" />

    <CampoDialog v-model="mostrarCampoDialog" :campo="campoSeleccionado" :system-id="systemId"
      :entity-id="entidadSeleccionada?.id" @guardado="cargarCampos" />

    <RelacionDialog v-model="mostrarRelacionDialog" :relacion="relacionSeleccionada" :entidades="entidades"
      :system-id="systemId" @guardado="cargarRelaciones" />

    <PublicarDialog v-model="mostrarPublicarDialog" :sistema-id="Number(systemId)" :nombre="sistema?.name"
      @publicado="alPublicar" />
  </v-container>
</template>

<script setup>
// Diseñador de un sistema: encabezado, pestañas y diálogos compartidos.
// Cada pestaña y diálogo vive en ./editor/; el estado y la lógica, en ./editor/useSistemaEditor.js.
import { provide } from 'vue'
import EntidadDialog from './componentes/EntidadDialog.vue'
import CampoDialog from './componentes/CampoDialog.vue'
import RelacionDialog from './componentes/RelacionDialog.vue'
import PublicarDialog from './componentes/PublicarDialog.vue'
import TabDatos from './editor/TabDatos.vue'
import TabHerramientas from './editor/TabHerramientas.vue'
import TabFrontend from './editor/TabFrontend.vue'
import TabBackend from './editor/TabBackend.vue'
import DialogoBackendEntidad from './editor/DialogoBackendEntidad.vue'
import DialogoFrontendEntidad from './editor/DialogoFrontendEntidad.vue'
import DialogoFrontendCampo from './editor/DialogoFrontendCampo.vue'
import { useSistemaEditor } from './editor/useSistemaEditor.js'

const editor = useSistemaEditor()
provide('sistemaEditor', editor)

const {
  campoSeleccionado,
  cargarCampos,
  cargarEntidades,
  cargarRelaciones,
  entidadSeleccionada,
  entidadSeleccionadaEdicion,
  entidades,
  alPublicar,
  mostrarCampoDialog,
  mostrarEntidadDialog,
  mostrarPublicarDialog,
  mostrarRelacionDialog,
  relacionSeleccionada,
  sistema,
  systemId,
  tab,
  volver
} = editor
</script>

<style scoped src="./editor/sistema-editor.css"></style>
