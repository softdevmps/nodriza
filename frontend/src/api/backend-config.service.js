import api from './axios';

export default {
  getBySystem(systemId) {
    return api.get(`/sistemas/${systemId}/backend-config`);
  },

  guardar(systemId, data) {
    return api.put(`/sistemas/${systemId}/backend-config`, data);
  }
};
