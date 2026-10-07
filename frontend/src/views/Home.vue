<template>
  <v-container fluid class="home-dashboard">
    <v-row class="mb-4">
      <v-col cols="12">
        <v-card class="card hero-card">
          <div class="hero-content">
            <div class="hero-text">
              <div class="hero-icon">
                <v-icon color="primary" size="26">mdi-chart-areaspline</v-icon>
              </div>
              <div>
                <h1>Panel general</h1>
                <p>Vista rápida del estado de tus sistemas y usuarios.</p>
              </div>
            </div>
            <div class="hero-actions">
              <v-btn class="sb-btn ghost" variant="text" @click="$router.push('/sistemas')">
                Ver sistemas
              </v-btn>
              <v-btn class="sb-btn primary" @click="$router.push('/usuarios')">
                Ver usuarios
              </v-btn>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col v-for="card in statCards" :key="card.label" cols="12" sm="6" md="4" lg="2">
        <v-card class="card stat-card">
          <div class="stat-icon">
            <v-icon :color="card.color || 'primary'">{{ card.icon }}</v-icon>
          </div>
          <div>
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value">{{ card.value }}</div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4" dense>
      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-check-circle-outline</v-icon>
            Estado de sistemas
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="progress-row">
              <span>Activos</span>
              <span>{{ activeSystems }} / {{ totalSystems }}</span>
            </div>
            <v-progress-linear :model-value="activeRatio" color="green" height="8" rounded />

            <div class="progress-row mt-3">
              <span>Publicados</span>
              <span>{{ publishedSystems }}</span>
            </div>
            <v-progress-linear :model-value="publishedRatio" color="primary" height="8" rounded />
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-clock-outline</v-icon>
              Actividad reciente
            </div>
            <span class="text-caption text-medium-emphasis">{{ lastUpdatedLabel }}</span>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loading" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="recentSystems.length === 0" class="text-caption text-medium-emphasis">
              No hay sistemas recientes.
            </div>
            <div v-else class="recent-list">
              <div v-for="item in recentSystems" :key="item.Id || item.id" class="recent-item">
                <div class="recent-dot"></div>
                <div>
                  <div class="recent-title">{{ getSystemName(item) }}</div>
                  <div class="recent-meta">
                    {{ formatDate(getSystemDate(item)) }} · {{ getSystemStatusLabel(item) }}
                  </div>
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import sistemaService from '../api/sistema.service'
import usuarioService from '../api/usuario.service'

const loading = ref(true)
const error = ref('')

const systems = ref([])
const users = ref([])

function pick(obj, ...keys) {
  for (const key of keys) {
    const value = obj?.[key]
    if (value !== undefined && value !== null && value !== '') return value
  }
  return null
}

function normalizeStatus(status) {
  const raw = String(status || '').trim().toLowerCase()
  if (!raw) return ''
  if (raw === 'draft') return 'Borrador'
  if (raw === 'published') return 'Publicado'
  if (raw === 'active') return 'Activo'
  if (raw === 'inactive') return 'Inactivo'
  return raw.charAt(0).toUpperCase() + raw.slice(1)
}

function getSystemName(item) {
  return pick(item, 'name', 'Name', 'slug', 'Slug') || 'Sistema'
}

function getSystemDate(item) {
  return pick(item, 'updatedAt', 'UpdatedAt', 'createdAt', 'CreatedAt')
}

function getSystemStatusLabel(item) {
  const explicit = normalizeStatus(pick(item, 'status', 'Status'))
  if (explicit) return explicit

  const isPublished = Boolean(pick(item, 'publishedAt', 'PublishedAt'))
  if (isPublished) return 'Publicado'

  const isActive = Boolean(pick(item, 'isActive', 'IsActive'))
  return isActive ? 'Activo' : 'Sin estado'
}

const totalSystems = computed(() => systems.value.length)
const activeSystems = computed(() => systems.value.filter(s => s?.IsActive || s?.isActive).length)
const publishedSystems = computed(() => systems.value.filter(s => {
  if (s?.PublishedAt || s?.publishedAt) return true
  const status = String(s?.Status || s?.status || '').trim().toLowerCase()
  return status === 'published'
}).length)
const totalUsers = computed(() => users.value.length)
const draftSystems = computed(() => Math.max(totalSystems.value - publishedSystems.value, 0))

const activeRatio = computed(() => totalSystems.value ? Math.round((activeSystems.value / totalSystems.value) * 100) : 0)
const publishedRatio = computed(() => totalSystems.value ? Math.round((publishedSystems.value / totalSystems.value) * 100) : 0)

const statCards = computed(() => ([
  { label: 'Sistemas', value: totalSystems.value, icon: 'mdi-rocket-launch-outline' },
  { label: 'Activos', value: activeSystems.value, icon: 'mdi-check-circle-outline', color: 'green' },
  { label: 'Publicados', value: publishedSystems.value, icon: 'mdi-cloud-upload-outline', color: 'blue' },
  { label: 'Borradores', value: draftSystems.value, icon: 'mdi-file-document-outline', color: 'orange' },
  { label: 'Usuarios', value: totalUsers.value, icon: 'mdi-account-group-outline' }
]))

function parseDateValue(raw) {
  if (!raw) return null
  const direct = new Date(raw)
  if (!Number.isNaN(direct.getTime())) return direct.getTime()
  return null
}

function formatDate(value) {
  const parsed = parseDateValue(value)
  if (!parsed) return ''
  return new Date(parsed).toLocaleString('es-AR')
}

const recentSystems = computed(() => {
  return [...systems.value]
    .sort((a, b) => {
      const ta = parseDateValue(getSystemDate(a)) || 0
      const tb = parseDateValue(getSystemDate(b)) || 0
      return tb - ta
    })
    .slice(0, 5)
})

const lastUpdatedLabel = computed(() => {
  if (!systems.value.length) return '—'
  const times = systems.value
    .map(s => parseDateValue(getSystemDate(s)))
    .filter(Boolean)
  if (!times.length) return '—'
  return new Date(Math.max(...times)).toLocaleString('es-AR')
})

async function load() {
  loading.value = true
  error.value = ''

  const results = await Promise.allSettled([
    sistemaService.getAll(),
    usuarioService.obtener()
  ])

  const [systemsRes, usersRes] = results
  systems.value = systemsRes.status === 'fulfilled' ? (systemsRes.value?.data || []) : []
  users.value = usersRes.status === 'fulfilled' ? (usersRes.value?.data || []) : []

  if (results.some(r => r.status === 'rejected')) {
    error.value = 'No se pudieron cargar algunos datos.'
  }

  loading.value = false
}

onMounted(load)
</script>

<style scoped>
.home-dashboard {
  padding-bottom: 32px;
}

.hero-card {
  padding: 20px 24px;
}

.hero-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.hero-text {
  display: flex;
  gap: 14px;
  align-items: center;
}

.hero-text h1 {
  margin: 0;
  font-size: 1.4rem;
}

.hero-text p {
  margin: 4px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.hero-icon {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.hero-actions {
  display: flex;
  gap: 10px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
}

.stat-icon {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-label {
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--sb-text-soft, var(--sb-muted));
}

.stat-value {
  font-size: 1.2rem;
  font-weight: 600;
}

.progress-row {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  color: var(--sb-text-soft, var(--sb-muted));
  margin-bottom: 6px;
}

.recent-list {
  display: grid;
  gap: 12px;
}

.recent-item {
  display: flex;
  gap: 10px;
  align-items: flex-start;
}

.recent-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--sb-primary);
  margin-top: 6px;
}

.recent-title {
  font-weight: 600;
}

.recent-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

@media (max-width: 960px) {
  .hero-content {
    flex-direction: column;
    align-items: flex-start;
  }
  .hero-actions {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
