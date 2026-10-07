<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card elevation="10">
          <v-card-title class="text-center text-h6">
            <v-icon class="mr-2">mdi-account-plus</v-icon>
            Crear cuenta
          </v-card-title>

          <v-card-text>
            <v-form @submit.prevent="registrar">
              <v-text-field
                v-model="usuario"
                label="Usuario"
                prepend-inner-icon="mdi-account"
                :rules="[v => !!v || 'Campo requerido']"
                required
              />

              <v-text-field
                v-model="email"
                label="Email"
                prepend-inner-icon="mdi-email"
                :rules="[v => !!v || 'Campo requerido']"
                required
              />

              <v-text-field
                v-model="password"
                label="Contraseña"
                type="password"
                prepend-inner-icon="mdi-lock"
                :rules="[v => !!v || 'Campo requerido']"
                required
              />

              <v-alert v-if="error" type="error" density="compact" class="mb-3">
                {{ error }}
              </v-alert>

              <v-btn block color="primary" size="large" type="submit" :loading="loading">
                Registrar
              </v-btn>
            </v-form>
          </v-card-text>

          <v-card-actions class="justify-center">
            <v-btn variant="text" @click="$router.push('/login')">
              Volver al login
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '../api/auth.service'

const usuario = ref('')
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const router = useRouter()

async function registrar() {
  error.value = ''
  loading.value = true

  try {
    await authService.register({
      usuario: usuario.value,
      email: email.value,
      password: password.value
    })

    router.push('/login')
  } catch {
    error.value = 'No se pudo registrar'
  } finally {
    loading.value = false
  }
}
</script>
