import api from './axios';

export default {
  getBySystem(systemId) {
    return api.get(`/sistemas/${systemId}/entidades`);
  },

  getBySystemRuntime(systemId) {
    return api.get(`/sistemas/${systemId}/entidades/runtime`);
  },

  getById(systemId, id) {
    return api.get(`/sistemas/${systemId}/entidades/${id}`);
  },

  getByName(systemId, name) {
    return api.get(`/sistemas/${systemId}/entidades/by-name/${name}`);
  },

  crear(systemId, data) {
    return api.post(`/sistemas/${systemId}/entidades`, data);
  },

  editar(systemId, id, data) {
    return api.put(`/sistemas/${systemId}/entidades/${id}`, data);
  },

  eliminar(systemId, id, dropTable = false) {
    const flag = dropTable ? 'true' : 'false'
    return api.delete(`/sistemas/${systemId}/entidades/${id}?dropTable=${flag}`);
  }
};
