import { createRouter, createWebHistory } from 'vue-router'

import Login from '../modulos/auth/Login.vue'
import Register from '../modulos/auth/Register.vue'
import Home from '../modulos/home/Home.vue'
import SistemaRuntime from '../modulos/runtime/SistemaRuntime.vue'
import MainLayout from '../comun/layout/MainLayout.vue'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: Login },
  { path: '/register', component: Register },
  {
    path: '/',
    component: MainLayout,
    children: [
      { path: 'home', component: Home },
      { path: ':entity', component: SistemaRuntime },
      { path: ':autoPath(.*)*', redirect: '/home' }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const publicRoutes = ['/login', '/register']

  if (!token && !publicRoutes.includes(to.path)) {
    next('/login')
  } else {
    next()
  }
})

export default router
