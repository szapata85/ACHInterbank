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
