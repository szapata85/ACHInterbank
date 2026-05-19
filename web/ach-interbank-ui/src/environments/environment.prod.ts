export const environment = {
  production: true,
  // Produccion/UAT debe publicar la API detras del mismo host o reverse proxy de la SPA.
  // En Docker Compose, nginx.conf proxya /api, /health, /openapi y /scalar a achinterbank-api:8080.
  apiBaseUrl: '',
  authEndpoint: 'auth',
  requestTimeoutMs: 15000,
  appVersion: '0.1.0'
};
