# Contrato API objetivo — TPA-GOV-001

## 1. Principio

Separar endpoints de **master data genérico** de endpoints de **validación/elegibilidad contextual**.

Nunca pedir cámara para crear un tercero o una cuenta. Nunca ofrecer endpoint manual para establecer `Validated/Rejected` interbancario.

## 2. Master data

Rutas conceptuales; nombres finales deben respetar convenciones del API.

- `GET /api/customers/{customerId}/third-parties`
- `POST /api/customers/{customerId}/third-parties`
- `GET /api/customers/{customerId}/third-parties/{thirdPartyId}`
- `PATCH /api/customers/{customerId}/third-parties/{thirdPartyId}`
- `POST /api/customers/{customerId}/third-parties/{thirdPartyId}/accounts`
- `PATCH /api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{accountId}`
- `POST .../accounts/{accountId}/deactivate`
- `POST .../accounts/{accountId}/reactivate`

Create/update de cuenta incluye entidad, tipo y número. **No incluye ClearingHouseId ni nature.**

## 3. Validación operacional

- `GET .../accounts/{accountId}/validations`
- `GET .../accounts/{accountId}/validations/{validationId}`
- `POST .../accounts/{accountId}/validations/prenotifications`
- `POST .../accounts/{accountId}/validations/{validationId}/retry`

Request de prenotificación sí incluye el contexto mínimo necesario:

- `clearingHouseId`;
- `transactionNature`;
- `serviceType` cuando aplique;
- effective date/contexto requerido por la política.

El backend resuelve la regla efectiva; el cliente no envía `prenotificationRequired=true` como autoridad.

## 4. Elegibilidad

Para selector/validación transaccional, preferir caso de uso interno. Si se requiere endpoint:

`POST /api/third-party-accounts/{accountId}/eligibility/evaluate`

Entrada: cámara, naturaleza, servicio, fecha efectiva y sólo datos transaccionales necesarios.  
Salida: `EligibilityDecision` + reason code estable y datos de validación resumidos.

No persistir la decisión como autorización universal.

## 5. Compatibilidad legacy

`GET /api/customer-third-parties` permanece durante branch-by-abstraction. En TPA4/TPA8 puede adaptarse al read model canónico sin romper su contrato hasta decisión explícita.

## 6. Semántica de cambios materiales

Modificar institución, tipo/número de cuenta o identidad relevante:

- actualiza master data conforme a reglas de concurrencia;
- no borra historial;
- invalida/suspende los contextos de validación afectados según caso de uso;
- una nueva prenotificación crea un intento nuevo.

## 7. Permisos sugeridos

Alinear nombres finales con IAM existente:

- `ThirdParties.Read`
- `ThirdParties.Manage`
- `ThirdParties.Validation.Request`
- `ThirdParties.SensitiveData.Read`
- opcional `ThirdParties.Export`

No heredar privilegio total sólo desde un permiso amplio si el modelo actual soporta permisos finos.

## 8. Errores funcionales

- `THIRD_PARTY_NOT_FOUND`
- `THIRD_PARTY_ACCOUNT_DUPLICATE`
- `THIRD_PARTY_ACCOUNT_CONFLICT`
- `ACCOUNT_INACTIVE`
- `ACCOUNT_NOT_ELIGIBLE`
- `PRENOTIFICATION_REQUIRED`
- `PRENOTIFICATION_NOT_REQUIRED`
- `PRENOTIFICATION_ALREADY_PENDING`
- `PRENOTIFICATION_POLICY_NOT_CONFIGURED`
- `ACCOUNT_REVALIDATION_REQUIRED`

Rxx/Dxx se adjuntan como datos del resultado; no sustituyen códigos internos HTTP/dominio.

## 9. OpenAPI y PII

- summaries/descriptions en español;
- policy explícita;
- ejemplos sintéticos;
- lista/detalle enmascaran cuenta/documento por defecto;
- full data sólo bajo permiso explícito;
- actualizar tests OpenAPI por adición real, sin debilitarlos.
