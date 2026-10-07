// Styles
import 'vuetify/styles'

// Vuetify core
import { createVuetify } from 'vuetify'
import frontendConfig from '../config/frontend-config.json'

// Components & directives (CLAVE)
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// Icons
import { aliases, mdi } from 'vuetify/iconsets/mdi'
import '@mdi/font/css/materialdesignicons.css'

const system = frontendConfig?.system || {}
const theme = frontendConfig?.theme || {}
const themeDark = frontendConfig?.themeDark || {}
const brand = theme?.brand || {}
const brandDark = themeDark?.brand || brand

export const vuetify = createVuetify({
    components,
    directives,
    icons: {
        defaultSet: 'mdi',
        aliases,
        sets: {
            mdi
        }
    },
    theme: {
        defaultTheme: (typeof localStorage !== 'undefined' && localStorage.getItem('sb-theme') === 'dark') ? 'systembaseDark' : 'systembase',
        themes: {
            systembase: {
                dark: false,
                colors: {
                    primary: brand.primary || system.primaryColor || '#1d4ed8',
                    secondary: brand.secondary || system.secondaryColor || '#0ea5e9',
                    accent: brand.accent || '#f97316',
                    background: theme.background || '#f8fafc',
                    surface: theme.surface || '#ffffff',
                    error: '#ef4444',
                    info: '#0ea5e9',
                    success: '#22c55e',
                    warning: '#f59e0b'
                }
            },
            systembaseDark: {
                dark: true,
                colors: {
                    primary: brandDark.primary || system.primaryColor || '#1d4ed8',
                    secondary: brandDark.secondary || system.secondaryColor || '#0ea5e9',
                    accent: brandDark.accent || '#f59e0b',
                    background: themeDark.background || '#0b1120',
                    surface: themeDark.surface || '#0f172a',
                    error: '#f87171',
                    info: '#38bdf8',
                    success: '#4ade80',
                    warning: '#fbbf24'
                }
            }
        }
    }
})
