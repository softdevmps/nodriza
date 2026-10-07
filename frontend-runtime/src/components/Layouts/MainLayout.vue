<template>
  <v-app>
    <AppHeader @toggle-drawer="drawer = !drawer" />

    <AppSidebar
      :drawer="drawer"
      :menu="menuItems"
      @update:drawer="drawer = $event"
    />

    <v-main class="app-main">
      <div class="app-scroll">
        <router-view />
      </div>
    </v-main>
  </v-app>
</template>

<script setup>
import { computed, ref } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug.js'

const drawer = ref(true)

function entityRoute(entity) {
  return toKebab(entity?.routeSlug || entity?.name || 'item')
}

function prettyTitle(value) {
  if (!value) return ''
  return String(value)
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim()
}

const menuItems = computed(() => {
  const entities = (frontendConfig?.entities || []).filter(e => e?.showInMenu !== false)

  const items = [
    {
      id: 'home',
      titulo: 'Home',
      icono: 'mdi-home',
      ruta: '/home'
    }
  ]

  const incidentesGroup = {
    id: 'group-incidentes',
    titulo: 'Incidentes',
    icono: 'mdi-folder-multiple-outline',
    children: []
  }

  const isIncidenteRelated = entity => {
    const name = String(entity?.name || '').toLowerCase()
    const label = String(entity?.menuLabel || '').toLowerCase()
    const slug = String(entityRoute(entity) || '').toLowerCase()
    return (
      name.includes('incidente') ||
      label.includes('incidente') ||
      slug.includes('incidente') ||
      name.includes('catalogohechos') ||
      label.includes('catalogohechos')
    )
  }

  entities.forEach(entity => {
    const menuItem = {
      id: entity.entityId ?? entity.id ?? entity.name,
      titulo: prettyTitle(entity.menuLabel || entity.displayName || entity.name),
      icono: entity.menuIcon || 'mdi-table',
      ruta: `/${entityRoute(entity)}`
    }

    if (isIncidenteRelated(entity)) {
      incidentesGroup.children.push(menuItem)
    } else {
      items.push(menuItem)
    }
  })

  if (incidentesGroup.children.length) {
    items.push(incidentesGroup)
  }

  return items
})
</script>

<style scoped>
.app-main {
  height: calc(100vh - var(--v-layout-top));
  overflow: hidden;
}

.app-scroll {
  height: 100%;
  overflow-y: auto;
  padding: 16px;
}
</style>
