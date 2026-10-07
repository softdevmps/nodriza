import api from './axios';

export default {
  getAll() {
    return api.get('/sistemas');
  },

  getById(id) {
    return api.get(`/sistemas/${id}`);
  },

  getBySlug(slug) {
    return api.get(`/sistemas/slug/${slug}`);
  },

  crear(data) {
    return api.post('/sistemas', data);
  },

  editar(id, data) {
    return api.put(`/sistemas/${id}`, data);
  },

  eliminar(id) {
    return api.delete(`/sistemas/${id}`);
  },

  publicar(id) {
    return api.post(`/sistemas/${id}/publicar`);
  },

  exportarZip(id) {
    return api.post(`/sistemas/${id}/export?mode=zip&source=workspace`, null, { responseType: 'blob' });
  },

  exportarWorkspace(id, overwrite = false) {
    const flag = overwrite ? 'true' : 'false'
    return api.post(`/sistemas/${id}/export?mode=workspace&overwrite=${flag}`);
  },

  ejecutarSql(id, script, importMetadata = true) {
    return api.post(`/sistemas/${id}/sql/execute`, { script, importMetadata });
  },

  generarBackend(id, overwrite = false) {
    const flag = overwrite ? 'true' : 'false'
    return api.post(`/sistemas/${id}/generar-backend?overwrite=${flag}`);
  },

  generarFrontend(id, overwrite = false) {
    const flag = overwrite ? 'true' : 'false'
    return api.post(`/sistemas/${id}/generar-frontend?overwrite=${flag}`);
  },

  iniciarBackend(id) {
    return api.post(`/sistemas/${id}/backend/start`);
  },

  detenerBackend(id) {
    return api.post(`/sistemas/${id}/backend/stop`);
  },

  pingBackend(id) {
    return api.get(`/sistemas/${id}/backend/ping`);
  },

  logsBackend(id, after = 0, take = 200) {
    return api.get(`/sistemas/${id}/backend/logs`, { params: { after, take } });
  },

  iniciarFrontend(id) {
    return api.post(`/sistemas/${id}/frontend/start`);
  },

  detenerFrontend(id) {
    return api.post(`/sistemas/${id}/frontend/stop`);
  },

  pingFrontend(id) {
    return api.get(`/sistemas/${id}/frontend/ping`);
  },

  logsFrontend(id, after = 0, take = 200) {
    return api.get(`/sistemas/${id}/frontend/logs`, { params: { after, take } });
  }
};
