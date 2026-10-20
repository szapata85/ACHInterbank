# ACH Interbank UI

Aplicación Angular diseñada para crear transacciones ACH consumiendo el `TransactionsController` expuesto por la API .NET.

## Características

- **Formulario reactivo seguro** con validaciones estrictas, normalización de datos y prevención de envíos mientras el contenido es inválido.
- **Manejo de addendas** dinámico, permitiendo agregar o eliminar registros antes del envío.
- **Integración protegida** mediante un interceptor que adjunta el token `Bearer` almacenado de forma segura y fuerza el uso de `withCredentials`.
- **Listas de instituciones** consultadas desde el endpoint `/FinancialInstitution`, con filtrado automático para mostrar únicamente las entidades activas.
- **Retroalimentación clara** para el usuario con mensajes de éxito o error y restablecimiento automático del formulario.

## Configuración

1. Instale dependencias:

   ```bash
   npm install
   ```

2. Ajuste la variable `apiBaseUrl` en `src/environments/environment*.ts` para apuntar al host de la API.

3. Ejecute la aplicación:

   ```bash
   npm start
   ```

La aplicación quedará disponible en `http://localhost:4200/` y enviará solicitudes autenticadas al servicio de transacciones.

## Revisión de dependencias npm

La remediación del audit de octubre de 2026, la sustitución de Puppeteer por Playwright
para Karma y el procedimiento de regeneración verificada del lockfile están descritos
en [NPM_SECURITY_REMEDIATION.md](NPM_SECURITY_REMEDIATION.md).
Para resolver el problema de `npm ci` en Windows, consulta
[NPM_SECURITY_WINDOWS_FIX.md](NPM_SECURITY_WINDOWS_FIX.md).
