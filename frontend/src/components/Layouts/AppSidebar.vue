<template>
  <v-navigation-drawer
    app
    :model-value="drawer"
    @update:model-value="emit('update:drawer', $event)"
    width="280"
    class="sidebar"
  >
    <div class="sidebar-header">
      <v-icon color="primary" size="28">mdi-view-dashboard</v-icon>
      <span class="sidebar-title">{{ appTitle }}</span>
    </div>

    <v-divider />

    <v-list nav density="compact" class="sidebar-list">
      <SidebarItem
        v-for="item in menu"
        :key="item.id"
        :item="item"
        :depth="0"
        :active-path="route.path"
      />
    </v-list>
  </v-navigation-drawer>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import SidebarItem from './SidebarItem.vue'
import frontendConfig from '../../config/frontend-config.json'
import { useMenuStore } from '../../store/menu.store.js'

const props = defineProps({
  drawer: {
    type: Boolean,
    required: true
  },
  menu: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:drawer'])
const route = useRoute()
const { state, cargarMenuTree } = useMenuStore()

const appTitle = computed(() => frontendConfig?.system?.appTitle || 'Sistema')

const menu = computed(() => state.tree)

onMounted(() => {
  if (!state.tree.length) {
    cargarMenuTree()
  }
})
</script>

<style scoped>
.sidebar {
  border-right: 1px solid var(--sb-border);
  background: var(--sb-surface);
}

.sidebar-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px;
}

.sidebar-title {
  font-weight: 600;
  font-size: 1.05rem;
  font-family: var(--sb-font-display);
}

.sidebar-list {
  padding: 12px;
}

.sidebar-active {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.16), rgba(14, 165, 233, 0.12));
  color: var(--sb-primary);
  box-shadow: 0 6px 14px rgba(37, 99, 235, 0.18);
  position: relative;
}

.v-icon {
  opacity: 0.85;
}

.sidebar :deep(.sidebar-item) {
  margin: 4px 0;
  border-radius: 12px;
  min-height: 38px;
  transition: background 0.15s ease, transform 0.15s ease;
}

.sidebar :deep(.sidebar-item:hover) {
  background: rgba(37, 99, 235, 0.08);
}

.sidebar :deep(.sidebar-item .v-list-item__prepend) {
  margin-inline-end: 0;
  min-width: 28px;
  justify-content: center;
}

.sidebar :deep(.sidebar-item .v-list-item-title) {
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar :deep(.sidebar-item .v-icon) {
  color: #64748b;
}

.sidebar :deep(.sidebar-active .v-icon),
.sidebar :deep(.sidebar-active .v-list-item-title) {
  color: var(--sb-primary);
}

.sidebar :deep(.sidebar-active)::before {
  content: '';
  position: absolute;
  left: 6px;
  top: 8px;
  bottom: 8px;
  width: 3px;
  border-radius: 999px;
  background: var(--sb-primary);
}
</style>
