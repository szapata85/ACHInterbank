# Brechas Criticas - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Brechas bloqueantes para productivo

| ID | Brecha | Severidad | Impacto | Bloquea productivo | Accion requerida | Responsable sugerido |
| --- | --- | --- | --- | --- | --- | --- |
| DEF-UAT-020 | NACHA-M 1/5/6/7/8/9 requiere validacion campo-a-campo y homologacion/waiver; UAT integrado ACH Colombia/CENIT no genero archivo valido por prenotificacion previa ausente | Critica | Riesgo de interoperabilidad y cumplimiento | Si | Crear prenotificaciones UAT validas, generar archivo no vacio por sistema y ejecutar matriz campo-a-campo | Arquitectura ACH / QA |
| DEF-UAT-021 | `/NachaExport/{cycleId}` ya no devuelve 200 con archivo vacio; responde 422 controlado cuando faltan prerequisitos | Alta | Riesgo de falsa evidencia mitigado, pero archivo no vacio sigue pendiente | Si | Reintentar con transacciones exportables validas y confirmar archivo > 0 bytes | Backend / QA |
| DEF-UAT-022 | `Proc_Contrapartidas` opera en dry-run por defecto para UAT/local y no transmite externamente | Alta | Riesgo de endpoint externo mitigado en UAT/local; homologacion real pendiente | Si | Mantener dry-run hasta endpoint UAT/mock autorizado y aprobacion para modo Live | Integracion / DevOps / Seguridad |
| UAT-FORMAL | UAT funcional formal con actas pendiente | Critica | No hay aceptacion funcional formal | Si | Ejecutar UAT formal, evidencias y firmas | Negocio / QA |
| EVI-VISUAL | Evidencia visual/operativa pendiente | Alta | Debilita trazabilidad de aprobacion | Si | Completar evidencia de pantallas, flujos y operacion | QA / Operaciones |
| CENIT-CUD | CENIT/CUD pendiente | Critica | Interoperabilidad externa no validada | Si | Definir alcance, pruebas sinteticas y homologacion | Arquitectura Integracion |
| SOBRE-DIGITAL | Sobre digital/firma/certificados pendiente | Critica | Riesgo de seguridad e interoperabilidad | Si | Validar firma, certificados y manejo seguro | Seguridad / Integracion |
| OPENBAO | OpenBao/secrets pendiente segun alcance | Alta | Riesgo de gestion de secretos | Si | Definir alcance y completar validacion | Seguridad / DevOps |
| BKP-RESTORE | Backup/restore/rollback pendiente | Critica | Riesgo operativo ante incidente | Si | Ejecutar prueba documentada | Operaciones / SRE |
| UAT-BANCARIO | UAT bancario formal pendiente | Critica | Validacion externa/formal no concluida | Si | Coordinar y ejecutar UAT bancario formal | Negocio / Interoperabilidad |

## Brechas Cerradas En Este Ciclo

| ID | Cierre | Evidencia | Riesgo residual |
| --- | --- | --- | --- |
| DEF-UAT-015 | Usuario demo `admin` ahora evidencia roles `Admin` y `ACH.Operator` para UAT controlado. | `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, `UserRoleSeedTests`, login/JWT sanitizados y endpoints protegidos 200 con Bearer. | Evaluar usuario operador separado y matriz endpoint-rol antes de preproductivo/productivo. |

## Evidencia Nueva NACHA/SOAP

- Transacciones UAT por camara creadas: `UAT-ACHCOL-NACHA-SOAP-001` y `UAT-CENIT-NACHA-SOAP-001`.
- Evidencia NACHA-M: `docs/uat/evidencias/nacha-m-uat/`.
- Evidencia SOAP dry-run: `docs/uat/evidencias/soap-proc-contrapartidas/`.
- Resultado: DEF-UAT-021 y DEF-UAT-022 quedan cerrados tecnicamente para UAT/local; no se cierra DEF-UAT-020. Productivo sigue **NO-GO**.

## Parametrizacion Reglas Camara 2026-05-19

Se implemento `ClearingHouseTransactionRule` para reglas de prenotificacion por ACH Colombia/CENIT y naturaleza debit/credit. La brecha DEF-UAT-020 queda mejor acotada, pero sigue abierta hasta generar NACHA-M UAT no vacio con prenotificacion valida y validar campo-a-campo.
## Actualizacion 2026-05-20

La brecha NACHA-M pasa a estado **parcial tecnico**: existen archivos UAT no vacios generados por sistema para ACH Colombia y CENIT. Sigue bloqueando productivo hasta validar debitos monetarios post-prenotificacion madura, campo-a-campo, homologacion/waiver y actas.
