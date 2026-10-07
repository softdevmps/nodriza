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

const storedMode = typeof localStorage !== 'undefined' ? localStorage.getItem('sb-theme') : null
const colorMode = ref(storedMode === 'dark' ? 'dark' : 'light')
const isDark = computed(() => colorMode.value === 'dark')

const themeVars = computed(() => {
  const activeTheme = isDark.value ? darkTheme : theme
  const brand = activeTheme?.brand || theme?.brand || {}
  return {
    '--sb-primary': brand.primary || system.primaryColor || '#1d4ed8',
    '--sb-secondary': brand.secondary || system.secondaryColor || '#0ea5e9',
    '--sb-accent': brand.accent || '#f97316',
    '--sb-primary-soft': activeTheme.primarySoft || theme.primarySoft || 'rgba(29,78,216,0.12)',
    '--sb-bg': activeTheme.background || theme.background || '#f8fafc',
    '--sb-surface': activeTheme.surface || theme.surface || '#ffffff',
    '--sb-muted': activeTheme.muted || theme.muted || '#64748b',
    '--sb-border': activeTheme.border || theme.border || 'rgba(15,23,42,0.12)',
    '--sb-border-soft': activeTheme.borderSoft || theme.borderSoft || 'rgba(15,23,42,0.08)',
    '--sb-radius': `${activeTheme.radius ?? theme.radius ?? 16}px`,
    '--sb-shadow': activeTheme.shadow || theme.shadow || '0 12px 30px rgba(15, 23, 42, 0.12)',
    '--sb-font': activeTheme.fontBody || theme.fontBody || system.fontFamily || "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
    '--sb-font-display': activeTheme.fontDisplay || theme.fontDisplay || "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
    '--sb-gradient': activeTheme.gradient || theme.gradient || 'linear-gradient(135deg, rgba(29,78,216,0.16), rgba(14,165,233,0.08) 45%, rgba(248,250,252,0.95))',
    '--sb-pattern-opacity': activeTheme.patternOpacity ?? theme.patternOpacity ?? 0.06,
    '--sb-header-bg': activeTheme.headerBg || theme.headerBg || 'rgba(255,255,255,0.9)',
    '--sb-text': activeTheme.text || theme.text || '#0f172a',
    '--sb-text-soft': activeTheme.textSoft || theme.textSoft || '#334155'
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
