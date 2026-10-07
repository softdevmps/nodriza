import axios from 'axios'
import api from './axios'
import frontendConfig from '../config/frontend-config.json'

function resolveAuthClient() {
  const mode = frontendConfig?.system?.authMode || 'local'
  if (mode !== 'central') {
    return api
  }

  const base = frontendConfig?.system?.authBaseUrl || 'http://localhost:5032/api/v1'
  return axios.create({
    baseURL: base,
    headers: {
      'Content-Type': 'application/json'
    }
  })
}

export const authService = {
  login(data) {
    return resolveAuthClient().post('/auth/login', data)
  },

  register(data) {
    return resolveAuthClient().post('/auth/registrar', data)
  }
}
