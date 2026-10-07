import api from './axios';

export default {
  getBySystem(systemId) {
    return api.get(`/sistemas/${systemId}/relaciones`);
  },

  crear(systemId, data) {
    return api.post(`/sistemas/${systemId}/relaciones`, data);
  },

  editar(systemId, id, data) {
    return api.put(`/sistemas/${systemId}/relaciones/${id}`, data);
  }
};
