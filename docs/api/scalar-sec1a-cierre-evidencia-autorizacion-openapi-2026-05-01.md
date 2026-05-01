# Scalar-SEC-1A — Cierre de evidencia de autorización OpenAPI (2026-05-01)

## 1. Resumen ejecutivo
Se cerró la evidencia faltante de Scalar-SEC-1: generación OpenAPI runtime actual, generación/versionado de CSV de seguridad por operación y build final de validación documental.

## 2. Contexto Scalar-SEC-1
Scalar-SEC-1 dejó hallazgos críticos de autorización explícita y dependencia de seguridad global en controllers específicos, pero faltaba versionar evidencia CSV consolidada de `security` por operación en OpenAPI.

## 3. Evidencia pendiente que se cierra
- CSV completo de operaciones OpenAPI con columna de `security`.
- CSV de operaciones críticas OpenAPI con columna de `security`.
- CSV de compatibilidad nominal para Scalar-SEC-1.
- Build final de cierre.

## 4. Resultado build inicial
`dotnet build ACHInterbank.sln -c Release`: exitoso (sin errores; warnings de nulabilidad preexistentes).

## 5. Resultado OpenAPI real actual
- OpenAPI runtime generado desde `http://127.0.0.1:5194/openapi/v1.json`.
- Archivo obtenido: `/tmp/openapi-sec1a.json`.
- Puerto usado: `5194`.

## 6. CSV generados
1. `docs/api/scalar-sec1a-openapi-security-operaciones-2026-05-01.csv`
2. `docs/api/scalar-sec1a-openapi-security-operaciones-criticas-2026-05-01.csv`
3. `docs/api/scalar-sec1-openapi-security-operaciones-2026-05-01.csv`

## 7. Conteos
- `TOTAL_OPERACIONES_OPENAPI=213`
- `OPERACIONES_CON_SECURITY=0`
- `OPERACIONES_SIN_SECURITY=213`
- `TOTAL_CRITICOS_OPENAPI=128`
- `CRITICOS_CON_SECURITY=0`
- `CRITICOS_SIN_SECURITY=128`

## 8. Confirmación de controladores P0
- `TransactionsController`: sigue sin `[Authorize]` explícito.
- `AchTraceabilityController`: sigue sin `[Authorize]` explícito.
- `AchReturnsController`: sigue sin `[Authorize]` explícito.
- En esta ejecución no se detectaron cambios de estado respecto a Scalar-SEC-1 en estos tres controladores.

## 9. Resultado build final
`dotnet build ACHInterbank.sln -c Release`: exitoso.

## 10. Qué no se implementó
## Qué no se implementó en Scalar-SEC-1A
- No se agregó [Authorize].
- No se agregó AllowAnonymous.
- No se cambiaron políticas.
- No se modificó Program.cs.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambió lógica de negocio.
- No se corrigió metadata security de OpenAPI.
- No se declara seguridad API cerrada.
- No se declara producción lista.

## 11. Estado final de Scalar-SEC-1
Con la evidencia CSV y el build final versionados, Scalar-SEC-1 queda cerrado administrativamente en su alcance documental/auditor.

## 12. Veredicto
**Scalar-SEC-1A CERRADO**.
