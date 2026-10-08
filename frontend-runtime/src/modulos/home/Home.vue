<template>
  <v-container fluid class="home-dashboard">
    <v-row class="mb-4">
      <v-col cols="12">
        <v-card class="card hero-card">
          <div class="hero-text">
            <div class="hero-icon">
              <v-icon color="primary" size="26">mdi-view-dashboard-outline</v-icon>
            </div>
            <div>
              <h1>{{ appTitle }}</h1>
              <p>Elegí una sección para ver y cargar sus registros.</p>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row v-if="secciones.length" dense>
      <v-col v-for="seccion in secciones" :key="seccion.ruta" cols="12" sm="6" md="4" lg="3">
        <v-card class="card seccion-card" :to="seccion.ruta" hover>
          <div class="seccion-icon">
            <v-icon color="primary">{{ seccion.icono }}</v-icon>
          </div>
          <div>
            <div class="seccion-titulo">{{ seccion.titulo }}</div>
            <div class="seccion-meta">{{ seccion.campos }} campos</div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-alert v-else type="info" variant="tonal">
      Este sistema todavía no tiene secciones para mostrar.
    </v-alert>
  </v-container>
</template>

<script setup>
import { computed } from 'vue'
import frontendConfig from '../../comun/config/frontend-config.json'
import { toKebab } from '../../comun/utils/slug'

const appTitle = computed(() => frontendConfig?.system?.appTitle || 'Sistema')

// Las mismas entidades que muestra el menú lateral
const secciones = computed(() => (frontendConfig?.entities || [])
  .filter(entity => entity?.showInMenu !== false)
  .map(entity => ({
    ruta: `/${toKebab(entity.routeSlug || entity.name || 'item')}`,
    titulo: entity.menuLabel || entity.displayName || entity.name,
    icono: entity.menuIcon || 'mdi-table',
    campos: (entity.fields || []).length
  })))
</script>

<style scoped>
.home-dashboard {
  padding-bottom: 32px;
}

.hero-card {
  padding: 20px 24px;
}

.hero-text {
  display: flex;
  gap: 14px;
  align-items: center;
}

.hero-text h1 {
  margin: 0;
  font-size: 1.4rem;
}

.hero-text p {
  margin: 4px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.hero-icon,
.seccion-icon {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.seccion-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
}

.seccion-titulo {
  font-weight: 600;
}

.seccion-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
}
</style>
