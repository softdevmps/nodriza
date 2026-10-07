<template>
  <v-list-group v-if="hasChildren" v-model="opened" class="sidebar-group">
    <template #activator="{ props }">
      <v-list-item v-bind="props" class="sidebar-item" :style="indentStyle">
        <template #prepend>
          <v-icon :size="iconSize">{{ item.icono || 'mdi-folder' }}</v-icon>
        </template>
        <v-list-item-title>
          {{ item.titulo }}
        </v-list-item-title>
      </v-list-item>
    </template>

    <SidebarItem
      v-for="child in item.children"
      :key="child.id"
      :item="child"
      :depth="depth + 1"
      :active-path="activePath"
    />
  </v-list-group>

  <v-list-item
    v-else
    :to="item.ruta || undefined"
    router
    link
    active-class="sidebar-active"
    class="sidebar-item"
    :style="indentStyle"
  >
    <template #prepend>
      <v-icon :size="iconSize">{{ item.icono || 'mdi-circle-small' }}</v-icon>
    </template>
    <v-list-item-title>
      {{ item.titulo }}
    </v-list-item-title>
  </v-list-item>
</template>

<script setup>
import { computed, ref, watch } from 'vue'

defineOptions({ name: 'SidebarItem' })

const props = defineProps({
  item: {
    type: Object,
    required: true
  },
  depth: {
    type: Number,
    default: 0
  },
  activePath: {
    type: String,
    default: ''
  }
})

const hasChildren = computed(() => props.item?.children?.length > 0)

function hasActiveDescendant(item, path) {
  if (!item?.children?.length) return false
  for (const child of item.children) {
    if (child.ruta === path) return true
    if (hasActiveDescendant(child, path)) return true
  }
  return false
}

const opened = ref(false)

const indentStyle = computed(() => ({
  paddingLeft: props.depth === 0 ? '16px' : '8px'
}))

const iconSize = computed(() => (props.depth >= 1 ? 18 : 20))

watch(
  () => props.activePath,
  () => {
    if (hasActiveDescendant(props.item, props.activePath)) {
      opened.value = true
    }
  },
  { immediate: true }
)
</script>

<style scoped>
.sidebar-item {
  min-height: 36px;
}

.sidebar-group :deep(.v-list-group__items .v-list-item) {
  padding-top: 2px;
  padding-bottom: 2px;
}
</style>
