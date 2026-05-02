# Scalar-SEC-PERM-1 — Diseño de permisos finos por dominio crítico (2026-05-01)

## 1. Resumen ejecutivo
Este documento define el diseño objetivo de permisos finos por dominio crítico para evolucionar el modelo actual basado en `CanReadAch` y `CanManageAch`. Se toma como línea base el cierre técnico de Scalar-SEC-5 y se propone una hoja de ruta gradual, segura y auditable, sin modificar código en esta fase.

## 2. Contexto desde Scalar-SEC-5
Scalar-SEC-5 cerró el frente de autorización explícita y metadata OpenAPI/Scalar para el alcance evaluado. Se confirmó cobertura de seguridad en OpenAPI y protección de endpoints críticos, pero quedó identificado el riesgo residual de granularidad insuficiente por uso de permisos genéricos.

## 3. Problema actual: permisos genéricos CanReadAch/CanManageAch
- Ventaja actual: simplicidad y cobertura transversal rápida.
- Limitación principal: segregación de funciones insuficiente para dominios críticos.
- Riesgo: sobreasignación de privilegios en operaciones de alto impacto.

## 4. Objetivo del modelo de permisos finos
Establecer un modelo de permisos por dominio y acción (`<DOMINIO>.<ACCION>`) que permita:
- mínimo privilegio;
- segregación de funciones;
- trazabilidad de autorizaciones;
- control de riesgos operativos y regulatorios.

## 5. Principios de diseño
1. Mínimo privilegio por acción real del endpoint.
2. Separación estricta entre lectura, mutación y acciones operativas críticas.
3. Compatibilidad temporal con permisos genéricos durante migración.
4. Trazabilidad y auditabilidad de decisiones de autorización.
5. Priorización por criticidad (P0/P1/P2).

## 6. Convención de nombres
Formato estándar:

`<DOMINIO>.<ACCION>`

Ejemplos: `Transactions.Read`, `Nacha.Export`, `Certificates.Revoke`, `Users.AssignRoles`, `Maintenance.Seed`.

## 7. Matriz dominio → permisos finos
| Dominio | Permiso fino propuesto | Tipo | Reemplaza o complementa | Justificación |
|---|---|---|---|---|
| Transacciones ACH | Transactions.Read | Lectura | Complementa CanReadAch | Consulta operativa de transacciones |
| Transacciones ACH | Transactions.Create | Escritura | Complementa CanManageAch | Alta de transacción individual |
| Transacciones ACH | Transactions.BulkSubmit | Acción operativa | Complementa CanManageAch | Envío masivo con impacto operativo |
| Transacciones ACH | Transactions.PolicyPreview | Lectura | Complementa CanReadAch | Validación de políticas previa |
| NACHA-M | Nacha.Read | Lectura | Complementa CanReadAch | Consulta técnica de información NACHA |
| NACHA-M | Nacha.Upload | Acción operativa | Complementa CanManageAch | Carga de archivos NACHA |
| NACHA-M | Nacha.Export | Acción operativa | Complementa CanManageAch | Exportación de archivos |
| NACHA-M | Nacha.Generate | Acción operativa | Complementa CanManageAch | Generación/armado de archivos |
| NACHA-M | Nacha.Configure | Configuración | Complementa CanManageAch | Configuración de estructuras/perfiles |
| NACHA-M | Nacha.PublishConfig | Administración | Complementa CanManageAch | Publicación de configuración |
| NACHA-M | Nacha.ArchiveConfig | Administración | Complementa CanManageAch | Archivado de configuración |
| NACHA Security | NachaSecurity.Read | Lectura | Complementa CanReadAch | Consulta de operaciones de seguridad |
| NACHA Security | NachaSecurity.GenerateEncrypted | Seguridad | Complementa FineGrainedPermissions | Generación cifrada |
| NACHA Security | NachaSecurity.ManualEncrypt | Seguridad | Complementa FineGrainedPermissions | Cifrado manual |
| NACHA Security | NachaSecurity.ManualDecrypt | Seguridad | Complementa FineGrainedPermissions | Descifrado manual |
| NACHA Security | NachaSecurity.AuthorizeDownload | Seguridad | Complementa CanReadAch | Autorización de descarga |
| Sobre digital | DigitalEnvelope.Encrypt | Seguridad | Complementa CanManageAch | Cifrado sobre digital |
| Sobre digital | DigitalEnvelope.Decrypt | Seguridad | Complementa CanManageAch | Descifrado sobre digital |
| Sobre digital | DigitalEnvelope.Test | Seguridad | Complementa CanManageAch | Prueba técnica criptográfica |
| Certificados | Certificates.Read | Lectura | Complementa CanReadAch | Consulta de inventario |
| Certificados | Certificates.UploadPublic | Seguridad | Complementa permisos actuales | Carga certificado público |
| Certificados | Certificates.RegisterPrivate | Seguridad | Complementa permisos actuales | Registro de certificado privado |
| Certificados | Certificates.Activate | Administración | Complementa permisos actuales | Activación de certificado |
| Certificados | Certificates.Revoke | Administración | Complementa permisos actuales | Revocación de certificado |
| Certificados | Certificates.Validate | Seguridad | Complementa permisos actuales | Validación de material criptográfico |
| Certificados | Certificates.Audit | Auditoría | Complementa permisos actuales | Consulta de auditoría de certificados |
| Reportes | Reports.Read | Lectura | Complementa CanReadAch | Consulta de reportes |
| Reportes | Reports.Export | Acción operativa | Complementa CanReadAch | Exportación PDF/archivos |
| CENIT | Cenit.ReadQueues | Lectura | Complementa CanReadAch | Consulta de colas |
| CENIT | Cenit.ReadNetPositions | Lectura | Complementa CanReadAch | Posiciones netas |
| CENIT | Cenit.ReadOptimization | Lectura | Complementa CanReadAch | Decisiones de optimización |
| CENIT | Cenit.ReadTraceability | Lectura | Complementa CanReadAch | Trazabilidad CENIT |
| Command Center | CommandCenter.Read | Lectura | Complementa CanReadAch | Consulta de estado |
| Command Center | CommandCenter.Retry | Acción operativa | Complementa CanManageAch | Reintento |
| Command Center | CommandCenter.Unblock | Acción operativa | Complementa CanManageAch | Desbloqueo |
| Command Center | CommandCenter.Requeue | Acción operativa | Complementa CanManageAch | Reencolado |
| Command Center | CommandCenter.MarkFailedFinal | Acción operativa | Complementa CanManageAch | Marcado de fallo final |
| Cargas masivas | BulkIngestion.Upload | Acción operativa | Complementa CanManageAch | Carga de lote |
| Cargas masivas | BulkIngestion.Read | Lectura | Complementa CanReadAch | Consulta de lote |
| Cargas masivas | BulkIngestion.Retry | Acción operativa | Complementa CanManageAch | Reintento de lote |
| Cargas masivas | BulkIngestion.Cancel | Acción operativa | Complementa CanManageAch | Cancelación de lote |
| Trazabilidad | Traceability.Read | Lectura | Complementa CanReadAch | Consulta de trazabilidad |
| Trazabilidad | Traceability.CertifySol02 | Acción operativa | Complementa CanManageAch | Certificación SOL02 |
| Devoluciones ACH | Returns.Read | Lectura | Complementa CanReadAch | Consulta devoluciones |
| Devoluciones ACH | Returns.GenerateFile | Acción operativa | Complementa CanManageAch | Generación de archivo |
| Usuarios | Users.Read | Lectura | Complementa CanReadAch | Consulta usuarios |
| Usuarios | Users.Create | Escritura | Complementa CanManageAch | Alta usuarios |
| Usuarios | Users.Update | Escritura | Complementa CanManageAch | Actualización usuarios |
| Usuarios | Users.Deactivate | Escritura | Complementa CanManageAch | Inactivación usuarios |
| Usuarios | Users.AssignRoles | Administración | Complementa CanManageAch | Asignación de roles |
| Usuarios | Users.ManageBranding | Configuración | Complementa CanManageAch | Gestión branding |
| Usuarios | Users.ManagePasswordRules | Configuración | Complementa CanManageAch | Reglas contraseña |
| Usuarios | Users.ManageLockout | Configuración | Complementa CanManageAch | Lockout |
| Roles | Roles.Read | Lectura | Complementa CanReadAch | Consulta roles |
| Roles | Roles.Create | Escritura | Complementa CanManageAch | Alta roles |
| Roles | Roles.Update | Escritura | Complementa CanManageAch | Edición roles |
| Roles | Roles.Delete | Escritura | Complementa CanManageAch | Baja roles |
| Permisos | Permissions.Read | Lectura | Complementa CanReadAch | Consulta permisos |
| Permisos | Permissions.Assign | Administración | Complementa CanManageAch | Asignación permisos |
| Configuración operativa | Config.Read | Lectura | Complementa CanReadAch | Consulta de configuración |
| Configuración operativa | Config.Manage | Configuración | Complementa CanManageAch | Gestión de configuración |
| Integraciones SOAP | Integrations.Read | Lectura | Complementa CanReadAch | Consulta integración |
| Integraciones SOAP | Integrations.ManageMappings | Configuración | Complementa CanManageAch | Gestión de mapping sets |
| Integraciones SOAP | Integrations.Validate | Acción operativa | Complementa CanManageAch | Validación de integración |
| Integraciones SOAP | Integrations.Publish | Acción operativa | Complementa CanManageAch | Publicación de configuración |
| Integraciones SOAP | Integrations.Compare | Lectura | Complementa CanReadAch | Comparación de configuración |
| Catálogos regulatorios | RegulatoryCatalogs.Read | Lectura | Complementa CanReadAch | Consulta regulatoria |
| Maintenance/Admin | Maintenance.Seed | Acción operativa | Complementa CanManageAch | Seed de datos |
| Maintenance/Admin | Maintenance.RunAdminTask | Administración | Complementa CanManageAch | Tareas administrativas |

## 8. Matriz endpoint/controller → permiso actual → permiso fino propuesto
| Controller | Método | Ruta o acción | Policy actual | Permiso fino propuesto | Criticidad | Fase |
|---|---|---|---|---|---|---|
| TransactionsController | GET | /Transactions | CanReadAch | Transactions.Read | P0 | Fase 1 |
| TransactionsController | POST | /Transactions | CanManageAch | Transactions.Create | P0 | Fase 1 |
| TransactionsController | POST | /Transactions/bulk/submit | CanManageAch | Transactions.BulkSubmit | P0 | Fase 1 |
| TransactionsController | GET | /Transactions/policies/preview | CanReadAch | Transactions.PolicyPreview | P0 | Fase 1 |
| AchTraceabilityController | GET | report/transactions | CanReadAch | Traceability.Read | P0 | Fase 1 |
| AchTraceabilityController | POST | certify SOL02 | CanManageAch | Traceability.CertifySol02 | P0 | Fase 1 |
| AchReturnsController | GET | devoluciones por ciclo | CanReadAch | Returns.Read | P0 | Fase 1 |
| AchReturnsController | POST | generar archivo devolución | CanManageAch | Returns.GenerateFile | P0 | Fase 1 |
| BulkIngestionController | POST | upload | CanManageAch | BulkIngestion.Upload | P1 | Fase 1 |
| BulkIngestionController | GET | estado/items/summary | CanManageAch | BulkIngestion.Read | P1 | Fase 1 |
| BulkIngestionController | POST | retry/cancel | CanManageAch | BulkIngestion.Retry / BulkIngestion.Cancel | P1 | Fase 1 |
| IncomingNachaCommandCenterController | GET | consulta command center | CanReadAch | CommandCenter.Read | P1 | Fase 1 |
| IncomingNachaCommandCenterController | POST | retry/unblock/requeue/mark | CanManageAch | CommandCenter.* | P1 | Fase 1 |
| NachaController | POST | /Nacha/header | CanManageAch | Nacha.Configure | P1 | Fase 2 |
| NachaUploadController | POST | upload/ingestión | CanManageAch | Nacha.Upload | P1 | Fase 2 |
| NachaExportController | GET | exportación NACHA | CanReadAch | Nacha.Export | P1 | Fase 2 |
| NachaSecurityOperationsController | POST | generate/manual ops | FineGrainedPermissions.* | NachaSecurity.* | P1 | Fase 2 |
| CertificateManagementController | POST/PUT | gestión certificados | FineGrainedPermissions.* | Certificates.* | P1 | Fase 2 |
| SobreDigitalController | POST | encrypt/decrypt/testRSA | CanManageAch | DigitalEnvelope.* | P1 | Fase 2 |
| ReportsController | GET | reportes | CanReadAch | Reports.Read / Reports.Export | P2 | Fase 3 |
| CenitOperationsController | GET | colas/neteo/optimización | CanReadAch | Cenit.* | P2 | Fase 3 |
| UsersController | CRUD | /api/Users* | Authorize/CanManageAch | Users.* | P1 | Fase 2 |
| RolesController | CRUD | /api/Roles* | Authorize | Roles.* | P1 | Fase 2 |
| PermissionsController | GET/assign | /api/Permissions* | Authorize | Permissions.* | P1 | Fase 2 |
| RegulatoryCatalogsController | GET | /api/regulatory-catalogs/* | CanReadAch | RegulatoryCatalogs.Read | P2 | Fase 3 |
| MaintenanceController | POST | /Maintenance/seed | CanManageAch | Maintenance.Seed | P1 | Fase 2 |
| IntegrationCatalogController | CRUD | /api/integrations/* | CanManageAch | Integrations.* | P2 | Fase 3 |
| IntegrationMappingSetsController | CRUD | mapping sets | CanManageAch | Integrations.ManageMappings | P2 | Fase 3 |

## 9. Matriz rol sugerido → permisos
| Rol sugerido | Descripción | Permisos principales | Restricciones |
|---|---|---|---|
| Operador ACH | Operación diaria de transacciones | Transactions.Read/Create, BulkIngestion.Read | Sin gestión de certificados ni usuarios |
| Supervisor ACH | Supervisión operativa y excepciones | Transactions.*, Returns.*, Traceability.Read | No administra permisos globales |
| Tesorería / Liquidez | Monitoreo CENIT/liquidez | Cenit.Read*, Reports.Read | Sin mutaciones de configuración |
| Seguridad Operativa | Seguridad NACHA y sobre digital | NachaSecurity.*, DigitalEnvelope.* | Sin administración de usuarios |
| Administrador de Certificados | Ciclo de vida de certificados | Certificates.* | Sin operaciones transaccionales |
| Auditor | Auditoría y trazabilidad | Reports.Read, Certificates.Audit, Traceability.Read | Solo lectura |
| Administrador de Usuarios | Gestión de identidades | Users.*, Roles.Read, Permissions.Read/Assign | Sin operaciones NACHA/CENIT |
| Administrador Técnico | Configuración de plataforma | Config.*, Integrations.*, Maintenance.RunAdminTask | Control dual para tareas críticas |
| Consulta Ejecutiva | Consulta de alto nivel | Reports.Read, Cenit.Read* | Sin escritura |
| Soporte N1/N2 | Soporte operativo controlado | CommandCenter.Read, BulkIngestion.Read, Traceability.Read | Sin acciones destructivas |

## 10. Matriz de segregación de funciones
| Conflicto | No combinar permisos | Riesgo | Control recomendado |
|---|---|---|---|
| Crear transacciones y aprobar/reprocesar | Transactions.Create + CommandCenter.MarkFailedFinal | Fraude/abuso operativo | Doble control supervisor |
| Cargar certificados y activarlos | Certificates.UploadPublic/RegisterPrivate + Certificates.Activate | Introducción de material malicioso | Flujo de 4 ojos |
| Cifrar/descifrar y autorizar descarga | DigitalEnvelope.* + NachaSecurity.AuthorizeDownload | Exfiltración de datos | Separación seguridad-operación |
| Administrar usuarios y asignarse permisos | Users.* + Permissions.Assign | Escalada de privilegios | Aprobación externa + auditoría |
| Ejecutar maintenance seed en ambientes sensibles | Maintenance.Seed | Alteración de datos | Control por ambiente + ventana de cambio |
| Modificar configuración NACHA y publicar | Nacha.Configure + Nacha.PublishConfig | Error sistémico de compensación | Revisión técnica y aprobación |
| Reintentar colas y marcar fallos finales | CommandCenter.Retry + CommandCenter.MarkFailedFinal | Ocultamiento de incidentes | Registro inmutable + supervisión |

## 11. Permisos P0/P1/P2 priorizados
- **P0 prioritario:** Transactions.*, Traceability.*, Returns.*
- **P1 prioritario:** BulkIngestion.*, CommandCenter.*, Nacha.*, NachaSecurity.*, Certificates.*, DigitalEnvelope.*
- **P2 prioritario:** Reports.*, Cenit.*, Users.*, Roles.*, Permissions.*, RegulatoryCatalogs.*, Integrations.*, Maintenance.*

## 12. Plan de implementación por fases
- **Fase 1:** diseñar constantes de permisos finos y mapping de policies sin cambiar comportamiento.
- **Fase 2:** agregar policies nuevas manteniendo CanReadAch/CanManageAch como compatibilidad temporal.
- **Fase 3:** migrar controladores P0/P1 a permisos finos.
- **Fase 4:** migrar P2/P3.
- **Fase 5:** revalidar OpenAPI security, tests, UAT y matriz de roles.
- **Fase 6:** deprecar gradualmente permisos genéricos donde aplique.

## 13. Riesgos de migración
- Riesgo de regresión de autorización por mapping incompleto.
- Riesgo de sobrerrestricción en flujos críticos operativos.
- Riesgo de dependencia temporal larga en permisos genéricos.
- Riesgo de inconsistencias entre claims de identidad y policies internas.

## 14. Pruebas requeridas
- Pruebas unitarias de resolución de policy por endpoint crítico.
- Pruebas de integración por rol sugerido.
- Pruebas de regresión OpenAPI (`security` por operación).
- Pruebas de segregación de funciones (escenarios de conflicto).
- UAT de seguridad y pruebas de penetración en etapas posteriores.

## 15. Qué NO se implementó
## Qué NO se implementó en Scalar-SEC-PERM-1
- No se implementaron nuevos permisos en código.
- No se cambiaron policies existentes.
- No se cambiaron controladores.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se ejecutaron migraciones.
- No se declara producción lista.
- Este documento es diseño y hoja de ruta.

## 16. Recomendación siguiente
Se recomienda ejecutar:
- **Scalar-SEC-PERM-2 — Implementación controlada de constantes y policies de permisos finos sin migrar controladores.**
