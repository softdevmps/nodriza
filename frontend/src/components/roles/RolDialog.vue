<template>
  <v-dialog v-model="model" max-width="420">
    <v-card class="rol-dialog-card">

      <!-- HEADER -->
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ rol ? 'mdi-shield-edit' : 'mdi-shield-plus' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ rol ? 'Editar rol' : 'Nuevo rol' }}
        </span>
      </v-card-title>

      <v-divider />

      <!-- BODY -->
      <v-card-text>
        <v-text-field v-model="form.nombre" label="Nombre del rol" prepend-inner-icon="mdi-shield-account" />
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

<script>
import rolService from '../../api/rol.service.js';

export default {
  props: {
    modelValue: Boolean,
    rol: Object
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        nombre: ''
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
    rol: {
      immediate: true,
      handler(rol) {
        this.form = {
          nombre: rol ? rol.nombre : ''
        };
      }
    }
  },

  methods: {
    cerrar() {
      this.model = false;
    },

    async guardar() {
      if (this.rol?.id) {
        // EDITAR
        await rolService.editar(this.rol.id, {
          nombre: this.form.nombre
        });
      } else {
        // CREAR
        await rolService.crear({
          nombre: this.form.nombre
        });
      }

      this.$emit('guardado');
      this.cerrar();
    }
  }
};
</script>

<style scoped>
.rol-dialog-card {
  border-radius: 14px;
}
</style>
