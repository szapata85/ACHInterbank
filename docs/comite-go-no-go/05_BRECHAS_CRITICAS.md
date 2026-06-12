# Brechas Criticas - Comite GO/NO-GO

Fecha de actualizacion: 2026-06-12
Estado general: Brechas bloqueantes para productivo

| ID | Brecha | Severidad | Impacto | Bloquea productivo | Accion requerida | Responsable sugerido |
| --- | --- | --- | --- | --- | --- | --- |
| DEF-UAT-020 | NACHA-M tecnico cerrado; validacion campo-a-campo firmada y homologacion/waiver pendientes | Critica productiva | Riesgo residual de interoperabilidad y cumplimiento | Si | Completar homologacion externa y acta | Arquitectura ACH / QA / Compliance |
| DEF-UAT-021 | `/NachaExport/{cycleId}` evita falso exito y G3.6B confirma archivo no vacio `.6` | Cerrada tecnica | Riesgo de falsa evidencia mitigado | No por si solo | Mantener pruebas positivas/negativas | Backend / QA |
| DEF-UAT-022 | `Proc_Contrapartidas` opera en dry-run por defecto para UAT/local y no transmite externamente | Alta | Riesgo de endpoint externo mitigado en UAT/local; homologacion real pendiente | Si | Mantener dry-run hasta endpoint UAT/mock autorizado y aprobacion para modo Live | Integracion / DevOps / Seguridad |
| UAT-FORMAL | UAT funcional formal con actas pendiente | Critica | No hay aceptacion funcional formal | Si | Ejecutar UAT formal, evidencias y firmas | Negocio / QA |
| EVI-VISUAL | Evidencia visual/operativa pendiente | Alta | Debilita trazabilidad de aprobacion | Si | Completar evidencia de pantallas, flujos y operacion | QA / Operaciones |
| CENIT-CUD | CENIT/CUD pendiente | Critica | Interoperabilidad externa no validada | Si | Definir alcance, pruebas sinteticas y homologacion | Arquitectura Integracion |
| SOBRE-DIGITAL | Sobre digital/firma/certificados pendiente | Critica | Riesgo de seguridad e interoperabilidad | Si | Validar firma, certificados y manejo seguro | Seguridad / Integracion |
| CUSTODIA-SECRETOS | Aprobacion del mecanismo corporativo de custodia pendiente | Alta | Riesgo de gestion de secretos | Si | Completar validacion y aprobacion corporativa | Seguridad / DevOps |
| BKP-RESTORE | Backup/restore/rollback pendiente | Critica | Riesgo operativo ante incidente | Si | Ejecutar prueba documentada | Operaciones / SRE |
| UAT-BANCARIO | UAT bancario formal pendiente | Critica | Validacion externa/formal no concluida | Si | Coordinar y ejecutar UAT bancario formal | Negocio / Interoperabilidad |

## Brechas Cerradas En Este Ciclo

| ID | Cierre | Evidencia | Riesgo residual |
| --- | --- | --- | --- |
| DEF-UAT-015 | Usuario demo `admin` ahora evidencia roles `Admin` y `ACH.Operator` para UAT controlado. | `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, `UserRoleSeedTests`, login/JWT sanitizados y endpoints protegidos 200 con Bearer. | Evaluar usuario operador separado y matriz endpoint-rol antes de preproductivo/productivo. |
| G3.5 | Naming dinamico inbound/outbound. | Commit `7c3cbb21`. | Homologacion normativa formal. |
| G3.5.1 | Dependencia activa OpenBao/HashiCorp Vault retirada. | Commit `ebf7a8a5`. | KeyVault se inventaria separadamente. |
| G3.5.2 | Migraciones deshabilitadas por defecto. | Commit `c7a5ad50`. | Proceso DBA futuro. |
| G3.6A | Inbound real hasta `Proc_Transacciones` dry-run. | Commit `e5721150`, 2/2. | No SOAP real ni movimiento monetario. |
| G3.6B | Outbound real hasta `Proc_Contrapartidas` dry-run. | Commit `e5721150`, 2/2. | Correlacion por `AchCycleId`, no causalidad. |

## Evidencia Nueva NACHA/SOAP

- Transacciones UAT por camara creadas: `UAT-ACHCOL-NACHA-SOAP-001` y `UAT-CENIT-NACHA-SOAP-001`.
- Evidencia NACHA-M: `docs/uat/evidencias/nacha-m-uat/`.
- Evidencia SOAP dry-run: `docs/uat/evidencias/soap-proc-contrapartidas/`.
- Resultado: DEF-UAT-021 y DEF-UAT-022 quedan cerrados tecnicamente para UAT/local; no se cierra DEF-UAT-020. Productivo sigue **NO-GO**.

## Parametrizacion Reglas Camara 2026-05-19

Se implemento `ClearingHouseTransactionRule` para reglas de prenotificacion por ACH Colombia/CENIT y naturaleza debit/credit. La brecha DEF-UAT-020 queda mejor acotada, pero sigue abierta hasta generar NACHA-M UAT no vacio con prenotificacion valida y validar campo-a-campo.
## Actualizacion 2026-05-20

La brecha NACHA-M pasa a estado **parcial tecnico**: existen archivos UAT no vacios generados por sistema para ACH Colombia y CENIT. Sigue bloqueando productivo hasta validar debitos monetarios post-prenotificacion madura, campo-a-campo, homologacion/waiver y actas.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron vigente: `RRRRTTT.ZZZ.N`; los artefactos historicos de esta seccion usan `N=1`.
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

La brecha de preparacion de archivos inbound sinteticos queda cerrada tecnicamente para UAT/local mediante simulador separado del procesamiento real. Persisten brechas bloqueantes productivas:

- Carga y procesamiento real por NachaUpload cerrados tecnicamente en G3.6A; homologacion formal pendiente.
- Homologacion normativa formal por camara pendiente.
- Actas UAT funcionales formales pendientes.

Productivo sigue **NO-GO** por UAT formal, homologacion externa, CENIT/CUD, sobre digital/certificados, backup/restore/rollback y aprobaciones humanas.
