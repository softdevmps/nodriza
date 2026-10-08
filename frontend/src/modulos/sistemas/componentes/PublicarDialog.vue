<template>
  <v-dialog v-model="model" max-width="720px" scrollable>
    <v-card class="dialog-card publicar-dialog">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="green">mdi-cloud-upload</v-icon>
        <span class="text-h6 font-weight-medium">Publicar {{ nombre }}</span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-progress-linear v-if="cargando" indeterminate color="primary" />

        <v-alert v-else-if="errorCarga" type="error" variant="tonal">{{ errorCarga }}</v-alert>

        <template v-else-if="preview">
          <v-alert v-if="preview.errores.length" type="error" variant="tonal" title="No se puede publicar todavía">
            <ul class="lista">
              <li v-for="(e, i) in preview.errores" :key="i">{{ e }}</li>
            </ul>
          </v-alert>

          <template v-else>
            <p v-if="!hayCambios && !preview.tablasNuevas.length" class="text-medium-emphasis">
              No hay cambios en la estructura de la base: se actualizan menús y permisos.
            </p>

            <section v-if="preview.tablasNuevas.length" class="seccion">
              <div class="seccion-titulo">Tablas nuevas (se crean vacías)</div>
              <v-chip v-for="t in preview.tablasNuevas" :key="t" size="small" class="mr-1 mb-1" color="green" variant="tonal">{{ t }}</v-chip>
            </section>

            <section v-if="preview.renombres.length" class="seccion">
              <div class="seccion-titulo">Renombrados (los datos se conservan)</div>
              <ul class="lista">
                <li v-for="r in preview.renombres" :key="r">{{ r }}</li>
              </ul>
              <div class="text-caption text-medium-emphasis">
                Si ya generaste el backend o el frontend del sistema, volvé a generarlos para que usen los nombres nuevos.
              </div>
            </section>

            <section v-if="preview.cambios.length" class="seccion">
              <v-alert type="warning" variant="tonal" density="compact" class="mb-2">
                Se van a modificar tablas que ya existen ({{ preview.cambios.length }} {{ preview.cambios.length === 1 ? 'sentencia' : 'sentencias' }}). Todo se aplica junto: si algo falla, no se cambia nada.
              </v-alert>
              <v-expansion-panels variant="accordion">
                <v-expansion-panel title="Ver SQL">
                  <v-expansion-panel-text>
                    <pre class="sql">{{ preview.cambios.join('\n') }}</pre>
                  </v-expansion-panel-text>
                </v-expansion-panel>
              </v-expansion-panels>
            </section>
          </template>
        </template>

        <v-alert v-if="errorPublicar" type="error" variant="tonal" class="mt-3">{{ errorPublicar }}</v-alert>
      </v-card-text>

      <v-divider />

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" :disabled="publicando" @click="model = false">Cancelar</v-btn>
        <v-btn color="green" variant="flat" :disabled="!preview?.ok || cargando" :loading="publicando" @click="publicar">
          Publicar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
// Antes de publicar muestra qué va a pasar en la base (GET .../publicar/preview):
// tablas nuevas, renombrados, SQL sobre tablas existentes y errores. Publica solo si se confirma.
import { computed, ref, watch } from 'vue'
import sistemaService from '../sistema.service.js'
import { mensajeError } from '../../../comun/utils/mensajeError.js'

const props = defineProps({
  modelValue: Boolean,
  sistemaId: { type: Number, default: null },
  nombre: { type: String, default: '' }
})
const emit = defineEmits(['update:modelValue', 'publicado'])

const model = computed({
  get: () => props.modelValue,
  set: v => emit('update:modelValue', v)
})

const cargando = ref(false)
const publicando = ref(false)
const preview = ref(null)
const errorCarga = ref('')
const errorPublicar = ref('')

const hayCambios = computed(() => (preview.value?.renombres.length || 0) + (preview.value?.cambios.length || 0) > 0)

watch(model, async abierto => {
  if (!abierto || !props.sistemaId) return
  preview.value = null
  errorCarga.value = ''
  errorPublicar.value = ''
  cargando.value = true
  try {
    preview.value = (await sistemaService.previewPublicar(props.sistemaId)).data
  } catch (e) {
    errorCarga.value = mensajeError(e, 'No se pudo calcular qué cambia al publicar.')
  } finally {
    cargando.value = false
  }
})

async function publicar () {
  publicando.value = true
  errorPublicar.value = ''
  try {
    const { data } = await sistemaService.publicar(props.sistemaId)
    emit('publicado', data)
    model.value = false
  } catch (e) {
    errorPublicar.value = mensajeError(e, 'Error al publicar el sistema.')
  } finally {
    publicando.value = false
  }
}
</script>

<style scoped>
.seccion {
  margin-bottom: 16px;
}

.seccion-titulo {
  font-weight: 600;
  margin-bottom: 6px;
}

.lista {
  margin: 0;
  padding-left: 18px;
}

.sql {
  font-size: 0.78rem;
  white-space: pre-wrap;
  word-break: break-all;
  margin: 0;
}
</style>
