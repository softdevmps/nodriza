<template>
  <v-dialog v-model="model" max-width="600px">
    <v-card class="usuario-dialog-card">

      <!-- HEADER -->
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ usuarioId ? 'mdi-account-edit' : 'mdi-account-plus' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ usuarioId ? 'Editar usuario' : 'Nuevo usuario' }}
        </span>
      </v-card-title>

      <v-divider />

      <!-- BODY -->
      <v-card-text>
        <v-form class="usuario-form">

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.username" label="Username" prepend-inner-icon="mdi-account" />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field v-model="form.email" label="Email" prepend-inner-icon="mdi-email" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field v-model="form.nombre" label="Nombre" prepend-inner-icon="mdi-card-account-details" />
            </v-col>

            <v-col cols="12" md="6">
              <v-text-field v-model="form.apellido" label="Apellido"
                prepend-inner-icon="mdi-card-account-details-outline" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-select v-model="form.rolId" :items="roles" item-title="nombre" item-value="id" label="Rol"
                prepend-inner-icon="mdi-shield-account" />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12">
              <v-text-field v-model="form.password" label="Password" type="password" prepend-inner-icon="mdi-lock"
                :hint="usuarioId ? 'Dejar vacío para no cambiar' : ''" persistent-hint />
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

<script>
import usuarioService from '../../api/usuario.service.js';

export default {
  props: {
    modelValue: Boolean,
    usuarioId: Number,
    roles: Array
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        username: '',
        email: '',
        nombre: '',
        apellido: '',
        rolId: null,
        password: ''
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
    usuarioId: {
      immediate: true,
      handler(id) {
        this.reset();
        if (id) this.cargarUsuario(id);
      }
    }
  },

  methods: {
    cargarUsuario(id) {
      usuarioService.obtenerPorId(id).then(res => {
        this.form = {
          username: res.data.username,
          email: res.data.email,
          nombre: res.data.nombre,
          apellido: res.data.apellido,
          rolId: Number(res.data.rolId),
          password: ''
        };
      });
    },

    guardar() {
      const data = { ...this.form };
      if (!data.password) delete data.password;

      const req = this.usuarioId
        ? usuarioService.editar(this.usuarioId, data)
        : usuarioService.crear(data);

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
        username: '',
        email: '',
        nombre: '',
        apellido: '',
        rolId: null,
        password: ''
      };
    }
  }
};
</script>

<style scoped>
.usuario-dialog-card {
  border-radius: 14px;
}

.usuario-form {
  margin-top: 8px;
}

.usuario-form :deep(.v-field) {
  margin-bottom: 4px;
}
</style>
