<template>
  <v-dialog v-model="open" max-width="520">
    <v-card class="menu-dialog-card">

      <!-- HEADER -->
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ menu ? 'mdi-pencil' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ menu ? 'Editar menú' : 'Nuevo menú' }}
        </span>
      </v-card-title>

      <v-divider />

      <!-- BODY -->
      <v-card-text>
        <v-form class="menu-form">

          <v-row>
            <v-col cols="12">
              <v-text-field v-model="form.titulo" label="Título" prepend-inner-icon="mdi-format-title" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.icono" label="Icono (mdi-*)" prepend-inner-icon="mdi-icons" />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field v-model="form.orden" label="Orden" type="number"
                prepend-inner-icon="mdi-sort-numeric-ascending" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-text-field v-model="form.ruta" label="Ruta" prepend-inner-icon="mdi-link-variant" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-select v-model="form.padreId" :items="padres" item-title="titulo" item-value="id" label="Menú padre"
                prepend-inner-icon="mdi-file-tree" clearable />
            </v-col>
          </v-row>

        </v-form>
      </v-card-text>

      <v-divider />

      <!-- ACTIONS -->
      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="cerrar">
          Cancelar
        </v-btn>

        <v-btn color="primary" @click="guardar">
          Guardar
        </v-btn>
      </v-card-actions>

    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import MenuService from '../../api/menu.service'
import { useMenuStore } from '../../store/menu.store.js'

/* =========================
   PROPS / EMITS
========================= */
const props = defineProps({
  modelValue: Boolean,
  menu: Object,
  menus: Array
})

const emit = defineEmits(['update:modelValue', 'guardado'])

/* =========================
   STATE
========================= */
const open = ref(false)

const form = ref({
  titulo: '',
  icono: '',
  ruta: '',
  orden: 1,
  padreId: null,
  rolesIds: [1]
})

const { cargarMenuTree } = useMenuStore()

/* =========================
   SYNC DIALOG
========================= */
watch(() => props.modelValue, v => open.value = v)
watch(open, v => emit('update:modelValue', v))

/* =========================
   WATCH MENU EDIT
========================= */
watch(
  () => props.menu,
  (m) => {
    if (!m) {
      form.value = {
        titulo: '',
        icono: '',
        ruta: '',
        orden: 1,
        padreId: null,
        rolesIds: [1]
      }
      return
    }

    form.value = {
      titulo: m.titulo,
      icono: m.icono,
      ruta: m.ruta,
      orden: m.orden,
      padreId: m.padreId ?? null,
      rolesIds: m.rolesIds ?? [1]
    }
  },
  { immediate: true }
)

const padres = computed(() =>
  props.menus.map(m => ({
    id: m.id,
    titulo: m.titulo
  }))
)

/* =========================
   METHODS
========================= */
function cerrar() {
  open.value = false
}

async function guardar() {
  if (props.menu) {
    await MenuService.editar(props.menu.id, form.value)
  } else {
    await MenuService.crear(form.value)
  }

  await cargarMenuTree()
  emit('guardado')
  open.value = false
}

</script>

<style scoped>
.menu-dialog-card {
  border-radius: 14px;
}

.menu-form :deep(.v-field) {
  margin-bottom: 6px;
}
</style>
