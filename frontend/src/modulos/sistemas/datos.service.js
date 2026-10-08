import api from '../../comun/api/axios';

export default {
  // consulta (opcional): { take, skip, buscar, filtroCampo, filtroValor, ordenarPor, orden }.
  // El total sin paginar viene en el header X-Total-Count.
  listar(systemId, entityId, consulta) {
    return api.get(`/sistemas/${systemId}/entidades/${entityId}/datos`, {
      params: consulta
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
