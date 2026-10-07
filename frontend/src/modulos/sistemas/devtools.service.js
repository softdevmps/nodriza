import api from '../../comun/api/axios';

export default {
  restartBackend() {
    return api.post('/dev/restart');
  }
};
