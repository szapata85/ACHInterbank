# UAT Plan — NACHA Security Backend/SPA (2026-04-22)

## 1) Objetivo UAT
Validar en ambiente controlado que la solución NACHA Security cumple operación segura end-to-end (certificados, generación NACHA-M, cifrado/descifrado `.ENV`, auditoría y permisos) sin modificar criptografía base ni activar flujos automáticos de CI.

## 2) Alcance
- Gobierno de certificados (listado, alta, validación, activación, revocación, auditoría).
- Generación NACHA-M plano y cifrado.
- Operación manual encrypt/decrypt.
- Descarga segura autorizada por `operationId`.
- Auditoría y trazabilidad.
- Verificación de permisos finos.
- Estado de interoperabilidad/vector oficial y Go/No-Go de `identifier/IV`.

## 3) Fuera de alcance
- Hardening de `identifier/IV`.
- Certificación oficial con vector aprobado.
- Cambios en `CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`.
- Cambios SOAP, XML o padding criptográfico.

## 4) Prerrequisitos
- Backend build OK y tests críticos verdes.
- SPA build OK.
- Migraciones aplicadas (incluye `NachaSecurityOperations`).
- Certificados de prueba disponibles fuera de repo.
- Usuarios de prueba con roles/permisos.
- Workflow PostgreSQL manual-only confirmado.

## 5) Roles participantes
- AdminCertificados
- OperadorNacha
- OperadorSobreDigital
- SupervisorOperaciones
- Auditor
- SoporteTecnico
- AdministradorSistema
- UsuarioSinPermisos

## 6) Ambientes
- UAT backend (.NET + DB con migraciones aplicadas).
- UAT SPA (Angular build desplegado).
- Secret manager UAT para referencias de secretos/PFX (no en repositorio).

## 7) Datos de prueba
- Ciclos ACH sintéticos.
- Transacciones sintéticas (sin datos reales).
- NACHA planos sintéticos.
- `.ENV` válido generado por backend.
- `.ENV` alterado para escenario de firma inválida.

## 8) Certificados de prueba
- `.cer/.crt` públicos de prueba.
- `.pfx` de prueba custodiado fuera de repo.
- `SecretRef` de prueba en gestor seguro.
- Prohibido usar certificados productivos/reales.

## 9) Matriz de escenarios UAT mínimos

| ID | Escenario | Rol ejecutor | Permisos requeridos | Resultado esperado | Severidad si falla | Evidencia |
|---|---|---|---|---|---|---|
| 1 | Listar certificados | AdminCertificados | CanReadAch/CanManageCertificates | Lista visible sin secretos | Media | Screenshot + timestamp |
| 2 | Cargar `.cer/.crt` | AdminCertificados | CanManageCertificates | Alta OK y metadata visible | Alta | operationId/audit |
| 3 | Registrar PFX vía backend | AdminCertificados | CanManageCertificates | Registro OK sin exponer password | Crítica | logs sanitizados |
| 4 | Validar certificado | AdminCertificados | CanManageCertificates | Validación OK/errores sanitizados | Alta | resultado validación |
| 5 | Activar certificado | AdminCertificados | CanManageCertificates | Estado activo | Alta | auditoría |
| 6 | Revocar certificado | AdminCertificados | CanManageCertificates | Estado revocado | Alta | auditoría |
| 7 | Auditoría certificados | Auditor | CanViewNachaSecurityAudit | Consulta sin secretos | Alta | captura auditoría |
| 8 | Verificar no SecretRef completo | Auditor | CanReadAch | Solo masked | Crítica | screenshot |
| 9 | Generar NACHA plano | OperadorNacha | CanGenerateNacha | Operación Success | Alta | operationId/hash |
| 10 | Validar nombre externo | OperadorNacha | CanGenerateNacha | Nombre regulatorio aplicado | Alta | nombre + hash |
| 11 | Descargar plano autorizado | OperadorNacha | CanDownloadPlainNacha | Descarga solo tras authorize | Crítica | logs de autorización |
| 12 | Auditoría generación | Auditor | CanViewNachaSecurityAudit | Trazabilidad completa | Alta | operación auditada |
| 13 | Generar NACHA cifrado | OperadorNacha | CanGenerateEncryptedNacha | `.ENV` generado | Alta | hash plano/cifrado |
| 14 | Verificar firma/cifrado backend | SupervisorOperaciones | CanGenerateEncryptedNacha | Proceso backend-only | Crítica | evidencia API |
| 15 | Descargar `.ENV` | OperadorNacha | CanDownloadEnvelope | Descarga autorizada | Alta | authorize+download |
| 16 | Ver hashes | SupervisorOperaciones | CanReadAch | Hashes visibles/sanitizados | Media | captura |
| 17 | Certificados masked | Auditor | CanReadAch | Thumbprint/SecretRef masked | Alta | captura |
| 18 | Auditoría cifrado | Auditor | CanViewNachaSecurityAudit | Registro correcto | Alta | auditoría |
| 19 | Manual encrypt `.ENV` | OperadorSobreDigital | CanManualEncryptEnvelope | Success + descarga autorizable | Alta | operationId |
| 20 | Descargar `.ENV` manual | OperadorSobreDigital | CanDownloadEnvelope | descarga OK | Alta | evidencia descarga |
| 21 | Manual decrypt `.ENV` válido | OperadorSobreDigital | CanManualDecryptEnvelope | Success y plano autorizado | Alta | operationId |
| 22 | Descargar plano decrypt | OperadorSobreDigital | CanDownloadPlainNacha | solo autorizado | Crítica | authorize+download |
| 23 | Cargar `.ENV` alterado | OperadorSobreDigital | CanManualDecryptEnvelope | operación Failed | Crítica | error code |
| 24 | Verificar no plano por firma inválida | Auditor | CanReadAch | no hay plano | Crítica | captura + logs |
| 25 | Error `SIGNATURE_VALIDATION_FAILED` | Auditor | CanReadAch | código sanitizado | Crítica | payload/error |
| 26 | Sin permiso generar | UsuarioSinPermisos | - | 403/denegado | Crítica | evidencia respuesta |
| 27 | Sin permiso descifrar | UsuarioSinPermisos | - | 403/denegado | Crítica | evidencia respuesta |
| 28 | Sin permiso descargar plano | UsuarioSinPermisos | - | denegado | Crítica | evidencia respuesta |
| 29 | Auditor consulta trazabilidad | Auditor | CanViewNachaSecurityAudit | acceso sin secretos | Alta | reporte |
| 30 | Descarga expirada falla | OperadorNacha | CanDownload* | denegado por expiración | Crítica | código error |
| 31 | Consulta por operationId | SoporteTecnico | CanReadAch | detalle operativo | Alta | operationId |
| 32 | Trazar authorize/download | SoporteTecnico | CanReadAch | auditoría de descarga | Alta | auditoría |
| 33 | Ver fail-close audit | Auditor | CanViewNachaSecurityAudit | evento fail-close registrado | Alta | auditoría |
| 34 | Verificar no contenido en logs | SoporteTecnico | CanReadAch | logs sanitizados | Crítica | muestra logs |
| 35 | Vector oficial pendiente | SupervisorOperaciones | CanReadAch | estado Pending | Media | captura estado |
| 36 | Go/No-Go identifier/IV | SupervisorOperaciones | CanReadAch | NO_GO hasta vector oficial | Alta | acta UAT |
| 37 | No marcar certificación oficial | AdministradorSistema | CanReadAch | no aprobado sin vector | Crítica | acta decisión |

## 10) Matriz de roles y permisos
- **AdminCertificados:** `CanManageCertificates`, `CanViewNachaSecurityAudit`.
- **OperadorNacha:** `CanGenerateNacha`, `CanGenerateEncryptedNacha`, `CanDownloadEnvelope`, `CanDownloadPlainNacha` (según política).
- **OperadorSobreDigital:** `CanManualEncryptEnvelope`, `CanManualDecryptEnvelope`, `CanDownloadEnvelope`, `CanDownloadPlainNacha` (según política).
- **SupervisorOperaciones:** lectura/validación transversal + aprobación operativa.
- **Auditor:** `CanViewNachaSecurityAudit`, lectura sin secretos.
- **SoporteTecnico:** diagnóstico controlado sin exposición de secretos.
- **AdministradorSistema:** control operativo y rollback.
- **UsuarioSinPermisos:** validación de denegaciones.

## 11) Evidencia requerida por caso
- Screenshot SPA (sin datos sensibles).
- `operationId`, timestamp UTC, usuario/rol.
- hash de artefacto, nombre externo.
- estado operación y código error sanitizado.
- evidencia de autorización/expiración de descarga.
- consulta de auditoría correspondiente.

## 12) Reglas de evidencia prohibida
No incluir passwords, PFX, private keys, SecretRef completo, NACHA real productivo, ni contenido sensible en capturas/logs.

## 13) Criterios de aceptación
- Escenarios P0/P1 de seguridad y operación aprobados.
- Permisos finos efectivos en backend + SPA.
- Descarga segura y bloqueo de plano ante firma inválida verificados.
- Auditoría completa sin secretos.

## 14) Criterios de rechazo
- Cualquier exposición de secreto/material privado.
- Descarga sin autorización o fuera de ventana válida.
- Devolver plano tras `SIGNATURE_VALIDATION_FAILED`.
- Falta de trazabilidad de operación crítica.

## 15) Riesgos operativos actuales
- Bloqueo de `identifier/IV` hasta vector oficial (no bloquea UAT interno, sí certificación oficial).
- `npm test` Angular no estable en entorno actual (Karma/Chrome/rimraf).

## 16) Plan para corrección futura npm test Angular (deuda QA)
1. Fijar matriz compatible Node/npm + Angular CLI/Karma.
2. Definir `CHROME_BIN` estable en CI/local.
3. Revisar `rimraf` options de toolchain.
4. Añadir script `test:ci` reproducible.
5. Ejecutar suite frontend en runner con navegador provisionado.

## 17) Condiciones para UAT
- UAT puede iniciar con backend no-regresión verde y SPA build OK.
- Se marca deuda explícita de pruebas automáticas frontend hasta estabilizar entorno.
- No realizar hardening `identifier/IV` hasta vector oficial.
