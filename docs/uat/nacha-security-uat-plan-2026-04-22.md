# UAT Plan Ejecutable — NACHA Security Backend/SPA (2026-04-22)

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

## 7) Datos y certificados de prueba
- Ciclos ACH sintéticos.
- Transacciones sintéticas (sin datos reales).
- NACHA planos sintéticos.
- `.ENV` válido generado por backend.
- `.ENV` alterado para escenario de firma inválida.
- `.cer/.crt` públicos de prueba.
- `.pfx` de prueba custodiado fuera de repo.
- `SecretRef` de prueba en gestor seguro.

## 8) Matriz UAT ejecutable (37 escenarios)

> Leyenda estado: Pendiente / Ejecutado / Aprobado / Fallido / Bloqueado.
>
> Regla de prioridad: escenarios de seguridad y autorización son P0.

| ScenarioId | Prioridad | Escenario | Rol requerido | Permisos requeridos | Estado | Resultado real | Fecha ejecución (UTC) | Ejecutor | EvidenceId | DefectId | Observaciones | Aprobación |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | P1 | Listar certificados | AdminCertificados | CanReadAch/CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Sin secretos visibles | Pendiente |
| 2 | P1 | Cargar `.cer/.crt` | AdminCertificados | CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Metadata X.509 válida | Pendiente |
| 3 | P0 | Registrar PFX vía backend | AdminCertificados | CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Sin exposición password/PFX | Pendiente |
| 4 | P1 | Validar certificado | AdminCertificados | CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Errores sanitizados | Pendiente |
| 5 | P1 | Activar certificado | AdminCertificados | CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Cambio de estado auditable | Pendiente |
| 6 | P1 | Revocar certificado | AdminCertificados | CanManageCertificates | Pendiente | N/A | N/A | N/A | N/A | N/A | Motivo auditable | Pendiente |
| 7 | P1 | Auditoría certificados | Auditor | CanViewNachaSecurityAudit | Pendiente | N/A | N/A | N/A | N/A | N/A | Sin secretos | Pendiente |
| 8 | P0 | Confirmar no SecretRef completo | Auditor | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | Solo SecretRefMasked | Pendiente |
| 9 | P1 | Generar NACHA plano | OperadorNacha | CanGenerateNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | operationId + hash | Pendiente |
| 10 | P1 | Validar nombre externo | OperadorNacha | CanGenerateNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | Política regulatoria aplicada | Pendiente |
| 11 | P0 | Descargar plano autorizado | OperadorNacha | CanDownloadPlainNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | Requiere authorize+download | Pendiente |
| 12 | P1 | Auditoría generación | Auditor | CanViewNachaSecurityAudit | Pendiente | N/A | N/A | N/A | N/A | N/A | Trazabilidad por operationId | Pendiente |
| 13 | P1 | Generar NACHA cifrado | OperadorNacha | CanGenerateEncryptedNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | `.ENV` + hashes | Pendiente |
| 14 | P0 | Verificar firma/cifrado backend | SupervisorOperaciones | CanGenerateEncryptedNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | Crypto solo backend | Pendiente |
| 15 | P1 | Descargar `.ENV` | OperadorNacha | CanDownloadEnvelope | Pendiente | N/A | N/A | N/A | N/A | N/A | Descarga autorizada | Pendiente |
| 16 | P2 | Ver hashes | SupervisorOperaciones | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | Hash plano/cifrado visibles | Pendiente |
| 17 | P1 | Certificados masked | Auditor | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | Thumbprint/secret masked | Pendiente |
| 18 | P1 | Auditoría cifrado | Auditor | CanViewNachaSecurityAudit | Pendiente | N/A | N/A | N/A | N/A | N/A | Evento completo | Pendiente |
| 19 | P1 | Manual encrypt `.ENV` | OperadorSobreDigital | CanManualEncryptEnvelope | Pendiente | N/A | N/A | N/A | N/A | N/A | operationId generado | Pendiente |
| 20 | P1 | Descargar `.ENV` manual | OperadorSobreDigital | CanDownloadEnvelope | Pendiente | N/A | N/A | N/A | N/A | N/A | authorize+download | Pendiente |
| 21 | P1 | Manual decrypt `.ENV` válido | OperadorSobreDigital | CanManualDecryptEnvelope | Pendiente | N/A | N/A | N/A | N/A | N/A | Success controlado | Pendiente |
| 22 | P0 | Descargar plano decrypt | OperadorSobreDigital | CanDownloadPlainNacha | Pendiente | N/A | N/A | N/A | N/A | N/A | Solo autorizado | Pendiente |
| 23 | P0 | Cargar `.ENV` alterado | OperadorSobreDigital | CanManualDecryptEnvelope | Pendiente | N/A | N/A | N/A | N/A | N/A | Debe fallar | Pendiente |
| 24 | P0 | No plano por firma inválida | Auditor | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | Nunca exponer plano | Pendiente |
| 25 | P0 | Error `SIGNATURE_VALIDATION_FAILED` | Auditor | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | Código sanitizado | Pendiente |
| 26 | P0 | Sin permiso generar | UsuarioSinPermisos | N/A | Pendiente | N/A | N/A | N/A | N/A | N/A | 403 esperado | Pendiente |
| 27 | P0 | Sin permiso descifrar | UsuarioSinPermisos | N/A | Pendiente | N/A | N/A | N/A | N/A | N/A | 403 esperado | Pendiente |
| 28 | P0 | Sin permiso descargar plano | UsuarioSinPermisos | N/A | Pendiente | N/A | N/A | N/A | N/A | N/A | denegado esperado | Pendiente |
| 29 | P1 | Auditor consulta trazabilidad | Auditor | CanViewNachaSecurityAudit | Pendiente | N/A | N/A | N/A | N/A | N/A | sin secretos | Pendiente |
| 30 | P0 | Descarga expirada falla | OperadorNacha | CanDownload* | Pendiente | N/A | N/A | N/A | N/A | N/A | expiración efectiva | Pendiente |
| 31 | P1 | Consulta por operationId | SoporteTecnico | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | detalle operativo | Pendiente |
| 32 | P1 | Trazar authorize/download | SoporteTecnico | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | auditoría descarga | Pendiente |
| 33 | P1 | Ver fail-close audit | Auditor | CanViewNachaSecurityAudit | Pendiente | N/A | N/A | N/A | N/A | N/A | evento fail-close | Pendiente |
| 34 | P0 | Verificar no contenido en logs | SoporteTecnico | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | logs sanitizados | Pendiente |
| 35 | P2 | Vector oficial pendiente | SupervisorOperaciones | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | estado Pending | Pendiente |
| 36 | P1 | Go/No-Go identifier/IV | SupervisorOperaciones | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | NO_GO hasta vector oficial | Pendiente |
| 37 | P1 | No marcar certificación oficial | AdministradorSistema | CanReadAch | Pendiente | N/A | N/A | N/A | N/A | N/A | no aprobado sin vector | Pendiente |

## 9) Criterios formales de cierre UAT

### UAT aprobado si:
- 100% de escenarios P0 aprobados.
- 0 defectos críticos abiertos.
- 0 defectos altos sin plan aprobado.
- Auditoría validada por `operationId`.
- Descarga segura validada.
- No exposición de secretos validada.
- Firma inválida no devuelve plano.
- Permisos finos validados con usuarios/claims reales.
- Rollback documentado.
- Evidencias mínimas adjuntas.

### UAT rechazado si:
- Falla cualquier P0 sin workaround aprobado.
- Se expone secreto.
- Se descarga plano sin autorización.
- Se devuelve plano con firma inválida.
- No hay auditoría de operaciones críticas.
- Migraciones no aplican.
- Permisos no son efectivos.

## 10) Defect tracking (template)

```text
DefectId:
EscenarioId:
Severidad:
Prioridad:
Estado:
Ambiente:
Usuario/Rol:
Fecha:
Descripción:
Resultado esperado:
Resultado obtenido:
Evidencia:
OperationId:
Logs sanitizados:
Responsable:
Acción correctiva:
Fecha compromiso:
Resultado retest:
```

### Matriz resumida de defectos
| Métrica | Cantidad |
|---|---:|
| Críticos abiertos | 0 |
| Altos abiertos | 0 |
| Medios abiertos | 0 |
| Bajos abiertos | 0 |
| Cerrados | 0 |
| Bloqueados | 0 |

## 11) Plan de evidencias fortalecidas
Reglas:
- Cada evidencia debe tener `EvidenceId`.
- Cada screenshot debe ocultar datos sensibles.
- Cada operación crítica debe incluir `operationId`.
- Cada descarga debe tener evento authorize + download.
- Cada fail-close debe incluir error code + auditoría.
- Cada prueba de permisos debe incluir usuario/rol.
- Cada prueba de secretos debe registrar confirmación de no exposición.

Formato obligatorio:
```text
EvidenceId:
ScenarioId:
Fecha/Hora UTC:
Ejecutor:
Rol:
Ambiente:
OperationId:
Archivo/Hash:
Resultado:
Adjunto:
Observaciones:
```

## 12) Plan de smoke test UAT
- Certificados: listar/cargar/activar/revocar.
- NACHA plano: generar + descarga autorizada.
- NACHA cifrado: generar `.ENV` + descarga.
- Manual encrypt/decrypt.
- Caso firma inválida: `SIGNATURE_VALIDATION_FAILED` + no plano.
- Auditoría por `operationId`.
- Pruebas negativas de permisos.

## 13) Monitoreo durante UAT
- Tasa de errores por endpoint crítico.
- Reintentos/autorizaciones de descarga expiradas.
- Eventos fail-close.
- Alertas de seguridad (accesos denegados anómalos).
- Latencia de operaciones y descarga.

## 14) Rollback drill (documental)
- Ejecutar simulacro de rollback backend + SPA + verificación post-rollback.
- Confirmar tiempo objetivo de recuperación y responsables.
- Documentar resultado en acta UAT.

## 15) Deuda QA frontend (`npm test`)
- Estado: bloqueado por CHROME_BIN/Karma/rimraf en este entorno.
- Plan:
  1. fijar matriz Node/npm + Angular CLI,
  2. runner con ChromeHeadless provisionado,
  3. corregir compatibilidad de `rimraf`,
  4. estandarizar script `test:ci`.

## 16) Regla inamovible `identifier/IV`
- `identifier/IV` permanece sin hardening hasta vector oficial ACH/CENIT.
- Esto no bloquea UAT interna controlada.
- Sí bloquea certificación oficial de interoperabilidad.

## 17) Referencia cruzada de validación técnica
- Antes de ejecutar la matriz UAT (sección 8), correr los comandos de validación técnica del checklist de despliegue, sección **13) Comandos técnicos de validación**.
- Registrar los resultados (comando + salida + fecha/hora UTC + ejecutor) como evidencia base para los escenarios P0/P1.
