<template>
  <v-dialog v-model="model" max-width="640px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ sistema?.id ? 'mdi-pencil' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ sistema?.id ? 'Editar sistema' : 'Nuevo sistema' }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-form class="form">
          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.slug" label="Slug" prepend-inner-icon="mdi-pound"
                :disabled="!!sistema?.id" />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field v-model="form.name" label="Nombre" prepend-inner-icon="mdi-format-title" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.namespace" label="Namespace" prepend-inner-icon="mdi-code-tags" />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field v-model="form.version" label="Version" prepend-inner-icon="mdi-tag-outline" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-textarea v-model="form.description" label="Descripcion" rows="2"
                prepend-inner-icon="mdi-text" />
            </v-col>
          </v-row>

          <v-row v-if="sistema?.id">
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
import sistemaService from '../../api/sistema.service.js';

export default {
  props: {
    modelValue: Boolean,
    sistema: Object
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        slug: '',
        name: '',
        namespace: '',
        description: '',
        version: '',
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
    sistema: {
      immediate: true,
      handler(value) {
        if (!value) {
          this.reset();
          return;
        }

        this.form = {
          slug: value.slug,
          name: value.name,
          namespace: value.namespace,
          description: value.description ?? '',
          version: value.version ?? '',
          isActive: value.isActive ?? true
        };
      }
    }
  },

  methods: {
    guardar() {
      const payload = {
        slug: this.form.slug,
        name: this.form.name,
        namespace: this.form.namespace,
        description: this.form.description,
        version: this.form.version,
        isActive: this.form.isActive
      };

      const req = this.sistema?.id
        ? sistemaService.editar(this.sistema.id, payload)
        : sistemaService.crear(payload);

      req.then(() => {
        this.$emit('guardado');
        this.cerrar();
      });
    },

    cerrar() {
      this.model = false;
    },

    reset() {
      this.form = {
        slug: '',
        name: '',
        namespace: '',
        description: '',
        version: '',
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
