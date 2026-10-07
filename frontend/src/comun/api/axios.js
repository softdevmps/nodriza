import axios from 'axios'

const api = axios.create({
    // VITE_API_URL permite apuntar a otra API (p. ej. un entorno de pruebas); por defecto, la fábrica local.
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5032/api/v1',
    headers: {
        'Content-Type': 'application/json'
    }
})

api.interceptors.request.use(config => {
    const token = localStorage.getItem('token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

export default api
