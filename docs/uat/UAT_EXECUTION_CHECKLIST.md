# Checklist ejecucion UAT ACH Colombia/CENIT - Fase 6C.10

Estados permitidos: `Pendiente`, `Listo para ejecutar`, `Ejecutado OK`, `Ejecutado con observacion`, `Bloqueado`, `No aplica`.

Productivo permanece NO-GO. No SOAP real, no movimientos monetarios, no datos reales, no secretos, no mutaciones criticas.

| ID | Check | Evidencia esperada | Estado | Observacion |
| --- | --- | --- | --- | --- |
| UAT-CHK-001 | Ambiente UAT aislado disponible | URL UAT, version desplegada, ventana UAT | Pendiente | Sin acceso productivo no aprobado |
| UAT-CHK-002 | Seguridad y accesos read-only validados | Usuarios/roles UAT, permisos de consulta | Pendiente | Sin credenciales reales en documentos |
| UAT-CHK-003 | Datos sinteticos ACH Colombia cargados | Dataset sintetico y hash/control interno | Pendiente | Sin clientes reales |
| UAT-CHK-004 | Datos sinteticos CENIT cargados | Dataset sintetico y hash/control interno | Pendiente | Sin clientes reales |
| UAT-CHK-005 | Perfiles `nacha-config` ACH/CENIT disponibles | Captura/config read-only y version | Pendiente | Legacy no oficial |
| UAT-CHK-006 | CI backend/Angular/Playwright verde | Run CI, resumen de resultados | Pendiente | Baseline esperado: 1602/308/34 |
| UAT-CHK-007 | NACHA-M salida ACH Colombia | Archivo/evidencia validada en UAT | Pendiente | Golden files son semirreales |
| UAT-CHK-008 | NACHA-M salida CENIT | Archivo/evidencia validada en UAT | Pendiente | Requiere contraste formal |
| UAT-CHK-009 | Incoming ACH Colombia/CENIT | Dashboard/read-store y detalle archivo | Pendiente | Datos parciales deben quedar marcados |
| UAT-CHK-010 | Returns `.RET` ACH Colombia/CENIT | Evidencia `.RET` y conciliacion | Pendiente | No mueve dinero directamente |
| UAT-CHK-011 | Prenotificaciones aprobadas/rechazadas | Estado y conciliacion read-only | Pendiente | No monetario |
| UAT-CHK-012 | ROR / Return of Return | Evidencia ROR y auditoria | Pendiente | Sin ejecucion monetaria |
| UAT-CHK-013 | Consola SOAP/UAT read-only | Captura de bloqueos NO-GO/SOAP | Pendiente | SOAP real bloqueado |
| UAT-CHK-014 | Conciliacion ACH | Items, detalle y warnings sanitizados | Pendiente | Sin acciones criticas |
| UAT-CHK-015 | Evidencias Playwright CI disponibles | `playwright-report`, `test-results`, `uat-evidence-playwright` | Pendiente | CI no reemplaza certificacion |
| UAT-CHK-016 | Aprobaciones QA/UAT/Operaciones/Riesgo | Acta o registro de decision | Pendiente | Puede ser aprobado con observaciones |
| UAT-CHK-017 | Decision Go/No-Go UAT registrada | Decision UAT y plan siguiente fase | Pendiente | Productivo sigue NO-GO |

## Estado ronda 1 controlada

| ID | Check | Estado ronda 1 | Observacion |
| --- | --- | --- | --- |
| UAT-CHK-003 | Datos sinteticos ACH Colombia cargados | Ejecutado con observacion | Dataset documentado; carga en ambiente UAT formal pendiente |
| UAT-CHK-004 | Datos sinteticos CENIT cargados | Ejecutado con observacion | Dataset documentado; carga en ambiente UAT formal pendiente |
| UAT-CHK-006 | CI backend/Angular/Playwright verde | Ejecutado OK | Baseline 1602/308/34 registrado desde 6C.11 |
| UAT-CHK-007 | NACHA-M salida ACH Colombia | Ejecutado con observacion | Validado con golden semirreal; certificacion oficial pendiente |
| UAT-CHK-008 | NACHA-M salida CENIT | Ejecutado con observacion | Validado con golden semirreal; certificacion oficial pendiente |
| UAT-CHK-010 | Returns `.RET` ACH Colombia/CENIT | Ejecutado con observacion | `.RET` semirreal disponible; homologacion formal pendiente |
| UAT-CHK-013 | Consola SOAP/UAT read-only | Ejecutado OK | Playwright/read-only; SOAP real bloqueado |
| UAT-CHK-014 | Conciliacion ACH | Ejecutado OK | Playwright/read-only; sin mutaciones criticas |
| UAT-CHK-015 | Evidencias Playwright CI disponibles | Ejecutado OK | Artefactos documentados |
| UAT-CHK-017 | Decision Go/No-Go UAT registrada | Ejecutado con observacion | Recomendacion: continuar UAT formal ampliado; Productivo NO-GO |

## Checks ronda ampliada 6D.2

| ID | Check | Evidencia esperada | Estado | Observacion |
| --- | --- | --- | --- | --- |
| UAT-EXP-CHK-001 | Datos sinteticos ampliados definidos | `UAT_EXPANDED_SYNTHETIC_DATASET.md` | Listo para ejecutar | Sin datos reales |
| UAT-EXP-CHK-002 | Evidencia CI Playwright disponible | Artifacts `playwright-report`, `test-results`, `uat-evidence-playwright` | Listo para ejecutar | No reemplaza certificacion |
| UAT-EXP-CHK-003 | Validacion manual review | Escenario UAT-EXP-001 | Pendiente | Causales ambiguas no mutan estados |
| UAT-EXP-CHK-004 | Validacion inconsistencias | Escenario UAT-EXP-002 | Pendiente | Sin POST/PUT/PATCH/DELETE |
| UAT-EXP-CHK-005 | Ciclo/cola/neteo CENIT sintetico | DS-EXP-CEN-CYCLE | Pendiente | Requiere ambiente UAT |
| UAT-EXP-CHK-006 | Revision hallazgos cerrados | `UAT_ROUND_1_FINDINGS_CLOSURE.md` | Listo para ejecutar | UAT-FND-006 cerrado documentalmente |
| UAT-EXP-CHK-007 | Revision hallazgos diferidos/externos | Matriz defectos actualizada | Listo para ejecutar | Certificacion/SOAP real siguen pendientes |
