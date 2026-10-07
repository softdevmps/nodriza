<template>
  <!-- FILA -->
  <tr class="menu-row">
    <!-- MENÚ -->
    <td>
      <div class="menu-title" :style="{ paddingLeft: `${nivel * 20}px` }">
        <v-icon size="small" class="mr-2" color="primary">
          {{ menu.icono }}
        </v-icon>

        <span class="menu-text">
          {{ menu.titulo }}
        </span>
      </div>
    </td>

    <!-- RUTA -->
    <td class="text-grey">
      {{ menu.ruta || '-' }}
    </td>

    <!-- ORDEN -->
    <td class="text-center">
      <v-chip size="small" variant="outlined" color="primary">
        {{ menu.orden }}
      </v-chip>
    </td>

    <!-- ACCIONES -->
    <td class="text-center">
      <v-tooltip text="Editar menú">
        <template #activator="{ props }">
          <v-btn v-bind="props" icon size="small" variant="text" color="primary" @click="$emit('editar', menu)">
            <v-icon>mdi-pencil</v-icon>
          </v-btn>
        </template>
      </v-tooltip>
    </td>
  </tr>

  <!-- HIJOS -->
  <template v-for="child in menu.children" :key="child.id">
    <MenuRow :menu="child" :nivel="nivel + 1" @editar="$emit('editar', $event)" />
  </template>
</template>

<script setup>
defineProps({
  menu: {
    type: Object,
    required: true
  },
  nivel: {
    type: Number,
    required: true
  }
})

defineEmits(['editar'])
</script>

<style scoped>
.menu-row:hover {
  background-color: rgba(0, 0, 0, 0.02);
}

.menu-title {
  display: flex;
  align-items: center;
}

.menu-text {
  font-weight: 500;
}

.text-grey {
  color: #6b7280;
}
</style>
