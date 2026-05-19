# Plan de Cierre de Brechas - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Plan ejecutivo para habilitar reconsideracion futura de GO

## Plan por Fases

| Fase | Objetivo | Brechas relacionadas | Entregable | Criterio de cierre | Responsable | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| Fase 1 | Cerrar ACH.Operator o definir usuario operador sintetico | DEF-UAT-015 | Evidencia de rol visible/asignado y pruebas de autorizacion | Login/claims/politicas validados sin secretos | Seguridad / Backend | Pendiente |
| Fase 2 | Validar NACHA-M campo-a-campo con archivo sintetico | DEF-UAT-020 | Matriz NACHA-M completa y evidencia de validacion | Registros 1/5/6/7/8/9 aprobados u homologacion/waiver documentado | Arquitectura ACH / QA | Pendiente |
| Fase 3 | Validar CENIT/CUD | CENIT-CUD | Evidencia de integracion o alcance formal de waiver | Pruebas sinteticas aprobadas o excepcion formal | Integracion / Negocio | Pendiente |
| Fase 4 | Validar sobre digital, firma y certificados | SOBRE-DIGITAL | Evidencia de firma/certificados sin exponer privados | Flujo criptografico aprobado por seguridad | Seguridad / Integracion | Pendiente |
| Fase 5 | Validar backup, restore y rollback | BKP-RESTORE | Acta tecnica de recuperacion | Recuperacion y rollback ejecutados con tiempos y resultado | Operaciones / SRE | Pendiente |
| Fase 6 | Ejecutar UAT funcional formal y actas | UAT-FORMAL, EVI-VISUAL, UAT-BANCARIO | Evidencias funcionales y actas firmadas | Aprobacion formal de negocio y QA | QA / Negocio | Pendiente |
| Fase 7 | Realizar comite final GO/NO-GO | ACTAS-APR y brechas remanentes | Paquete final actualizado | Todas las brechas bloqueantes cerradas o aceptadas formalmente | Direccion / PMO | Pendiente |

## Reglas de Cierre

- No cerrar brechas sin evidencia.
- No usar datos reales para pruebas controladas.
- No documentar passwords, tokens completos ni certificados privados.
- No recomendar GO productivo hasta completar los criterios bloqueantes.

## Resultado Esperado

Al completar las fases anteriores, el proyecto podria someterse a un nuevo comite GO/NO-GO con evidencia actualizada, scorecard recalculado y actas formales.
