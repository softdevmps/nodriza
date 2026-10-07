<template>
  <div class="app-shell" :style="themeVars">
    <router-view />
  </div>
</template>

<script setup>
import { computed, ref, watchEffect, provide } from 'vue'
import frontendConfig from './config/frontend-config.json'
import { vuetify } from './plugins/vuetify'

const system = frontendConfig?.system || {}
const theme = frontendConfig?.theme || {}
const darkTheme = frontendConfig?.themeDark || {}

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

const storedMode = typeof localStorage !== 'undefined' ? localStorage.getItem('sb-theme') : null
const colorMode = ref(storedMode === 'dark' ? 'dark' : 'light')
const isDark = computed(() => colorMode.value === 'dark')

const themeVars = computed(() => {
  const defaults = isDark.value ? darkThemeDefaults : lightThemeDefaults
  const activeTheme = isDark.value ? darkTheme : theme
  const baseBrand = theme?.brand || {}
  const darkBrand = darkTheme?.brand || {}
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

watchEffect(() => {
  if (typeof document === 'undefined') return
  const root = document.documentElement
  const vars = themeVars.value || {}
  Object.entries(vars).forEach(([key, value]) => {
    root.style.setProperty(key, String(value))
  })
  root.classList.toggle('sb-dark', isDark.value)
  if (vuetify?.theme?.global?.name) {
    vuetify.theme.global.name.value = isDark.value ? 'systembaseDark' : 'systembase'
  }
})

function toggleTheme() {
  colorMode.value = isDark.value ? 'light' : 'dark'
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem('sb-theme', colorMode.value)
  }
}

provide('colorMode', {
  isDark,
  toggleTheme
})
</script>

<style>
.app-shell {
  min-height: 100vh;
  font-family: var(--sb-font);
  background-color: var(--sb-bg);
  color: var(--sb-text, #0f172a);
  background-image:
    var(--sb-gradient),
    radial-gradient(circle at top right, rgba(15, 23, 42, calc(var(--sb-pattern-opacity) + 0.02)) 0, transparent 55%),
    radial-gradient(circle at 20% 20%, rgba(29, 78, 216, var(--sb-pattern-opacity)) 0, transparent 55%);
  background-attachment: fixed;
}
</style>
