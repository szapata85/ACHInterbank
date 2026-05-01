# Scalar-3A — Documentación explícita Capability Registry (2026-05-01)

## 1. Resumen ejecutivo
Se reemplazó la dependencia del fallback contextual en el módulo Capability Registry por documentación explícita en los tres endpoints REST reales del controller `PaymentRailCapabilityRegistryController`. Se reforzaron `EndpointSummary`, `EndpointDescription` y `ProducesResponseType` sin cambiar lógica, rutas, permisos ni contratos funcionales.

## 2. Contexto Scalar-3
Scalar-3 inicia la documentación explícita por módulo sobre una base OpenAPI gobernada. En este corte se atendió `Capability Registry / PaymentRail Capability Registry`, manteniendo operación en paralelo/shadow y legado como source of truth operativo.

## 3. Endpoints documentados
- `GET /api/payment-rails/capability-registry/rails`
- `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities`
- `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}`

## 4. Cambios realizados
- Se redactaron `EndpointSummary` explícitos orientados a uso operativo real por riel/capacidad.
- Se redactaron `EndpointDescription` explícitos con: permiso, modo solo lectura, auditoría/gobernanza, riesgos operativos, relación PaymentRail/ACH/CENIT, interpretación `RegistryOverride` vs `StrategyDefault`, y advertencia de no cutover/no cambio legacy.
- Se agregaron `ProducesResponseType` para códigos 200/400/401/403/404/500 según cada endpoint.

## 5. Permisos documentados
- Política aplicada en el controller: `FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry`.
- Fallback de autorización documentado: `CanManageAch` y `CanReadAch` (según registro de políticas de seguridad).

## 6. Validación read-only
Comando ejecutado:

```bash
rg -n "\[Http(Post|Put|Patch|Delete)" src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs -S
```

Resultado: sin coincidencias. El controller permanece solo con métodos `GET`.

## 7. Resultado OpenAPI real
Se generó OpenAPI real desde runtime (`/openapi/v1.json`) y se validó el módulo con script Python.

- `TOTAL_ENDPOINTS_CAPABILITY_REGISTRY=3`
- `SIN_SUMMARY=0`
- `SIN_DESCRIPTION=0`
- `CON_TEXTOS_GENERICOS=0`
- Solo métodos `GET`.

## 8. Resultado pruebas específicas
Pruebas del módulo ejecutadas con filtro de controller/service/authorization policy: **15/15 passed**.

## 9. Resultado suite completa
Suite backend completa ejecutada: **408/408 passed**.

## 10. Resultado build final
`dotnet build ACHInterbank.sln -c Release`: **exitoso**.

## 11. Riesgos restantes
- Durante `dotnet run` se observaron errores de conexión a PostgreSQL (`localhost:5432`) en `SchedulerSyncService`; no bloquearon la publicación de `/openapi/v1.json`, pero deben considerarse para validaciones de entorno integradas con scheduler/DB.
- El módulo sigue en modo administrativo de lectura; no existe cutover ni activación automática de capacidades.

## 12. Veredicto
**Scalar-3A CERRADO para Capability Registry** en alcance de documentación explícita OpenAPI/Scalar del módulo, con evidencia de build, OpenAPI real y pruebas.

## Matriz de endpoints

| Método | Ruta | Summary explícito | Description explícito | Permiso | Solo lectura | Responses | Estado |
|---|---|---|---|---|---|---|---|
| GET | `/api/payment-rails/capability-registry/rails` | Sí | Sí | `CanViewPaymentRailCapabilityRegistry` (fallback `CanManageAch`/`CanReadAch`) | Sí | 200, 401, 403, 500 | Documentado y validado en OpenAPI real |
| GET | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities` | Sí | Sí | `CanViewPaymentRailCapabilityRegistry` (fallback `CanManageAch`/`CanReadAch`) | Sí | 200, 400, 401, 403, 500 | Documentado y validado en OpenAPI real |
| GET | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}` | Sí | Sí | `CanViewPaymentRailCapabilityRegistry` (fallback `CanManageAch`/`CanReadAch`) | Sí | 200, 400, 401, 403, 404, 500 | Documentado y validado en OpenAPI real |
