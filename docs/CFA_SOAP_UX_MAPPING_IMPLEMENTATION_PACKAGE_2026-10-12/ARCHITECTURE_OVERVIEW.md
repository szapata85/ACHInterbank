# Arquitectura objetivo — CFA SOAP UX + Mapping dinámico

## 1. Capas que se conservan

El programa mantiene Clean Architecture:

```text
Angular SPA
   ↓ HTTP
API
   ↓
Application
   ↓ interfaces
Persistence / External
   ↓
DB + servicios SOAP CFA
```

La autoridad de negocio permanece en backend. La SPA administra configuración y presenta estado; no decide cómo se construye una transacción monetaria ni si una operación es elegible.

## 2. Arquitectura objetivo de extremo a extremo

```text
┌───────────────────────────────────────────────────────────────┐
│ SPA Angular Material                                         │
│ Centro de integración CFA                                    │
│  - Servicios                                                 │
│  - Versiones de contrato                                     │
│  - Relación de información                                   │
│  - Vista previa / validación                                 │
│  - Publicación / activación                                  │
│  - Conexión técnica avanzada                                 │
└───────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────┐
│ API / Application                                            │
│ IntegrationWorkspaceQuery                                    │
│ ContractVersionService                                       │
│ MappingSetService                                            │
│ MappingValidationService                                     │
│ MappingPreviewService                                        │
│ IntegrationRequestResolver                                   │
│ ContractActivationService                                    │
└───────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴──────────────────┐
              ▼                                  ▼
┌───────────────────────────────┐   ┌───────────────────────────┐
│ Persistence                   │   │ External                  │
│ Contract versions             │   │ Contract-driven SOAP     │
│ Parameters                    │   │ body writer               │
│ Mapping sets/rules            │   │ WSCFAACH transport        │
│ Source catalog                │   │ WSAXON transport          │
│ History/audit                 │   │ Existing response parsers │
└───────────────────────────────┘   └───────────────────────────┘
              │                                  │
              ▼                                  ▼
           Database                          CFA SOAP
```

## 3. Un único modelo para los tres servicios

Los tres métodos deben compartir los mismos conceptos:

```text
IntegrationMethod
   1 ─── N IntegrationContractVersion
             1 ─── N IntegrationMethodParameter
             1 ─── N IntegrationMappingSet
                         1 ─── N IntegrationMappingRule
```

`IntegrationMethod` identifica el servicio funcional.

`IntegrationContractVersion` define qué parámetros CFA existen en una versión concreta.

`IntegrationMethodParameter` deja de ser un catálogo global sembrado en código y pasa a pertenecer a una versión de contrato.

`IntegrationMappingSet` queda asociado a la versión exacta de contrato que resuelve.

`IntegrationMappingRule` conserva el vínculo con el parámetro y el origen del valor.

## 4. Flujo runtime objetivo

```text
Transacción / respuesta elegible
        ↓
Determinar IntegrationMethod
        ↓
Obtener ContractVersion ACTIVE
        ↓
Obtener MappingSet PUBLISHED de esa versión
        ↓
IntegrationRequestResolver
        ↓
Resolver parámetros INPUT ordenados
        ↓
Validar required/type/length/format
        ↓
ResolvedSoapRequest
        ↓
ContractDrivenSoapBodyWriter
        ↓
WSCFAACH / WSAXON transport
        ↓
Response parser específico existente
        ↓
Outcome / auditoría / idempotencia existentes
```

La elegibilidad funcional permanece fuera del mapping. El mapping convierte datos válidos de una operación ya clasificada en el contrato CFA correspondiente.

## 5. Flujo de administración de un campo nuevo

```text
Nueva especificación CFA
        ↓
Crear versión de contrato borrador
        ↓
Importar WSDL o registrar cambio aprobado
        ↓
Diff vs contrato activo
        ↓
Campo nuevo aparece como "Pendiente de relacionar"
        ↓
Clonar mapping anterior
        ↓
Mapear nuevo campo
        ↓
Validar
        ↓
Vista previa
        ↓
Publicar mapping / activar contrato
        ↓
Runtime usa nueva versión
```

## 6. Separación entre contrato y mapping

### Contrato

Responde:

- ¿Qué campo espera CFA?
- ¿Cómo se llama técnicamente?
- ¿Cuál es su tipo?
- ¿Es obligatorio?
- ¿En qué orden se serializa?
- ¿Pertenece a entrada o salida?
- ¿Es escalar, objeto o colección?

### Mapping

Responde:

- ¿De dónde toma ACHInterbank el valor?
- ¿Qué transformación aprobada se aplica?
- ¿Hay valor fijo/default permitido?
- ¿Está completa la relación?

### Conexión

Responde:

- ¿A qué endpoint se transmite?
- ¿Cuál es la SOAP Action?
- ¿Está habilitado?
- ¿Cuál es el timeout?
- ¿Cuál es el modo operativo?

Estas tres responsabilidades no se mezclan en una sola pantalla ni en una sola entidad.

## 7. Compatibilidad incremental

La migración debe usar branch-by-abstraction:

```text
Fase inicial
Legacy typed mapper ───────────────► transporte

Fase de transición
Legacy typed mapper ─┐
                     ├─ feature flag / adapter ─► transporte
New contract engine ─┘

Fase final
New contract engine ───────────────► transporte
```

Los métodos públicos existentes pueden mantenerse como adapters mientras las llamadas internas se migran. No se elimina el camino antiguo hasta demostrar paridad.

## 8. Autoridades

| Responsabilidad | Autoridad |
|---|---|
| Elegibilidad monetaria | Application/domain existente |
| Contrato técnico CFA | ContractVersion activo |
| Origen del dato | MappingSet publicado |
| Resolución de campos | Source providers backend |
| Transformación | Transformation registry backend |
| Orden XML | ContractVersion/Parameter.SortOrder |
| Construcción SOAP | Contract-driven writer |
| Endpoint/action/mode | SoapIntegrationSettings |
| Estado visual | Read model del servidor |
| Branding | BrandingService/tokens runtime |

## 9. Propiedades de calidad

### Extensibilidad

Agregar un parámetro escalar de CFA no exige modificar un DTO request tipado ni un `XElement` por campo.

### Seguridad

Solo se serializan parámetros registrados en contrato. No se permite XML libre ni reflection arbitraria.

### Auditabilidad

Cada ejecución conserva:

- MethodId;
- ContractVersionId;
- MappingSetId;
- MappingVersion;
- snapshot/hash;
- reglas aplicadas;
- correlation id.

### Reversibilidad

Contrato y mapping anteriores permanecen inmutables. El rollback selecciona una versión compatible ya publicada; no edita historia.

### Mantenibilidad

Las features de Angular y los componentes backend se dividen por responsabilidad y dependen de interfaces estables.
