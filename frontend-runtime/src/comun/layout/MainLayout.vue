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
import frontendConfig from '../config/frontend-config.json'
import { toKebab } from '../utils/slug.js'

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

  entities.forEach(entity => {
    items.push({
      id: entity.entityId ?? entity.id ?? entity.name,
      titulo: prettyTitle(entity.menuLabel || entity.displayName || entity.name),
      icono: entity.menuIcon || 'mdi-table',
      ruta: `/${entityRoute(entity)}`
    })
  })

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
