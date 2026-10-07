import api from './axios';

export default {
  restartBackend() {
    return api.post('/dev/restart');
  }
};
