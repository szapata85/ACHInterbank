# Brechas Criticas - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Brechas bloqueantes para productivo

| ID | Brecha | Severidad | Impacto | Bloquea productivo | Accion requerida | Responsable sugerido |
| --- | --- | --- | --- | --- | --- | --- |
| DEF-UAT-020 | NACHA-M 1/5/6/7/8/9 requiere validacion campo-a-campo y homologacion/waiver; UAT integrado ACH Colombia/CENIT no genero archivo valido por respuesta 0 bytes y prenotificacion previa ausente | Critica | Riesgo de interoperabilidad y cumplimiento | Si | Resolver prerequisitos de exportacion, generar archivo no vacio por sistema y ejecutar matriz campo-a-campo | Arquitectura ACH / QA |
| DEF-UAT-021 | `/NachaExport/{cycleId}` devuelve 200 con archivo vacio cuando no hay transacciones exportables validas | Alta | Riesgo de falsa evidencia operativa o archivo invalido | Si | Devolver error controlado y probar archivo no vacio | Backend / QA |
| DEF-UAT-022 | `Proc_Contrapartidas` genero evidencia XML dry-run, pero el job automatico intento endpoint externo/no resoluble en UAT | Alta | Riesgo de intento de conexion externa no autorizada | Si | Configurar endpoint UAT/mock o guardrail dry-run | Integracion / DevOps / Seguridad |
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
- Resultado: no se cierra DEF-UAT-020; productivo sigue **NO-GO**.
