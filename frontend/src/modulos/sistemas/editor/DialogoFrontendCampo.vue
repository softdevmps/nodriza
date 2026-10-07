<template>
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
</template>

<script setup>
// Diálogo de configuración del frontend de un campo. Estado y lógica: useSistemaEditor.js (inyectado por SistemaEditor.vue).
import { inject } from 'vue'

const {
  frontendFieldActual,
  frontendFieldFormatOptions,
  frontendFieldInputOptions,
  mostrarFrontendFieldDialog
} = inject('sistemaEditor')
</script>

<style scoped src="./sistema-editor.css"></style>
