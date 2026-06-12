# Matriz de Datos UAT - ACH Interbank

Fecha de generacion: 2026-05-18
Version: 0.1 preliminar
Rama analizada: `ACH-Interbank-Postgresql`
Estado: matriz inicial; requiere validacion humana.
Politica: datos reales sensibles no deben quedar en Git.

## Reglas Generales

- Usar datos anonimizados representativos o sinteticos cuando sea posible.
- Datos reales solo pueden usarse en ambiente controlado y evidencias externas seguras.
- En Git solo deben quedar referencias, hashes y valores anonimizados no reversibles.
- Certificados privados, PFX, llaves privadas, tokens y passwords deben mantenerse fuera del repositorio.

## Matriz

| ID dato | Escenario UAT | Tipo de dato | Real / anonimizado / sintetico | Origen | Sensibilidad | Regla de anonimizacion | Archivo o tabla destino | Responsable | Estado | Observacion |
|---|---|---|---|---|---|---|---|---|---|---|
| DAT-UAT-001 | UAT-REAL-007 | Cliente ordenante | Anonimizado | Fuente operativa UAT | Alta | Reemplazar nombre/ID por valores ficticios consistentes | `Customers`, `CustomerAccounts` | Negocio/Operaciones | PENDIENTE | No usar PII real |
| DAT-UAT-002 | UAT-REAL-007 | Cliente beneficiario | Anonimizado | Fuente operativa UAT | Alta | Reemplazar nombre/ID y mantener tipo persona | `CustomerThirdParties`, `AchTransactions` | Negocio | PENDIENTE | |
| DAT-UAT-003 | UAT-REAL-007 | Cuentas | Anonimizado | Fuente operativa UAT | Alta | Sustituir por cuentas ficticias de misma longitud | `CustomerAccounts`, `AchTransactions` | Operaciones | PENDIENTE | No conservar ultimos digitos reales |
| DAT-UAT-004 | UAT-REAL-003 | Bancos/instituciones | Sintetico/publico | Catalogo UAT | Media | Usar codigos publicos o catalogo UAT aprobado | `FinancialInstitutions` | Operaciones | PENDIENTE | Validar codigos |
| DAT-UAT-005 | UAT-REAL-003 | Camaras | Sintetico/controlado | Catalogo sistema | Media | No aplica si no sensible | `ClearingHouses` | Operaciones | PENDIENTE | ACH/CENIT/STA si aplica |
| DAT-UAT-006 | UAT-REAL-004 | Ciclos | Sintetico/controlado | Normativa/config UAT | Media | Mantener horarios representativos | `AchCycles`, `ClearingHouseCycleConfigs` | Operaciones | PENDIENTE | Validar cutoff |
| DAT-UAT-007 | UAT-REAL-015 | Causales | Sintetico/normativo | `docs/normativa/md` y catalogos | Baja/Media | No modificar regla; usar codigos permitidos | `AchReturnCodes`, `AchFileRejectionCodes`, `ReturnReasons` | Compliance | PENDIENTE | Firma normativa pendiente |
| DAT-UAT-008 | UAT-REAL-007 | Transacciones | Anonimizado/sintetico | Generacion UAT | Alta | Montos representativos, referencias ficticias | `AchTransactions`, `AchBatches` | Operaciones | PENDIENTE | Cubrir limites |
| DAT-UAT-009 | UAT-REAL-005 | Archivos NACHA-M | Anonimizado | Generador/carga UAT | Alta | Reemplazar PII/cuentas; registrar hash | Repositorio seguro externo; tablas NACHA | Operaciones | PENDIENTE | No versionar archivo real |
| DAT-UAT-010 | UAT-REAL-006 | Registros NACHA-M | Anonimizado | Archivo UAT | Alta | Mantener estructura y longitudes | `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls` | QA | PENDIENTE | Tipos 1/5/6/7/8/9 |
| DAT-UAT-011 | UAT-REAL-024 | Certificados publicos | Real UAT o sintetico | Seguridad | Media | Publicos aprobados; sin material privado | `DigitalCertificates`, `DigitalCertificateVersions` | Seguridad | PENDIENTE | Validar uso |
| DAT-UAT-012 | UAT-REAL-024 | Certificados privados | Referencia externa | Seguridad | Critica | Nunca en Git; usar `secretRef` enmascarado | repositorio seguro corporativo | Seguridad | PENDIENTE | No registrar PFX |
| DAT-UAT-013 | UAT-REAL-023 | Firmas | Anonimizado/controlado | Seguridad | Alta | Registrar hash/resultado, no llaves | `DigitalEnvelopeOperationLogs`, evidencia externa | Seguridad | PENDIENTE | |
| DAT-UAT-014 | UAT-REAL-022 | Sobres digitales | Anonimizado/controlado | Seguridad/Operaciones | Alta | No incluir payload sensible; hash y referencia | `NachaSecurityOperations`, evidencia externa | Seguridad | PENDIENTE | |
| DAT-UAT-015 | UAT-REAL-026 | Eventos | Sintetico/controlado | Ejecucion UAT | Media | Redactar usuario si aplica | `AuditLogs`, `AuthLogs`, `NavigationLogs`, `AchTransactionStateEvents` | Auditoria | PENDIENTE | |
| DAT-UAT-016 | UAT-REAL-020 | Conciliacion | Anonimizado | Reportes UAT | Alta | Montos controlados; no saldos reales | `Reports`/evidencia externa | Operaciones/Auditoria | PENDIENTE | No contabilidad definitiva |
| DAT-UAT-017 | UAT-REAL-019 | CUD | Referencia externa segura | Tesoreria/operacion | Critica | No subir soporte real; registrar hash y aprobador | Repositorio seguro externo | Tesoreria/Riesgo | PENDIENTE | Bloqueante productivo si aplica |
| DAT-UAT-018 | UAT-REAL-014 | ROR | Anonimizado | Casos UAT | Alta | Referencias ficticias y causales validas | `ReturnOfReturnFlows`, `AchReturnOfReturnGeneratedFileAudits` | Operaciones | PENDIENTE | Validar por camara |
| DAT-UAT-019 | UAT-REAL-001 | Usuarios de prueba | Sintetico | Seguridad/Tecnologia | Media | Usuarios UAT no personales o controlados | `Users`, `UserRoles` | Seguridad | PENDIENTE | Evitar passwords en documentos |
| DAT-UAT-020 | UAT-REAL-002 | Roles de prueba | Sintetico/controlado | Seguridad | Media | Mapear perfil a permisos sin datos sensibles | `Roles`, `Permissions`, `RolePermissions` | Seguridad | PENDIENTE | Roles UAT finos pendientes |
| DAT-UAT-021 | UAT-REAL-008 | Archivo bulk | Anonimizado | Generacion UAT | Alta | Reemplazar PII/cuentas/referencias | `BulkIngestionBatches`, `BulkIngestionItems` | Operaciones | PENDIENTE | JSON/CSV/Excel |
| DAT-UAT-022 | UAT-REAL-030 | Datos de reproceso | Anonimizado | Ejecucion UAT | Alta | Reutilizar referencias ficticias controladas | `BulkIngestionAttempts`, `IncomingNachaDispatchQueue` | Tecnologia | PENDIENTE | Validar idempotencia |
| DAT-UAT-023 | UAT-REAL-028 | Datos de reportes | Anonimizado | Dataset UAT | Alta | Montos/cuentas ficticias | Reportes API/PDF | Auditoria | PENDIENTE | |
| DAT-UAT-024 | UAT-REAL-025 | SecretRef corporativo | Referencia enmascarada | Seguridad | Critica | Registrar solo alias/mascara, no valor | configuracion UAT aprobada | Seguridad | PENDIENTE | Fuera del compose local |

