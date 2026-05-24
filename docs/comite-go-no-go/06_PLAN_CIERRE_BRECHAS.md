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

## Nota De Parametrizacion 2026-05-19

La fase previa al reintento NACHA-M ahora incluye aplicar la migracion `AddClearingHouseTransactionRules`, validar seeds ACH Colombia/CENIT y operar la pantalla `Transacciones > Reglas por camara` para confirmar reglas vigentes antes de crear prenotificaciones UAT.
## Actualizacion 2026-05-20

Agregar al plan de cierre NACHA-M: usar las prenotificaciones UAT `UAT-ACH-PRE-001` y `UAT-CEN-PRE-001` una vez cumplan 3 dias habiles, crear debitos monetarios posteriores y repetir exportacion no vacia con validacion campo-a-campo.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.1.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.

## Actualizacion 2026-05-20 - Simulador NACHA-M Entrada

| Fase | Objetivo | Entregable | Estado |
|---|---|---|---|
| UAT inbound 1 | Generar archivos sinteticos de entrada | Simulador API/SPA | OK tecnico |
| UAT inbound 2 | Cargar manualmente por NachaUpload | Evidencia de procesamiento | Pendiente |
| UAT inbound 3 | Validar estados/auditoria/errores | Matriz UAT | Pendiente |
| Comite | Presentar evidencia formal | Acta UAT | Pendiente |
