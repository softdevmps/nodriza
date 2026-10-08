import { onMounted, onBeforeUnmount, ref, computed, watch, reactive, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../sistema.service.js'
import frontendConfigService from '../frontend-config.service.js'
import entidadService from '../entidad.service.js'
import campoService from '../campo.service.js'
import relacionService from '../relacion.service.js'
import backendConfigService from '../backend-config.service.js'
import { toKebab } from '@runtime/comun/utils/slug.js'
import { useMenuStore } from '../../../comun/store/menu.store.js'

/**
 * Estado y lógica del diseñador de sistemas (SistemaEditor.vue). Lo comparten, vía provide/inject,
 * las pestañas (Tab*.vue) y los diálogos (Dialogo*.vue) de esta carpeta.
 */
export function useSistemaEditor () {
  const route = useRoute()
  const router = useRouter()
  const systemId = Number(route.params.id)
  const { cargarMenuTree } = useMenuStore()

  const sistema = ref(null)
  const entidades = ref([])
  const campos = ref([])
  const entidadSeleccionada = ref(null)
  const relaciones = ref([])
  const backendSystemConfig = ref({
    apiBase: 'api/v1',
    requireAuth: true,
    schemaPrefix: 'sys',
    persistence: 'sql',
    defaultPageSize: 50,
    maxPageSize: 200
  })
  const backendEntities = ref([])
  const tab = ref(localStorage.getItem('systemEditorTab') || 'datos')

  const mostrarEntidadDialog = ref(false)
  const entidadSeleccionadaEdicion = ref(null)

  const mostrarCampoDialog = ref(false)
  const campoSeleccionado = ref(null)

  const mostrarRelacionDialog = ref(false)
  const relacionSeleccionada = ref(null)
  const mostrarBackendDialog = ref(false)
  const backendEntidadActual = ref(null)
  const mostrarFrontendDialog = ref(false)
  const frontendEntidadActual = ref(null)
  const mostrarFrontendFieldDialog = ref(false)
  const frontendFieldActual = ref(null)
  const frontendFieldDragIndex = ref(null)
  const frontendFieldDragOver = ref(null)
  const frontendEntityDragIndex = ref(null)
  const frontendEntityDragOver = ref(null)
  const apiConsole = ref({
    baseUrl: '',
    method: 'GET',
    path: '',
    body: '',
    sendToken: true,
    responseStatus: '',
    responseTime: '',
    responseBody: '',
    responseHeaders: ''
  })
  const sqlConsole = ref({
    script: '',
    running: false,
    resultType: 'info',
    resultMessage: ''
  })
  const apiRouteSeleccionada = ref(null)
  const apiRouteFilterText = ref('')
  const apiRouteFilterMethod = ref('')
  const endpointPanel = ref([])
  const endpointOnlyMode = ref(false)
  const endpointOnlyKey = ref(null)
  const restartDialog = reactive({
    open: false,
    status: 'idle',
    message: ''
  })
  const frontendDialog = reactive({
    open: false,
    status: 'idle',
    message: ''
  })

  const frontendConfig = ref({
    system: {
      appTitle: 'SystemBase',
      showSearch: true,
      showFilters: true,
      defaultItemsPerPage: 10,
      itemsPerPageOptions: [10, 20, 50, 100],
      primaryColor: '#2563eb',
      secondaryColor: '#0ea5e9',
      density: 'comfortable',
      fontFamily: 'Inter, system-ui, -apple-system, Segoe UI, sans-serif',
      uiMode: 'enterprise',
      locale: 'es-AR',
      currency: 'ARS',
      authMode: 'local',
      authBaseUrl: 'http://localhost:5032/api/v1'
    },
    entities: []
  })

  const backendHealth = reactive({
    status: 'unknown',
    lastChecked: null
  })
  const frontendHealth = reactive({
    status: 'unknown',
    lastChecked: null
  })
  const healthIntervalId = ref(null)
  const backendLogs = reactive({
    entries: [],
    lastId: 0,
    status: 'idle',
    autoScroll: true
  })
  const backendLogRef = ref(null)

  const headersEntidades = [
    { title: 'Nombre', key: 'name' },
    { title: 'TableName', key: 'tableName' },
    { title: 'Activo', key: 'isActive' },
    { title: 'Acciones', key: 'actions', sortable: false, width: 180 }
  ]

  const headersCampos = [
    { title: 'Nombre', key: 'name' },
    { title: 'ColumnName', key: 'columnName' },
    { title: 'DataType', key: 'dataType' },
    { title: 'Required', key: 'required' },
    { title: 'PK', key: 'isPrimaryKey' },
    { title: 'Acciones', key: 'actions', sortable: false }
  ]

  const headersRelaciones = [
    { title: 'Origen', key: 'sourceEntityId' },
    { title: 'FK', key: 'foreignKey' },
    { title: 'Destino', key: 'targetEntityId' },
    { title: 'Tipo', key: 'relationType' },
    { title: 'Cascade', key: 'cascadeDelete' },
    { title: 'Acciones', key: 'actions', sortable: false }
  ]

  const headersBackend = [
    { title: 'Entidad', key: 'name' },
    { title: 'Ruta', key: 'route' },
    { title: 'Generar CRUD', key: 'isEnabled', sortable: false },
    { title: 'Acciones', key: 'actions', sortable: false }
  ]

  const headersBackendFields = [
    { title: 'Campo', key: 'name' },
    { title: 'Expose', key: 'expose', sortable: false },
    { title: 'ReadOnly', key: 'readOnly', sortable: false },
    { title: 'Required', key: 'required', sortable: false },
    { title: 'Max', key: 'maxLength', sortable: false },
    { title: 'Unique', key: 'unique', sortable: false },
    { title: 'Default', key: 'defaultValue', sortable: false },
    { title: 'Display', key: 'displayAs', sortable: false }
  ]

  const headersApiRoutes = [
    { title: 'Metodo', key: 'method' },
    { title: 'Path', key: 'path' },
    { title: 'Acciones', key: 'actions', sortable: false }
  ]

  const backendAuthOptions = [
    { title: 'Heredar', value: null },
    { title: 'Requerir', value: true },
    { title: 'No requerir', value: false }
  ]

  const backendSystemAuthOptions = [
    { title: 'Requerir', value: true },
    { title: 'No requerir', value: false }
  ]

  const backendPersistenceOptions = [
    'sql'
  ]

  const backendRequiredOptions = [
    { title: 'Heredar', value: null },
    { title: 'Si', value: true },
    { title: 'No', value: false }
  ]

  const endpointSoftDeleteOptions = [
    { title: 'Heredar', value: null },
    { title: 'Soft delete', value: true },
    { title: 'Hard delete', value: false }
  ]

  const backendSortOptions = [
    'asc',
    'desc'
  ]

  const frontendDensityOptions = [
    'comfortable',
    'compact',
    'default'
  ]

  const frontendUiModeOptions = [
    'enterprise',
    'minimal'
  ]

  const frontendAuthModeOptions = [
    { title: 'Local (backend del sistema)', value: 'local' },
    { title: 'Central (SystemBase)', value: 'central' }
  ]

  const frontendFieldInputOptions = [
    'auto',
    'text',
    'textarea',
    'number',
    'date',
    'datetime',
    'select',
    'checkbox',
    'switch'
  ]

  const frontendFieldFormatOptions = [
    'text',
    'uppercase',
    'lowercase',
    'date',
    'datetime',
    'money',
    'badge',
    'boolean'
  ]

  const frontendFormLayoutOptions = [
    'single',
    'sections',
    'tabs'
  ]

  const frontendSortOptions = [
    'asc',
    'desc'
  ]

  // Los puertos de los sistemas generados los define el backend (Comun/PuertosSistemas.cs)
  const systembasePort = 5032
  const backendPort = computed(() => sistema.value?.puertoBackend ?? '—')
  const backendBaseUrl = computed(() => `http://localhost:${backendPort.value}`)
  const systembaseBaseUrl = computed(() => `http://localhost:${systembasePort}`)
  const portsFilePath = computed(() => 'systems/ports.json')
  const frontendPort = computed(() => sistema.value?.puertoFrontend ?? '—')
  const frontendBaseUrl = computed(() => `http://localhost:${frontendPort.value}`)
  const sqlTargetSchema = computed(() => {
    const slug = (sistema.value?.slug || '').trim().toLowerCase()
    return slug ? `sys_${slug}` : 'sys_<slug>'
  })
  const sqlScriptTemplate = computed(() => {
    const schema = sqlTargetSchema.value
    return `IF OBJECT_ID('[${schema}].[Recurso]', 'U') IS NULL
  BEGIN
      CREATE TABLE [${schema}].[Recurso] (
          [Id] INT IDENTITY(1,1) NOT NULL,
          [Codigo] NVARCHAR(80) NOT NULL,
          [Descripcion] NVARCHAR(200) NULL,
          [Activo] BIT NOT NULL CONSTRAINT [DF_${schema}_Recurso_Activo] DEFAULT (1),
          [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_${schema}_Recurso_CreatedAt] DEFAULT (SYSUTCDATETIME()),
          CONSTRAINT [PK_${schema}_Recurso] PRIMARY KEY ([Id])
      );
  END
  GO

  IF NOT EXISTS (
      SELECT 1
      FROM sys.indexes
      WHERE name = 'UX_${schema}_Recurso_Codigo'
        AND object_id = OBJECT_ID('[${schema}].[Recurso]')
  )
  BEGIN
      CREATE UNIQUE INDEX [UX_${schema}_Recurso_Codigo]
      ON [${schema}].[Recurso] ([Codigo]);
  END`
  })

  const backendHealthLabel = computed(() => {
    if (backendHealth.status === 'online') return 'Online'
    if (backendHealth.status === 'offline') return 'Offline'
    if (backendHealth.status === 'checking') return 'Verificando...'
    return 'Sin estado'
  })

  const backendHealthColor = computed(() => {
    if (backendHealth.status === 'online') return 'green'
    if (backendHealth.status === 'offline') return 'red'
    if (backendHealth.status === 'checking') return 'orange'
    return 'grey'
  })

  const frontendHealthLabel = computed(() => {
    if (frontendHealth.status === 'online') return 'Online'
    if (frontendHealth.status === 'offline') return 'Offline'
    if (frontendHealth.status === 'checking') return 'Verificando...'
    return 'Sin estado'
  })

  const frontendHealthColor = computed(() => {
    if (frontendHealth.status === 'online') return 'green'
    if (frontendHealth.status === 'offline') return 'red'
    if (frontendHealth.status === 'checking') return 'orange'
    return 'grey'
  })

  const apiRouteOptions = computed(() => {
    const base = backendSystemConfig.value.apiBase || 'api/v1'
    const basePath = `/${String(base).replace(/^\/+|\/+$/g, '')}`
    const routes = []

    backendEntities.value.forEach(entity => {
      const route = entity.route || toKebab(entity.name || '')
      const entityId = entity.entityId ?? entity.id
      const fullBase = `${basePath}/${route}`

      if (entity.endpoints?.list !== false) {
        routes.push({ title: `GET ${fullBase}`, method: 'GET', path: fullBase, entityId })
      }
      if (entity.endpoints?.get !== false) {
        routes.push({ title: `GET ${fullBase}/{id}`, method: 'GET', path: `${fullBase}/:id`, entityId })
      }
      if (entity.endpoints?.create !== false) {
        routes.push({ title: `POST ${fullBase}`, method: 'POST', path: fullBase, entityId })
      }
      if (entity.endpoints?.update !== false) {
        routes.push({ title: `PUT ${fullBase}/{id}`, method: 'PUT', path: `${fullBase}/:id`, entityId })
      }
      if (entity.endpoints?.delete !== false) {
        routes.push({ title: `DELETE ${fullBase}/{id}`, method: 'DELETE', path: `${fullBase}/:id`, entityId })
      }
    })

    return routes
  })

  const filteredApiRoutes = computed(() => {
    const text = apiRouteFilterText.value.trim().toLowerCase()
    const methodFilter = apiRouteFilterMethod.value
    return apiRouteOptions.value.filter(item => {
      if (methodFilter && methodFilter !== item.method) return false
      if (!text) return true
      return (
        item.path.toLowerCase().includes(text) ||
        item.title.toLowerCase().includes(text)
      )
    })
  })

  const apiMethodOptions = [
    { title: 'Todos', value: '' },
    { title: 'GET', value: 'GET' },
    { title: 'POST', value: 'POST' },
    { title: 'PUT', value: 'PUT' },
    { title: 'DELETE', value: 'DELETE' }
  ]

  const frontendItemsPerPageText = computed({
    get() {
      const options = frontendConfig.value?.system?.itemsPerPageOptions || []
      return options.join(', ')
    },
    set(value) {
      const numbers = String(value)
        .split(',')
        .map(v => parseInt(v.trim(), 10))
        .filter(n => Number.isFinite(n) && n > 0)
      if (numbers.length) {
        frontendConfig.value.system.itemsPerPageOptions = numbers
      }
    }
  })

  function apiMethodColor(method) {
    if (method === 'GET') return 'green'
    if (method === 'POST') return 'blue'
    if (method === 'PUT') return 'orange'
    if (method === 'DELETE') return 'red'
    return 'grey'
  }

  const endpointConfigKeyMap = {
    list: 'listConfig',
    get: 'getConfig',
    create: 'createConfig',
    update: 'updateConfig',
    delete: 'deleteConfig'
  }

  const endpointTitleMap = {
    list: 'Listar (GET)',
    get: 'Obtener (GET /:id)',
    create: 'Crear (POST)',
    update: 'Editar (PUT)',
    delete: 'Eliminar (DELETE)'
  }

  function getEndpointConfig(key) {
    const entity = backendEntidadActual.value
    if (!entity) return {}
    if (!entity.endpoints) entity.endpoints = {}
    const prop = endpointConfigKeyMap[key]
    if (!prop) return {}
    if (!entity.endpoints[prop]) {
      entity.endpoints[prop] = {
        requireAuth: null,
        useSoftDelete: null
      }
    }
    return entity.endpoints[prop]
  }

  function endpointKeyFromRoute(item) {
    if (!item?.method) return null
    const method = item.method.toUpperCase()
    if (method === 'GET') return item.path.includes(':id') ? 'get' : 'list'
    if (method === 'POST') return 'create'
    if (method === 'PUT') return 'update'
    if (method === 'DELETE') return 'delete'
    return null
  }

  const endpointOnlyTitle = computed(() => {
    if (!endpointOnlyKey.value) return ''
    return endpointTitleMap[endpointOnlyKey.value] || ''
  })

  function shouldShowEndpointPanel(key) {
    return !endpointOnlyMode.value || endpointOnlyKey.value === key
  }

  function backendBasePath() {
    const base = backendSystemConfig.value.apiBase || 'api/v1'
    return `/${String(base).replace(/^\/+|\/+$/g, '')}`
  }

  const apiSelectedEntity = computed(() => {
    const entityId = apiRouteSeleccionada.value?.entityId
    if (!entityId) return null
    return backendEntities.value.find(entity => (entity.entityId ?? entity.id) === entityId) || null
  })

  function toPascalCase(value) {
    if (!value) return ''
    return String(value)
      .replace(/[_\-\s]+(.)?/g, (_, chr) => (chr ? chr.toUpperCase() : ''))
      .replace(/^(.)/, chr => chr.toUpperCase())
  }

  function fieldKey(field) {
    return toPascalCase(field.columnName || field.name || '')
  }

  function sampleValueForField(field) {
    const type = String(field.dataType || '').toLowerCase()
    if (type.includes('uniqueidentifier') || type.includes('uuid')) return '00000000-0000-0000-0000-000000000000'
    if (type.includes('int')) return 1
    if (type.includes('decimal') || type.includes('numeric') || type.includes('money')) return 10.5
    if (type.includes('float') || type.includes('real')) return 10.5
    if (type.includes('bit') || type.includes('bool')) return true
    if (type.includes('date') || type.includes('time')) return '2024-01-01T00:00:00Z'
    if (type.includes('char') || type.includes('text')) return 'string'
    return 'valor'
  }

  function buildExampleObject(fields) {
    const result = {}
    fields.forEach(field => {
      const key = fieldKey(field)
      if (!key) return
      result[key] = sampleValueForField(field)
    })
    return result
  }

  function getCreateFields(entity) {
    return (entity.fields || []).filter(field => field.expose && !field.readOnly && !field.isIdentity)
  }

  function getUpdateFields(entity) {
    return (entity.fields || []).filter(
      field => field.expose && !field.readOnly && !field.isPrimaryKey && !field.isIdentity
    )
  }

  function getResponseFields(entity) {
    return (entity.fields || []).filter(field => field.expose)
  }

  const apiExampleRequestText = computed(() => {
    const route = apiRouteSeleccionada.value
    const entity = apiSelectedEntity.value
    if (!route || !entity) return ''
    if (route.method === 'POST') return JSON.stringify(buildExampleObject(getCreateFields(entity)), null, 2)
    if (route.method === 'PUT') return JSON.stringify(buildExampleObject(getUpdateFields(entity)), null, 2)
    return ''
  })

  const apiExampleResponseText = computed(() => {
    const route = apiRouteSeleccionada.value
    const entity = apiSelectedEntity.value
    if (!route || !entity) return ''
    const payload = buildExampleObject(getResponseFields(entity))
    if (route.method === 'GET' && route.path.includes(':id')) return JSON.stringify(payload, null, 2)
    if (route.method === 'GET') return JSON.stringify([payload], null, 2)
    if (route.method === 'POST' || route.method === 'PUT') return JSON.stringify(payload, null, 2)
    return ''
  })

  function usarEjemploRequest() {
    if (!apiExampleRequestText.value) return
    apiConsole.value.body = apiExampleRequestText.value
  }

  async function cargarSistema() {
    const { data } = await sistemaService.getById(systemId)
    sistema.value = data
  }

  async function cargarEntidades() {
    const { data } = await entidadService.getBySystem(systemId)
    entidades.value = data
    if (entidadSeleccionada.value) {
      const match = data.find(e => e.id === entidadSeleccionada.value.id)
      entidadSeleccionada.value = match ?? null
      if (entidadSeleccionada.value) await cargarCampos()
    }
  }

  async function cargarCampos() {
    if (!entidadSeleccionada.value) {
      campos.value = []
      return
    }

    const { data } = await campoService.getByEntity(systemId, entidadSeleccionada.value.id)
    campos.value = data
  }

  async function cargarRelaciones() {
    const { data } = await relacionService.getBySystem(systemId)
    relaciones.value = data
  }

  async function cargarBackendConfig() {
    const { data } = await backendConfigService.getBySystem(systemId)
    backendSystemConfig.value = data?.system || backendSystemConfig.value
    ensureBackendSystemConfig(backendSystemConfig.value)
    backendEntities.value = data?.entities || []
  }

  function seleccionarEntidad(item) {
    entidadSeleccionada.value = item
    cargarCampos()
  }

  function nuevaEntidad() {
    entidadSeleccionadaEdicion.value = null
    mostrarEntidadDialog.value = true
  }

  function editarEntidad(item) {
    entidadSeleccionadaEdicion.value = item
    mostrarEntidadDialog.value = true
  }

  async function eliminarEntidad(item) {
    const entityLabel = item?.displayName || item?.name || item?.tableName || `#${item?.id}`
    const ok = window.confirm(`Eliminar entidad "${entityLabel}"?`)
    if (!ok) return

    const tableLabel = item?.tableName || item?.name || 'tabla_runtime'
    const dropTable = window.confirm(
      `Tambien eliminar la tabla fisica [${sqlTargetSchema.value}].[${tableLabel}]?\n\nAceptar = si, Cancelar = no`
    )

    try {
      const { data } = await entidadService.eliminar(systemId, item.id, dropTable)
      const message = data?.message || data?.Message || 'Entidad eliminada.'
      window.alert(message)
      if (entidadSeleccionada.value?.id === item.id) {
        entidadSeleccionada.value = null
        campos.value = []
      }
      await cargarEntidades()
      await cargarRelaciones()
    } catch (error) {
      const raw = error?.response?.data
      const rawText =
        typeof raw === 'string'
          ? raw
          : raw && typeof raw === 'object'
            ? (raw.message || raw.Message || '')
            : ''
      const message =
        rawText ||
        (error?.response?.status ? `HTTP ${error.response.status}` : '') ||
        'Error al eliminar entidad.'
      window.alert(message)
    }
  }

  function verDatos(item) {
    if (!sistema.value?.slug) {
      window.alert('Sistema sin slug.')
      return
    }
    router.push(`/s/${sistema.value.slug}/${toKebab(item.name)}`)
  }

  function nuevoCampo() {
    campoSeleccionado.value = null
    mostrarCampoDialog.value = true
  }

  function editarCampo(item) {
    campoSeleccionado.value = item
    mostrarCampoDialog.value = true
  }

  function nuevaRelacion() {
    relacionSeleccionada.value = null
    mostrarRelacionDialog.value = true
  }

  function editarRelacion(item) {
    relacionSeleccionada.value = item
    mostrarRelacionDialog.value = true
  }

  function volver() {
    router.push('/sistemas')
  }

  // Publicar abre PublicarDialog (muestra qué cambia en la base y pide confirmación)
  const mostrarPublicarDialog = ref(false)

  function publicarSistema() {
    mostrarPublicarDialog.value = true
  }

  async function alPublicar() {
    await cargarSistema()
    await cargarMenuTree()
  }

  function limpiarSqlConsole() {
    sqlConsole.value.script = ''
    sqlConsole.value.resultMessage = ''
    sqlConsole.value.resultType = 'info'
  }

  async function ejecutarSqlConsole() {
    const script = sqlConsole.value.script?.trim()
    if (!script) {
      sqlConsole.value.resultType = 'warning'
      sqlConsole.value.resultMessage = 'Pega un script SQL antes de ejecutar.'
      return
    }

    const ok = window.confirm(
      `Ejecutar script SQL en ${sqlTargetSchema.value}?\n\nSolo usar para bootstrap controlado.`
    )
    if (!ok) return

    sqlConsole.value.running = true
    sqlConsole.value.resultMessage = ''

    try {
      const { data } = await sistemaService.ejecutarSql(systemId, script)
      const message = data?.message || data?.Message || 'Script SQL ejecutado.'
      sqlConsole.value.resultType = 'success'
      sqlConsole.value.resultMessage = message
      await cargarEntidades()
      await cargarRelaciones()
    } catch (error) {
      const message =
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        'Error ejecutando script SQL.'
      sqlConsole.value.resultType = 'error'
      sqlConsole.value.resultMessage = message
    } finally {
      sqlConsole.value.running = false
    }
  }

  async function guardarBackendConfig() {
    try {
      const payload = {
        system: backendSystemConfig.value,
        entities: backendEntities.value
      }
      await backendConfigService.guardar(systemId, payload)
      window.alert('Configuracion de backend guardada.')
    } catch (error) {
      const message =
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        'Error al guardar configuracion de backend.'
      window.alert(message)
    }
  }

  function abrirBackendEntidad(item) {
    backendEntidadActual.value = item
    mostrarBackendDialog.value = true
    endpointPanel.value = []
    endpointOnlyMode.value = false
    endpointOnlyKey.value = null
  }

  function usarApiRoute(item) {
    apiRouteSeleccionada.value = item
  }

  function configurarApi(item) {
    const entityId = item?.entityId
    if (!entityId) return
    const entity = backendEntities.value.find(e => (e.entityId ?? e.id) === entityId)
    if (!entity) return

    backendEntidadActual.value = entity
    mostrarBackendDialog.value = true

    const key = endpointKeyFromRoute(item)
    if (key) {
      endpointOnlyMode.value = true
      endpointOnlyKey.value = key
      const indexMap = { list: 0, get: 1, create: 2, update: 3, delete: 4 }
      const index = indexMap[key]
      if (index !== undefined) endpointPanel.value = [index]
    } else {
      endpointOnlyMode.value = false
      endpointOnlyKey.value = null
    }
  }

  async function copiarTexto(value, label = 'Texto') {
    if (!value) return
    try {
      await navigator.clipboard.writeText(value)
      window.alert(`${label} copiado.`)
    } catch {
      window.prompt('Copia esto:', value)
    }
  }

  watch(
    () => apiConsole.value.baseUrl,
    value => {
      if (value) localStorage.setItem('backendConsoleBaseUrl', value)
    }
  )

  watch(apiRouteSeleccionada, value => {
    if (!value) return
    apiConsole.value.method = value.method
    apiConsole.value.path = value.path
    apiConsole.value.body = ''
    apiConsole.value.responseStatus = ''
    apiConsole.value.responseTime = ''
    apiConsole.value.responseBody = ''
    apiConsole.value.responseHeaders = ''
  })

  watch(
    tab,
    value => {
      if (value) localStorage.setItem('systemEditorTab', value)
      if (value === 'herramientas') {
        startHealthPolling()
        startLogsPolling()
      } else {
        stopHealthPolling()
        stopLogsPolling()
      }
    }
  )

  async function enviarApiConsole() {
    apiConsole.value.responseStatus = ''
    apiConsole.value.responseTime = ''
    apiConsole.value.responseBody = ''
    apiConsole.value.responseHeaders = ''

    const baseUrl = apiConsole.value.baseUrl.replace(/\/+$/, '')
    const path = apiConsole.value.path.startsWith('/') ? apiConsole.value.path : `/${apiConsole.value.path}`
    const url = `${baseUrl}${path}`.replace('/:id', '/1')
    const method = apiConsole.value.method.toUpperCase()

    const headers = {
      'Content-Type': 'application/json'
    }

    if (apiConsole.value.sendToken) {
      const token = localStorage.getItem('token')
      if (token) headers.Authorization = `Bearer ${token}`
    }

    let body = undefined
    if (method !== 'GET' && method !== 'DELETE') {
      if (apiConsole.value.body) {
        try {
          body = JSON.stringify(JSON.parse(apiConsole.value.body))
        } catch {
          body = apiConsole.value.body
        }
      }
    }

    const start = performance.now()
    try {
      const response = await fetch(url, { method, headers, body })
      const time = Math.round(performance.now() - start)
      apiConsole.value.responseStatus = `${response.status} ${response.statusText}`
      apiConsole.value.responseTime = `${time} ms`
      apiConsole.value.responseHeaders = [...response.headers.entries()]
        .map(([k, v]) => `${k}: ${v}`)
        .join('\n')

      const text = await response.text()
      try {
        apiConsole.value.responseBody = JSON.stringify(JSON.parse(text), null, 2)
      } catch {
        apiConsole.value.responseBody = text
      }
    } catch (error) {
      apiConsole.value.responseStatus = 'Error'
      apiConsole.value.responseBody = error?.message || 'Error al llamar la API.'
    }
  }

  async function reiniciarBackend() {
    const ok = window.confirm('Reiniciar backend? (requiere dotnet watch en modo dev)')
    if (!ok) return

    try {
      restartDialog.open = true
      restartDialog.status = 'restarting'
      restartDialog.message = 'Reiniciando backend...'

      const token = localStorage.getItem('token')
      const basePath = backendBasePath()
      const url = `${backendBaseUrl.value}${basePath}/dev/restart`
      const headers = token ? { Authorization: `Bearer ${token}` } : undefined
      const response = await fetch(url, { method: 'POST', headers })
      if (!response.ok) {
        const text = await response.text()
        throw new Error(text || 'Error al reiniciar backend.')
      }

      restartDialog.status = 'waiting'
      restartDialog.message = 'Esperando que el backend vuelva...'

      const online = await esperarBackendOnline()
      if (online) {
        restartDialog.status = 'online'
        restartDialog.message = 'Backend reiniciado.'
        backendHealth.status = 'online'
        await cargarBackendLogs()
        setTimeout(() => {
          restartDialog.open = false
        }, 1200)
      } else {
        restartDialog.status = 'timeout'
        restartDialog.message = 'Timeout esperando el backend.'
      }
    } catch (error) {
      restartDialog.status = 'error'
      restartDialog.message = error?.message || 'Error al reiniciar backend.'
    }
  }

  async function iniciarBackend() {
    const ok = window.confirm('Iniciar backend? (modo dev)')
    if (!ok) return

    try {
      restartDialog.open = true
      restartDialog.status = 'restarting'
      restartDialog.message = 'Iniciando backend...'

      await cargarBackendLogs(true)
      await sistemaService.iniciarBackend(systemId)

      restartDialog.status = 'waiting'
      restartDialog.message = 'Esperando que el backend este online...'

      const online = await esperarBackendOnline()
      if (online) {
        restartDialog.status = 'online'
        restartDialog.message = 'Backend iniciado.'
        backendHealth.status = 'online'
        await cargarBackendLogs(true)
        setTimeout(() => {
          restartDialog.open = false
        }, 1200)
      } else {
        restartDialog.status = 'timeout'
        restartDialog.message = 'Timeout esperando el backend.'
      }
    } catch (error) {
      restartDialog.status = 'error'
      restartDialog.message = error?.response?.data?.message || error?.message || 'Error al iniciar backend.'
    }
  }

  async function detenerBackend() {
    const ok = window.confirm('Detener backend?')
    if (!ok) return

    try {
      restartDialog.open = true
      restartDialog.status = 'restarting'
      restartDialog.message = 'Deteniendo backend...'

      await sistemaService.detenerBackend(systemId)

      backendHealth.status = 'offline'
      restartDialog.status = 'online'
      restartDialog.message = 'Backend detenido.'
      setTimeout(() => {
        restartDialog.open = false
      }, 1200)
    } catch (error) {
      restartDialog.status = 'error'
      restartDialog.message = error?.response?.data?.message || error?.message || 'Error al detener backend.'
    }
  }

  async function esperarBackendOnline() {
    const maxAttempts = 20
    for (let i = 0; i < maxAttempts; i += 1) {
      try {
        const { data } = await sistemaService.pingBackend(systemId)
        if (data?.online) return true
      } catch {
        // ignore
      }
      await new Promise(resolve => setTimeout(resolve, 800))
    }

    return false
  }

  async function iniciarFrontend() {
    const ok = window.confirm('Iniciar frontend? (modo dev)')
    if (!ok) return

    try {
      frontendDialog.open = true
      frontendDialog.status = 'restarting'
      frontendDialog.message = 'Iniciando frontend...'

      await sistemaService.iniciarFrontend(systemId)

      frontendDialog.status = 'waiting'
      frontendDialog.message = 'Esperando que el frontend este online...'

      const online = await esperarFrontendOnline()
      if (online) {
        frontendDialog.status = 'online'
        frontendDialog.message = 'Frontend iniciado.'
        frontendHealth.status = 'online'
        setTimeout(() => {
          frontendDialog.open = false
        }, 1200)
      } else {
        frontendDialog.status = 'timeout'
        frontendDialog.message = 'Timeout esperando el frontend.'
      }
    } catch (error) {
      frontendDialog.status = 'error'
      frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al iniciar frontend.'
    }
  }

  async function detenerFrontend() {
    const ok = window.confirm('Detener frontend?')
    if (!ok) return

    try {
      frontendDialog.open = true
      frontendDialog.status = 'restarting'
      frontendDialog.message = 'Deteniendo frontend...'

      await sistemaService.detenerFrontend(systemId)

      frontendHealth.status = 'offline'
      frontendDialog.status = 'online'
      frontendDialog.message = 'Frontend detenido.'
      setTimeout(() => {
        frontendDialog.open = false
      }, 1200)
    } catch (error) {
      frontendDialog.status = 'error'
      frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al detener frontend.'
    }
  }

  async function reiniciarFrontend() {
    const ok = window.confirm('Reiniciar frontend?')
    if (!ok) return

    try {
      frontendDialog.open = true
      frontendDialog.status = 'restarting'
      frontendDialog.message = 'Reiniciando frontend...'

      await sistemaService.detenerFrontend(systemId)
      await sistemaService.iniciarFrontend(systemId)

      frontendDialog.status = 'waiting'
      frontendDialog.message = 'Esperando que el frontend vuelva...'

      const online = await esperarFrontendOnline()
      if (online) {
        frontendDialog.status = 'online'
        frontendDialog.message = 'Frontend reiniciado.'
        frontendHealth.status = 'online'
        setTimeout(() => {
          frontendDialog.open = false
        }, 1200)
      } else {
        frontendDialog.status = 'timeout'
        frontendDialog.message = 'Timeout esperando el frontend.'
      }
    } catch (error) {
      frontendDialog.status = 'error'
      frontendDialog.message = error?.response?.data?.message || error?.message || 'Error al reiniciar frontend.'
    }
  }

  async function esperarFrontendOnline() {
    const maxAttempts = 20
    for (let i = 0; i < maxAttempts; i += 1) {
      try {
        const { data } = await sistemaService.pingFrontend(systemId)
        if (data?.online) return true
      } catch {
        // ignore
      }
      await new Promise(resolve => setTimeout(resolve, 800))
    }

    return false
  }

  async function checkFrontendHealth() {
    frontendHealth.status = 'checking'

    try {
      const { data } = await sistemaService.pingFrontend(systemId)
      frontendHealth.status = data?.online ? 'online' : 'offline'
    } catch {
      frontendHealth.status = 'offline'
    } finally {
      frontendHealth.lastChecked = new Date().toISOString()
    }
  }

  async function checkBackendHealth() {
    backendHealth.status = 'checking'

    try {
      const { data } = await sistemaService.pingBackend(systemId)
      backendHealth.status = data?.online ? 'online' : 'offline'
    } catch {
      backendHealth.status = 'offline'
    } finally {
      backendHealth.lastChecked = new Date().toISOString()
    }
  }

  function startHealthPolling() {
    if (healthIntervalId.value) return
    checkBackendHealth()
  }

  function stopHealthPolling() {
    if (!healthIntervalId.value) return
    clearInterval(healthIntervalId.value)
    healthIntervalId.value = null
  }

  function formatLogTime(value) {
    if (!value) return ''
    try {
      return new Date(value).toLocaleTimeString()
    } catch {
      return ''
    }
  }

  function scrollBackendLogs() {
    if (!backendLogs.autoScroll) return
    nextTick(() => {
      const el = backendLogRef.value
      if (!el) return
      el.scrollTop = el.scrollHeight
    })
  }

  function limpiarBackendLogs() {
    backendLogs.entries.splice(0, backendLogs.entries.length)
  }

  async function cargarBackendLogs(forceReset = false) {
    if (forceReset) {
      backendLogs.entries.splice(0, backendLogs.entries.length)
      backendLogs.lastId = 0
    }
    try {
      const { data } = await sistemaService.logsBackend(systemId, backendLogs.lastId, 200)
      const items = data?.items || data?.Items || []
      if (items.length) {
        backendLogs.entries.push(...items)
        const lastItem = items[items.length - 1]
        backendLogs.lastId = lastItem?.id ?? lastItem?.Id ?? data?.lastId ?? backendLogs.lastId
        if (backendLogs.entries.length > 500) {
          backendLogs.entries.splice(0, backendLogs.entries.length - 500)
        }
        scrollBackendLogs()
      } else if (data?.lastId) {
        backendLogs.lastId = data.lastId
      }
      backendLogs.status = 'ok'
    } catch {
      backendLogs.status = 'error'
    }
  }

  function startLogsPolling() {
    cargarBackendLogs(true)
  }

  function stopLogsPolling() {
    // no-op (polling disabled)
  }

  async function cargarFrontendConfig() {
    if (!systemId) return
    try {
      const { data } = await frontendConfigService.getBySystem(systemId)
      frontendConfig.value = data
      ensureFrontendSystemConfig(frontendConfig.value?.system)
      if (frontendConfig.value?.entities?.length) {
        frontendConfig.value.entities.forEach(entity => ensureFrontendEntityConfig(entity))
      }
    } catch {
      // keep defaults
    }
  }

  async function guardarFrontendConfig() {
    if (!systemId) return
    try {
      await frontendConfigService.guardar(systemId, frontendConfig.value)
      window.alert('Configuracion de frontend guardada.')
    } catch (error) {
      const message =
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        'Error al guardar configuracion de frontend.'
      window.alert(message)
    }
  }

  function frontendEntityLabel(entity) {
    return entity?.displayName || entity?.name || ''
  }

  function ensureBackendSystemConfig(systemConfig) {
    if (!systemConfig) return
  }

  function ensureFrontendSystemConfig(systemConfig) {
    if (!systemConfig) return
    if (!systemConfig.authMode) systemConfig.authMode = 'local'
    if (!systemConfig.authBaseUrl) systemConfig.authBaseUrl = 'http://localhost:5032/api/v1'
  }

  function ensureFrontendEntityConfig(entityConfig) {
    if (!entityConfig) return
    if (entityConfig.showInMenu === undefined) entityConfig.showInMenu = true
    if (!entityConfig.menuIcon) entityConfig.menuIcon = 'mdi-table'
    if (entityConfig.routeSlug === undefined) entityConfig.routeSlug = ''
    if (entityConfig.listStickyHeader === undefined) entityConfig.listStickyHeader = false
    if (entityConfig.listShowTotals === undefined) entityConfig.listShowTotals = true
    if (!entityConfig.defaultSortDirection) entityConfig.defaultSortDirection = 'asc'
    if (!entityConfig.formLayout) entityConfig.formLayout = 'single'
    if (entityConfig.confirmSave === undefined) entityConfig.confirmSave = true
    if (entityConfig.confirmDelete === undefined) entityConfig.confirmDelete = true
    if (entityConfig.enableDuplicate === undefined) entityConfig.enableDuplicate = true
    if (!entityConfig.messages) {
      entityConfig.messages = {
        empty: 'No hay registros todavia.',
        error: 'Ocurrio un error al procesar la solicitud.',
        successCreate: 'Registro creado.',
        successUpdate: 'Registro actualizado.',
        successDelete: 'Registro eliminado.'
      }
    }
  }

  function ensureFrontendFieldConfigs(entityConfig, fields) {
    if (!entityConfig.fields) entityConfig.fields = []
    const map = new Map(entityConfig.fields.map(f => [f.fieldId, f]))
    fields.forEach(field => {
      if (map.has(field.id)) {
        const existing = map.get(field.id)
        if (!existing.dataType && field.dataType) existing.dataType = field.dataType
        if (existing.isPrimaryKey === undefined) existing.isPrimaryKey = field.isPrimaryKey
        if (existing.isIdentity === undefined) existing.isIdentity = field.isIdentity
        if (existing.required === undefined) existing.required = field.required
        if (existing.maxLength === undefined || existing.maxLength === null) existing.maxLength = field.maxLength
        return
      }
      entityConfig.fields.push({
        fieldId: field.id,
        name: field.name,
        columnName: field.columnName,
        dataType: field.dataType,
        isPrimaryKey: field.isPrimaryKey,
        isIdentity: field.isIdentity,
        required: field.required,
        maxLength: field.maxLength,
        label: field.name,
        showInList: true,
        showInForm: !field.isIdentity,
        showInFilter: true,
        placeholder: '',
        helpText: '',
        inputType: '',
        section: 'General',
        format: '',
        min: null,
        max: null,
        pattern: '',
        quickToggle: false
      })
    })
  }

  function onFrontendFieldDragStart(index) {
    frontendFieldDragIndex.value = index
  }

  function onFrontendFieldDragOver(index) {
    frontendFieldDragOver.value = index
  }

  function onFrontendFieldDrop(index) {
    const from = frontendFieldDragIndex.value
    if (from === null || from === undefined) return
    if (!frontendEntidadActual.value?.fields?.length) return
    if (from === index) {
      frontendFieldDragIndex.value = null
      frontendFieldDragOver.value = null
      return
    }

    const list = frontendEntidadActual.value.fields
    const [moved] = list.splice(from, 1)
    list.splice(index, 0, moved)
    frontendFieldDragIndex.value = null
    frontendFieldDragOver.value = null
  }

  function onFrontendFieldDragEnd() {
    frontendFieldDragIndex.value = null
    frontendFieldDragOver.value = null
  }

  function abrirFrontendField(field) {
    frontendFieldActual.value = field
    mostrarFrontendFieldDialog.value = true
  }

  function onFrontendEntityDragStart(index) {
    frontendEntityDragIndex.value = index
  }

  function onFrontendEntityDragOver(index) {
    frontendEntityDragOver.value = index
  }

  function onFrontendEntityDrop(index) {
    const from = frontendEntityDragIndex.value
    if (from === null || from === undefined) return
    if (!frontendConfig.value?.entities?.length) return
    if (from === index) {
      frontendEntityDragIndex.value = null
      frontendEntityDragOver.value = null
      return
    }

    const list = frontendConfig.value.entities
    const [moved] = list.splice(from, 1)
    list.splice(index, 0, moved)
    frontendEntityDragIndex.value = null
    frontendEntityDragOver.value = null
  }

  function onFrontendEntityDragEnd() {
    frontendEntityDragIndex.value = null
    frontendEntityDragOver.value = null
  }

  async function abrirFrontendEntidad(entity) {
    frontendEntidadActual.value = entity
    ensureFrontendEntityConfig(frontendEntidadActual.value)
    const fields = await campoService.getByEntity(systemId, entity.entityId || entity.id)
    ensureFrontendFieldConfigs(frontendEntidadActual.value, fields.data || [])
    mostrarFrontendDialog.value = true
  }

  async function generarBackend() {
    const nombre = sistema.value?.name || 'sistema'
    const ok = window.confirm(`Generar backend para ${nombre}?`)
    if (!ok) return

    try {
      const { data } = await sistemaService.generarBackend(systemId, false)
      const outputPath = data?.outputPath || data?.OutputPath
      const restoreOk = data?.restoreOk ?? data?.RestoreOk
      const restoreError = data?.restoreError || data?.RestoreError
      window.alert(`Backend generado en:\n${outputPath}`)
      if (restoreOk === false) {
        window.alert(`dotnet restore fallo:\n${restoreError || 'Revisa la consola del backend.'}`)
      }
    } catch (error) {
      const message =
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        'Error al generar backend.'

      if (message.includes('overwrite=true')) {
        const overwrite = window.confirm(`${message}\n\nDeseas reemplazarlo?`)
        if (overwrite) {
          try {
            const { data } = await sistemaService.generarBackend(systemId, true)
            const outputPath = data?.outputPath || data?.OutputPath
            window.alert(`Backend generado en:\n${outputPath}`)
            return
          } catch (innerError) {
            const innerMessage =
              innerError?.response?.data?.message ||
              innerError?.response?.data?.Message ||
              'Error al reemplazar el backend.'
            window.alert(innerMessage)
            return
          }
        }
      }

      window.alert(message)
    }
  }

  async function generarFrontend() {
    const nombre = sistema.value?.name || 'sistema'
    const ok = window.confirm(`Generar frontend para ${nombre}?`)
    if (!ok) return

    try {
      const { data } = await sistemaService.generarFrontend(systemId, false)
      const outputPath = data?.outputPath || data?.OutputPath
      await cargarMenuTree()
      window.alert(`Frontend generado en:\n${outputPath}`)
    } catch (error) {
      const message =
        error?.response?.data?.message ||
        error?.response?.data?.Message ||
        'Error al generar frontend.'

      if (message.includes('overwrite=true')) {
        const overwrite = window.confirm(`${message}\n\nDeseas reemplazarlo?`)
        if (overwrite) {
          try {
            const { data } = await sistemaService.generarFrontend(systemId, true)
            const outputPath = data?.outputPath || data?.OutputPath
            await cargarMenuTree()
            window.alert(`Frontend generado en:\n${outputPath}`)
            return
          } catch (innerError) {
            const innerMessage =
              innerError?.response?.data?.message ||
              innerError?.response?.data?.Message ||
              'Error al reemplazar el frontend.'
            window.alert(innerMessage)
            return
          }
        }
      }

      window.alert(message)
    }
  }

  onMounted(async () => {
    const savedBase = localStorage.getItem('backendConsoleBaseUrl')
    apiConsole.value.baseUrl = savedBase || backendBaseUrl.value
    await cargarSistema()
    sqlConsole.value.script = sqlScriptTemplate.value
    await cargarEntidades()
    await cargarRelaciones()
    await cargarBackendConfig()
    await cargarFrontendConfig()
    await checkFrontendHealth()
    if (tab.value === 'herramientas') {
      startHealthPolling()
      startLogsPolling()
    }
  })

  onBeforeUnmount(() => {
    stopHealthPolling()
    stopLogsPolling()
  })

  function entidadNombre(id) {
    const entidad = entidades.value.find(e => e.id === id)
    return entidad?.displayName || entidad?.name || `#${id}`
  }

  return {
    abrirBackendEntidad,
    abrirFrontendEntidad,
    abrirFrontendField,
    apiConsole,
    apiExampleRequestText,
    apiExampleResponseText,
    apiMethodColor,
    apiMethodOptions,
    apiRouteFilterMethod,
    apiRouteFilterText,
    apiRouteOptions,
    apiRouteSeleccionada,
    apiSelectedEntity,
    backendAuthOptions,
    backendBasePath,
    backendBaseUrl,
    backendConfigService,
    backendEntidadActual,
    backendEntities,
    backendHealth,
    backendHealthColor,
    backendHealthLabel,
    backendLogRef,
    backendLogs,
    backendPersistenceOptions,
    backendPort,
    backendRequiredOptions,
    backendSortOptions,
    backendSystemAuthOptions,
    backendSystemConfig,
    buildExampleObject,
    campoSeleccionado,
    campoService,
    campos,
    cargarBackendConfig,
    cargarBackendLogs,
    cargarCampos,
    cargarEntidades,
    cargarFrontendConfig,
    cargarMenuTree,
    cargarRelaciones,
    cargarSistema,
    checkBackendHealth,
    checkFrontendHealth,
    configurarApi,
    copiarTexto,
    detenerBackend,
    detenerFrontend,
    editarCampo,
    editarEntidad,
    editarRelacion,
    ejecutarSqlConsole,
    eliminarEntidad,
    endpointConfigKeyMap,
    endpointKeyFromRoute,
    endpointOnlyKey,
    endpointOnlyMode,
    endpointOnlyTitle,
    endpointPanel,
    endpointSoftDeleteOptions,
    endpointTitleMap,
    ensureBackendSystemConfig,
    ensureFrontendEntityConfig,
    ensureFrontendFieldConfigs,
    ensureFrontendSystemConfig,
    entidadNombre,
    entidadSeleccionada,
    entidadSeleccionadaEdicion,
    entidadService,
    entidades,
    enviarApiConsole,
    esperarBackendOnline,
    esperarFrontendOnline,
    fieldKey,
    filteredApiRoutes,
    formatLogTime,
    frontendAuthModeOptions,
    frontendBaseUrl,
    frontendConfig,
    frontendConfigService,
    frontendDensityOptions,
    frontendDialog,
    frontendEntidadActual,
    frontendEntityDragIndex,
    frontendEntityDragOver,
    frontendEntityLabel,
    frontendFieldActual,
    frontendFieldDragIndex,
    frontendFieldDragOver,
    frontendFieldFormatOptions,
    frontendFieldInputOptions,
    frontendFormLayoutOptions,
    frontendHealth,
    frontendHealthColor,
    frontendHealthLabel,
    frontendItemsPerPageText,
    frontendPort,
    frontendSortOptions,
    frontendUiModeOptions,
    generarBackend,
    generarFrontend,
    getCreateFields,
    getEndpointConfig,
    getResponseFields,
    getUpdateFields,
    guardarBackendConfig,
    guardarFrontendConfig,
    headersApiRoutes,
    headersBackend,
    headersBackendFields,
    headersCampos,
    headersEntidades,
    headersRelaciones,
    healthIntervalId,
    iniciarBackend,
    iniciarFrontend,
    limpiarBackendLogs,
    limpiarSqlConsole,
    mostrarBackendDialog,
    mostrarCampoDialog,
    mostrarEntidadDialog,
    mostrarFrontendDialog,
    mostrarFrontendFieldDialog,
    mostrarRelacionDialog,
    nuevaEntidad,
    nuevaRelacion,
    nuevoCampo,
    onFrontendEntityDragEnd,
    onFrontendEntityDragOver,
    onFrontendEntityDragStart,
    onFrontendEntityDrop,
    onFrontendFieldDragEnd,
    onFrontendFieldDragOver,
    onFrontendFieldDragStart,
    onFrontendFieldDrop,
    portsFilePath,
    publicarSistema,
    mostrarPublicarDialog,
    alPublicar,
    reiniciarBackend,
    reiniciarFrontend,
    relacionSeleccionada,
    relacionService,
    relaciones,
    restartDialog,
    route,
    router,
    sampleValueForField,
    scrollBackendLogs,
    seleccionarEntidad,
    shouldShowEndpointPanel,
    sistema,
    sistemaService,
    sqlConsole,
    sqlScriptTemplate,
    sqlTargetSchema,
    startHealthPolling,
    startLogsPolling,
    stopHealthPolling,
    stopLogsPolling,
    systemId,
    systembaseBaseUrl,
    systembasePort,
    tab,
    toKebab,
    toPascalCase,
    usarApiRoute,
    usarEjemploRequest,
    useMenuStore,
    verDatos,
    volver
  }
}
