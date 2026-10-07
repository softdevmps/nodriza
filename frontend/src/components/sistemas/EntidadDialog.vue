<template>
  <v-dialog v-model="model" max-width="640px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ entidad?.id ? 'mdi-pencil' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ entidad?.id ? 'Editar entidad' : 'Nueva entidad' }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-form class="form">
          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.name" label="Nombre" prepend-inner-icon="mdi-format-title" />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.tableName" label="TableName" prepend-inner-icon="mdi-table" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.displayName" label="DisplayName" prepend-inner-icon="mdi-tag-outline" />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.sortOrder" type="number" label="Orden"
                prepend-inner-icon="mdi-sort-numeric-ascending" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-textarea v-model="form.description" label="Descripcion" rows="2"
                prepend-inner-icon="mdi-text" />
            </v-col>
          </v-row>

          <v-row v-if="entidad?.id">
            <v-col cols="12">
              <v-switch v-model="form.isActive" inset color="green" label="Activo" />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import entidadService from '../../api/entidad.service.js';

export default {
  props: {
    modelValue: Boolean,
    entidad: Object,
    systemId: Number
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        name: '',
        tableName: '',
        displayName: '',
        description: '',
        sortOrder: 1,
        isActive: true
      }
    };
  },

  computed: {
    model: {
      get() {
        return this.modelValue;
      },
      set(v) {
        this.$emit('update:modelValue', v);
      }
    }
  },

  watch: {
    entidad: {
      immediate: true,
      handler(value) {
        this.syncForm(value);
      }
    },
    modelValue(value) {
      if (!value) return;
      // When opening, always sync to avoid stale data
      this.syncForm(this.entidad);
    }
  },

  methods: {
    syncForm(value) {
      if (!value) {
        this.reset();
        return;
      }

      this.form = {
        name: value.name,
        tableName: value.tableName,
        displayName: value.displayName ?? '',
        description: value.description ?? '',
        sortOrder: value.sortOrder ?? 1,
        isActive: value.isActive ?? true
      };
    },
    guardar() {
      const payload = {
        name: this.form.name,
        tableName: this.form.tableName,
        displayName: this.form.displayName,
        description: this.form.description,
        sortOrder: Number(this.form.sortOrder),
        isActive: this.form.isActive
      };

      const req = this.entidad?.id
        ? entidadService.editar(this.systemId, this.entidad.id, payload)
        : entidadService.crear(this.systemId, payload);

      req.then(() => {
        this.$emit('guardado');
        this.cerrar();
      });
    },

    cerrar() {
      this.reset();
      this.model = false;
    },

    reset() {
      this.form = {
        name: '',
        tableName: '',
        displayName: '',
        description: '',
        sortOrder: 1,
        isActive: true
      };
    }
  }
};
</script>

<style scoped>
.dialog-card {
  border-radius: 14px;
}

.form :deep(.v-field) {
  margin-bottom: 4px;
}
</style>
