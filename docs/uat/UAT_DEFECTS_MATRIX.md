# Matriz de defectos/hallazgos UAT - Ronda 1 - Fase 6D.1

Estados permitidos: `Abierto`, `En analisis`, `Corregido`, `Diferido`, `No aplica`, `Cerrado`.

No se registran defectos criticos de codigo en esta ronda documental/controlada. Se registran hallazgos/brechas UAT para seguimiento formal. Productivo permanece NO-GO.

| ID defecto | Escenario | Severidad | Tipo | Descripcion | Evidencia | Estado | Responsable sugerido | Fase sugerida de correccion | Workaround | Impacto | Observacion | Cierre 6D.2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-FND-001 | UAT-ACH-001/003/007, UAT-CEN-001/003 | Alto | Brecha certificacion | Golden files y evidencias son semirreales; falta contraste oficial ACH Colombia/CENIT | `tests/.../GoldenFiles`, `UAT_EXPANDED_EXECUTION_ROUND.md` | Abierto | QA/UAT + Operaciones ACH | UAT formal ampliado/certificacion | Usar solo como regresion tecnica | Bloquea GO productivo | No es bug funcional; es brecha obligatoria | 6D.3: sigue abierto; requiere tercero/certificacion |
| UAT-FND-002 | UAT-ACH-002, UAT-CEN-002 | Medio | Brecha ambiente/datos | No se ejecuto ingesta contra ambiente UAT externo con base cargada | `UAT_EXPANDED_SYNTHETIC_DATASET.md` | En analisis | DevOps + QA/UAT | UAT formal ampliado | Mantener evidencia read-only/local | Limita validacion end-to-end operativa | Requiere dataset cargado | 6D.3: dataset cargado/documentado; cierre real requiere UAT externo |
| UAT-FND-003 | UAT-ACH-004, UAT-CEN-003/004, UAT-EXP-001/002/004 | Medio | Brecha normativa | Homologacion formal de causales rechazo/devolucion CENIT/ACH pendiente | `UAT_EXPANDED_EXECUTION_ROUND.md` | En analisis | Operaciones ACH + Riesgo | UAT formal ampliado | Marcar manual review si ambiguo | Puede afectar conciliacion final | No mover dinero | 6D.3: parcial; manual review/inconsistencias documentadas |
| UAT-FND-004 | UAT-CEN-005, UAT-EXP-003 | Medio | Brecha escenario | Ciclos/colas/neteo CENIT no ejecutados por falta de datos representativos de ciclo | `UAT_EXPANDED_EXECUTION_ROUND.md` | Abierto | Operaciones CENIT + DevOps | UAT formal ampliado | Validar dashboard con datos parciales | Limita cobertura CENIT operativa | Productivo NO-GO | 6D.3: observado; requiere ejecucion CENIT externa |
| UAT-FND-005 | Transversal | Bajo | Toolchain | Warnings Browserslist/Node DEP0205 permanecen documentados | `PRE_UAT_TECHNICAL_HARDENING.md` | Diferido | Frontend/DevOps | Hardening toolchain futuro | Aceptar como no bloqueante | Ruido CI, no falla pruebas | No actualizar dependencias mayores pre-UAT | Diferido sin cambio tecnico |
| UAT-FND-006 | Transversal | Observacion | Control productivo | SOAP real y movimientos monetarios siguen bloqueados | `nacha-soap-uat-console.spec.ts`, `PRE_UAT_AUTOMATED_CHECKLIST.md` | Cerrado | QA/UAT + Arquitectura | Fase SOAP real controlada futura | Mantener dry-run/read-only | Bloquea validacion externa real | Es control requerido NO-GO | Cerrado documentalmente como control esperado |
| UAT-FND-007 | UAT-EXP-003 | Medio | Brecha escenario externo | Ciclo/cola/neteo CENIT ampliado requiere evidencia con operador externo | `UAT_EXPANDED_EXECUTION_ROUND.md` | Abierto | Operaciones CENIT + DevOps | UAT formal con CENIT | Mantener evidencia sintetica/read-only | Limita cobertura operativa CENIT | Productivo NO-GO | Nuevo 6D.3; no es bug de codigo |

## Criterio de severidad

- Critico: compromete dinero, datos reales, secretos o productivo.
- Alto: bloquea certificacion/GO.
- Medio: limita cobertura UAT formal.
- Bajo: deuda tecnica no bloqueante.
- Observacion: control o restriccion esperada.
