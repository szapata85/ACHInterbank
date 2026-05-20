# Plan de Cierre de Brechas - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Plan ejecutivo para habilitar reconsideracion futura de GO

| Fase | Objetivo | Brechas relacionadas | Entregable | Criterio de cierre | Responsable | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| Fase 1 | Cerrar ACH.Operator para UAT controlado | DEF-UAT-015 | Evidencia de rol visible/asignado | Login/claims/politicas validados sin secretos | Seguridad / Backend | Cerrado para UAT controlado |
| Fase 2 | Validar NACHA-M campo-a-campo con archivo sintetico | DEF-UAT-020 | Matriz NACHA-M completa y archivo no vacio generado por sistema | Prenotificacion UAT valida, registros 1/5/6/7/8/9 aprobados u homologacion/waiver | Arquitectura ACH / QA | Bloqueado |
| Fase 3 | Validar CENIT/CUD | CENIT-CUD | Evidencia de integracion o waiver | Pruebas sinteticas aprobadas o excepcion formal | Integracion / Negocio | Pendiente |
| Fase 4 | Validar sobre digital, firma, certificados y SOAP dry-run/mock | SOBRE-DIGITAL | Evidencia de firma/certificados y `Proc_Contrapartidas` sin transmision externa no autorizada | Flujo criptografico aprobado y endpoint UAT/mock homologado; guardrail dry-run ya validado para UAT/local | Seguridad / Integracion | Parcial |
| Fase 5 | Validar backup, restore y rollback | BKP-RESTORE | Acta tecnica de recuperacion | Recuperacion y rollback ejecutados | Operaciones / SRE | Pendiente |
| Fase 6 | Ejecutar UAT funcional formal y actas | UAT-FORMAL, EVI-VISUAL, UAT-BANCARIO | Evidencias funcionales y actas firmadas | Aprobacion formal de negocio y QA | QA / Negocio | Pendiente |
| Fase 7 | Realizar comite final GO/NO-GO | ACTAS y brechas remanentes | Paquete final actualizado | Brechas bloqueantes cerradas o aceptadas formalmente | Direccion / PMO | Pendiente |

## Nota De Replanificacion 2026-05-19

El UAT integrado NACHA/SOAP avanzo en transacciones sinteticas por camara, evidencia XML dry-run, cierre tecnico DEF-UAT-021 y cierre tecnico UAT/local DEF-UAT-022. No avanzo a cierre normativo porque no se genero archivo NACHA-M no vacio. Antes de reintentar se debe crear prenotificacion UAT valida sin bypass/backdating y usar ciclos exportables posteriores.
