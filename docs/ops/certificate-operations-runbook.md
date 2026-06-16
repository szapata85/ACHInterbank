# Runbook operativo — Certificados, firma y sobre digital

## 1. Propósito
Definir operación segura de certificados X.509 para:
- firma;
- cifrado;
- descifrado;
- verificación;
- sobre digital;
- validación de cadena;
- evidencias UAT/productivas;
- rotación;
- expiración;
- permisos;
- incidentes.

Este runbook no habilita producción por sí solo.

## 2. Estado actual
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- No legacy/bypass exitoso.
- CRL/OCSP pendiente.
- EKU/KeyUsage pendiente.
- Trust store productivo pendiente de cierre.
- Evidencia real de permisos/certificados pendiente.

## 3. Fuentes
- `docs/audits/digital-envelope-signature-certificate-matrix-current.md`
- `docs/uat/digital-envelope-certificate-acceptance-checklist.md`
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/CryptoServiceScoped.cs`
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/DigitalEnvelopeSignatureValidator.cs`
- `src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeCertificateResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeSignatureAuditService.cs`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeCertificateCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeSignatureFailCloseTests.cs`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`

## 4. Roles y responsabilidades
| Rol | Responsabilidad | Evidencia esperada |
|---|---|---|
| Tecnología / DevSecOps | Gestión técnica de certificados, despliegue seguro, controles de acceso | bitácora de cambios, checklist técnico |
| Operaciones | Ejecución de ventana, monitoreo, respuesta operativa | acta operativa, logs de ejecución |
| Seguridad informática | Política de llaves/certificados, revisión de riesgos | visto bueno de seguridad |
| Riesgo | Validación de riesgo residual | acta de riesgo |
| Compliance | Validación de cumplimiento normativo interno | visto bueno compliance |
| Negocio | Aprobación de impacto operativo | aprobación negocio |
| Tesorería (si aplica) | Coordinación impacto en procesos de liquidación | evidencia de coordinación |
| Administrador de certificados | Inventario, instalación, rotación, retiro | inventario actualizado |
| Aprobador de cambios | Autorización formal de cambios | ticket aprobado |
| Auditor | Verificación de trazabilidad y evidencia | informe de auditoría |

## 5. Inventario obligatorio de certificados
| Campo | Descripción | Obligatorio | Evidencia |
|---|---|---|---|
| alias funcional | Nombre operacional (`CertSign`, `CertDecrypt`, etc.) | Sí | inventario |
| propósito | SIGN/DECRYPT/ENCRYPT/VERIFY | Sí | inventario |
| ambiente | DEV/UAT/PROD | Sí | inventario |
| cámara/entidad | CENIT/ACH u otra (si aplica) | Sí | inventario |
| subject | Subject X.509 | Sí | metadata cert |
| issuer | Issuer X.509 | Sí | metadata cert |
| serial | Serial X.509 | Sí | metadata cert |
| thumbprint | Thumbprint X.509 | Sí | metadata cert |
| NotBefore | inicio vigencia | Sí | metadata cert |
| NotAfter | fin vigencia | Sí | metadata cert |
| algoritmo | algoritmo de llave/firma | Sí | metadata cert |
| tamaño de llave | bits de llave | Sí | metadata cert |
| private key | sí/no según propósito | Sí | validación técnica |
| ubicación | store/PFX protegido por ambiente | Sí | evidencia instalación |
| owner | dueño funcional/técnico | Sí | inventario |
| responsable operativo | responsable de operación | Sí | inventario |
| fecha instalación | fecha efectiva | Sí | acta cambio |
| fecha rotación | fecha planificada/ejecutada | Sí | plan rotación |
| estado | activo/revocado/retirado | Sí | inventario |
| observación | notas de operación | No | inventario |

## 6. Propósitos permitidos
| Propósito | Requiere private key | Cert permitido | Uso | Validación |
|---|---|---|---|---|
| SIGN | Sí | Certificado privado | Firma de contenido/sobre | private key + vigencia |
| DECRYPT | Sí | Certificado privado | Descifrado de llave/contenido | private key + vigencia |
| ENCRYPT | No | Certificado público receptor | Cifrado hacia receptor | vigencia |
| VERIFY | No | Certificado público firmante | Verificación de firma | vigencia + firma |
| CHAIN/TRUST | No | CA raíz/intermedios | confianza de cadena | política de cadena |
| AUDIT | No | metadata | trazabilidad | control de logs |

- Certificado público nunca debe usarse para firmar/descifrar.
- Certificado privado nunca debe exponerse en logs/config sin protección.

## 7. Instalación de certificados
### 7.1 Instalación en Windows X509 Store
- Definir `CurrentUser` vs `LocalMachine` según servicio.
- Usar `StoreName=My` para identidad y `Root/CA` para confianza cuando aplique.
- Asignar permisos de private key al usuario de proceso/IIS AppPool.
- Registrar evidencia de `thumbprint/serial` instalado.
- Validar `HasPrivateKey` cuando el propósito es SIGN/DECRYPT.
- Probar acceso por `GetRSAPrivateKey` en ambiente controlado.

### 7.2 Instalación en Linux/containers
- Ubicar PFX/CER solo en rutas seguras (si aplica).
- Restringir permisos de filesystem.
- Validar usuario efectivo del proceso.
- Evitar exposición de secretos en variables no protegidas.
- Usar montaje seguro para material sensible.
- Rotar sin dejar residuos (archivos/copias temporales).
- Adjuntar evidencia de acceso correcto.

### 7.3 Modelo operativo vigente
- La custodia de certificados debe usar el mecanismo corporativo aprobado.
- Windows: X509 Store + permisos de private key.
- Linux/containers: PFX/CER protegido por ambiente y permisos mínimos.
- BD: solo metadata, inventario, auditoría y evidencia.
- Prohibido guardar PFX + password en BD.
- Prohibido guardar private key en texto plano.
- Prohibido depender de proveedores retirados para abrir, firmar o descifrar sobres.

## 8. Validación posterior a instalación
- certificado encontrado.
- subject/issuer/serial/thumbprint coinciden.
- NotBefore/NotAfter vigente.
- HasPrivateKey correcto según propósito.
- private key accesible para SIGN/DECRYPT.
- certificado público usable para VERIFY/ENCRYPT.
- chain validation según política.
- no secretos en logs.
- auditoría generada.
- prueba sobre firmado válido.
- prueba unsigned rechazado.
- prueba private key faltante rechazada.

## 9. Permisos de llave privada
- Identificar usuario de proceso.
- Validar acceso mínimo necesario.
- No usar cuentas administradoras salvo justificación formal.
- No compartir PFX por canales inseguros.
- No almacenar password en appsettings.
- No registrar password/private key.
- Adjuntar evidencia de permisos.
- Aplicar doble control para cambios productivos.

## 10. Rotación de certificados
1. Solicitud de rotación.
2. Generación/recepción de nuevo certificado.
3. Validación de metadata.
4. Instalación en ambiente no productivo.
5. Prueba de firma/cifrado/descifrado/verificación.
6. Validación UAT.
7. Ventana de cambio.
8. Instalación productiva.
9. Activación.
10. Monitoreo.
11. Retiro controlado del certificado anterior.
12. Actualización de inventario.
13. Acta de cierre.

**Rollback**
- Restaurar certificado anterior (si vigente).
- Revertir configuración asociada.
- Bloquear procesamiento si no hay certificado válido.
- Registrar incidente y causa raíz.

## 11. Expiración y alertas
| Umbral | Alerta | Responsable | Acción | Evidencia |
|---|---|---|---|---|
| 90 días | Preventiva inicial | Admin cert + Operaciones | plan de rotación | ticket/plan |
| 60 días | Preventiva reforzada | DevSecOps + Seguridad | confirmar ventana | acta |
| 30 días | Crítica | Operaciones + Riesgo | ejecutar rotación | evidencia técnica |
| 15 días | Alta urgencia | Operaciones + Tecnología | escalamiento ejecutivo | correo/acta |
| 7 días | Emergencia | Tecnología + Seguridad | cambio inmediato | acta emergencia |
| Vencido | Incidente | Operaciones + Seguridad | bloquear uso y remediar | incidente cerrado |

- Certificado vencido falla por runtime.
- No se debe usar override para aceptar vencidos en productivo.

## 12. Cadena de confianza
- Definir CA raíz y CA intermedia.
- Definir trust store por ambiente.
- Aplicar chain on/off según política.
- Permitir self-signed solo en desarrollo/UAT controlado cuando chain esté desactivada.
- En productivo, cadena/trust store requiere política definida y aprobada.
- Adjuntar evidencia de instalación de CA.
- Errores esperados:
  - `SIGNER_CERTIFICATE_NOT_TRUSTED`
  - `CERTIFICATE_CHAIN_NOT_TRUSTED`

## 13. Revocación
Actualmente CRL/OCSP real no está implementado; modo técnico actual: `X509RevocationMode.NoCheck`.

Pendientes operativos:
- definir política de revocación.
- definir fuente CRL/OCSP.
- definir modo online/offline.
- asignar responsable.
- definir comportamiento si fuente no responde.
- definir evidencia requerida.
- mantener NO-GO hasta aprobación.

## 14. EKU/KeyUsage
No implementado actualmente.

Pendiente definir:
- `DigitalSignature` para firma (si aplica).
- `KeyEncipherment/DataEncipherment` para cifrado (si aplica).
- política de aceptación.
- evidencia requerida.
- decisión de riesgo/compliance.
- criterio NO-GO hasta aprobación/justificación.

## 15. Operación diaria
Checklist diario/semanal:
- certificados vigentes.
- próximos vencimientos.
- errores de firma.
- errores de descifrado.
- errores de private key.
- errores de cadena.
- eventos `FAILED`.
- intentos unsigned.
- logs sin secretos.
- auditoría disponible.
- correlación con archivos ACH/CENIT/NACHA.

## 16. Manejo de incidentes
| Incidente | Acción inmediata | Responsable | Evidencia | Criterio de cierre |
|---|---|---|---|---|
| certificado vencido | bloquear operación y activar rotación | Operaciones + DevSecOps | logs + acta | certificado vigente activo |
| private key inaccesible | validar permisos/almacenamiento | DevSecOps | evidencia permisos | firma/descifrado exitoso |
| certificado incorrecto | revertir a certificado válido | Admin cert | inventario + rollback | validación cruzada OK |
| firma inválida | fail-close y análisis | Operaciones + Seguridad | audit FAILED | causa raíz cerrada |
| sobre unsigned recibido | rechazo controlado | Operaciones | evidencia audit | validación upstream definida |
| chain no confiable | validar trust store | DevSecOps + Seguridad | evidencia trust store | chain OK según política |
| posible compromiso private key | revocar/retirar certificado | Seguridad + Riesgo | incidente crítico | nuevo certificado operativo |
| password filtrado | rotación inmediata de secreto | Seguridad + Operaciones | acta incidente | secreto reemplazado |
| dependencia de proveedor retirado detectada en certificados | retirar dependencia y aplicar modelo vigente | DevSecOps | evidencia de remediación | flujo con custodia corporativa aprobada |
| mismatch issuer/serial/thumbprint | detener procesamiento | Operaciones | evidencia mismatch | fuente certificada validada |

## 17. Controles de seguridad
- principio de mínimo privilegio.
- doble control.
- segregación de roles.
- no secretos en repositorio.
- no PFX sin protección.
- no password en appsettings.
- no uso de SecretRef en certificados.
- auditoría de acceso.
- revisión periódica.
- backup seguro.
- eliminación segura de certificados retirados.

## 18. Evidencia requerida para UAT/Productivo
- inventario de certificados.
- captura/configuración de store.
- permisos de private key.
- prueba firma válida.
- prueba descifrado válido.
- prueba unsigned rechazado.
- prueba certificado vencido rechazado.
- prueba private key faltante rechazada.
- prueba chain policy.
- evidencia NoCheck/revocación pendiente.
- auditoría OK/FAILED.
- no secretos en logs.
- acta UAT.
- aprobación operaciones/riesgo/compliance/tecnología.

## 19. Criterios de salida NO-GO certificados
1. Inventario certificado completo.
2. Certificados SIGN/DECRYPT/ENCRYPT/VERIFY identificados.
3. Private key validada para SIGN.
4. Private key validada para DECRYPT.
5. Cert público validado para VERIFY.
6. Cert público validado para ENCRYPT.
7. Vigencia validada.
8. Chain/trust store definido.
9. Revocación definida o excepción aprobada.
10. EKU/KeyUsage definido o excepción aprobada.
11. Permisos IIS/Linux validados.
12. Secretos protegidos.
13. Rotación definida.
14. Alertas de expiración definidas.
15. Incidentes documentados.
16. UAT ejecutado.
17. Evidencia adjunta.
18. Aprobación seguridad.
19. Aprobación riesgo/compliance.
20. Aprobación tecnología.
21. Scorecard actualizado.

## 20. Decisión vigente
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- Este runbook no habilita producción.
- Próximo paso: cerrar trust store/cadena o evaluar EKU/KeyUsage/revocación cuando exista política aprobada.
