<template>
  <v-card class="menu-card" elevation="2">

    <!-- HEADER -->
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          mdi-view-list
        </v-icon>
        <span class="text-h6 font-weight-medium">
          Estructura de Menú
        </span>
      </div>

      <v-chip color="primary" variant="outlined" size="small">
        {{ menus.length }} items raíz
      </v-chip>
    </v-card-title>

    <v-divider />

    <!-- TABLE -->
    <v-table class="menu-table">
      <thead>
        <tr>
          <th>Menú</th>
          <th>Ruta</th>
          <th class="text-center">Orden</th>
          <th class="text-center">Acciones</th>
        </tr>
      </thead>

      <tbody>
        <template v-for="menu in menus" :key="menu.id">
          <MenuRow :menu="menu" :nivel="0" @editar="$emit('editar', $event)" />
        </template>
      </tbody>
    </v-table>

  </v-card>
</template>

<script setup>
import MenuRow from './MenuRow.vue'

defineProps({
  menus: {
    type: Array,
    required: true
  }
})

defineEmits(['editar'])
</script>

<style scoped>
.menu-card {
  border-radius: 12px;
}

.menu-table {
  width: 100%;
}

.menu-table thead th {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.75rem;
  color: #6b7280;
}

.menu-table tbody tr:hover {
  background-color: rgba(0, 0, 0, 0.02);
}

.menu-table th,
.menu-table td {
  padding: 12px 16px;
}
</style>
