# Scorecard Readiness - Comite GO/NO-GO

Fecha de actualizacion: 2026-06-12
Score actual: 68.1 / 100
Estado: Naranja / NO-GO con brechas altas

## Semaforo

| Rango | Color | Interpretacion |
| --- | --- | --- |
| >= 90 | Verde | Candidato a GO sujeto a aprobaciones finales. |
| 75-89 | Amarillo | Requiere cierres puntuales antes de GO. |
| 60-74 | Naranja | NO-GO con brechas altas; puede continuar UAT controlado. |
| < 60 | Rojo | NO-GO con brechas criticas generalizadas. |

## Tabla por Categoria

| Categoria | Peso | Puntaje | Aporte | Estado | Observacion |
| --- | ---: | ---: | ---: | --- | --- |
| Funcionalidad core | 20% | 82 | 16.4 | Parcial alto | Trazabilidad e idempotencia documentada avanzaron. |
| UAT y evidencias | 20% | 64 | 12.8 | Parcial | G3.6A/G3.6B 4/4 con SPA/API/PostgreSQL/Quartz reales; actas formales pendientes. |
| Seguridad | 15% | 73 | 10.95 | Parcial | NU1903 corregido; `ACH.Operator` cerrado para UAT; pendientes custodia de secretos/certificados y matriz endpoint-rol formal. |
| Interoperabilidad externa | 15% | 40 | 6.00 | Bajo | Naming, inbound y outbound dry-run cerrados tecnicamente; CENIT/CUD, SOAP autorizado y homologacion siguen pendientes. |
| Operacion y soporte | 10% | 68 | 6.8 | Parcial | Runtime OK; backup/restore/rollback pendiente. |
| Observabilidad | 10% | 68 | 6.8 | Parcial | Logs y runtime controlado OK. |
| Documentacion y trazabilidad | 10% | 83 | 8.3 | Parcial alto | Documentacion avanzada; actas formales pendientes. |
| Total | 100% |  | 68.1 | Naranja | NO-GO productivo / continuar UAT controlado. |

El score no se recalcula automaticamente por G3.5-G3.6. Requiere decision humana del comite. G3.6B aporta correlacion por `AchCycleId`, no causalidad directa.
