<template>
  <v-container fluid :style="themeStyle" :class="['runtime-container', uiMode]">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-database</v-icon>
          </div>
          <div>
            <h2 class="mb-1">{{ frontendConfig?.system?.appTitle || sistema?.name || 'Sistema' }}</h2>
            <span class="sb-page-subtitle text-body-2">
              /s/{{ slug }}{{ entitySlug ? `/${entitySlug}` : '' }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn class="sb-btn ghost" variant="text" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
        <v-btn class="sb-btn primary" color="primary" :disabled="!entidadSeleccionada" @click="nuevoRegistro">
          <v-icon left>mdi-plus</v-icon>
          Nuevo registro
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="3">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-folder-outline</v-icon>
            <span class="text-h6">Entidades</span>
          </v-card-title>
          <v-divider />
          <v-list :density="uiDensity">
            <v-list-item
              v-for="entidad in runtimeEntities"
              :key="entidad.id"
              :active="entidadSeleccionada?.id === entidad.id"
              :prepend-icon="entidadMenuIcon(entidad)"
              @click="irEntidad(entidad)"
            >
              <v-list-item-title>{{ entidadLabel(entidad) }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>

      <v-col cols="12" md="9">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-table</v-icon>
              <span class="text-h6">{{ entidadTitulo }}</span>
            </div>
            <v-btn icon variant="text" @click="cargarDatos" :disabled="!entidadSeleccionada">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />

          <v-row v-if="showSearch || showFilters" class="px-4 py-2" dense>
            <v-col v-if="showSearch" cols="12" md="4">
              <v-text-field
                v-model="search"
                label="Buscar"
                clearable
                prepend-inner-icon="mdi-magnify"
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-select
                v-model="filterField"
                :items="filterFields"
                item-title="title"
                item-value="value"
                label="Filtrar por"
                clearable
                :density="uiDensity"
              />
            </v-col>
            <v-col v-if="showFilters" cols="12" md="4">
              <v-text-field
                v-model="filterValue"
                label="Valor"
                :disabled="!filterField"
                clearable
                :density="uiDensity"
              />
            </v-col>
          </v-row>

          <v-alert v-if="error" type="error" variant="tonal" class="ma-4">
            {{ error }}
          </v-alert>

          <div v-if="loading" class="pa-4">
            Cargando...
          </div>

          <div v-else-if="!entidadSeleccionada" class="pa-4">
            Selecciona una entidad para ver los registros.
          </div>

          <v-data-table
            v-else
            :headers="headers"
            :items="paginatedRegistros"
            class="table"
            :density="uiDensity"
            :fixed-header="listStickyHeader"
            :height="listStickyHeader ? 420 : undefined"
            :no-data-text="entityMessages.empty"
            hover
          >
            <template #item="{ item, columns }">
              <tr>
                <td v-for="col in columns" :key="col.key">
                  <template v-if="col.key === 'actions'">
                    <v-tooltip text="Editar">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="primary"
                          variant="text"
                          @click="editarRegistro(item.raw || item)"
                        >
                          <v-icon>mdi-pencil</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip v-if="enableDuplicate" text="Duplicar">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="blue"
                          variant="text"
                          @click="duplicarRegistro(item.raw || item)"
                        >
                          <v-icon>mdi-content-copy</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip v-if="quickToggleField" :text="`Toggle ${quickToggleField.label}`">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="teal"
                          variant="text"
                          @click="toggleQuickField(item.raw || item)"
                        >
                          <v-icon>mdi-toggle-switch</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                    <v-tooltip text="Eliminar">
                      <template #activator="{ props }">
                        <v-btn
                          v-bind="props"
                          icon
                          size="small"
                          color="red"
                          variant="text"
                          @click="eliminarRegistro(item.raw || item)"
                        >
                          <v-icon>mdi-delete</v-icon>
                        </v-btn>
                      </template>
                    </v-tooltip>
                  </template>
                  <template v-else>
                    <template v-if="formattedCell(item.raw || item, col).isChip">
                      <v-chip size="small" :color="formattedCell(item.raw || item, col).color">
                        {{ formattedCell(item.raw || item, col).text }}
                      </v-chip>
                    </template>
                    <template v-else>
                      {{ formattedCell(item.raw || item, col).text }}
                    </template>
                  </template>
                </td>
              </tr>
            </template>
          </v-data-table>

          <div v-if="listShowTotals" class="px-4 pt-2 text-caption text-medium-emphasis">
            Total: {{ sortedRegistros.length }} registros
          </div>

          <v-row class="px-4 pb-4 pt-2 align-center" dense>
            <v-col cols="12" md="4">
              <v-select
                v-model="itemsPerPage"
                :items="itemsPerPageOptions"
                label="Filas por pagina"
                :density="uiDensity"
              />
            </v-col>
            <v-col cols="12" md="8" class="d-flex justify-end">
              <v-pagination v-model="page" :length="pageCount" :density="uiDensity" />
            </v-col>
          </v-row>
        </v-card>
      </v-col>
    </v-row>

    <RegistroDialog
      v-model="mostrarDialog"
      :record="registroSeleccionado"
      :fields="formFields"
      :fk-options="fkOptions"
      :layout="frontendEntityConfig?.formLayout || 'single'"
      :density="uiDensity"
      :messages="entityMessages"
      :confirm-save="confirmSave"
      :mode="dialogMode"
      :system-id="sistema?.id"
      :entity-id="entidadSeleccionada?.id"
      @guardado="cargarDatos"
      @crear-fk="abrirFkDialog"
    />

    <RegistroDialog
      v-model="mostrarFkDialog"
      :record="null"
      :fields="fkDialogFields"
      :density="uiDensity"
      :system-id="sistema?.id"
      :entity-id="fkEntityId"
      @guardado="onFkGuardado"
    />
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import entidadService from '../../api/entidad.service.js'
import campoService from '../../api/campo.service.js'
import datosService from '../../api/datos.service.js'
import relacionService from '../../api/relacion.service.js'
import frontendConfigService from '../../api/frontendConfig.service.js'
import baseFrontendConfig from '../../config/frontend-config.json'
import RegistroDialog from '../../components/sistemas/RegistroDialog.vue'
import { toKebab } from '../../utils/slug.js'

const route = useRoute()
const router = useRouter()

const sistema = ref(null)
const entidades = ref([])
const entidadSeleccionada = ref(null)
const campos = ref([])
const registros = ref([])
const loading = ref(false)
const error = ref(null)
const relaciones = ref([])
const fkOptions = ref({})
const mostrarFkDialog = ref(false)
const fkDialogFields = ref([])
const fkEntityId = ref(null)

const mostrarDialog = ref(false)
const registroSeleccionado = ref(null)
const dialogMode = ref('create')
const frontendConfig = ref(JSON.parse(JSON.stringify(baseFrontendConfig)))

const slug = computed(() => String(route.params.slug || ''))
const entitySlug = computed(() => String(route.params.entity || ''))
const search = ref('')
const filterField = ref(null)
const filterValue = ref('')
const itemsPerPage = ref(10)
const itemsPerPageOptions = computed(() => {
  const options = frontendConfig.value?.system?.itemsPerPageOptions
  return options && options.length ? options : [10, 20, 50, 100]
})
const page = ref(1)

const uiDensity = computed(() => frontendConfig.value?.system?.density || 'comfortable')
const uiMode = computed(() => frontendConfig.value?.system?.uiMode || 'enterprise')
const locale = computed(() => frontendConfig.value?.system?.locale || 'es-AR')
const currency = computed(() => frontendConfig.value?.system?.currency || 'ARS')
const themeStyle = computed(() => ({
  '--sb-primary': frontendConfig.value?.system?.primaryColor || '#2563eb',
  '--sb-secondary': frontendConfig.value?.system?.secondaryColor || '#0ea5e9',
  '--sb-font': frontendConfig.value?.system?.fontFamily || 'Inter, system-ui, -apple-system, Segoe UI, sans-serif'
}))

const entityConfigList = computed(() => frontendConfig.value?.entities || [])
const entityConfigMap = computed(() => {
  const map = new Map()
  entityConfigList.value.forEach(cfg => {
    map.set(cfg.entityId, cfg)
  })
  return map
})

const runtimeEntities = computed(() => {
  const map = new Map(entidades.value.map(ent => [ent.id, ent]))
  const result = []
  const used = new Set()

  entityConfigList.value.forEach(cfg => {
    const ent = map.get(cfg.entityId)
    if (!ent) return
    used.add(ent.id)
    if (cfg.showInMenu === false) return
    result.push({ ...ent, _config: cfg })
  })

  entidades.value.forEach(ent => {
    if (used.has(ent.id)) return
    result.push({ ...ent, _config: null })
  })

  return result
})

const frontendEntityConfig = computed(() => {
  if (!entidadSeleccionada.value) return null
  return entityConfigMap.value.get(entidadSeleccionada.value.id) || null
})

const fieldConfigMap = computed(() => {
  const map = new Map()
  const fields = frontendEntityConfig.value?.fields || []
  fields.forEach(f => {
    map.set(f.fieldId, f)
  })
  return map
})

const orderedFields = computed(() => {
  const allFields = campos.value || []
  const configFields = frontendEntityConfig.value?.fields || []
  if (!configFields.length) return allFields

  const map = new Map(allFields.map(field => [field.id, field]))
  const ordered = []
  const configuredIds = new Set()

  configFields.forEach(cfg => {
    const field = map.get(cfg.fieldId)
    if (!field) return
    ordered.push(field)
    configuredIds.add(cfg.fieldId)
  })

  allFields.forEach(field => {
    if (!configuredIds.has(field.id)) {
      ordered.push(field)
    }
  })

  return ordered
})

const listFields = computed(() => {
  const map = fieldConfigMap.value
  return orderedFields.value
    .filter(field => {
      const cfg = map.get(field.id)
      return cfg ? cfg.showInList !== false : true
    })
    .map(field => {
      const cfg = map.get(field.id)
      return {
        ...field,
        label: cfg?.label || field.name || field.columnName,
        format: cfg?.format || '',
        quickToggle: cfg?.quickToggle || false
      }
    })
})

const formFields = computed(() => {
  const map = fieldConfigMap.value
  return orderedFields.value
    .filter(field => {
      const cfg = map.get(field.id)
      if (cfg) return cfg.showInForm !== false
      if (field.isIdentity) return false
      return true
    })
    .map(field => {
      const cfg = map.get(field.id)
      return {
        ...field,
        label: cfg?.label || field.name || field.columnName,
        placeholder: cfg?.placeholder || '',
        helpText: cfg?.helpText || '',
        inputType: cfg?.inputType || '',
        section: cfg?.section || 'General',
        min: cfg?.min ?? null,
        max: cfg?.max ?? null,
        pattern: cfg?.pattern || '',
        quickToggle: cfg?.quickToggle || false
      }
    })
})

const filterableFields = computed(() => {
  const map = fieldConfigMap.value
  return orderedFields.value
    .filter(field => {
      const cfg = map.get(field.id)
      return cfg ? cfg.showInFilter !== false : true
    })
    .map(field => ({
      ...field,
      label: map.get(field.id)?.label || field.name || field.columnName
    }))
})

const headers = computed(() => {
  if (!listFields.value.length) return []
  const cols = listFields.value.map(field => ({
    title: field.label || field.name || field.columnName,
    key: field.columnName,
    fieldId: field.id,
    format: field.format || ''
  }))
  cols.push({ title: 'Acciones', key: 'actions', sortable: false })
  return cols
})

const listFieldMap = computed(() => {
  const map = new Map()
  listFields.value.forEach(field => {
    map.set(field.columnName, field)
  })
  return map
})

const filterFields = computed(() => {
  return filterableFields.value.map(field => ({
    title: field.label || field.name || field.columnName,
    value: field.columnName
  }))
})

const listStickyHeader = computed(() => frontendEntityConfig.value?.listStickyHeader === true)
const listShowTotals = computed(() => frontendEntityConfig.value?.listShowTotals !== false)

const enableDuplicate = computed(() => frontendEntityConfig.value?.enableDuplicate !== false)
const confirmSave = computed(() => frontendEntityConfig.value?.confirmSave !== false)
const confirmDelete = computed(() => frontendEntityConfig.value?.confirmDelete !== false)
const entityMessages = computed(() => frontendEntityConfig.value?.messages || {
  empty: 'No hay registros todavia.',
  error: 'Ocurrio un error al procesar la solicitud.',
  successCreate: 'Registro creado.',
  successUpdate: 'Registro actualizado.',
  successDelete: 'Registro eliminado.'
})

const quickToggleField = computed(() => {
  return formFields.value.find(field => field.quickToggle && field.dataType === 'bool') || null
})

const defaultSortField = computed(() => {
  const fieldId = frontendEntityConfig.value?.defaultSortFieldId
  if (!fieldId) return null
  return (campos.value || []).find(field => field.id === fieldId) || null
})

const defaultSortDirection = computed(() => {
  const dir = frontendEntityConfig.value?.defaultSortDirection || 'asc'
  return dir.toLowerCase() === 'desc' ? 'desc' : 'asc'
})

const filteredRegistros = computed(() => {
  let items = [...(registros.value || [])]

  if (search.value) {
    const term = search.value.toString().toLowerCase()
    items = items.filter(row =>
      Object.values(row).some(value =>
        value !== null &&
        value !== undefined &&
        value.toString().toLowerCase().includes(term)
      )
    )
  }

  if (filterField.value && filterValue.value) {
    const term = filterValue.value.toString().toLowerCase()
    items = items.filter(row => {
      const value = row[filterField.value]
      return value !== null && value !== undefined && value.toString().toLowerCase().includes(term)
    })
  }

  return items
})

const sortedRegistros = computed(() => {
  const items = [...filteredRegistros.value]
  if (!defaultSortField.value) return items

  const key = defaultSortField.value.columnName
  const dir = defaultSortDirection.value === 'desc' ? -1 : 1

  items.sort((a, b) => {
    const va = a?.[key]
    const vb = b?.[key]
    if (va == null && vb == null) return 0
    if (va == null) return -1 * dir
    if (vb == null) return 1 * dir

    if (typeof va === 'number' && typeof vb === 'number') {
      return (va - vb) * dir
    }

    const sa = va.toString().toLowerCase()
    const sb = vb.toString().toLowerCase()
    if (sa < sb) return -1 * dir
    if (sa > sb) return 1 * dir
    return 0
  })

  return items
})

const pageCount = computed(() => {
  const total = sortedRegistros.value.length
  return total === 0 ? 1 : Math.ceil(total / itemsPerPage.value)
})

const paginatedRegistros = computed(() => {
  const start = (page.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return sortedRegistros.value.slice(start, end)
})

const pkField = computed(() => campos.value.find(field => field.isPrimaryKey))

const showSearch = computed(() => frontendConfig.value?.system?.showSearch !== false)
const showFilters = computed(() => frontendConfig.value?.system?.showFilters !== false)

async function cargarSistema() {
  const { data } = await sistemaService.getBySlug(slug.value)
  sistema.value = data
}

async function cargarEntidades() {
  if (!sistema.value) return
  const { data } = await entidadService.getBySystemRuntime(sistema.value.id)
  entidades.value = data
}

async function cargarRelaciones() {
  if (!sistema.value) return
  const { data } = await relacionService.getBySystem(sistema.value.id)
  relaciones.value = data
}

async function cargarFrontendConfig() {
  if (!sistema.value) return
  try {
    const { data } = await frontendConfigService.getBySystem(sistema.value.id)
    frontendConfig.value = data
  } catch {
    // keep base config
  } finally {
    normalizeFrontendConfig()
  }
}

function normalizeFrontendConfig() {
  if (!frontendConfig.value?.system) {
    frontendConfig.value.system = {}
  }
  const sys = frontendConfig.value.system
  sys.primaryColor = sys.primaryColor || '#2563eb'
  sys.secondaryColor = sys.secondaryColor || '#0ea5e9'
  sys.density = sys.density || 'comfortable'
  sys.fontFamily = sys.fontFamily || 'Inter, system-ui, -apple-system, Segoe UI, sans-serif'
  sys.uiMode = sys.uiMode || 'enterprise'
  sys.locale = sys.locale || 'es-AR'
  sys.currency = sys.currency || 'ARS'

  if (!Array.isArray(frontendConfig.value.entities)) {
    frontendConfig.value.entities = []
  }

  frontendConfig.value.entities.forEach(entity => {
    if (entity.showInMenu === undefined) entity.showInMenu = true
    if (!entity.menuIcon) entity.menuIcon = 'mdi-table'
    if (entity.routeSlug === undefined) entity.routeSlug = ''
    if (entity.listStickyHeader === undefined) entity.listStickyHeader = false
    if (entity.listShowTotals === undefined) entity.listShowTotals = true
    if (!entity.defaultSortDirection) entity.defaultSortDirection = 'asc'
    if (!entity.formLayout) entity.formLayout = 'single'
    if (entity.confirmSave === undefined) entity.confirmSave = true
    if (entity.confirmDelete === undefined) entity.confirmDelete = true
    if (entity.enableDuplicate === undefined) entity.enableDuplicate = true
    if (!entity.messages) {
      entity.messages = {
        empty: 'No hay registros todavia.',
        error: 'Ocurrio un error al procesar la solicitud.',
        successCreate: 'Registro creado.',
        successUpdate: 'Registro actualizado.',
        successDelete: 'Registro eliminado.'
      }
    }
    if (!Array.isArray(entity.fields)) entity.fields = []
    entity.fields.forEach(field => {
      if (field.placeholder === undefined) field.placeholder = ''
      if (field.helpText === undefined) field.helpText = ''
      if (field.inputType === undefined) field.inputType = ''
      if (field.section === undefined) field.section = 'General'
      if (field.format === undefined) field.format = ''
      if (field.min === undefined) field.min = null
      if (field.max === undefined) field.max = null
      if (field.pattern === undefined) field.pattern = ''
      if (field.quickToggle === undefined) field.quickToggle = false
    })
  })
}

function resolverEntidad() {
  if (!runtimeEntities.value.length) {
    entidadSeleccionada.value = null
    return
  }

  const target = entitySlug.value
    ? runtimeEntities.value.find(ent => entidadRoute(ent) === entitySlug.value)
    : runtimeEntities.value[0]

  if (!target) {
    entidadSeleccionada.value = runtimeEntities.value[0]
    router.replace(`/s/${slug.value}/${entidadRoute(entidadSeleccionada.value)}`)
    return
  }

  entidadSeleccionada.value = target

  if (!entitySlug.value && entidadSeleccionada.value) {
    router.replace(`/s/${slug.value}/${entidadRoute(entidadSeleccionada.value)}`)
  }
}

async function cargarCampos() {
  if (!sistema.value || !entidadSeleccionada.value) return
  const { data } = await campoService.getByEntity(sistema.value.id, entidadSeleccionada.value.id)
  campos.value = data
}

async function cargarDatos() {
  if (!sistema.value || !entidadSeleccionada.value) return
  try {
    const { data } = await datosService.listar(sistema.value.id, entidadSeleccionada.value.id)
    registros.value = data
    page.value = 1
    error.value = null
  } catch (err) {
    error.value =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      entityMessages.value?.error ||
      'No se pudo cargar los registros.'
  }
}

function entidadLabel(entidad) {
  const config = entityConfigMap.value.get(entidad.id)
  return config?.menuLabel || entidad.displayName || entidad.name
}

const entidadTitulo = computed(() => {
  const entidad = entidadSeleccionada.value
  if (!entidad) return 'Registros'
  const config = entityConfigMap.value.get(entidad.id)
  return config?.displayName || entidad.displayName || entidad.name || 'Registros'
})

function entidadMenuIcon(entidad) {
  const config = entityConfigMap.value.get(entidad.id)
  return config?.menuIcon || 'mdi-database'
}

function entidadRoute(entidad) {
  const config = entityConfigMap.value.get(entidad.id)
  const slugValue = (config?.routeSlug || '').trim()
  if (slugValue) return toKebab(slugValue)
  return toKebab(entidad.name || entidad.tableName || '')
}

function formattedCell(row, column) {
  const field = listFieldMap.value.get(column.key)
  const value = row?.[column.key]
  if (!field) {
    return { text: value ?? '', isChip: false, color: 'primary' }
  }
  return formatValue(value, field)
}

function formatValue(value, field) {
  const format = (field.format || '').toLowerCase()
  if (value === null || value === undefined) {
    return { text: '', isChip: false, color: 'primary' }
  }

  if (format === 'uppercase') {
    return { text: value.toString().toUpperCase(), isChip: false, color: 'primary' }
  }
  if (format === 'lowercase') {
    return { text: value.toString().toLowerCase(), isChip: false, color: 'primary' }
  }
  if (format === 'date') {
    const date = new Date(value)
    return { text: isNaN(date.getTime()) ? value.toString() : date.toLocaleDateString(locale.value), isChip: false, color: 'primary' }
  }
  if (format === 'datetime') {
    const date = new Date(value)
    return { text: isNaN(date.getTime()) ? value.toString() : date.toLocaleString(locale.value), isChip: false, color: 'primary' }
  }
  if (format === 'money') {
    const num = Number(value)
    if (Number.isNaN(num)) {
      return { text: value.toString(), isChip: false, color: 'primary' }
    }
    return {
      text: new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value }).format(num),
      isChip: false,
      color: 'primary'
    }
  }
  if (format === 'boolean') {
    const text = value ? 'Si' : 'No'
    return { text, isChip: true, color: value ? 'green' : 'grey' }
  }
  if (format === 'badge') {
    return { text: value.toString(), isChip: true, color: 'primary' }
  }

  return { text: value.toString(), isChip: false, color: 'primary' }
}

function elegirDisplayField(fields, pkField) {
  const byName = fields.find(f => {
    const name = f.columnName?.toLowerCase()
    return name === 'nombre' || name === 'name'
  })
  if (byName) return byName

  const firstString = fields.find(f => f.dataType?.toLowerCase() === 'string')
  if (firstString) return firstString

  return pkField || fields[0]
}

async function cargarFkOptions() {
  fkOptions.value = {}
  if (!sistema.value || !entidadSeleccionada.value) return

  const rels = relaciones.value.filter(r =>
    r.sourceEntityId === entidadSeleccionada.value.id && r.foreignKey
  )

  for (const rel of rels) {
    const target = entidades.value.find(e => e.id === rel.targetEntityId)
    if (!target) continue

    const { data: targetFields } = await campoService.getByEntity(sistema.value.id, target.id)
    if (!targetFields?.length) continue

    const pk = targetFields.find(f => f.isPrimaryKey) || targetFields[0]
    const display = elegirDisplayField(targetFields, pk)

    const { data: rows } = await datosService.listar(sistema.value.id, target.id)
    const options = (rows || []).map(row => ({
      value: row[pk.columnName],
      title: row[display.columnName] ?? row[pk.columnName]
    }))

    fkOptions.value[rel.foreignKey] = {
      options,
      targetEntityId: target.id,
      pkField: pk.columnName,
      displayField: display.columnName,
      fields: targetFields
    }
  }
}

async function inicializar() {
  loading.value = true
  error.value = null
  try {
    await cargarSistema()
    await cargarEntidades()
    await cargarRelaciones()
    await cargarFrontendConfig()
    resolverEntidad()
    await cargarCampos()
    await cargarDatos()
    await cargarFkOptions()
    if (frontendConfig.value?.system?.defaultItemsPerPage) {
      itemsPerPage.value = frontendConfig.value.system.defaultItemsPerPage
    }
  } catch (err) {
    error.value =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      entityMessages.value?.error ||
      'No se pudo cargar el sistema.'
  } finally {
    loading.value = false
  }
}

function irEntidad(entidad) {
  router.push(`/s/${slug.value}/${entidadRoute(entidad)}`)
}

function abrirFkDialog(field) {
  const meta = fkOptions.value?.[field.columnName]
  if (!meta) return
  fkEntityId.value = meta.targetEntityId
  fkDialogFields.value = meta.fields || []
  mostrarFkDialog.value = true
}

async function onFkGuardado() {
  await cargarFkOptions()
}

function nuevoRegistro() {
  dialogMode.value = 'create'
  registroSeleccionado.value = null
  mostrarDialog.value = true
}

function editarRegistro(item) {
  dialogMode.value = 'edit'
  registroSeleccionado.value = { ...item }
  mostrarDialog.value = true
}

function duplicarRegistro(item) {
  dialogMode.value = 'duplicate'
  registroSeleccionado.value = { ...item }
  mostrarDialog.value = true
}

async function eliminarRegistro(item) {
  if (!pkField.value) {
    window.alert('Entidad sin PK, no se puede eliminar.')
    return
  }

  if (confirmDelete.value) {
    const ok = window.confirm('Eliminar registro?')
    if (!ok) return
  }

  try {
    await datosService.eliminar(sistema.value.id, entidadSeleccionada.value.id, item[pkField.value.columnName])
    await cargarDatos()
    if (entityMessages.value?.successDelete) {
      window.alert(entityMessages.value.successDelete)
    }
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      entityMessages.value?.error ||
      'No se pudo eliminar.'
    window.alert(message)
  }
}

async function toggleQuickField(item) {
  if (!quickToggleField.value || !pkField.value) return
  const key = quickToggleField.value.columnName
  const payload = { ...item, [key]: !item[key] }
  try {
    await datosService.editar(sistema.value.id, entidadSeleccionada.value.id, item[pkField.value.columnName], payload)
    await cargarDatos()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      entityMessages.value?.error ||
      'No se pudo actualizar.'
    window.alert(message)
  }
}

function volver() {
  router.push('/sistemas')
}

watch([search, filterField, filterValue, itemsPerPage], () => {
  page.value = 1
})

watch(sortedRegistros, () => {
  if (page.value > pageCount.value) {
    page.value = pageCount.value
  }
})

watch(
  () => [slug.value, entitySlug.value],
  () => {
    inicializar()
  }
)

onMounted(() => {
  inicializar()
})
</script>

<style scoped>
.card {
  border-radius: 12px;
}

.table :deep(thead th) {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.7rem;
  color: #6b7280;
}

.runtime-container {
  font-family: var(--sb-font, inherit);
}

.runtime-container.enterprise .card {
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.08);
}

.runtime-container.minimal .card {
  box-shadow: none;
  border: 1px solid #e5e7eb;
}

.runtime-container .sb-page-icon {
  background: color-mix(in srgb, var(--sb-primary, #2563eb) 12%, white);
}

.runtime-container .sb-page-icon .v-icon {
  color: var(--sb-primary, #2563eb);
}
</style>
