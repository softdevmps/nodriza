<template>
  <v-container class="fill-height login-shell" fluid>
    <div class="login-orb orb-1"></div>
    <div class="login-orb orb-2"></div>

    <v-row align="center" justify="center" class="login-row">
      <v-col cols="12" md="6" lg="5" class="login-hero">
        <div class="login-brand">
          <div class="login-brand-icon">
            <v-icon size="28" color="primary">mdi-view-dashboard</v-icon>
          </div>
          <div>
            <h1>{{ appTitle }}</h1>
            <p>{{ tagline }}</p>
          </div>
        </div>

        <div class="login-metrics">
          <div class="metric-card">
            <span>Modo</span>
            <strong>{{ uiModeLabel }}</strong>
          </div>
          <div class="metric-card">
            <span>Base</span>
            <strong>SystemBase · CRUD · API</strong>
          </div>
        </div>

        <div class="login-features">
          <div class="feature">
            <v-icon size="18" color="primary">mdi-shield-check-outline</v-icon>
            <span>Seguridad y control de acceso</span>
          </div>
          <div class="feature">
            <v-icon size="18" color="primary">mdi-map-marker-check</v-icon>
            <span>Configuración flexible por sistema</span>
          </div>
          <div class="feature">
            <v-icon size="18" color="primary">mdi-waveform</v-icon>
            <span>Datos centralizados y auditables</span>
          </div>
        </div>
      </v-col>

      <v-col cols="12" sm="8" md="6" lg="4">
        <v-card elevation="8" class="login-card">
          <v-card-title class="text-h6 login-title">
            <v-icon class="mr-2">mdi-lock</v-icon>
            Iniciar sesión
          </v-card-title>

          <v-card-text>
            <v-form @submit.prevent="login" class="login-form">
              <v-text-field
                v-model="usuario"
                label="Usuario o Email"
                prepend-inner-icon="mdi-account"
                :rules="[v => !!v || 'Campo requerido']"
                required
                variant="outlined"
                density="compact"
              />

              <v-text-field
                v-model="password"
                label="Contraseña"
                type="password"
                prepend-inner-icon="mdi-lock"
                :rules="[v => !!v || 'Campo requerido']"
                required
                variant="outlined"
                density="compact"
              />

              <v-alert v-if="error" type="error" density="compact" class="mb-3">
                {{ error }}
              </v-alert>

              <v-btn block class="sb-btn primary login-submit" type="submit" :loading="loading">
                Ingresar
              </v-btn>
            </v-form>
          </v-card-text>

          <v-card-actions class="justify-center">
            <v-btn variant="text" @click="$router.push('/register')">
              Crear cuenta
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '../api/auth.service'
import frontendConfig from '../config/frontend-config.json'

const usuario = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const router = useRouter()
const appTitle = computed(() => frontendConfig?.system?.appTitle || 'SystemBase')
const uiModeLabel = computed(() => frontendConfig?.system?.uiMode || 'Enterprise')
const tagline = computed(() => frontendConfig?.system?.tagline || 'Tu plataforma configurable para gestionar datos en tiempo real.')

async function login() {
  error.value = ''
  loading.value = true

  try {
    const res = await authService.login({
      usuario: usuario.value,
      password: password.value
    })

    const token = res.data?.token || res.data?.Token
    if (token) {
      localStorage.setItem('token', token)
    } else {
      throw new Error('Token no recibido')
    }
    router.push('/home')
  } catch {
    error.value = 'Usuario o contraseña incorrectos'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-shell {
  background: transparent;
  position: relative;
  overflow: hidden;
}

.login-row {
  gap: 24px;
}

.login-hero {
  display: flex;
  flex-direction: column;
  gap: 24px;
  color: var(--sb-text, #0f172a);
}

.login-brand {
  display: flex;
  align-items: center;
  gap: 16px;
}

.login-brand h1 {
  font-family: var(--sb-font-display);
  font-size: 2rem;
  margin: 0;
}

.login-brand p {
  margin: 6px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.login-brand-icon {
  width: 56px;
  height: 56px;
  border-radius: 16px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-metrics {
  display: grid;
  gap: 12px;
}

.metric-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
  padding: 12px 14px;
  background: color-mix(in srgb, var(--sb-surface) 86%, transparent);
  box-shadow: 0 8px 18px rgba(15, 23, 42, 0.08);
}

.metric-card span {
  display: block;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--sb-text-soft, var(--sb-muted));
}

.metric-card strong {
  font-weight: 600;
  color: var(--sb-text, #0f172a);
}

.login-features {
  display: grid;
  gap: 10px;
}

.feature {
  display: flex;
  gap: 10px;
  align-items: center;
  font-size: 0.95rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

.login-card {
  border-radius: calc(var(--sb-radius) + 2px);
  border: 1px solid var(--sb-border);
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
  backdrop-filter: blur(12px);
  color: var(--sb-text, #0f172a);
}

.login-title {
  font-family: var(--sb-font-display);
}

.login-form :deep(.v-input) {
  margin-bottom: 12px;
}

.login-form :deep(.v-field) {
  background: color-mix(in srgb, var(--sb-surface) 92%, transparent);
}

.login-submit {
  margin-top: 6px;
}

.login-orb {
  position: absolute;
  border-radius: 999px;
  filter: blur(0px);
  opacity: 0.35;
  z-index: 0;
}

.orb-1 {
  width: 360px;
  height: 360px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.5), transparent 70%);
  top: -80px;
  left: -60px;
}

.orb-2 {
  width: 420px;
  height: 420px;
  background: radial-gradient(circle, rgba(14, 165, 233, 0.4), transparent 70%);
  bottom: -120px;
  right: -80px;
}

@media (max-width: 960px) {
  .login-hero {
    text-align: center;
    align-items: center;
  }
  .metric-card {
    width: 100%;
  }
}
</style>
