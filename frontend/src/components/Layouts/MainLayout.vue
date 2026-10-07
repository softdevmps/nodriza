<template>
  <v-app>
    <AppHeader @toggle-drawer="drawer = !drawer" />

    <AppSidebar
      :drawer="drawer"
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
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import { useMenuStore } from '../../store/menu.store.js'

const drawer = ref(true)

const route = useRoute()
const router = useRouter()
const { state, cargarMenuTree } = useMenuStore()

function normalizePath(path) {
  if (!path) return ''
  if (!path.startsWith('/')) path = `/${path}`
  if (path.length > 1 && path.endsWith('/')) path = path.slice(0, -1)
  return path.trim().toLowerCase()
}

function routeMatches(menuRoute, currentPath) {
  const menuPath = normalizePath(menuRoute)
  const current = normalizePath(currentPath)
  if (!menuPath) return false
  if (current === menuPath) return true
  return current.startsWith(`${menuPath}/`)
}

function isRouteInMenu(items, currentPath) {
  for (const item of items || []) {
    if (item?.ruta && routeMatches(item.ruta, currentPath)) return true
    if (item?.children?.length && isRouteInMenu(item.children, currentPath)) return true
  }
  return false
}

function hasMenuRoute(items, targetPath) {
  const normalized = normalizePath(targetPath)
  for (const item of items || []) {
    if (item?.ruta) {
      const route = normalizePath(item.ruta)
      if (route === normalized) return true
      if (routeMatches(route, normalized) || routeMatches(normalized, route)) return true
    }
    if (item?.children?.length && hasMenuRoute(item.children, targetPath)) return true
  }
  return false
}

function findFirstRoute(items) {
  for (const item of items || []) {
    if (item?.ruta) return item.ruta
    if (item?.children?.length) {
      const found = findFirstRoute(item.children)
      if (found) return found
    }
  }
  return null
}

onMounted(() => {
  if (!state.tree.length) {
    cargarMenuTree()
  }
})

watch(
  () => [state.loading, state.tree, route.path],
  () => {
    if (state.loading) return
    if (!state.tree.length) return
    if (isRouteInMenu(state.tree, route.path)) return

    if (route.meta?.menuBase && hasMenuRoute(state.tree, route.meta.menuBase)) {
      return
    }

    const fallback = findFirstRoute(state.tree) || '/home'
    if (route.path !== fallback) {
      router.replace(fallback)
    }
  },
  { deep: true }
)
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
