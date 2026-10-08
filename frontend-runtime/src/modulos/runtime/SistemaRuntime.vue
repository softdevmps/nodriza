<template>
  <v-container fluid :style="themeStyle" :class="['runtime-container', uiMode]">
    <v-row class="mb-4 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-database</v-icon>
          </div>
          <div>
            <h2 class="mb-1">{{ systemTitle }}</h2>
            <span class="sb-page-subtitle text-body-2">
              {{ entidadSeleccionada ? `/${entidadRoute(entidadSeleccionada)}` : '/' }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn class="cta-button primary" :disabled="!entidadSeleccionada" @click="nuevoRegistro">
          <v-icon left>mdi-plus</v-icon>
          Nuevo registro
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="3">
        <v-card elevation="2" class="card side-card summary-card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-chart-box-outline</v-icon>
            <span class="text-h6">Resumen</span>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="summaryItems.length" class="summary-grid">
              <div v-for="item in summaryItems" :key="item.label" class="summary-item">
                <div class="summary-icon">
                  <v-icon :color="item.color || 'primary'" size="18">{{ item.icon }}</v-icon>
                </div>
                <div>
                  <div class="summary-label">{{ item.label }}</div>
                  <div class="summary-value">{{ item.value }}</div>
                </div>
              </div>
            </div>
            <div v-else class="text-caption text-medium-emphasis">Sin datos para resumir.</div>

            <v-divider v-if="summaryMeta" class="my-3" />
            <div v-if="summaryMeta" class="summary-meta">
              <span class="summary-meta-label">Actualizado:</span>
              <span>{{ summaryMeta }}</span>
            </div>
          </v-card-text>
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
            <v-skeleton-loader type="heading, text, table" class="sb-skeleton" />
          </div>

          <div v-else-if="!entidadSeleccionada" class="pa-4">
            Selecciona una vista para ver registros.
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
            :items-per-page="-1"
            hide-default-footer
            hover
          >
            <template #item="{ item, columns }">
              <tr>
                <td v-for="col in columns" :key="col.key" :class="{ 'actions-td': col.key === 'actions' }">
                  <template v-if="col.key === 'actions'">
                    <div class="actions-cell actions-grid">
                      <v-tooltip text="Editar">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="primary" variant="text" @click="editarRegistro(item.raw || item)">
                            <v-icon>mdi-pencil</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="enableDuplicate" text="Duplicar">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="blue" variant="text" @click="duplicarRegistro(item.raw || item)">
                            <v-icon>mdi-content-copy</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip text="Copiar datos">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="indigo" variant="text" @click="copiarRegistro(item.raw || item)">
                            <v-icon>mdi-clipboard-text-outline</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip v-if="quickToggleField" :text="`Toggle ${quickToggleField.label || quickToggleField.name}`">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="teal" variant="text" @click="toggleQuickField(item.raw || item)">
                            <v-icon>mdi-toggle-switch</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                      <v-tooltip text="Eliminar">
                        <template #activator="{ props }">
                          <v-btn v-bind="props" icon size="x-small" color="red" variant="text" @click="eliminarRegistro(item.raw || item)">
                            <v-icon>mdi-delete</v-icon>
                          </v-btn>
                        </template>
                      </v-tooltip>
                    </div>
                  </template>
                  <template v-else>
                    <template v-if="formattedCell(item.raw || item, col).isChip">
                      <v-chip size="small" :color="formattedCell(item.raw || item, col).color">
                        {{ formattedCell(item.raw || item, col).text }}
                      </v-chip>
                    </template>
                    <template v-else>
                      <span
                        class="cell-text"
                        :title="formattedCell(item.raw || item, col).text"
                      >
                        {{ formattedCell(item.raw || item, col).text }}
                      </span>
                    </template>
                  </template>
                </td>
              </tr>
            </template>
          </v-data-table>

          <div v-if="listShowTotals" class="px-4 pt-2 text-caption text-medium-emphasis">
            Total: {{ totalRegistros }} registros
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
              <v-pagination
                v-model="page"
                :length="pageCount"
                density="compact"
              />
            </v-col>
          </v-row>
        </v-card>
      </v-col>
    </v-row>

    <RegistroDialog
      v-model="dialog"
      :record="registroActual"
      :fields="campos"
      :layout="formLayout"
      :density="uiDensity"
      :messages="entityMessages"
      :confirm-save="confirmSave"
      :mode="dialogMode"
      :api-route="apiRoute"
      @guardado="cargarDatos"
    />

    <v-snackbar v-model="toastOpen" :timeout="2200" :color="toastColor">
      {{ toastMessage }}
    </v-snackbar>
  </v-container>
</template>

<script setup>
import { computed, inject, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import frontendConfig from '../../comun/config/frontend-config.json'
import { toKebab } from '../../comun/utils/slug.js'
import RegistroDialog from './componentes/RegistroDialog.vue'
import runtimeApi from './runtime.service.js'

const route = useRoute()
const router = useRouter()
const colorMode = inject('colorMode', null)
const isDark = computed(() => {
  if (colorMode?.isDark?.value != null) return colorMode.isDark.value
  if (typeof localStorage !== 'undefined') {
    return localStorage.getItem('sb-theme') === 'dark'
  }
  return false
})

const config = ref(JSON.parse(JSON.stringify(frontendConfig || {})))

const registros = ref([])
// Si la API de la entidad pagina (responde X-Total-Count), registros es solo la página actual
// y la búsqueda la hace el servidor. Si no, llega todo y se filtra/pagina acá.
const paginadoServidor = ref(false)
const totalServidor = ref(0)
const loading = ref(false)
const error = ref('')

const search = ref('')
const filterField = ref(null)
const filterValue = ref('')

const page = ref(1)
const itemsPerPage = ref(10)

const dialog = ref(false)
const dialogMode = ref('create')
const registroActual = ref(null)

const toastOpen = ref(false)
const toastMessage = ref('')
const toastColor = ref('green')

const entidadSeleccionada = ref(null)

const systemTitle = computed(() => config.value?.system?.appTitle || 'Sistema')

const uiDensity = computed(() => config.value?.system?.density || 'comfortable')
const uiMode = computed(() => config.value?.system?.uiMode || 'enterprise')
const locale = computed(() => config.value?.system?.locale || 'es-AR')
const currency = computed(() => config.value?.system?.currency || 'ARS')

const lightThemeDefaults = {
  primary: '#1d4ed8',
  secondary: '#0ea5e9',
  accent: '#f97316',
  primarySoft: 'rgba(29,78,216,0.12)',
  background: '#f8fafc',
  surface: '#ffffff',
  muted: '#64748b',
  border: 'rgba(15,23,42,0.12)',
  borderSoft: 'rgba(15,23,42,0.08)',
  radius: 16,
  shadow: '0 12px 30px rgba(15, 23, 42, 0.12)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(29,78,216,0.16), rgba(14,165,233,0.08) 45%, rgba(248,250,252,0.95))',
  patternOpacity: 0.06,
  headerBg: 'rgba(255,255,255,0.9)',
  text: '#0f172a',
  textSoft: '#334155'
}

const darkThemeDefaults = {
  primary: '#1d4ed8',
  secondary: '#0ea5e9',
  accent: '#f97316',
  primarySoft: 'rgba(59,130,246,0.18)',
  background: '#0b1120',
  surface: '#0f172a',
  muted: '#94a3b8',
  border: 'rgba(148,163,184,0.28)',
  borderSoft: 'rgba(148,163,184,0.16)',
  radius: 16,
  shadow: '0 12px 30px rgba(15, 23, 42, 0.35)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(59,130,246,0.18), rgba(15,23,42,0.9) 55%)',
  patternOpacity: 0.12,
  headerBg: 'rgba(15,23,42,0.85)',
  text: '#e2e8f0',
  textSoft: '#94a3b8'
}

const themeStyle = computed(() => {
  const system = config.value?.system || {}
  const theme = config.value?.theme || {}
  const themeDark = config.value?.themeDark || {}
  const defaults = isDark.value ? darkThemeDefaults : lightThemeDefaults
  const activeTheme = isDark.value ? themeDark : theme
  const baseBrand = theme?.brand || {}
  const darkBrand = themeDark?.brand || {}
  const brand = isDark.value ? { ...baseBrand, ...darkBrand } : baseBrand
  return {
    '--sb-primary': brand.primary || system.primaryColor || defaults.primary,
    '--sb-secondary': brand.secondary || system.secondaryColor || defaults.secondary,
    '--sb-accent': brand.accent || defaults.accent,
    '--sb-primary-soft': activeTheme.primarySoft || defaults.primarySoft,
    '--sb-bg': activeTheme.background || defaults.background,
    '--sb-surface': activeTheme.surface || defaults.surface,
    '--sb-muted': activeTheme.muted || defaults.muted,
    '--sb-border': activeTheme.border || defaults.border,
    '--sb-border-soft': activeTheme.borderSoft || defaults.borderSoft,
    '--sb-radius': `${activeTheme.radius ?? theme.radius ?? defaults.radius}px`,
    '--sb-shadow': activeTheme.shadow || defaults.shadow,
    '--sb-font': activeTheme.fontBody || theme.fontBody || system.fontFamily || defaults.fontBody,
    '--sb-font-display': activeTheme.fontDisplay || theme.fontDisplay || defaults.fontDisplay,
    '--sb-gradient': activeTheme.gradient || defaults.gradient,
    '--sb-pattern-opacity': activeTheme.patternOpacity ?? defaults.patternOpacity,
    '--sb-header-bg': activeTheme.headerBg || defaults.headerBg,
    '--sb-text': activeTheme.text || defaults.text,
    '--sb-text-soft': activeTheme.textSoft || defaults.textSoft
  }
})

const entities = computed(() => config.value?.entities || [])

const runtimeEntities = computed(() => entities.value.filter(entity => entity.showInMenu !== false))

function entidadRoute(entidad) {
  return toKebab(entidad?.routeSlug || entidad?.name || entidad?.menuLabel || 'item')
}

function entidadLabel(entidad) {
  return entidad?.menuLabel || entidad?.displayName || entidad?.name || 'Entidad'
}

function entidadMenuIcon(entidad) {
  return entidad?.menuIcon || 'mdi-table'
}

const entitySlug = computed(() => route.params.entity || '')

const entidadTitulo = computed(() => entidadSeleccionada.value ? entidadLabel(entidadSeleccionada.value) : 'Entidad')

const campos = computed(() => entidadSeleccionada.value?.fields || [])

const listFields = computed(() => campos.value.filter(field => field.showInList !== false))

const pkField = computed(() => {
  return campos.value.find(f => f.isPrimaryKey) || campos.value.find(f => String(f.columnName || f.name).toLowerCase() === 'id')
})

const quickToggleField = computed(() => campos.value.find(f => f.quickToggle))

const entityMessages = computed(() => entidadSeleccionada.value?.messages || {
  empty: 'No hay registros todavia.',
  error: 'Ocurrio un error al procesar la solicitud.',
  successCreate: 'Registro creado.',
  successUpdate: 'Registro actualizado.',
  successDelete: 'Registro eliminado.'
})

const listStickyHeader = computed(() => entidadSeleccionada.value?.listStickyHeader === true)
const listShowTotals = computed(() => entidadSeleccionada.value?.listShowTotals !== false)
const formLayout = computed(() => entidadSeleccionada.value?.formLayout || 'single')
const confirmSave = computed(() => entidadSeleccionada.value?.confirmSave !== false)
const confirmDelete = computed(() => entidadSeleccionada.value?.confirmDelete !== false)
const enableDuplicate = computed(() => entidadSeleccionada.value?.enableDuplicate !== false)

const apiRoute = computed(() => (entidadSeleccionada.value ? entidadRoute(entidadSeleccionada.value) : ''))

const summaryItems = computed(() => {
  if (!entidadSeleccionada.value) return []
  return [{ label: 'Total', value: totalRegistros.value, icon: 'mdi-format-list-bulleted' }]
})

const summaryMeta = computed(() => {
  const list = registros.value || []
  if (!list.length) return ''
  const timestamps = list
    .map(item => item?.UpdateAt || item?.updateAt || item?.CreatedAt || item?.createdAt)
    .filter(Boolean)
    .map(value => new Date(value).getTime())
    .filter(value => Number.isFinite(value))
  if (!timestamps.length) return ''
  const latest = new Date(Math.max(...timestamps))
  return latest.toLocaleString(locale.value)
})

const itemsPerPageOptions = computed(() => config.value?.system?.itemsPerPageOptions || [10, 20, 50])

const showSearch = computed(() => config.value?.system?.showSearch !== false)
// El filtro por campo es del lado del navegador: con paginación del servidor solo queda "Buscar"
const showFilters = computed(() => config.value?.system?.showFilters !== false && !paginadoServidor.value)

const filterFields = computed(() => listFields.value.filter(f => f.showInFilter !== false).map(f => ({
  title: f.label || f.name || f.columnName,
  value: f.columnName
})))

const filteredRegistros = computed(() => {
  let items = [...registros.value]
  if (paginadoServidor.value) return items // el servidor ya buscó

  if (search.value) {
    const term = search.value.toLowerCase()
    items = items.filter(item => {
      return listFields.value.some(field => {
        const value = item[field.columnName]
        return value != null && value.toString().toLowerCase().includes(term)
      })
    })
  }

  if (filterField.value && filterValue.value) {
    const term = filterValue.value.toLowerCase()
    items = items.filter(item => {
      const value = item[filterField.value]
      return value != null && value.toString().toLowerCase().includes(term)
    })
  }

  return items
})

const sortedRegistros = computed(() => {
  const items = [...filteredRegistros.value]
  const entity = entidadSeleccionada.value
  if (!entity) return items

  const sortFieldId = entity.defaultSortFieldId
  const sortField = campos.value.find(f => f.fieldId === sortFieldId) || pkField.value
  const sortKey = sortField?.columnName
  const dir = entity.defaultSortDirection === 'desc' ? -1 : 1

  if (!sortKey) return items

  items.sort((a, b) => {
    const va = a[sortKey]
    const vb = b[sortKey]
    if (va == null && vb == null) return 0
    if (va == null) return -1 * dir
    if (vb == null) return 1 * dir
    if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir
    const sa = va.toString().toLowerCase()
    const sb = vb.toString().toLowerCase()
    if (sa < sb) return -1 * dir
    if (sa > sb) return 1 * dir
    return 0
  })

  return items
})

const totalRegistros = computed(() => paginadoServidor.value ? totalServidor.value : sortedRegistros.value.length)

const pageCount = computed(() => {
  const total = totalRegistros.value
  return total === 0 ? 1 : Math.ceil(total / itemsPerPage.value)
})

const paginatedRegistros = computed(() => {
  if (paginadoServidor.value) return sortedRegistros.value
  const start = (page.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return sortedRegistros.value.slice(start, end)
})

const headers = computed(() => {
  const cols = listFields.value.map(field => ({
    title: field.label || field.name || field.columnName,
    key: field.columnName
  }))

  return [
    ...cols,
    { title: 'Acciones', key: 'actions', sortable: false }
  ]
})

function normalizeConfig() {
  if (!config.value?.system) config.value.system = {}
  const sys = config.value.system
  sys.primaryColor = sys.primaryColor || '#2563eb'
  sys.secondaryColor = sys.secondaryColor || '#0ea5e9'
  sys.density = sys.density || 'comfortable'
  sys.fontFamily = sys.fontFamily || "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif"
  sys.uiMode = sys.uiMode || 'enterprise'
  sys.locale = sys.locale || 'es-AR'
  sys.currency = sys.currency || 'ARS'

  if (!Array.isArray(config.value.entities)) config.value.entities = []

  config.value.entities.forEach(entity => {
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
    router.replace(`/${entidadRoute(runtimeEntities.value[0])}`)
    return
  }

  entidadSeleccionada.value = target
  paginadoServidor.value = false
  page.value = 1
  cargarDatos()
}

function irEntidad(entidad) {
  const slug = entidadRoute(entidad)
  router.push(`/${slug}`)
}

let ultimaConsulta = 0
async function cargarDatos(options = {}) {
  if (!entidadSeleccionada.value) return
  const silent = options.silent === true
  if (!silent) {
    loading.value = true
    error.value = ''
  }
  const consulta = ++ultimaConsulta
  try {
    // take/skip siempre: una API sin paginación los ignora. search solo si el servidor pagina,
    // porque busca únicamente en los campos marcados como filtro en la config del backend.
    const response = await runtimeApi.list(apiRoute.value, {
      take: itemsPerPage.value,
      skip: (page.value - 1) * itemsPerPage.value,
      search: paginadoServidor.value && search.value ? search.value : undefined
    })
    if (consulta !== ultimaConsulta) return // llegó tarde: ya se pidió otra página o búsqueda
    const { data } = response
    const items = Array.isArray(data) ? data : (data?.items || [])
    registros.value = items.map(item => normalizeRecord(item))
    const total = response.headers?.['x-total-count']
    const eraPaginado = paginadoServidor.value
    paginadoServidor.value = total != null
    totalServidor.value = Number(total ?? items.length)
    const pedidas = itemsPerPage.value
    if (paginadoServidor.value && items.length > 0 && items.length < pedidas &&
        (page.value - 1) * pedidas + items.length < totalServidor.value) {
      // La API tiene un máximo de filas por página menor al elegido: se usa ese (y se recarga)
      itemsPerPage.value = items.length
      return
    }
    if (paginadoServidor.value && (!eraPaginado ? search.value : page.value > pageCount.value)) {
      // Recién se supo que pagina y ya había búsqueda, o se borró lo último de la última página
      if (page.value > pageCount.value) page.value = pageCount.value
      else cargarDatos({ silent: true })
      return
    }
  } catch (err) {
    if (!silent) {
      error.value = entityMessages.value.error
    }
  } finally {
    if (!silent) {
      loading.value = false
    }
  }
}

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  const keyMap = new Map(Object.keys(copy).map(k => [k.toLowerCase(), k]))
  campos.value.forEach(field => {
    const key = field.columnName
    if (!key) return
    if (copy[key] === undefined) {
      const matchKey = keyMap.get(String(key).toLowerCase())
      if (matchKey) copy[key] = copy[matchKey]
    }
  })
  return copy
}

function nuevoRegistro() {
  dialogMode.value = 'create'
  registroActual.value = null
  dialog.value = true
}

function editarRegistro(item) {
  dialogMode.value = 'edit'
  registroActual.value = { ...item }
  dialog.value = true
}

function duplicarRegistro(item) {
  dialogMode.value = 'duplicate'
  registroActual.value = { ...item }
  dialog.value = true
}

async function eliminarRegistro(item) {
  if (!pkField.value) return
  if (confirmDelete.value) {
    const ok = window.confirm(entityMessages.value.confirmDelete || 'Eliminar registro?')
    if (!ok) return
  }

  try {
    await runtimeApi.remove(apiRoute.value, item[pkField.value.columnName])
    await cargarDatos()
  } catch (err) {
    window.alert(entityMessages.value.error)
  }
}

async function copiarRegistro(item) {
  const record = item || {}
  const lines = campos.value.map(field => {
    const label = field.label || field.name || field.columnName || 'Campo'
    const value = formatValueForCopy(record, field)
    return `${label}: ${value}`
  })
  const text = lines.join('\n')

  try {
    if (navigator?.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
      showToast('Datos copiados.', 'green')
      return
    }
    fallbackCopy(text)
  } catch {
    fallbackCopy(text)
  }
}

function formatValueForCopy(item, field) {
  if (!field?.columnName) return ''
  const res = formattedCell(item, { key: field.columnName })
  return res?.text ?? ''
}

function fallbackCopy(text) {
  const el = document.createElement('textarea')
  el.value = text
  el.setAttribute('readonly', '')
  el.style.position = 'absolute'
  el.style.left = '-9999px'
  document.body.appendChild(el)
  el.select()
  try {
    document.execCommand('copy')
    showToast('Datos copiados.', 'green')
  } catch {
    window.alert('No se pudo copiar.')
  } finally {
    document.body.removeChild(el)
  }
}

function showToast(message, color = 'green') {
  toastMessage.value = message
  toastColor.value = color
  toastOpen.value = false
  requestAnimationFrame(() => {
    toastOpen.value = true
  })
}

async function toggleQuickField(item) {
  if (!quickToggleField.value) return
  if (!pkField.value) return

  const payload = { ...item }
  const key = quickToggleField.value.columnName
  payload[key] = !payload[key]

  try {
    await runtimeApi.update(apiRoute.value, item[pkField.value.columnName], payload)
    await cargarDatos()
  } catch (err) {
    window.alert(entityMessages.value.error)
  }
}

function formattedCell(item, col) {
  const field = campos.value.find(f => f.columnName === col.key)
  if (!field) return { text: item[col.key], isChip: false }

  let value = item[col.key]
  const format = field.format
  const dataType = String(field.dataType || '').toLowerCase()

  if (value == null) return { text: '', isChip: false }

  if (format === 'uppercase') {
    value = String(value).toUpperCase()
  }

  if (format === 'money') {
    const formatter = new Intl.NumberFormat(locale.value, {
      style: 'currency',
      currency: currency.value
    })
    return { text: formatter.format(value), isChip: false }
  }

  if (format === 'date' || dataType.includes('date')) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleDateString(locale.value), isChip: false }
    }
  }

  if (format === 'datetime') {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return { text: date.toLocaleString(locale.value), isChip: false }
    }
  }

  if (format === 'badge') {
    return { text: value, isChip: true, color: value ? 'green' : 'red' }
  }

  if (dataType.includes('bit') || dataType.includes('bool')) {
    return { text: value ? 'Si' : 'No', isChip: true, color: value ? 'green' : 'grey' }
  }

  return { text: value, isChip: false }
}

watch(
  () => entitySlug.value,
  () => resolverEntidad()
)

// Con paginación del servidor, cambiar de página, de tamaño o la búsqueda vuelve a pedir datos
let esperaBusqueda = null
watch(page, () => {
  if (paginadoServidor.value) cargarDatos({ silent: true })
})
watch(itemsPerPage, () => {
  if (page.value !== 1) page.value = 1
  else if (paginadoServidor.value) cargarDatos({ silent: true })
})
watch(search, () => {
  if (!paginadoServidor.value) return
  clearTimeout(esperaBusqueda)
  esperaBusqueda = setTimeout(() => {
    if (page.value !== 1) page.value = 1
    else cargarDatos({ silent: true })
  }, 300)
})

onMounted(() => {
  normalizeConfig()
  if (config.value?.system?.defaultItemsPerPage) {
    itemsPerPage.value = config.value.system.defaultItemsPerPage
  }
  resolverEntidad()
})
</script>

<style scoped>
.runtime-container {
  font-family: var(--sb-font, "Manrope", system-ui, sans-serif);
}

.sb-page-header {
  padding: 12px;
  background: var(--sb-surface);
  border-radius: calc(var(--sb-radius) + 2px);
  box-shadow: var(--sb-shadow);
  border: 1px solid var(--sb-border-soft);
  position: relative;
}

.sb-page-icon {
  width: 48px;
  height: 48px;
  background: var(--sb-primary-soft);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
}

.sb-page-header::after {
  content: '';
  position: absolute;
  top: 14px;
  left: 14px;
  width: 6px;
  height: calc(100% - 28px);
  border-radius: 999px;
  background: linear-gradient(180deg, var(--sb-primary), var(--sb-secondary));
  opacity: 0.7;
}

.card {
  border-radius: 16px;
}

.side-card {
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.08);
}

.summary-card {
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
}

.summary-grid {
  display: grid;
  gap: 12px;
}

.summary-item {
  display: flex;
  gap: 10px;
  align-items: center;
}

.summary-icon {
  width: 34px;
  height: 34px;
  border-radius: 10px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.summary-label {
  font-size: 0.75rem;
  color: var(--sb-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.summary-value {
  font-size: 1.05rem;
  font-weight: 600;
}

.summary-meta {
  font-size: 0.8rem;
  color: var(--sb-muted);
  display: flex;
  gap: 6px;
}

.summary-meta-label {
  font-weight: 600;
}

.table :deep(th) {
  font-weight: 600;
}

.table :deep(th),
.table :deep(td) {
  padding: 4px 8px;
  font-size: 0.85rem;
  line-height: 1.2;
  vertical-align: middle;
}

.cell-text {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  word-break: break-word;
  max-width: 260px;
}

.actions-td {
  width: 140px;
  min-width: 140px;
}

.actions-cell {
  display: grid;
  grid-template-columns: repeat(3, 28px);
  gap: 6px;
  justify-content: center;
  align-content: center;
}

.actions-cell :deep(.v-btn) {
  min-width: 28px;
  height: 28px;
  border-radius: 10px;
  background: color-mix(in srgb, var(--sb-border) 55%, transparent);
}

.actions-cell :deep(.v-icon) {
  font-size: 16px;
}

.actions-cell :deep(.v-btn:hover) {
  background: var(--sb-primary-soft);
}

.cta-button {
  border-radius: 999px;
  font-weight: 600;
  letter-spacing: 0.3px;
  text-transform: none;
}

.cta-button.primary {
  background: linear-gradient(135deg, var(--sb-primary), var(--sb-secondary));
  color: #fff;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.25);
}

.cta-button.ghost {
  color: var(--sb-text);
  border: 1px solid color-mix(in srgb, var(--sb-border) 70%, transparent);
  background: color-mix(in srgb, var(--sb-surface) 88%, transparent);
}
</style>
