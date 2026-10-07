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
                        <v-form @submit.prevent="register">
                            <v-text-field v-model="form.username" label="Usuario" prepend-inner-icon="mdi-account"
                                required />

                            <v-text-field v-model="form.email" label="Email" prepend-inner-icon="mdi-email" type="email"
                                required />

                            <v-text-field v-model="form.nombre" label="Nombre" prepend-inner-icon="mdi-account-details"
                                required />

                            <v-text-field v-model="form.apellido" label="Apellido"
                                prepend-inner-icon="mdi-account-details-outline" required />

                            <v-text-field v-model="form.password" label="Contraseña" type="password"
                                prepend-inner-icon="mdi-lock" required />

                            <v-alert v-if="error" type="error" density="compact" class="mb-3">
                                {{ error }}
                            </v-alert>

                            <v-alert v-if="success" type="success" density="compact" class="mb-3">
                                Usuario creado correctamente
                            </v-alert>

                            <v-btn block color="primary" size="large" type="submit" :loading="loading">
                                Registrarse
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
import { authService } from '../api/auth.service'

const loading = ref(false)
const error = ref('')
const success = ref(false)

const form = ref({
    username: '',
    email: '',
    nombre: '',
    apellido: '',
    password: ''
})

async function register() {
    error.value = ''
    success.value = false
    loading.value = true

    try {
        await authService.register(form.value)
        success.value = true
    } catch {
        error.value = 'No se pudo crear el usuario'
    } finally {
        loading.value = false
    }
}
</script>
