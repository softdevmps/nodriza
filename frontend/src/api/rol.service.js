import api from './axios';

export default {
    getAll() {
        return api.get('/roles');
    },

    getById(id) {
        return api.get(`/roles/${id}`);
    },

    crear(data) {
        return api.post('/roles', data);
    },

    editar(id, data) {
        return api.put(`/roles/${id}`, data);
    },

    cambiarEstado(id, activo) {
        return api.put(`/roles/${id}/estado`, null, {
            params: { activo }
        });
    },

    asignarMenus(id, menusIds) {
        return api.put(`/roles/${id}/menus`, {
            menusIds
        });
    },

    getSystemMenus(id) {
        return api.get(`/roles/${id}/system-menus`);
    },

    asignarSystemMenus(id, systemIds) {
        return api.put(`/roles/${id}/system-menus`, {
            systemIds
        });
    },

    getPermissions(id, systemId) {
        return api.get(`/roles/${id}/permissions/${systemId}`);
    },

    asignarPermissions(id, systemId, permissionIds) {
        return api.put(`/roles/${id}/permissions/${systemId}`, {
            permissionIds
        });
    }
};
