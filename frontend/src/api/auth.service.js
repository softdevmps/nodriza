import api from './axios'

export const authService = {
    login(data) {
        return api.post('/auth/login', data)
    },

    register(data) {
        return api.post('/auth/registrar', data)
    }
}