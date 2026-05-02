# Scalar-SEC-PERM-3A — Pruebas de compatibilidad OR en policies P0 (2026-05-01)

## 1. Resumen ejecutivo
Se reforzaron pruebas de autorización para policies P0 verificando explícitamente que cada policy autoriza por permiso fino **o** por permiso legacy compatible, y rechaza usuarios sin claim válido o con legacy contrario.

## 2. Contexto PERM-3
PERM-3 migró controladores P0 a policies compuestas con compatibilidad temporal (`CanReadAch`/`CanManageAch`).

## 3. Objetivo de las pruebas OR
Validar comportamiento real de `IAuthorizationService.AuthorizeAsync(...)` contra policies P0 con `RequireAssertion`, no solo inspección estática de requirements.

## 4. Policies P0 evaluadas
- `P0.TransactionsRead`
- `P0.TransactionsCreate`
- `P0.TransactionsBulkSubmit`
- `P0.TransactionsPolicyPreview`
- `P0.TraceabilityRead`
- `P0.TraceabilityCertifySol02`
- `P0.ReturnsRead`
- `P0.ReturnsGenerateFile`

## 5. Matriz policy → permiso fino → permiso legacy → rechazo esperado
Para cada policy se validó:
- autoriza con permiso fino esperado;
- autoriza con legacy compatible;
- rechaza con legacy contrario;
- rechaza sin claim.

## 6. Pruebas agregadas
Se amplió `P0FineGrainedPolicyMigrationTests` con pruebas de compatibilidad OR usando:
- `ServiceCollection` + `AddExternal(config)`;
- `IAuthorizationService` real;
- `ClaimsPrincipal` con claim type `permission`;
- llamadas `AuthorizeAsync(user, null, policyName)`.

## 7. Resultado build inicial
`dotnet build ACHInterbank.sln -c Release`: exitoso.

## 8. Resultado pruebas específicas
`dotnet test ... --filter "...P0FineGrainedPolicyMigrationTests...Authorization...Security"`: exitoso (`32/32`).

## 9. Resultado suite completa
`dotnet test ... -c Release -v minimal`: exitoso (`424/424`).

## 10. Resultado build final
`dotnet build ACHInterbank.sln -c Release`: exitoso.

## 11. Validación de no cambio de controladores
`git diff -- src/Cfa.ACHInterbank.Api/Controllers`: sin cambios.

## 12. Qué NO se implementó
Este alcance fue exclusivamente de pruebas y evidencia.

## Qué NO se implementó en Scalar-SEC-PERM-3A
- No se cambiaron controladores.
- No se migraron P1/P2.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se eliminaron CanReadAch ni CanManageAch.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 13. Veredicto
**Scalar-SEC-PERM-3A: CERRADO** con compatibilidad OR validada por pruebas de autorización reales.
