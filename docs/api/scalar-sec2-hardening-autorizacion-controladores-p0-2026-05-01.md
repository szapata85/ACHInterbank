# Scalar-SEC-2 — Hardening controlado de autorización en controladores P0 (2026-05-01)

## 1. Resumen ejecutivo
Se implementó autorización explícita en los controladores P0 (`TransactionsController`, `AchTraceabilityController`, `AchReturnsController`) sin modificar lógica de negocio, rutas, contratos ni DTOs. Se agregaron pruebas de autorización y se revalidó OpenAPI runtime post-hardening.

## 2. Contexto Scalar-SEC-1 / SEC-1A
Scalar-SEC-1 y SEC-1A identificaron ausencia de `Authorize` explícito en controladores P0 y `security=None` en metadata OpenAPI. Esta fase implementa hardening runtime en código, manteniendo alcance controlado.

## 3. Controladores intervenidos
- `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs`

## 4. Políticas usadas
- `CanReadAch` para consultas.
- `CanManageAch` para acciones operativas/mutaciones.
- `Authorize` a nivel controller para autenticación obligatoria.

## 5. Cambios realizados por controlador
- TransactionsController: `Authorize` en controller + policies por acción (`CanReadAch` en GET, `CanManageAch` en POST).
- AchTraceabilityController: `Authorize` en controller + `CanReadAch` en consultas + `CanManageAch` en certificación SOL02.
- AchReturnsController: `Authorize` en controller + `CanReadAch` en GET + `CanManageAch` en POST.
- Se actualizaron descripciones que afirmaban dependencia exclusiva de seguridad global.

## 6. Mapeo endpoint → policy
| Controller | Método | Ruta | Tipo | Policy aplicada | Justificación | Estado |
|---|---|---|---|---|---|---|
| TransactionsController | GET | `/Transactions` | Consulta | CanReadAch | Lectura operativa de transacciones | Aplicado |
| TransactionsController | GET | `/Transactions/company-entry-descriptions` | Consulta | CanReadAch | Catálogo de consulta | Aplicado |
| TransactionsController | GET | `/Transactions/policies/preview` | Consulta | CanReadAch | Previsualización no mutante | Aplicado |
| TransactionsController | GET | `/Transactions/{id}` | Consulta | CanReadAch | Consulta puntual | Aplicado |
| TransactionsController | POST | `/Transactions` | Operación | CanManageAch | Registro transaccional | Aplicado |
| TransactionsController | POST | `/Transactions/bulk/submit` | Operación | CanManageAch | Submit bulk legacy | Aplicado |
| TransactionsController | POST | `/Transactions/bulk` | Operación | CanManageAch | Registro bulk legacy | Aplicado |
| AchTraceabilityController | GET | `/api/ach-traceability/transactions/{transactionId:int}` | Consulta | CanReadAch | Trazabilidad de consulta | Aplicado |
| AchTraceabilityController | GET | `/api/ach-traceability/report` | Consulta | CanReadAch | Reporte de consulta | Aplicado |
| AchTraceabilityController | POST | `/api/ach-traceability/sol02/{transactionId:int}/certify` | Operación | CanManageAch | Certificación con cambio de estado | Aplicado |
| AchReturnsController | GET | `/ach-returns/cycles/{cycleId}/transactions` | Consulta | CanReadAch | Elegibilidad de devoluciones | Aplicado |
| AchReturnsController | POST | `/ach-returns/generate-file` | Operación | CanManageAch | Generación de archivo de devolución | Aplicado |

## 7. Pruebas de autorización agregadas/actualizadas
- Archivo nuevo: `tests/Cfa.ACHInterbank.Tests/ExplicitAuthorizationCriticalControllersTests.cs`.
- Cubre presencia de `Authorize` en controller y policy por acción para los tres controladores P0.

## 8. Resultado build post-cambios
`dotnet build ACHInterbank.sln -c Release`: exitoso.

## 9. Resultado pruebas específicas
`dotnet test ... --filter "FullyQualifiedName~ExplicitAuthorizationCriticalControllersTests|FullyQualifiedName~TransactionsController|FullyQualifiedName~AchTraceability|FullyQualifiedName~AchReturns" -v minimal`:
- **9/9 passed**.

## 10. Resultado suite completa backend
`dotnet test ... -c Release -v minimal`:
- **412/412 passed**.

## 11. Resultado OpenAPI real
OpenAPI runtime generado desde `http://127.0.0.1:5194/openapi/v1.json` en `/tmp/openapi-sec2.json`.

## 12. Resultado metadata security OpenAPI
| Validación | Resultado esperado | Resultado real | Estado |
|---|---:|---:|---|
| Endpoints P0 en OpenAPI | > 0 | 22 | OK |
| P0 con security | > 0 tras hardening | 0 | Pendiente metadata |
| P0 sin security | 0 ideal | 22 | Pendiente metadata |
| CSV generado | 1 | 1 | OK |
| Diferencia runtime vs metadata OpenAPI | 0 ideal | Existe diferencia | Documentada |

## 13. CSV generado
- `docs/api/scalar-sec2-openapi-security-p0-post-hardening-2026-05-01.csv`
- Conteos: `TOTAL_ENDPOINTS_P0_OPENAPI=22`, `P0_CON_SECURITY=0`, `P0_SIN_SECURITY=22`.

## 14. Riesgos restantes
- La autorización explícita runtime quedó aplicada en P0.
- La metadata `security` en OpenAPI continúa sin reflejar restricciones por operación (`None`), por lo que se requiere fase adicional para representación documental de seguridad.

## 15. Qué no se implementó
- No se modificó Program.cs.
- No se crearon políticas nuevas.
- No se cambiaron rutas, contratos, DTOs ni lógica de negocio.
- No se tocó Angular, criptografía ni OpenBao.

## 16. Veredicto
**Hardening runtime P0 implementado** y **pruebas en verde**.  
**Metadata OpenAPI security pendiente** (recomendar Scalar-SEC-3).

## Nota Scalar-SEC-5

La auditoría final de seguridad API y matriz de aceptación quedó consolidada en:

`docs/api/scalar-sec5-auditoria-final-seguridad-api-matriz-aceptacion-2026-05-01.md`

La evidencia final OpenAPI/CSV quedó en:

- `docs/api/scalar-sec5-openapi-security-operaciones-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-endpoints-sin-security-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-allowanonymous-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-escritura-security-final-2026-05-01.csv`

Veredicto:
se declara cerrado el frente de autorización explícita y metadata OpenAPI/Scalar de seguridad para el alcance evaluado.

No se declara producción lista.
