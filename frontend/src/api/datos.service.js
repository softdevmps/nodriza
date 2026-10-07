import api from './axios';

export default {
  listar(systemId, entityId, take) {
    return api.get(`/sistemas/${systemId}/entidades/${entityId}/datos`, {
      params: take ? { take } : undefined
    });
  },

  crear(systemId, entityId, data) {
    return api.post(`/sistemas/${systemId}/entidades/${entityId}/datos`, data);
  },

  editar(systemId, entityId, id, data) {
    return api.put(`/sistemas/${systemId}/entidades/${entityId}/datos/${id}`, data);
  },

  eliminar(systemId, entityId, id) {
    return api.delete(`/sistemas/${systemId}/entidades/${entityId}/datos/${id}`);
  }
};
