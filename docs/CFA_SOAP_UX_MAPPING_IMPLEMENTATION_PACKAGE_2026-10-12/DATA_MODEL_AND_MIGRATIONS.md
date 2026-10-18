# Modelo de datos y migraciones

## 1. Estrategia

Las migraciones serán aditivas primero y contractivas solo al final del programa.

Compatibilidad requerida:

- PostgreSQL;
- SQL Server;
- mappings publicados existentes;
- configuración SOAP existente;
- historia existente.

No editar migraciones ya compartidas.

## 2. Nueva entidad `IntegrationContractVersion`

Campos recomendados:

| Campo | Tipo | Requerido |
|---|---|---:|
| Id | Guid | Sí |
| MethodId | int FK | Sí |
| VersionLabel | varchar(100) | Sí |
| Status | enum/int | Sí |
| OperationName | varchar(200) | Sí |
| TargetNamespace | varchar(500) | Sí |
| SoapAction | varchar(1000) | Sí |
| SourceType | varchar(50) | Sí |
| SourceDocumentName | varchar(260) | No |
| SourceDocumentHash | varchar(64) | No |
| SourceReference | varchar(1000) | No |
| Notes | varchar(2000) | No |
| Revision | bigint | Sí |
| ImportedAtUtc | datetime | No |
| ImportedBy | varchar(200) | No |
| ActivatedAtUtc | datetime | No |
| ActivatedBy | varchar(200) | No |
| AuditableEntity fields | existentes | Sí |

Índices:

- `(MethodId, VersionLabel)` unique;
- `(MethodId, Status)` no unique, validación de un solo Active en application transaction;
- `SourceDocumentHash` index opcional.

No usar filtered unique index dependiente de proveedor como única garantía de Active; mantener también validación transaccional provider-neutral.

## 3. Cambios a `IntegrationMethodParameter`

Agregar:

```text
ContractVersionId Guid?   // nullable durante migración
XmlElementName string
XsdType string
Nillable bool
MinOccurs int
MaxOccurs int?
SupportStatus string
```

`ContractVersionId` pasa a NOT NULL cuando todos los registros existentes se hayan vinculado.

Índice unique final:

```text
(ContractVersionId, ParameterPath)
```

No mantener unique global por MethodId + ParameterPath porque distintas versiones deben poder contener el mismo parámetro.

## 4. Cambios a `IntegrationMappingSet`

Agregar:

```text
ContractVersionId Guid?
Revision long default 1
```

Backfill:

- cada mapping existente se asocia al baseline contract version de su método;
- luego `ContractVersionId` pasa a requerido.

Índice:

```text
(MethodId, ContractVersionId, Version)
```

## 5. `IntegrationMappingRule`

No necesita nueva FK si `ParameterId` sigue apuntando a `IntegrationMethodParameter` versionado.

Agregar validación de DB/Application:

```text
rule.Parameter.ContractVersionId == mappingSet.ContractVersionId
```

EF no puede expresar fácilmente todas las restricciones cross-table como CHECK; Application debe validarlo siempre y tests de integración deben cubrirlo.

## 6. Metadatos funcionales de `IntegrationMethod`

Para eliminar hardcode de nombres funcionales en Angular, agregar si no existe read model derivable:

```text
FunctionalDisplayName
FunctionalDescription
FunctionalCategory
RiskLevel
```

Baseline recomendado:

- `Proc_Transacciones` → Créditos recibidos en CFA.
- `Proc_Contrapartidas` → Débitos originados en CFA.
- `RegistrarRespuestaTransaccion` → Respuestas de transacciones.

Los códigos técnicos existentes no se modifican.

## 7. Source catalog

Conservar `IntegrationSourceCatalogField`.

Agregar opcionalmente:

```text
DescriptionEs
FunctionalGroup
SensitiveClassification
ProviderKey
```

`ProviderKey`/`FieldPath` debe ser estable y resuelto solo por providers registrados.

No almacenar expresiones de código ni reflection paths ejecutables.

## 8. Transformaciones

No se requiere tabla nueva inicialmente.

El registry de transformaciones puede seguir siendo code-defined porque una transformación es comportamiento ejecutable. El API expone descriptors.

No permitir crear una transformación arbitraria desde DB.

## 9. Historia de contrato

Crear `IntegrationContractVersionHistory` si se necesita snapshot auditable independiente:

```text
Id Guid
ContractVersionId Guid
MethodId int
Action string
PerformedBy string
PerformedAtUtc DateTime
SnapshotJson string
SnapshotHash string
```

Recomendado porque contrato y mapping tienen ciclos distintos.

Si se decide no crear tabla, el snapshot de contrato debe persistirse en un mecanismo histórico equivalente; no depender únicamente de audit fields.

## 10. Migraciones propuestas

### M1 — `Integrations_AddContractVersioning`

- crea `IntegrationContractVersions`;
- crea baseline version por método;
- agrega `ContractVersionId` nullable a parameters/mapping sets;
- agrega metadata XML fields;
- agrega method functional metadata;
- agrega `Revision`.

### M2 — `Integrations_BackfillExistingSoapContracts`

Preferiblemente implementada como datos en la misma migración o bootstrap idempotente versionado:

- vincula los parámetros actuales a baseline contracts;
- vincula mapping sets actuales;
- conserva IDs de parámetros;
- no cambia mapping rules.

### M3 — `Integrations_EnforceContractVersionForeignKeys`

Después de pruebas de backfill:

- `ContractVersionId` NOT NULL;
- índices unique;
- FK restrict/no cascade que preserve historia.

### M4 — `Integrations_AddContractHistoryAndSourceMetadata`

Solo si P1/P3 demuestra necesidad.

## 11. Bootstrap

El bootstrap actual que enumera todos los parámetros en código debe evolucionar.

Estado objetivo:

- bootstrap crea únicamente métodos base si faltan;
- baseline contract existente se crea una vez mediante migración/compatibility bootstrap;
- después, los parámetros del contrato se administran por ContractVersion service/importador;
- bootstrap no desactiva parámetros de una versión porque no aparezcan en una lista compilada.

## 12. SoapIntegrationSetting

Mantener inicialmente:

```text
MethodName
Endpoint
SoapAction
OperatingMode
TimeoutSeconds
Enabled
InputParameterMappings
```

Durante transición, `InputParameterMappings` deja de ser la autoridad de mapping para los tres métodos objetivo.

Fase final:

- conservarlo como snapshot de compatibilidad o retirarlo mediante migración/documentación separada;
- endpoint/action/mode/timeout continúan en settings;
- parámetros provienen de ContractVersion + MappingSet.

No eliminar JSON legacy en P1.

## 13. Backfill de contratos actuales

Crear tres versiones baseline:

```text
WSCFAACH.Proc_Contrapartidas / baseline-v1
WSCFAACH.Proc_Transacciones / baseline-v1
WSAXON.RegistrarRespuestaTransaccion / baseline-v1
```

Su contenido se construye a partir del catálogo actual para preservar comportamiento.

El baseline de RegistrarRespuesta debe conservar exactamente los siete parámetros actuales.

## 14. Borrado y cascadas

No hacer hard delete normal de:

- contract versions;
- mapping sets publicados;
- parameters pertenecientes a versiones activas/retired;
- history.

Usar estados/inactividad.

FKs de historia deben evitar cascadas destructivas.

## 15. Rollback de migración

Las primeras migraciones son expand-only y su downgrade puede retirar columnas/tablas solo si no existe data nueva activa.

Una vez activado el runtime versionado, rollback de aplicación debe ser forward-compatible con las columnas nuevas. No depender de downgrades destructivos para recuperación operativa.
