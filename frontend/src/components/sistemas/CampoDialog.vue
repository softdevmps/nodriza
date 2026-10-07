<template>
  <v-dialog v-model="model" max-width="720px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ campo?.id ? 'mdi-pencil' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ campo?.id ? 'Editar campo' : 'Nuevo campo' }}
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
              <v-text-field v-model="form.columnName" label="ColumnName" prepend-inner-icon="mdi-table-column" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="4">
              <v-select v-model="form.dataType" :items="types" label="DataType"
                prepend-inner-icon="mdi-database" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="form.maxLength" type="number" label="MaxLength"
                prepend-inner-icon="mdi-ruler" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="form.sortOrder" type="number" label="Orden"
                prepend-inner-icon="mdi-sort-numeric-ascending" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="4">
              <v-switch v-model="form.required" inset color="green" label="Required" />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch v-model="form.isPrimaryKey" inset color="primary" label="PrimaryKey" />
            </v-col>
            <v-col cols="12" md="4">
              <v-switch v-model="form.isIdentity" inset color="secondary" label="Identity" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="4">
              <v-switch v-model="form.isUnique" inset color="primary" label="Unique" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="form.precision" type="number" label="Precision"
                prepend-inner-icon="mdi-decimal" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="form.scale" type="number" label="Scale" prepend-inner-icon="mdi-decimal" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-text-field v-model="form.defaultValue" label="DefaultValue" prepend-inner-icon="mdi-function" />
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
import campoService from '../../api/campo.service.js';

export default {
  props: {
    modelValue: Boolean,
    campo: Object,
    systemId: Number,
    entityId: Number
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      types: ['string', 'int', 'decimal', 'bool', 'datetime', 'guid'],
      form: {
        name: '',
        columnName: '',
        dataType: 'string',
        required: false,
        maxLength: null,
        precision: null,
        scale: null,
        defaultValue: '',
        isPrimaryKey: false,
        isIdentity: false,
        isUnique: false,
        sortOrder: 1
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
    campo: {
      immediate: true,
      handler(value) {
        this.syncForm(value);
      }
    },
    modelValue(value) {
      if (!value) return;
      // When opening, always sync to avoid stale data
      this.syncForm(this.campo);
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
        columnName: value.columnName,
        dataType: value.dataType ?? 'string',
        required: value.required ?? false,
        maxLength: value.maxLength,
        precision: value.precision,
        scale: value.scale,
        defaultValue: value.defaultValue ?? '',
        isPrimaryKey: value.isPrimaryKey ?? false,
        isIdentity: value.isIdentity ?? false,
        isUnique: value.isUnique ?? false,
        sortOrder: value.sortOrder ?? 1
      };
    },
    guardar() {
      const payload = {
        name: this.form.name,
        columnName: this.form.columnName,
        dataType: this.form.dataType,
        required: this.form.required,
        maxLength: this.form.maxLength ? Number(this.form.maxLength) : null,
        precision: this.form.precision ? Number(this.form.precision) : null,
        scale: this.form.scale ? Number(this.form.scale) : null,
        defaultValue: this.form.defaultValue,
        isPrimaryKey: this.form.isPrimaryKey,
        isIdentity: this.form.isIdentity,
        isUnique: this.form.isUnique,
        sortOrder: Number(this.form.sortOrder)
      };

      const req = this.campo?.id
        ? campoService.editar(this.systemId, this.entityId, this.campo.id, payload)
        : campoService.crear(this.systemId, this.entityId, payload);

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
        columnName: '',
        dataType: 'string',
        required: false,
        maxLength: null,
        precision: null,
        scale: null,
        defaultValue: '',
        isPrimaryKey: false,
        isIdentity: false,
        isUnique: false,
        sortOrder: 1
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
