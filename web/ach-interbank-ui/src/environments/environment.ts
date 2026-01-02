export const environment = {
  production: false,
  /**
   * URL base para los endpoints REST del backend ACH Interbank.
   * No incluye el prefijo /api porque ApiService lo añade según las rutas solicitadas.
   */
  apiBaseUrl: 'https://localhost:7269',
  /** Endpoint relativo para las operaciones de autenticación */
  authEndpoint: 'auth',
  /** Tiempo máximo de espera para peticiones HTTP (en milisegundos) */
  requestTimeoutMs: 15000,
  /** Versión visible de la aplicación */
  appVersion: '0.1.0'
};
