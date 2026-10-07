<template>
  <v-container fluid>

    <!-- HEADER -->
    <v-row class="mb-6 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-view-list</v-icon>
          </div>

          <div>
            <h2 class="mb-1">Menú</h2>
            <span class="sb-page-subtitle text-body-2">
              Administración de menús del sistema
            </span>
          </div>
        </div>
      </v-col>

      <v-col cols="auto">
        <v-btn color="primary" variant="tonal" @click="nuevoMenu">
          <v-icon left>mdi-plus</v-icon>
          Nuevo menú
        </v-btn>
      </v-col>
    </v-row>

    <!-- TABLA -->
    <MenuTable :menus="menus" @editar="editarMenu" />

    <!-- DIALOG -->
    <MenuForm v-model="showForm" :menu="menuSeleccionado" :menus="menus" @guardado="recargar" />

  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import MenuService from '../../api/menu.service'

import MenuTable from '../../components/menu/MenuTable.vue'
import MenuForm from '../../components/menu/MenuForm.vue'

/* =========================
   STATE
========================= */
const menus = ref([])
const showForm = ref(false)
const menuSeleccionado = ref(null)

/* =========================
   METHODS
========================= */
async function cargarMenus() {
  const { data } = await MenuService.getMenuTree()
  menus.value = data
}

function nuevoMenu() {
  menuSeleccionado.value = null
  showForm.value = true
}

function editarMenu(menu) {
  menuSeleccionado.value = menu
  showForm.value = true
}

async function recargar() {
  showForm.value = false
  await cargarMenus()
}

/* =========================
   LIFECYCLE
========================= */
onMounted(cargarMenus)
</script>

<style scoped>
</style>
