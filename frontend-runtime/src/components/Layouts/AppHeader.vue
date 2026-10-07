<template>
  <v-app-bar color="transparent" elevation="0" class="app-header">
    <v-btn icon @click="$emit('toggle-drawer')">
      <v-icon>mdi-menu</v-icon>
    </v-btn>

    <v-app-bar-title class="d-flex align-center">
      <v-icon class="mr-2" size="22">mdi-view-dashboard</v-icon>
      <span class="app-title">{{ appTitle }}</span>
    </v-app-bar-title>

    <v-spacer />

    <v-tooltip text="Modo nocturno">
      <template #activator="{ props }">
        <v-btn v-bind="props" icon @click="toggleTheme">
          <v-icon>{{ isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night' }}</v-icon>
        </v-btn>
      </template>
    </v-tooltip>

    <v-tooltip text="Cerrar sesión">
      <template #activator="{ props }">
        <v-btn v-bind="props" icon @click="logout">
          <v-icon>mdi-logout</v-icon>
        </v-btn>
      </template>
    </v-tooltip>
  </v-app-bar>
</template>

<script setup>
import { computed, inject } from 'vue'
import { useRouter } from 'vue-router'
import frontendConfig from '../../config/frontend-config.json'

const router = useRouter()
const colorMode = inject('colorMode', null)
const isDark = computed(() => colorMode?.isDark?.value ?? false)

const appTitle = computed(() => frontendConfig?.system?.appTitle || 'Sistema')

function logout() {
  localStorage.removeItem('token')
  router.push('/login')
}

function toggleTheme() {
  if (colorMode?.toggleTheme) {
    colorMode.toggleTheme()
  }
}
</script>

<style scoped>
.app-header {
  background: var(--sb-header-bg);
  border-bottom: 1px solid var(--sb-border);
  backdrop-filter: blur(10px);
}

.app-title {
  font-family: var(--sb-font-display);
  font-weight: 600;
  letter-spacing: 0.4px;
  color: var(--sb-primary);
}

.app-header :deep(.v-btn),
.app-header :deep(.v-icon),
.app-header :deep(.v-toolbar-title) {
  color: var(--sb-text, #0f172a);
}
</style>
