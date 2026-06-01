# Escenarios UAT ACH Colombia/CENIT - Fase 6C.10

Estado inicial permitido para todos los escenarios: `Pendiente`.

Restricciones globales: Productivo NO-GO, datos sinteticos, sin SOAP real, sin movimientos monetarios, sin mutaciones criticas, sin legacy oficial y sin `/NachaExport/{hash}`.

## ACH Colombia

| ID | Camara | Objetivo | Precondiciones | Pasos resumidos | Resultado esperado | Evidencia | Estado inicial |
| --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-ACH-001 | ACH Colombia | Salida NACHA-M valida | Perfil ACH publicado, datos sinteticos | Generar/consultar salida, validar records 1/5/6/7/8/9 | Archivo fixed-width 106, totales consistentes | Matriz RTM-001/003, archivo/evidencia UAT | Pendiente |
| UAT-ACH-002 | ACH Colombia | Entrada NACHA-M valida | Archivo sintetico entrante | Ingerir/procesar en UAT controlado | Clasificacion y dashboard sin SOAP real | Dashboard, detalle archivo, logs sanitizados | Pendiente |
| UAT-ACH-003 | ACH Colombia | `.RET` ACH | Devolucion sintetica | Procesar/consultar `.RET` | Conciliacion marca no monetario | Consola conciliacion, evidencia `.RET` | Pendiente |
| UAT-ACH-004 | ACH Colombia | Rechazo/devolucion | Causal sintetica | Consultar respuesta/rechazo | Causal visible sanitizada, manual review si ambiguo | Conciliacion ACH | Pendiente |
| UAT-ACH-005 | ACH Colombia | Prenotificacion aprobada | Prenote sintetica | Consultar respuesta aprobada | Estado no monetario registrado | Consola conciliacion | Pendiente |
| UAT-ACH-006 | ACH Colombia | Prenotificacion rechazada | Prenote rechazada sintetica | Consultar causal/estado | Estado no monetario, sin SOAP real | Conciliacion y auditoria | Pendiente |
| UAT-ACH-007 | ACH Colombia | Naming `RRRRTTT.ZZZ.1` | Consecutivo UAT | Validar nombre externo | Naming conforme, sin hash | Archivo/evidencia UAT | Pendiente |
| UAT-ACH-008 | ACH Colombia | FileIdModifier | Consecutivos 001-036 | Validar A-Z/0-9 | Modificador correcto o error controlado | Evidencia test/UAT | Pendiente |
| UAT-ACH-009 | ACH Colombia | Totales/padding/fixed-width | Archivo sintetico | Validar EntryHash, BlockCount, totals, padding 9 | Cierre consistente | Evidencia campo a campo | Pendiente |

## CENIT

| ID | Camara | Objetivo | Precondiciones | Pasos resumidos | Resultado esperado | Evidencia | Estado inicial |
| --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-CEN-001 | CENIT | Salida CENIT | Perfil CENIT publicado | Generar/consultar salida | Archivo consistente por perfil CENIT | Matriz RTM-002/003 | Pendiente |
| UAT-CEN-002 | CENIT | Entrada CENIT | Archivo entrante sintetico | Procesar/consultar entrada | Clasificacion y detalle read-only | Dashboard/detalle | Pendiente |
| UAT-CEN-003 | CENIT | `.RET` CENIT | Devolucion CENIT sintetica | Procesar/consultar `.RET` | No monetario, causal visible | Conciliacion ACH | Pendiente |
| UAT-CEN-004 | CENIT | Reglas diferenciales CENIT | Causales Anexo A/B sinteticas | Consultar respuesta/rechazo | Homologacion o manual review | Consola conciliacion | Pendiente |
| UAT-CEN-005 | CENIT | Ciclos/colas/neteo | Datos sinteticos de ciclo | Validar visibilidad operacional | Sin movimiento real, estado trazable | Dashboard/read-store | Pendiente |
| UAT-CEN-006 | CENIT | Conciliacion CENIT | Respuestas y transacciones sinteticas | Cruzar respuesta/NACHA/interno | Conciliado/pendiente/inconsistente segun caso | Consola conciliacion | Pendiente |

## Transversales

| ID | Camara | Objetivo | Precondiciones | Pasos resumidos | Resultado esperado | Evidencia | Estado inicial |
| --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-TRV-001 | Ambas | Dashboard operativo | API UAT disponible | Abrir dashboard | NO-GO, fuente read-only/parcial clara | Playwright/dashboard | Pendiente |
| UAT-TRV-002 | Ambas | Detalle archivo | Archivo persistido | Abrir detalle | Header/batch/entry/addenda/control sanitizados | Playwright/detalle | Pendiente |
| UAT-TRV-003 | Ambas | Consola SOAP/UAT | Read-store con candidatos | Abrir consola | SOAP real deshabilitado, candidatos controlados | Playwright SOAP/UAT | Pendiente |
| UAT-TRV-004 | Ambas | Conciliacion ACH | Respuestas sinteticas | Abrir consola | Items, detalle y badges sin acciones criticas | Playwright conciliacion | Pendiente |
| UAT-TRV-005 | Ambas | Config profiles | Perfiles cargados | Consultar perfiles | Modelo oficial read-only, legacy deprecated | Playwright config | Pendiente |
| UAT-TRV-006 | Ambas | Legacy deprecated | Rutas legacy accesibles/ocultas | Intentar navegar legacy | Deprecated/read-only, no oficial | Playwright legacy | Pendiente |
| UAT-TRV-007 | Ambas | Export `cycleId` / no hash | Ciclo exportable/no exportable | Ejecutar flujo controlado | No se llama `/NachaExport/{hash}` | Playwright export | Pendiente |
| UAT-TRV-008 | Ambas | Playwright evidence | CI ejecutado | Revisar artefactos | Reporte y resultados publicados | `playwright-report`, `test-results` | Pendiente |
