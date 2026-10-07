<template>
  <v-container fluid>
    <v-row class="mb-6 align-center sb-page-header">
      <v-col>
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="26">mdi-apps</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Sistemas</h2>
            <span class="sb-page-subtitle text-body-2">
              Administracion de sistemas generados
            </span>
          </div>
        </div>
      </v-col>

      <v-col cols="auto" class="d-flex ga-2">
        <v-btn color="primary" variant="tonal" @click="nuevoSistema">
          <v-icon left>mdi-plus</v-icon>
          Nuevo sistema
        </v-btn>
      </v-col>
    </v-row>

    <v-card elevation="2" class="sistemas-card card">
      <v-card-title class="d-flex align-center justify-space-between">
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary">mdi-apps</v-icon>
          <span class="text-h6 font-weight-medium">Listado de sistemas</span>
        </div>
        <v-chip color="primary" variant="outlined" size="small">
          {{ sistemas.length }} sistemas
        </v-chip>
      </v-card-title>

      <v-divider />

      <v-data-table :headers="headers" :items="sistemas" class="sistemas-table table" density="comfortable" hover>
        <template #item.status="{ item }">
          <v-chip size="small" :color="item.status === 'published' ? 'green' : 'grey'">
            {{ item.status }}
          </v-chip>
        </template>

        <template #item.isActive="{ item }">
          <v-chip size="small" :color="item.isActive ? 'green' : 'grey'">
            {{ item.isActive ? 'Activo' : 'Inactivo' }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <v-tooltip text="Editar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="primary" variant="text" @click="editarSistema(item)">
                <v-icon>mdi-pencil</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Disenar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="secondary" variant="text" @click="disenar(item)">
                <v-icon>mdi-vector-square</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Publicar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="green" variant="text" @click="publicar(item)">
                <v-icon>mdi-rocket-launch</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Eliminar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="red" variant="text" @click="eliminarSistema(item)">
                <v-icon>mdi-delete</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Exportar ZIP">
            <template #activator="{ props }">
              <v-btn
                v-if="isAdmin"
                v-bind="props"
                icon
                size="small"
                color="blue"
                variant="text"
                @click="exportarZip(item)"
              >
                <v-icon>mdi-download</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Generar carpeta">
            <template #activator="{ props }">
              <v-btn
                v-if="isAdmin"
                v-bind="props"
                icon
                size="small"
                color="indigo"
                variant="text"
                @click="exportarWorkspace(item)"
              >
                <v-icon>mdi-folder</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

        </template>
      </v-data-table>
    </v-card>

    <SistemaDialog v-model="mostrarDialog" :sistema="sistemaSeleccionado" @guardado="cargarSistemas" />
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import SistemaDialog from '../../components/sistemas/SistemaDialog.vue'
import { useMenuStore } from '../../store/menu.store.js'
import usuarioService from '../../api/usuario.service.js'

const router = useRouter()
const { cargarMenuTree } = useMenuStore()

const sistemas = ref([])
const mostrarDialog = ref(false)
const sistemaSeleccionado = ref(null)

const headers = [
  { title: 'Slug', key: 'slug' },
  { title: 'Nombre', key: 'name' },
  { title: 'Namespace', key: 'namespace' },
  { title: 'Status', key: 'status' },
  { title: 'Version', key: 'version' },
  { title: 'Activo', key: 'isActive' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const isAdmin = ref(false)

function decodeJwtPayload(token) {
  if (!token) return null
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => `%${('00' + c.charCodeAt(0).toString(16)).slice(-2)}`)
        .join('')
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}

function getUsuarioIdFromToken() {
  const token = localStorage.getItem('token')
  const payload = decodeJwtPayload(token)
  const id = payload?.usuarioId
  return id ? Number(id) : null
}

async function cargarPerfilAdmin() {
  const usuarioId = getUsuarioIdFromToken()
  if (!usuarioId) {
    isAdmin.value = false
    return
  }

  try {
    const { data } = await usuarioService.obtenerPorId(usuarioId)
    const rol = data?.rol || data?.Rol || ''
    const username = data?.username || data?.Username || ''
    isAdmin.value = String(rol).toLowerCase() === 'admin' || String(username).toLowerCase() === 'admin'
  } catch {
    isAdmin.value = false
  }
}

async function cargarSistemas() {
  const { data } = await sistemaService.getAll()
  sistemas.value = data
}

function nuevoSistema() {
  sistemaSeleccionado.value = null
  mostrarDialog.value = true
}

function editarSistema(item) {
  sistemaSeleccionado.value = item
  mostrarDialog.value = true
}

function disenar(item) {
  router.push(`/sistemas/${item.id}`)
}

async function publicar(item) {
  const ok = window.confirm(`Publicar sistema ${item.name}?`)
  if (!ok) return

  try {
    await sistemaService.publicar(item.id)
    await cargarSistemas()
    await cargarMenuTree()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al publicar el sistema.'
    window.alert(message)
  }
}

async function eliminarSistema(item) {
  const ok = window.confirm(`Eliminar sistema ${item.name}? Esta accion no se puede deshacer.`)
  if (!ok) return

  try {
    await sistemaService.eliminar(item.id)
    await cargarSistemas()
    await cargarMenuTree()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al eliminar el sistema.'
    window.alert(message)
  }
}

async function exportarZip(item) {
  const ok = window.confirm(`Exportar ZIP del sistema ${item.name}?`)
  if (!ok) return

  try {
    const response = await sistemaService.exportarZip(item.id)
    const blob = new Blob([response.data], { type: 'application/zip' })
    const url = window.URL.createObjectURL(blob)

    const disposition = response.headers?.['content-disposition']
    let fileName = `${item.slug || 'system'}-export.zip`
    if (disposition && disposition.includes('filename=')) {
      const match = disposition.match(/filename="?([^"]+)"?/)
      if (match && match[1]) fileName = match[1]
    }

    const link = document.createElement('a')
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (error) {
    let message = 'Error al exportar el sistema.'
    const data = error?.response?.data

    if (data instanceof Blob) {
      try {
        const text = await data.text()
        const parsed = JSON.parse(text)
        message = parsed?.message || parsed?.Message || message
      } catch {
        // ignore parsing errors
      }
    } else {
      message = data?.message || data?.Message || message
    }

    window.alert(message)
  }
}

async function exportarWorkspace(item) {
  const ok = window.confirm(`Generar carpeta del sistema ${item.name} en /systems?`)
  if (!ok) return

  try {
    const { data } = await sistemaService.exportarWorkspace(item.id, false)
    const exportPath = data?.exportPath || data?.ExportPath
    window.alert(`Carpeta generada en:\n${exportPath}`)
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al generar la carpeta.'

    if (message.includes('overwrite=true')) {
      const overwrite = window.confirm(`${message}\n\nDeseas reemplazarla?`)
      if (overwrite) {
        try {
          const { data } = await sistemaService.exportarWorkspace(item.id, true)
          const exportPath = data?.exportPath || data?.ExportPath
          window.alert(`Carpeta generada en:\n${exportPath}`)
          return
        } catch (innerError) {
          const innerMessage =
            innerError?.response?.data?.message ||
            innerError?.response?.data?.Message ||
            'Error al reemplazar la carpeta.'
          window.alert(innerMessage)
          return
        }
      }
    }

    window.alert(message)
  }
}

onMounted(async () => {
  await cargarPerfilAdmin()
  await cargarSistemas()
})
</script>

<style scoped>
.sistemas-card {
  border-radius: 12px;
}

.sistemas-table :deep(thead th) {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.75rem;
  color: #6b7280;
}
</style>
