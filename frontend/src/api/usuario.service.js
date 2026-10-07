import api from './axios';

export default {
  obtener() {
    return api.get('/usuarios');
  },

  obtenerPorId(id) {
    return api.get(`/usuarios/${id}`);
  },

  crear(data) {
    return api.post('/usuarios', data);
  },

  editar(id, data) {
    return api.put(`/usuarios/${id}`, data);
  },

  cambiarEstado(id, activo) {
    return api.put(`/usuarios/${id}/estado?activo=${activo}`);
  }
};
