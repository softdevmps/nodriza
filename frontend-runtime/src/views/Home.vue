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
                <p>Vista rápida del estado del sistema y la calidad de los datos.</p>
              </div>
            </div>
            <div class="hero-actions">
              <v-btn class="sb-btn ghost" variant="text" @click="goTo(incidentesRoute)">
                Ver incidentes
              </v-btn>
              <v-btn class="sb-btn primary" @click="goTo(jobsRoute)">
                Ver jobs
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
            <v-icon class="mr-2" color="primary">mdi-map-marker-check-outline</v-icon>
            Calidad de ubicaciones
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="progress-row">
              <span>Con coordenadas</span>
              <span>{{ coordsWith }} / {{ totalIncidentes }}</span>
            </div>
            <v-progress-linear :model-value="coordsRatio" color="green" height="8" rounded />

            <div class="progress-row mt-3">
              <span>Sin coordenadas</span>
              <span>{{ coordsWithout }}</span>
            </div>
            <v-progress-linear :model-value="100 - coordsRatio" color="orange" height="8" rounded />

            <div class="progress-row mt-3">
              <span>Confianza promedio</span>
              <span>{{ avgConfidenceLabel }}</span>
            </div>
            <v-progress-linear :model-value="avgConfidence * 100" color="primary" height="8" rounded />
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
            <div v-else-if="recentIncidentes.length === 0" class="text-caption text-medium-emphasis">
              No hay incidentes recientes.
            </div>
            <div v-else class="recent-list">
              <div v-for="item in recentIncidentes" :key="item.Id || item.id" class="recent-item">
                <div class="recent-dot"></div>
                <div>
                  <div class="recent-title">
                    {{ item.LugarNormalizado || item.LugarTexto || 'Sin lugar' }}
                  </div>
                  <div class="recent-meta">
                    {{ formatDate(item.FechaHora || item.CreatedAt) }} · {{ item.Descripcion || 'Sin descripción' }}
                  </div>
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4" dense>
      <v-col cols="12" md="6">
        <v-card class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-waveform</v-icon>
            Audio
          </v-card-title>
          <v-divider />
          <v-card-text class="split-stats">
            <div>
              <div class="stat-label">Audios cargados</div>
              <div class="stat-value">{{ totalAudios }}</div>
            </div>
            <div>
              <div class="stat-label">Duración total</div>
              <div class="stat-value">{{ totalDurationLabel }}</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="6">
        <v-card class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-cogs</v-icon>
            Jobs
          </v-card-title>
          <v-divider />
          <v-card-text class="split-stats">
            <div>
              <div class="stat-label">En proceso</div>
              <div class="stat-value">{{ jobsProcessing }}</div>
            </div>
            <div>
              <div class="stat-label">Errores</div>
              <div class="stat-value">{{ jobsError }}</div>
            </div>
            <div>
              <div class="stat-label">Completados</div>
              <div class="stat-value">{{ jobsDone }}</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../api/runtime.service'
import frontendConfig from '../config/frontend-config.json'
import { toKebab } from '../utils/slug'

const router = useRouter()
const loading = ref(true)
const error = ref('')

const incidentes = ref([])
const jobs = ref([])
const audios = ref([])

const entities = frontendConfig?.entities || []

const incidentesRoute = computed(() => {
  const entity = entities.find(e => (e?.name || '').toLowerCase() === 'incidentes')
  return `/${toKebab(entity?.routeSlug || entity?.name || 'incidentes')}`
})

const jobsRoute = computed(() => {
  const entity = entities.find(e => (e?.name || '').toLowerCase().includes('incidentejobs'))
  return `/${toKebab(entity?.routeSlug || entity?.name || 'incidente-jobs')}`
})

function goTo(route) {
  router.push(route)
}

function parseNumber(value) {
  if (value == null) return null
  const num = Number(String(value).replace(',', '.'))
  return Number.isFinite(num) ? num : null
}

function parseDateValue(raw) {
  if (!raw) return null
  if (raw instanceof Date) {
    const time = raw.getTime()
    return Number.isNaN(time) ? null : time
  }
  if (typeof raw === 'number') {
    if (!Number.isFinite(raw)) return null
    return raw > 1e12 ? raw : raw * 1000
  }
  const str = String(raw).trim()
  if (!str) return null

  const direct = new Date(str)
  if (!Number.isNaN(direct.getTime())) return direct.getTime()

  const match = str.match(/(\d{1,2})[\/-](\d{1,2})[\/-](\d{2,4})(?:[\s,]+(\d{1,2})(?::(\d{2}))?)?/)
  if (!match) return null
  const day = Number(match[1])
  const month = Number(match[2])
  let year = Number(match[3])
  const hour = match[4] ? Number(match[4]) : 0
  const minute = match[5] ? Number(match[5]) : 0
  if (!Number.isFinite(day) || !Number.isFinite(month) || !Number.isFinite(year)) return null
  if (year < 100) year += 2000
  const parsed = new Date(year, month - 1, day, hour, minute)
  return Number.isNaN(parsed.getTime()) ? null : parsed.getTime()
}

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  const keyMap = new Map(Object.keys(copy).map(k => [k.toLowerCase(), k]))
  const ensure = name => {
    if (copy[name] !== undefined) return
    const matchKey = keyMap.get(String(name).toLowerCase())
    if (matchKey) copy[name] = copy[matchKey]
  }
  ;[
    'LugarNormalizado',
    'LugarTexto',
    'Descripcion',
    'FechaHora',
    'CreatedAt',
    'UpdateAt',
    'Lat',
    'Lng',
    'Confidence'
  ].forEach(ensure)
  return copy
}

function hasCoords(item) {
  const lat = parseNumber(item?.Lat ?? item?.lat)
  const lng = parseNumber(item?.Lng ?? item?.lng)
  return lat != null && lng != null
}

function getStatus(item) {
  return (item?.Status ?? item?.status ?? item?.Step ?? item?.step ?? '').toString().toLowerCase()
}

const totalIncidentes = computed(() => incidentes.value.length)
const coordsWith = computed(() => incidentes.value.filter(hasCoords).length)
const coordsWithout = computed(() => Math.max(totalIncidentes.value - coordsWith.value, 0))
const coordsRatio = computed(() => totalIncidentes.value ? Math.round((coordsWith.value / totalIncidentes.value) * 100) : 0)

const avgConfidence = computed(() => {
  const values = incidentes.value
    .map(i => parseNumber(i?.Confidence ?? i?.confidence))
    .filter(v => v != null)
  if (!values.length) return 0
  return values.reduce((a, b) => a + b, 0) / values.length
})

const avgConfidenceLabel = computed(() => avgConfidence.value ? `${avgConfidence.value.toFixed(2)}` : '—')

const jobsProcessing = computed(() => jobs.value.filter(j => ['processing', 'pending', 'running', 'queued'].includes(getStatus(j))).length)
const jobsError = computed(() => jobs.value.filter(j => getStatus(j) === 'error').length)
const jobsDone = computed(() => jobs.value.filter(j => getStatus(j) === 'done').length)

const totalAudios = computed(() => audios.value.length)
const totalDuration = computed(() => audios.value
  .map(a => parseNumber(a?.DurationSec ?? a?.durationSec))
  .filter(v => v != null)
  .reduce((a, b) => a + b, 0))

const totalDurationLabel = computed(() => {
  if (!totalDuration.value) return '—'
  const minutes = Math.floor(totalDuration.value / 60)
  const seconds = Math.round(totalDuration.value % 60)
  return `${minutes}m ${seconds}s`
})

function getDateValue(item) {
  const raw = item?.UpdateAt ?? item?.CreatedAt ?? item?.FechaHora ?? item?.updateAt ?? item?.createdAt ?? item?.fechaHora
  return parseDateValue(raw)
}

function getSortValue(item) {
  const dateValue = getDateValue(item)
  if (dateValue) return dateValue
  const idValue = parseNumber(item?.Id ?? item?.id)
  return idValue ?? 0
}

const recentIncidentes = computed(() => {
  return [...incidentes.value]
    .sort((a, b) => getSortValue(b) - getSortValue(a))
    .slice(0, 5)
})

const lastUpdatedLabel = computed(() => {
  if (!incidentes.value.length) return '—'
  const times = incidentes.value.map(getDateValue).filter(t => t != null)
  if (!times.length) return '—'
  return new Date(Math.max(...times)).toLocaleString('es-AR')
})

const statCards = computed(() => ([
  { label: 'Incidentes', value: totalIncidentes.value, icon: 'mdi-alert-circle-outline' },
  { label: 'Con coords', value: coordsWith.value, icon: 'mdi-map-marker', color: 'green' },
  { label: 'Sin coords', value: coordsWithout.value, icon: 'mdi-map-marker-off-outline', color: 'orange' },
  { label: 'Jobs activos', value: jobsProcessing.value, icon: 'mdi-timer-sand', color: 'orange' },
  { label: 'Errores', value: jobsError.value, icon: 'mdi-alert-outline', color: 'red' },
  { label: 'Audios', value: totalAudios.value, icon: 'mdi-waveform' }
]))

function formatDate(value) {
  const parsed = parseDateValue(value)
  if (!parsed) return ''
  return new Date(parsed).toLocaleString('es-AR')
}

async function load() {
  loading.value = true
  error.value = ''
  const results = await Promise.allSettled([
    runtimeApi.list('incidentes'),
    runtimeApi.list('incidente-jobs'),
    runtimeApi.list('incidente-audio')
  ])

  const [incRes, jobRes, audioRes] = results
  incidentes.value = incRes.status === 'fulfilled' ? (incRes.value?.data || []).map(normalizeRecord) : []
  jobs.value = jobRes.status === 'fulfilled' ? (jobRes.value?.data || []).map(normalizeRecord) : []
  audios.value = audioRes.status === 'fulfilled' ? (audioRes.value?.data || []).map(normalizeRecord) : []
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

.split-stats {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
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
