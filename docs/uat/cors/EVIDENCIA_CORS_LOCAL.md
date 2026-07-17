# Evidencia CORS local

Fecha: 2026-07-16. Datos y credenciales no se incluyen.

## Validación HTTP real

| Caso | Resultado |
| --- | --- |
| `GET /api/users/branding`, Origin 743 | HTTP 200; origen exacto; credenciales habilitadas; `Vary: Origin` |
| Preflight branding | HTTP 204; GET y encabezados solicitados permitidos; sin JWT |
| Preflight login real `/Auth/login` | HTTP 204; POST permitido; origen exacto |
| Origen no autorizado | Sin `Access-Control-Allow-Origin`; no fue reflejado |
| Origen 4200 | Autorizado en Development |
| Configuración sin orígenes | Localhost 743 bloqueado de forma cerrada |

El encabezado permitido nunca fue `*`. Production no agrega localhost automáticamente.

## Pruebas automatizadas

- `CorsPolicyIntegrationTests`: 6/6.
- Suite dirigida de CORS, catálogo, dispatch y endpoint UAT: 28/28.
- Suite backend completa: 1.828 aprobadas, 1 diagnóstica omitida, 0 fallidas.
- Smoke Playwright sin SOAP: 1/1.

## Resultado

**GO CORS local.** Branding, login y navegación usan la política oficial; no se debilitó autenticación ni autorización.
