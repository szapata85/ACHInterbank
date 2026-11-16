# Plan de implementación y validación — cierre focal P1 RES-H02-01/02

Estado fuente: implementado; puerta de validación real: NO-GO. No incluye UAT ni cambia E2E-03.

| Fase | Acción/criterio | Resultado |
|---|---|---|
| C1 | Claim programado liga `AchCycleId`, clearing house, Open, lease y fase | Código modificado; modelo SQLite verificado |
| C2 | Dos guardas frescas por solicitud (pre-intención y pos-intención), en Quartz y manual | Código modificado; xUnit añadidos |
| C3 | Cancelación CAS `Preparing`/`InFlight` sin envío, auditoría `NotExecuted` y reasignación segura | Código modificado; xUnit añadidos |
| C4 | Regresión H01/H02/E2E/CENIT/ACH | 12/12 scripts estáticos/modelo SQLite + 27/27 focales |
| G1 | Compilación, xUnit focal + solución | Pendiente: `dotnet` ausente |
| G2 | SQL Server / PostgreSQL concurrencia + intentos + reinicios | Pendiente: motores/SDK ausentes |
| G3 | SOAP simulado, cierre real coordinado, idempotencia del receptor y ventana TOCTOU | Pendiente; bloqueo de Live |
| UAT | Solo al final general del proyecto | No ejecutada |

**Operación/rollback:** respaldo previo y despliegue solo después de G1–G3; no se requiere migración nueva. Un aborto confirmado deja `RetryPending` auditable, que el job futuro podrá reasignar solo si el próximo ciclo permite débito. Un `InFlight` interrumpido sin aborto confirmado se concilia y NO se reintenta automáticamente. Revertir el código no implica revertir estados financieros.
