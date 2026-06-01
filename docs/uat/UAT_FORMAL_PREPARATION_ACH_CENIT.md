# Preparacion UAT formal ACH Colombia/CENIT - Fase 6C.10

## Resumen ejecutivo

Este documento prepara la ejecucion UAT formal para NACHA-M ACH Colombia/CENIT usando evidencia automatizada, matriz trazable y consolas read-only existentes. No declara certificacion oficial ni habilita produccion. Productivo permanece NO-GO.

## Fuentes

- ACH Colombia MAN-004 V32: `docs/normativa/md/ACH-Colombia-V32.md`.
- CENIT/Banco de la Republica DSP-152 Anexo 2: `docs/normativa/md/CENIT-DSP-152-Anexo-2.md`.
- CENIT causales: `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md`, `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md`.
- Matriz trazable: `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md`.
- Evidencia CI/Playwright: `docs/uat/PLAYWRIGHT_EVIDENCE.md`.

## Alcance UAT

- ACH Colombia y CENIT.
- NACHA-M salida y entrada.
- Archivos `.RET`, respuestas diferenciales, devoluciones/rechazos, prenotificaciones y ROR.
- Naming `RRRRTTT.ZZZ.1`, FileIdModifier, totales, padding y fixed-width 106.
- `nacha-config profiles` como modelo oficial read-only.
- Dashboard/read-store, detalle operativo de archivo, consola SOAP/UAT read-only y consola de conciliacion ACH.
- Evidencias Playwright publicadas por CI.

## Exclusiones

- Produccion y cualquier cambio de Productivo NO-GO.
- SOAP real, certificados reales y endpoints productivos.
- Movimientos monetarios reales.
- Datos reales de clientes.
- Certificacion oficial ACH Colombia/CENIT.
- Mutaciones criticas, reprocesos, aprobaciones manuales o generacion de archivos productivos.
- Legacy `nacha-layouts` / `nacha-record-definitions` como fuente oficial.
- Descargas `/NachaExport/{hash}`.

## Precondiciones

- Ambiente UAT disponible, aislado y sin conectividad productiva no aprobada.
- Base de datos UAT con datos sinteticos y anonimizados.
- Perfiles `nacha-config` ACH Colombia/CENIT cargados/publicados para pruebas.
- Certificados mock/controlados si aplica; certificados reales no usados.
- CI verde: backend, Angular y Playwright.
- Artefactos `playwright-report`, `playwright-test-results` y `uat-evidence-playwright` disponibles.
- Matriz 6C.9 revisada por QA/UAT.

## Ambientes y datos

| Elemento | Requisito UAT | Restriccion |
| --- | --- | --- |
| API/SPA | UAT aislado | Sin secretos productivos |
| Base de datos | Datos sinteticos ACH/CENIT | Sin clientes reales |
| Archivos NACHA-M | `.ach`/`.RET` sinteticos | Golden files semirreales no son certificacion |
| SOAP | Read-only/dry-run/simulado | SOAP real bloqueado |
| CI | Evidencias publicadas | No habilita GO productivo |

## Criterios de entrada

- Checklist UAT en estado `Listo para ejecutar` para ambiente, seguridad, datos y evidencia.
- Matriz 6C.9 sin filas criticas desconocidas.
- Baselines conocidos: backend 1602 passed/1 skipped, Angular 308 success, Playwright 34 passed.
- Aprobacion de QA/UAT para ejecutar con datos sinteticos.

## Criterios de salida

- Escenarios ACH Colombia y CENIT ejecutados con evidencia.
- Riesgos/brechas actualizados con mitigacion o decision formal.
- Evidencias CI adjuntas al paquete UAT.
- No se detectan llamadas SOAP reales, mutaciones criticas, datos sensibles, legacy oficial ni `/NachaExport/{hash}`.
- Decision UAT documentada: aprobado para siguiente fase controlada, aprobado con observaciones o bloqueado.

## Roles y responsables

| Rol | Responsabilidad |
| --- | --- |
| QA/UAT Lead | Coordinar ejecucion, evidencias y cierre |
| DevOps/CI | Verificar workflows y artefactos |
| Backend Lead | Soporte API/read-store, trazabilidad y logs sanitizados |
| Frontend Lead | Soporte SPA read-only y Playwright |
| Operaciones ACH | Validar escenarios operativos y causales |
| Riesgo/Auditoria | Revisar brechas, NO-GO y evidencias |
| Comite UAT | Emitir decision formal |

## Plan de ejecucion

1. Validar precondiciones y congelar alcance UAT.
2. Ejecutar checklist `docs/uat/UAT_EXECUTION_CHECKLIST.md`.
3. Ejecutar escenarios `docs/uat/UAT_TEST_SCENARIOS_ACH_CENIT.md`.
4. Adjuntar artefactos CI/Playwright y logs sanitizados.
5. Actualizar `docs/uat/UAT_RISKS_AND_GAPS.md`.
6. Registrar decision del comite UAT.

## Rollback / no avance

No hay rollback productivo porque no se habilita produccion. Si aparece evidencia de SOAP real, movimiento monetario, datos sensibles, mutacion critica, legacy oficial o `/NachaExport/{hash}`, se bloquea la ejecucion, se conserva evidencia, se abre defecto y Productivo permanece NO-GO.

## Decision productiva

Productivo permanece NO-GO hasta certificacion oficial ACH Colombia/CENIT, UAT formal aprobado, integracion SOAP real controlada, plan de rollback/monitoreo y aprobacion explicita de comite.
