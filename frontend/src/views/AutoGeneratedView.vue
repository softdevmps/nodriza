<template>
  <component :is="currentView" v-if="currentView" />

  <v-container v-else fluid>
    <h1 class="text-h5">Vista no encontrada</h1>
    <p class="text-body-2">Ruta: {{ route.path }}</p>
  </v-container>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useMenuStore } from '../store/menu.store.js'
import { normalizePath, toSafeName } from '../utils/menuNaming.js'

const route = useRoute()
const { state, cargarMenuTree } = useMenuStore()

const currentView = ref(null)
const viewModules = import.meta.glob('./**/*.vue')

function findMenuByRoute(items, path, parentTitle = null) {
  for (const item of items) {
    if (normalizePath(item.ruta) === path) {
      return { item, parentTitle }
    }

    if (item.children?.length) {
      const found = findMenuByRoute(item.children, path, item.titulo)
      if (found) return found
    }
  }

  return null
}

async function resolveView() {
  currentView.value = null

  if (!state.tree.length) {
    await cargarMenuTree()
  }

  const targetPath = normalizePath(route.path)
  const match = findMenuByRoute(state.tree, targetPath)

  if (!match?.parentTitle) return

  const parentName = toSafeName(match.parentTitle)
  const childName = toSafeName(match.item.titulo)
  const key = `./${parentName}/${childName}.vue`

  const loader = viewModules[key]
  if (!loader) return

  const module = await loader()
  currentView.value = module.default
}

watch(
  () => route.path,
  () => {
    resolveView()
  },
  { immediate: true }
)
</script>
