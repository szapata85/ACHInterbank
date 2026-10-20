# Evidencia de pruebas Quartz

Fecha: 2026-07-20. Ambiente local controlado, sin operaciones financieras ni llamadas SOAP.

## Motores y persistencia

| Motor | Esquema Quartz | Migración EF | Reinicio | Reaplicación |
|---|---:|---:|---:|---:|
| PostgreSQL 16 | 11 tablas | 18 migraciones aplicadas | job 1 / trigger 1 preservados | 11 tablas, job y trigger preservados |
| SQL Server 2025 | 11 tablas | 11 migraciones aplicadas | job 1 / trigger 1 preservados | 11 tablas, job y trigger preservados |

`dotnet ef migrations has-pending-model-changes` informó que no existen cambios pendientes para PostgreSQL ni SQL Server. Los scripts de esquema fueron ejecutados dos veces sin pérdida de jobs, triggers ni datos. La migración funcional se aplicó y revirtió correctamente en una base temporal nueva de cada motor; ambas bases temporales fueron eliminadas al finalizar.

## Clúster PostgreSQL

- SchedulerName: `ACHInterbankScheduler`.
- InstanceId 1: `achinterbank-api-01639201828797383300`.
- InstanceId 2: `achinterbank-api-02639201828976613700`.
- Trigger probado: `SCHEDULER_CLUSTER_PROBE`.
- Adquisiciones observadas por request: 1.
- Ejecuciones lógicas por request: 1.
- Afectaciones funcionales por `ProbeKey`: 1.
- Las dos APIs reportaron readiness saludable y el job/trigger persistieron después de reiniciar ambas réplicas.

## Clúster SQL Server

- SchedulerName: `ACHInterbankScheduler`.
- InstanceId 1 después de reinicio: `achinterbank-api-01639201836971041165`.
- InstanceId 2 después de reinicio: `achinterbank-api-02639201836965137825`.
- Las dos APIs reportaron readiness saludable, IDs distintos y heartbeats en línea.
- El flujo Playwright ejecutó la sonda contra API y base reales; una solicitud produjo un historial y un efecto único.

## Recovery

- Request ID: `5b40fd5c-e332-46eb-a357-a0445e14af37`.
- Execution ID: `40493863-c8b9-446e-8aaa-22531aa3f23e`.
- Instancia detenida abruptamente: `achinterbank-api-01`.
- Instancia recuperadora: `achinterbank-api-02`.
- Resultado: `Recovered`; `IsRecovery=true`, fire original conservado, una fila de historial, una fila de sonda y una sola marca de efecto.

## Misfire

- `DoNothing`: un misfire funcional registrado y cero efectos adicionales.
- `FireAndProceed`: un misfire funcional registrado y exactamente un efecto posterior.
- La programación de la sonda se restauró al finalizar.

## Ejecución manual y seguridad

- Request ID inicial: `3f6c63d4-a987-4b2c-8e00-637efd207dbe`.
- Execution ID: `a5c54862-fbbe-4246-afab-d490056918ec`.
- Primera solicitud: HTTP 202; repetición: misma ejecución, ningún efecto nuevo.
- Con una ejecución activa, un request diferente recibió HTTP 409.
- Consulta anónima: HTTP 401; token con solo `Scheduler.View`: consulta HTTP 200 y ejecución HTTP 403.
- Playwright verificó motivo obligatorio, prevención de doble clic, historial, 409, pausa/reanudación, vista previa, permisos y layout móvil contra el backend real.

## Comandos y resultados

| Comando | Resultado | Pruebas | Fallos |
|---|---|---:|---:|
| `dotnet build ACHInterbank.sln -c Release --no-restore` | Aprobado, 0 warnings | 7 proyectos | 0 |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` | Aprobado, 5 omisiones configuradas | 1877 | 0 |
| `npm run build` | Aprobado | 1 build de producción | 0 |
| `npm test -- --watch=false --browsers=ChromeHeadless` | Aprobado | 427 | 0 |
| `dotnet test ... --filter FullyQualifiedName~Quartz\|FullyQualifiedName~Scheduler` | Aprobado | 48 | 0 |
| `npm test -- --watch=false ...task-definitions... ...app-routing-access...` | Aprobado | 9 | 0 |
| `npx playwright test e2e/scheduler-cluster.spec.ts --project=chromium` | Aprobado | 2 | 0 |
| `docker compose -f docker-compose.scheduler-cluster.yml config --quiet` | Aprobado | 1 | 0 |
| `docker compose -f docker-compose.scheduler-cluster.sqlserver.yml config --quiet` | Aprobado | 1 | 0 |

La evidencia completa se obtiene de `TaskExecutionLog`, `SchedulerProbeExecutions`, `SchedulerInstanceStates` y las tablas internas Quartz. No se copiaron XML, credenciales, tokens, cuentas ni payloads financieros.
