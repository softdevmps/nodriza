import api from './axios';

export default {
  getByEntity(systemId, entityId) {
    return api.get(`/sistemas/${systemId}/entidades/${entityId}/campos`);
  },

  crear(systemId, entityId, data) {
    return api.post(`/sistemas/${systemId}/entidades/${entityId}/campos`, data);
  },

  editar(systemId, entityId, id, data) {
    return api.put(`/sistemas/${systemId}/entidades/${entityId}/campos/${id}`, data);
  }
};
