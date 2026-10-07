import api from './axios';

export default {
  getBySystem(systemId) {
    return api.get(`/sistemas/${systemId}/frontend-config`);
  },

  guardar(systemId, payload) {
    return api.put(`/sistemas/${systemId}/frontend-config`, payload);
  }
};
