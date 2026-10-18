# Contratos API — Centro de integración CFA

Los nombres son propuestos; deben ajustarse al estilo de controllers/rutas del repositorio sin cambiar la semántica.

## 1. Workspace

### GET `/api/integrations/workspace`

Retorna los tres servicios con estado agregado.

```json
{
  "services": [
    {
      "methodId": 1,
      "methodCode": "WSCFAACH.Proc_Transacciones",
      "functionalTitle": "Créditos recibidos en CFA",
      "functionalDescription": "Información enviada a CFA cuando recibe una transferencia crédito.",
      "technicalMethodName": "Proc_Transacciones",
      "movesMoney": true,
      "activeContractVersion": "baseline-v1",
      "mappingStatus": "Published",
      "mappingCoverage": { "required": 20, "configured": 20, "pending": 0 },
      "connectionStatus": "Configured",
      "operatingMode": "Live",
      "permissions": {
        "canEditMapping": true,
        "canPublish": true,
        "canManageContract": false,
        "canManageConnection": false
      }
    }
  ]
}
```

## 2. Detalle de servicio

### GET `/api/integrations/methods/{methodId}/workspace`

Debe entregar:

- método funcional;
- contrato activo;
- contrato draft más reciente;
- mapping publicado;
- drafts;
- cobertura;
- conexión sanitizada;
- acciones permitidas.

No retornar secretos ni payloads SOAP completos.

## 3. Versiones de contrato

### GET `/api/integrations/methods/{methodId}/contracts`

Lista versiones.

### GET `/api/integrations/contracts/{contractVersionId}`

Detalle con parámetros y metadata.

### POST `/api/integrations/methods/{methodId}/contracts/clone`

```json
{
  "sourceContractVersionId": "...",
  "versionLabel": "cfa-2026-11",
  "notes": "Cambio informado por CFA",
  "actor": "..."
}
```

### POST `/api/integrations/methods/{methodId}/contracts/import-wsdl`

`multipart/form-data`:

- `file`;
- `versionLabel`;
- `operationName` opcional si el WSDL tiene múltiples operaciones;
- `actor`.

Respuesta inicial de análisis antes de persistir puede ser un endpoint separado:

### POST `/api/integrations/methods/{methodId}/contracts/analyze-wsdl`

Retorna diff + unsupported features sin guardar.

## 4. Edición de metadata funcional

### PUT `/api/integrations/contracts/{contractVersionId}/parameters/{parameterId}/presentation`

```json
{
  "displayName": "Tipo de cliente",
  "descriptionEs": "Indica si el titular es persona natural o jurídica.",
  "uiHelpText": "Seleccione el dato equivalente disponible en ACHInterbank.",
  "category": "Beneficiario",
  "exampleValue": "J",
  "expectedRevision": 4,
  "actor": "..."
}
```

No permite cambiar technical name/tipo de una versión Active.

## 5. Alta manual de parámetro en draft

### POST `/api/integrations/contracts/{contractVersionId}/parameters`

Solo rol técnico autorizado y contrato Draft.

```json
{
  "parameterPath": "TIPOCLIENTE",
  "xmlElementName": "TIPOCLIENTE",
  "dataType": "string",
  "xsdType": "xsd:string",
  "required": true,
  "nillable": false,
  "minOccurs": 1,
  "maxOccurs": 1,
  "sortOrder": 210,
  "direction": "Input",
  "displayName": "Tipo de cliente",
  "descriptionEs": "...",
  "sourceReference": "Especificación CFA ...",
  "expectedRevision": 4,
  "actor": "..."
}
```

## 6. Diff

### GET `/api/integrations/contracts/{leftId}/compare/{rightId}`

Respuesta:

```json
{
  "summary": {
    "added": 1,
    "removed": 0,
    "modified": 0,
    "requiresMapping": 1,
    "requiresDevelopment": 0
  },
  "changes": [
    {
      "changeType": "Added",
      "technicalName": "TIPOCLIENTE",
      "functionalName": "Tipo de cliente",
      "impact": "MappingRequired"
    }
  ]
}
```

## 7. Mapping sets por contrato

Extender endpoints existentes.

### GET `/api/integrations/contracts/{contractVersionId}/mappingsets`

### POST `/api/integrations/contracts/{contractVersionId}/mappingsets/clone-from-active`

Crea draft copiando reglas por `ParameterPath` compatible.

Respuesta incluye:

```text
copiedRules
newRequiredParameters
removedParameters
incompatibleParameters
```

## 8. Workspace de mapping

### GET `/api/integrations/mappingsets/{mappingSetId}/workspace`

Read model directo para UI:

```json
{
  "mappingSet": { "id": "...", "name": "...", "status": "Draft", "revision": 8 },
  "contract": { "id": "...", "versionLabel": "cfa-2026-11" },
  "coverage": { "total": 26, "configured": 25, "pendingRequired": 1 },
  "parameters": [
    {
      "parameterId": 123,
      "functionalName": "Tipo de cliente",
      "functionalDescription": "...",
      "technicalName": "TIPOCLIENTE",
      "required": true,
      "dataType": "string",
      "category": "Beneficiario",
      "status": "Pending",
      "source": null,
      "transformation": null,
      "technicalDetails": {
        "xmlElementName": "TIPOCLIENTE",
        "xsdType": "xsd:string",
        "sortOrder": 210
      }
    }
  ],
  "sourceGroups": [],
  "transformations": []
}
```

## 9. Actualizar una regla

Preferir comando por parámetro para reducir complejidad del SPA.

### PUT `/api/integrations/mappingsets/{mappingSetId}/parameters/{parameterId}`

```json
{
  "sourceCatalogFieldId": 88,
  "fixedValue": null,
  "defaultValue": null,
  "transformationCode": "Uppercase",
  "formatMask": null,
  "enabled": true,
  "expectedRevision": 8,
  "actor": "..."
}
```

Backend deriva `SourceKind`, `SourceFieldPath` y `MethodId` desde el catálogo. No confiar en esos valores enviados por la UI cuando pueden inferirse.

## 10. Validar y previsualizar

### POST `/api/integrations/mappingsets/{id}/validate`

Retorna issues con `code`, mensaje funcional, parameterId y technical detail.

### POST `/api/integrations/mappingsets/{id}/preview`

Entrada similar a la actual, con sanitización.

Respuesta para UI:

```json
{
  "items": [
    {
      "functionalName": "Tipo de cliente",
      "sourceDisplayName": "Beneficiario > Tipo de persona",
      "sourceValuePreview": "Jurídica",
      "mappedValuePreview": "J",
      "status": "Valid"
    }
  ]
}
```

La sección técnica puede incluir paths/codes separados.

## 11. Publicación de mapping

### POST `/api/integrations/mappingsets/{id}/publish`

Agregar `expectedRevision`.

Falla con 409 si cambió concurrentemente.

## 12. Activación de contrato

### POST `/api/integrations/contracts/{contractVersionId}/activate`

```json
{
  "mappingSetId": "...",
  "expectedContractRevision": 7,
  "expectedMappingRevision": 10,
  "activationNote": "Versión CFA noviembre 2026",
  "actor": "..."
}
```

Debe ser atómico.

## 13. Rollback

### POST `/api/integrations/contracts/{contractVersionId}/rollback`

Solo versión compatible previa, mapping publicado disponible y validación exitosa.

## 14. Connection settings

Mantener endpoint existente si es posible, pero añadir read model funcional:

### GET `/api/integrations/methods/{methodId}/connection`

```json
{
  "status": "Configured",
  "enabled": true,
  "operatingMode": "Live",
  "endpointDisplay": "https://cfa.example/...",
  "endpoint": "https://...",
  "soapAction": "...",
  "timeoutSeconds": 30,
  "mappingReady": true,
  "contractReady": true
}
```

### PUT `/api/integrations/methods/{methodId}/connection`

Solo permiso técnico.

No mezclar input mappings legacy en el formulario funcional.

## 15. Códigos HTTP

- `200/201` éxito;
- `400` contrato o comando inválido;
- `401` no autenticado;
- `403` sin permiso;
- `404` entidad no encontrada;
- `409` concurrencia/transición de estado/incompatibilidad;
- `422` contrato analizable pero no activable por feature no soportada, si el proyecto usa esta semántica; de lo contrario `400` con código estable.

## 16. Errores humanizables

Códigos mínimos:

```text
CONTRACT_WSDL_INVALID
CONTRACT_OPERATION_NOT_FOUND
CONTRACT_UNSUPPORTED_STRUCTURE
CONTRACT_CONCURRENCY_CONFLICT
CONTRACT_NOT_ACTIVATABLE
MAPPING_REQUIRED_PARAMETER_MISSING
MAPPING_SOURCE_INCOMPATIBLE
MAPPING_TRANSFORMATION_INVALID
MAPPING_CONCURRENCY_CONFLICT
MAPPING_NOT_PUBLISHABLE
ACTIVATION_MAPPING_NOT_READY
ACTIVATION_CONTRACT_NOT_READY
ACTIVATION_CONFLICT
```

El SPA no debe mostrar excepciones C# al usuario final.
