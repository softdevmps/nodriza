import api from '../../comun/api/axios'

export default {
  // params (opcional): { take, skip, search }. Si la API pagina, el total viene en X-Total-Count.
  list(route, params) {
    return api.get(`/${route}`, { params })
  },

  get(route, id) {
    return api.get(`/${route}/${id}`)
  },

  create(route, payload) {
    return api.post(`/${route}`, payload)
  },

  update(route, id, payload) {
    return api.put(`/${route}/${id}`, payload)
  },

  remove(route, id) {
    return api.delete(`/${route}/${id}`)
  }
}
