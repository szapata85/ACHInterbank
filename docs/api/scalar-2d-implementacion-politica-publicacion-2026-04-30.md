# Implementación controlada de política de publicación — Scalar-2D

**Fecha (UTC):** 2026-04-30  
**Objetivo:** materializar en código la política Scalar-2C para 7 rutas no emitidas, sin alterar lógica de negocio ni contratos funcionales.

## 1) Decisión implementada

Se aplicó control explícito de publicación OpenAPI mediante `ApiExplorerSettings(IgnoreApi = true)` en las 7 rutas conciliadas.

## 2) Matriz de estado posterior a implementación

| # | Método | Ruta | Estado OpenAPI aplicado | Clasificación de gobierno | Implementación técnica |
|---:|---|---|---|---|---|
| 1 | GET | `/Servers` | Oculta explícitamente | Interna / no contractual pública | `ServersController` marcado con `IgnoreApi = true` |
| 2 | GET | `/Tests` | Oculta explícitamente | Prueba/desarrollo | `TestsController` marcado con `IgnoreApi = true` |
| 3 | GET | `/Tests/Prueba` | Oculta explícitamente | Prueba/desarrollo | `TestsController` marcado con `IgnoreApi = true` |
| 4 | GET | `/oauth2/jwks` | Oculta explícitamente | Pendiente de revisión de seguridad | Acción `GetJwks` con `IgnoreApi = true` |
| 5 | GET | `/oauth2/TokenClientAssertions` | Oculta explícitamente | Ruta sensible: mantener fuera de OpenAPI | Acción `TokenClientAssertions` con `IgnoreApi = true` |
| 6 | POST | `/oauth2/client-assertion` | Oculta explícitamente | Pendiente de revisión de seguridad | Acción `Authenticate` con `IgnoreApi = true` |
| 7 | POST | `/oauth2/Genearte-client-assertion` | Oculta explícitamente | No publicable hasta corrección de nombre | Acción `GenerateClientAssertion` con `IgnoreApi = true` |

## 3) Garantías de no impacto funcional

1. No se cambiaron rutas HTTP.
2. No se alteró lógica de negocio.
3. No se cambiaron permisos/atributos de autorización existentes.
4. No se modificaron contratos request/response.
5. El cambio solo afecta exposición documental en OpenAPI/Scalar.

## 4) Excepciones de gobierno registradas

- `/Servers`, `/Tests`, `/Tests/Prueba`: excluidas por naturaleza interna o de prueba.
- `/oauth2/jwks` y `/oauth2/client-assertion`: bloqueadas a publicación hasta cierre de revisión de seguridad.
- `/oauth2/TokenClientAssertions`: exclusión explícita por sensibilidad.
- `/oauth2/Genearte-client-assertion`: exclusión explícita por sensibilidad y defecto nominal de ruta.

## 5) Próximo paso recomendado

Realizar fase Scalar-2E de validación OpenAPI real post-implementación para confirmar trazabilidad:
- que las 7 rutas continúan fuera del documento OpenAPI publicado;
- que no hubo regresiones de cobertura en las rutas sí publicables.
