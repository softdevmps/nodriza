import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { vuetify } from './plugins/vuetify'
import './styles/systembase.css'

const resizeObserverMessages = [
  'ResizeObserver loop completed with undelivered notifications',
  'ResizeObserver loop limit exceeded'
]

function isResizeObserverMessage(args) {
  return args.some(arg => {
    if (!arg) return false
    if (typeof arg === 'string') {
      return resizeObserverMessages.some(msg => arg.includes(msg))
    }
    if (typeof arg?.message === 'string') {
      return resizeObserverMessages.some(msg => arg.message.includes(msg))
    }
    return false
  })
}

const consoleError = console.error.bind(console)
const consoleWarn = console.warn.bind(console)

console.error = (...args) => {
  if (isResizeObserverMessage(args)) return
  consoleError(...args)
}

console.warn = (...args) => {
  if (isResizeObserverMessage(args)) return
  consoleWarn(...args)
}

window.addEventListener('error', event => {
  if (event?.message && resizeObserverMessages.some(msg => event.message.includes(msg))) {
    event.preventDefault()
  }
})

createApp(App)
  .use(router)      // ← si router es undefined, no se ve nada
  .use(vuetify)
  .mount('#app')
