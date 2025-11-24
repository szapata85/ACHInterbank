export const environment = {
  production: false,
  /**
   * URL base para los endpoints REST del backend ACH Interbank.
   * Incluye el prefijo /api para simplificar el consumo desde ApiService.
   */
  apiBaseUrl: 'http://localhost:7269/api',
  /** Endpoint relativo para las operaciones de autenticación */
  authEndpoint: 'auth',
  /** Tiempo máximo de espera para peticiones HTTP (en milisegundos) */
  requestTimeoutMs: 15000,
  /** Versión visible de la aplicación */
  appVersion: '0.1.0'
};
