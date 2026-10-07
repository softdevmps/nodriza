<template>
  <v-dialog :model-value="modelValue" @update:model-value="cerrar" max-width="900">
    <v-card class="rol-menus-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">mdi-shield-key</v-icon>
        <span class="text-h6 font-weight-medium">
          Permisos del rol: {{ rol?.nombre }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-select
              v-model="systemId"
              :items="systems"
              item-title="systemName"
              item-value="systemId"
              label="Sistema"
              clearable
            />
          </v-col>
        </v-row>

        <v-alert v-if="!systemId" type="info" variant="tonal" class="mt-2">
          Selecciona un sistema para ver los permisos.
        </v-alert>

        <v-data-table
          v-else
          :headers="headers"
          :items="rows"
          class="roles-table mt-3"
          density="compact"
          hover
        >
          <template #item.view="{ item }">
            <v-checkbox v-model="item.view.assigned" hide-details :disabled="!item.view.permissionId" />
          </template>
          <template #item.create="{ item }">
            <v-checkbox v-model="item.create.assigned" hide-details :disabled="!item.create.permissionId" />
          </template>
          <template #item.edit="{ item }">
            <v-checkbox v-model="item.edit.assigned" hide-details :disabled="!item.edit.permissionId" />
          </template>
          <template #item.delete="{ item }">
            <v-checkbox v-model="item.delete.assigned" hide-details :disabled="!item.delete.permissionId" />
          </template>
        </v-data-table>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" :disabled="!systemId" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import rolService from '../../api/rol.service.js';

export default {
  props: {
    modelValue: {
      type: Boolean,
      required: true
    },
    rol: {
      type: Object,
      required: true
    }
  },

  emits: ['update:modelValue'],

  data() {
    return {
      systems: [],
      systemId: null,
      rows: [],
      headers: [
        { title: 'Entidad', key: 'entityName' },
        { title: 'Ver', key: 'view', sortable: false },
        { title: 'Crear', key: 'create', sortable: false },
        { title: 'Editar', key: 'edit', sortable: false },
        { title: 'Eliminar', key: 'delete', sortable: false }
      ]
    };
  },

  watch: {
    modelValue(value) {
      if (value && this.rol) {
        this.cargarSistemas();
      }
      if (!value) {
        this.systemId = null;
        this.rows = [];
      }
    },

    systemId(value) {
      if (value) {
        this.cargarPermisos(value);
      } else {
        this.rows = [];
      }
    }
  },

  methods: {
    async cargarSistemas() {
      const res = await rolService.getSystemMenus(this.rol.id);
      this.systems = res.data || [];

      if (this.systems.length === 1) {
        this.systemId = this.systems[0].systemId;
      }
    },

    async cargarPermisos(systemId) {
      const res = await rolService.getPermissions(this.rol.id, systemId);
      const raw = res.data || [];

      const map = new Map();
      for (const perm of raw) {
        if (!map.has(perm.entityId)) {
          map.set(perm.entityId, {
            entityId: perm.entityId,
            entityName: perm.entityName,
            view: { permissionId: null, assigned: false },
            create: { permissionId: null, assigned: false },
            edit: { permissionId: null, assigned: false },
            delete: { permissionId: null, assigned: false }
          });
        }

        const row = map.get(perm.entityId);
        if (row[perm.action]) {
          row[perm.action] = {
            permissionId: perm.permissionId,
            assigned: !!perm.asignado
          };
        }
      }

      this.rows = Array.from(map.values());
    },

    cerrar() {
      this.$emit('update:modelValue', false);
    },

    async guardar() {
      const permissionIds = [];
      for (const row of this.rows) {
        for (const action of ['view', 'create', 'edit', 'delete']) {
          const perm = row[action];
          if (perm?.permissionId && perm.assigned) {
            permissionIds.push(perm.permissionId);
          }
        }
      }

      await rolService.asignarPermissions(this.rol.id, this.systemId, permissionIds);
      this.cerrar();
    }
  }
};
</script>

<style scoped>
.rol-menus-card {
  border-radius: 14px;
}

.roles-table :deep(thead th) {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.7rem;
  color: #6b7280;
}
</style>
