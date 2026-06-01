# Matriz de defectos/hallazgos UAT - Ronda 1 - Fase 6D.1

Estados permitidos: `Abierto`, `En analisis`, `Corregido`, `Diferido`, `No aplica`, `Cerrado`.

No se registran defectos criticos de codigo en esta ronda documental/controlada. Se registran hallazgos/brechas UAT para seguimiento formal. Productivo permanece NO-GO.

| ID defecto | Escenario | Severidad | Tipo | Descripcion | Evidencia | Estado | Responsable sugerido | Fase sugerida de correccion | Workaround | Impacto | Observacion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-FND-001 | UAT-ACH-001/003/007, UAT-CEN-001/003 | Alto | Brecha certificacion | Golden files y evidencias son semirreales; falta contraste oficial ACH Colombia/CENIT | `tests/.../GoldenFiles`, `UAT_EXECUTION_ROUND_1.md` | Abierto | QA/UAT + Operaciones ACH | UAT formal ampliado/certificacion | Usar solo como regresion tecnica | Bloquea GO productivo | No es bug funcional; es brecha obligatoria |
| UAT-FND-002 | UAT-ACH-002, UAT-CEN-002 | Medio | Brecha ambiente/datos | No se ejecuto ingesta contra ambiente UAT externo con base cargada | `UAT_EXECUTION_ROUND_1.md` | Abierto | DevOps + QA/UAT | UAT formal ampliado | Mantener evidencia read-only/local | Limita validacion end-to-end operativa | Requiere dataset cargado |
| UAT-FND-003 | UAT-ACH-004, UAT-CEN-003/004 | Medio | Brecha normativa | Homologacion formal de causales rechazo/devolucion CENIT/ACH pendiente | `REQUIREMENT_TRACEABILITY_MATRIX.md`, CENIT Anexos A/B | Abierto | Operaciones ACH + Riesgo | UAT formal ampliado | Marcar manual review si ambiguo | Puede afectar conciliacion final | No mover dinero |
| UAT-FND-004 | UAT-CEN-005 | Medio | Brecha escenario | Ciclos/colas/neteo CENIT no ejecutados por falta de datos representativos de ciclo | `UAT_EXECUTION_ROUND_1.md` | Abierto | Operaciones CENIT + DevOps | UAT formal ampliado | Validar dashboard con datos parciales | Limita cobertura CENIT operativa | Productivo NO-GO |
| UAT-FND-005 | Transversal | Bajo | Toolchain | Warnings Browserslist/Node DEP0205 permanecen documentados | `PRE_UAT_TECHNICAL_HARDENING.md` | Diferido | Frontend/DevOps | Hardening toolchain futuro | Aceptar como no bloqueante | Ruido CI, no falla pruebas | No actualizar dependencias mayores pre-UAT |
| UAT-FND-006 | Transversal | Observacion | Control productivo | SOAP real y movimientos monetarios siguen bloqueados | `nacha-soap-uat-console.spec.ts`, `PRE_UAT_AUTOMATED_CHECKLIST.md` | No aplica | QA/UAT + Arquitectura | Fase SOAP real controlada futura | Mantener dry-run/read-only | Bloquea validacion externa real | Es control requerido NO-GO |

## Criterio de severidad

- Critico: compromete dinero, datos reales, secretos o productivo.
- Alto: bloquea certificacion/GO.
- Medio: limita cobertura UAT formal.
- Bajo: deuda tecnica no bloqueante.
- Observacion: control o restriccion esperada.
