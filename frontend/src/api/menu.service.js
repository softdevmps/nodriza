import api from './axios'

export default {
  /* =========================
     CONSULTAS
  ========================= */

  // Árbol jerárquico (sidebar / frontend)
  getMenuTree() {
    return api.get('/menu/tree')
  },
  getSidebarTree() {
    return api.get('/menu/sidebar')
  },

  // Listado plano (ABM / admin)
  getAll() {
    return api.get('/menu')
  },

  /* =========================
     ALTAS
  ========================= */

  crear(data) {
    return api.post('/menu', data)
  },

  /* =========================
     EDICIÓN
  ========================= */

  editar(id, data) {
    return api.put(`/menu/${id}`, data)
  },

  /* =========================
     BAJA / DESACTIVAR
  ========================= */

  eliminar(id) {
    return api.delete(`/menu/${id}`)
  }
}
