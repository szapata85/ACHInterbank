# Deployment Checklist — NACHA Security (Backend + SPA) — 2026-04-22

## 1) Prechecks globales
- [ ] Confirmar alcance: solo operación NACHA security ya implementada.
- [ ] Confirmar no cambios criptográficos (`CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, `identifier/IV`).
- [ ] Confirmar workflow PostgreSQL manual-only (`workflow_dispatch`).
- [ ] Confirmar plan de rollback aprobado.
- [ ] Confirmar responsables on-call (backend, frontend, DB, seguridad).

## 2) Backend checklist
- [ ] Variables/appsettings validadas (sin secretos en texto plano).
- [ ] ConnectionStrings correctas para ambiente.
- [ ] `OperationArtifactOptions`/artifact store configurado fuera de repo.
- [ ] Límite de upload validado.
- [ ] Políticas/permisos finos cargados y claims disponibles.
- [ ] Endpoints `nacha-security/operations` smoke-tested.
- [ ] Endpoints certificados smoke-tested.
- [ ] Logs y auditoría sanitizados.
- [ ] HTTPS/CORS conforme política.

## 3) SPA checklist
- [ ] API base URL correcta en environment.
- [ ] Build producción validado.
- [ ] Rutas `nacha-security` accesibles por rol.
- [ ] Errores sanitizados visibles al usuario.
- [ ] Flujo `authorizeDownload -> downloadArtifact` validado.
- [ ] `sanitizeDownloadFileName` activo.
- [ ] Sin secretos en build/env públicos.
- [ ] Smoke test de navegación por rol completado.

## 4) BD / migraciones
- [ ] Backup previo a despliegue.
- [ ] Orden de migraciones validado.
- [ ] Migraciones aplicadas en ambiente:
  - [ ] ExternalFileName*
  - [ ] DigitalCertificate*
  - [ ] DigitalEnvelopeOperationLogs
  - [ ] NachaSecurityOperations
- [ ] Validar índices/constraints/RowVersion.
- [ ] Validar consultas críticas post-migración.

## 5) Certificados / SecretRef
- [ ] Certificados de prueba/no productivos cargados.
- [ ] PFX fuera de repositorio.
- [ ] Passwords fuera de repositorio.
- [ ] SecretRef en gestor seguro (no exposición completa).
- [ ] Revocación/rotación de material de prueba planificada.

## 6) Seguridad
- [ ] No secretos en appsettings/versionado.
- [ ] Descarga autorizada y expirable validada.
- [ ] No descarga de plano si firma falla.
- [ ] No contenido sensible en logs/auditoría.
- [ ] Revisión OWASP básica de endpoints de archivo.
- [ ] Antivirus/antimalware de archivos (si política del cliente lo exige).
- [ ] `identifier/IV` permanece sin hardening hasta vector oficial.

## 7) Post-deploy smoke test operativo
- [ ] Listar certificados.
- [ ] Generar NACHA plano.
- [ ] Generar NACHA cifrado `.ENV`.
- [ ] Manual encrypt y manual decrypt.
- [ ] Validar `SIGNATURE_VALIDATION_FAILED` en caso alterado.
- [ ] Validar auditoría por `operationId`.
- [ ] Validar denegaciones por permisos faltantes.

## 8) Rollback
### Backend
- [ ] Revertir release a versión previa estable.
- [ ] Reaplicar configuración previa.
- [ ] Revertir migración si procedimiento aprobado lo requiere.
- [ ] Limpiar artefactos temporales de versión fallida.

### SPA
- [ ] Revertir bundle a versión previa.
- [ ] Invalidar caché/CDN.
- [ ] Verificar rutas críticas post-rollback.

### BD
- [ ] Restaurar backup si rollback lógico no es suficiente.
- [ ] Preservar evidencia de auditoría.

### Certificados
- [ ] Desactivar/revocar material de prueba comprometido.
- [ ] Limpiar referencias temporales de secretos.

## 9) Go / No-Go
### GO (todos deben cumplirse)
- [ ] Backend build OK.
- [ ] SPA build OK.
- [ ] Tests backend críticos no regresión OK.
- [ ] Escenarios UAT P0/P1 aprobados.
- [ ] Permisos finos y descarga segura OK.
- [ ] Auditoría y trazabilidad OK.
- [ ] Sin exposición de secretos.
- [ ] Rollback listo y probado documentalmente.

### NO-GO (cualquiera bloquea)
- [ ] Plano devuelto con firma inválida.
- [ ] Descarga sin autorización.
- [ ] Exposición de PFX/password/SecretRef completo.
- [ ] Falla de migraciones críticas.
- [ ] Auditoría ausente en operaciones críticas.
- [ ] Permisos no efectivos.

## 10) Nota regulatoria/interoperabilidad
- `identifier/IV` **no** se endurece hasta vector oficial.
- Estado pendiente de vector oficial **no bloquea** UAT interna controlada.
- Estado pendiente **sí bloquea** certificación oficial de interoperabilidad.
