export const environment = {
  production: true,
  // Produccion/UAT debe publicar la API detras del mismo host o reverse proxy de la SPA.
  // No usar endpoints locales en builds productivos; si el despliegue requiere dominio dedicado,
  // parametrizarlo en el pipeline o reemplazo de ambiente aprobado.
  apiBaseUrl: '',
  authEndpoint: 'auth',
  requestTimeoutMs: 15000,
  appVersion: '0.1.0'
};
