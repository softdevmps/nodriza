// src/stores/menu.store.js
import { reactive } from 'vue'
import menuService from '../../modulos/menu/menu.service.js'

const state = reactive({
    tree: [],
    loading: false
})

async function cargarMenuTree() {
    state.loading = true
    try {
        const response = await menuService.getSidebarTree()
        state.tree = response.data ?? []   // 👈 CLAVE
    } catch (error) {
        console.error('[MenuStore] Error cargando menú:', error)
        state.tree = []
    } finally {
        state.loading = false
    }
}

function limpiarMenu() {
    state.tree = []
}

export function useMenuStore() {
    return {
        state,
        cargarMenuTree,
        limpiarMenu
    }
}
