# DOC_API

Documentación generada automáticamente a partir de controladores en `src/Cfa.ACHInterbank.Api/Controllers`.

- Controllers encontrados: **36**
- Acciones detectadas: **97**

## AchCyclesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs`
- Ruta base: `ach-cycles`
- Autorización de clase: `Authorize`

### GET `/ach-cycles`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `clearingHouseId`: `int?` (source: `FromQuery`)
- `startDate`: `DateTime?` (source: `FromQuery`)
- `endDate`: `DateTime?` (source: `FromQuery`)
- `processingDate`: `DateTime?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /ach-cycles HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/ach-cycles/exportable`
**Acción:** `GetExportable`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `clearingHouseId`: `int?` (source: `FromQuery`)
- `startDate`: `DateTime?` (source: `FromQuery`)
- `endDate`: `DateTime?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /ach-cycles/exportable HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/ach-cycles/{id}`
**Acción:** `GetById`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `string` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /ach-cycles/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/ach-cycles`
**Acción:** `Create`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `request`: `AchCycleRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /ach-cycles HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/ach-cycles/{id}`
**Acción:** `Update`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `string` (source: `inferred`)
- `request`: `AchCycleRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /ach-cycles/{id} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/ach-cycles/{id}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `string` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /ach-cycles/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## AuditLogsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/AuditLogsController.cs`
- Ruta base: `api/audit-logs`
- Autorización de clase: `Authorize`

### GET `/api/audit-logs`
**Acción:** `GetAuditLogsAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `startDate`: `DateTime?` (source: `FromQuery`)
- `endDate`: `DateTime?` (source: `FromQuery`)
- `changedBy`: `string?` (source: `FromQuery`)
- `action`: `string?` (source: `FromQuery`)
- `page`: `int` (source: `FromQuery`)
- `pageSize`: `int` (source: `FromQuery`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/audit-logs HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## AuthController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `NotSpecified`

### POST `/[controller]/login`
**Acción:** `Login`

**Descripción funcional:** <summary> Endpoint de la API ACH Interbank. </summary>

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `request`: `LoginRequest` (source: `FromBody`)
- `authService`: `IAuthService` (source: `FromServices`)
- `authLogsService`: `IAuthLogsService` (source: `FromServices`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/login HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/forgot-password`
**Acción:** `ForgotPassword`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `request`: `ForgotPasswordRequest` (source: `FromBody`)
- `authService`: `IAuthService` (source: `FromServices`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/forgot-password HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/reset-password`
**Acción:** `ResetPassword`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `request`: `ResetPasswordRequest` (source: `FromBody`)
- `authService`: `IAuthService` (source: `FromServices`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/reset-password HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/refresh`
**Acción:** `RefreshSession`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `authService`: `IAuthService` (source: `FromServices`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/refresh HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## AuthLogsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/AuthLogsController.cs`
- Ruta base: `api/auth-logs`
- Autorización de clase: `Authorize`

### GET `/api/auth-logs`
**Acción:** `GetAuthLogsAsync`

**Descripción funcional:** <summary> Endpoint de la API ACH Interbank. </summary>

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `startDate`: `DateTime?` (source: `FromQuery`)
- `endDate`: `DateTime?` (source: `FromQuery`)
- `username`: `string?` (source: `FromQuery`)
- `success`: `bool?` (source: `FromQuery`)
- `page`: `int` (source: `FromQuery`)
- `pageSize`: `int` (source: `FromQuery`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/auth-logs HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## BankHolidaysController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/BankHolidaysController.cs`
- Ruta base: `bank-holidays`
- Autorización de clase: `Authorize`

### GET `/bank-holidays`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `year`: `int?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /bank-holidays HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/bank-holidays/{id}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /bank-holidays/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## BrandingController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/BrandingController.cs`
- Ruta base: `api/users/branding`
- Autorización de clase: `NotSpecified`

### GET `/api/users/branding`
**Acción:** `GetBrandingAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<BrandingSettingsDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/users/branding HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/api/users/branding`
**Acción:** `SaveBrandingAsync`

**Descripción funcional:** <summary> Endpoint de la API ACH Interbank. </summary>

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `request`: `BrandingSettingsDto` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<BrandingSettingsDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /api/users/branding HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## CatalogTypesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/CatalogTypesController.cs`
- Ruta base: `catalog-types`
- Autorización de clase: `Authorize`

### GET `/catalog-types/{catalogType}`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `catalogType`: `string` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /catalog-types/{catalogType} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/catalog-types/{catalogType}`
**Acción:** `Create`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `catalogType`: `string` (source: `inferred`)
- `request`: `CatalogTypeUpsertRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /catalog-types/{catalogType} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/catalog-types/{catalogType}/{code}`
**Acción:** `Update`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `catalogType`: `string` (source: `inferred`)
- `code`: `string` (source: `inferred`)
- `request`: `CatalogTypeUpsertRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /catalog-types/{catalogType}/{code} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/catalog-types/{catalogType}/{code}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `catalogType`: `string` (source: `inferred`)
- `code`: `string` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /catalog-types/{catalogType}/{code} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## ClearingHouseSpecialDatesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseSpecialDatesController.cs`
- Ruta base: `clearing-house-special-dates`
- Autorización de clase: `Authorize`

### GET `/clearing-house-special-dates`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `year`: `int?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /clearing-house-special-dates HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/clearing-house-special-dates/{id}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /clearing-house-special-dates/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## ClearingHousesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs`
- Ruta base: `clearing-houses`
- Autorización de clase: `Authorize`

### GET `/clearing-houses`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `request`: `PaginationRequest` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /clearing-houses HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/clearing-houses/{id:int}`
**Acción:** `GetById`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /clearing-houses/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/clearing-houses/{id:int}/cycles`
**Acción:** `GetCyclesForClearingHouse`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `processingDate`: `DateTime?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /clearing-houses/{id:int}/cycles HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## CustomerThirdPartiesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/CustomerThirdPartiesController.cs`
- Ruta base: `api/customer-third-parties`
- Autorización de clase: `Authorize`

### GET `/api/customer-third-parties`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `search`: `string?` (source: `FromQuery`)
- `destinationAccountNumber`: `string?` (source: `FromQuery`)
- `recipientIdNumber`: `string?` (source: `FromQuery`)
- `destinationInstitutionId`: `int?` (source: `FromQuery`)
- `sourceAccountNumber`: `string?` (source: `FromQuery`)
- `status`: `CustomerThirdPartyStatusEnum?` (source: `FromQuery`)
- `page`: `int` (source: `FromQuery`)
- `pageSize`: `int` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/customer-third-parties HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PATCH `/api/customer-third-parties/{id:int}/status`
**Acción:** `UpdateStatus`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `request`: `UpdateCustomerThirdPartyStatusRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PATCH /api/customer-third-parties/{id:int}/status HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## CustomersController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs`
- Ruta base: `customers`
- Autorización de clase: `Authorize`

### GET `/customers`
**Acción:** `GetAll`

**Descripción funcional:** <summary> Obtiene el listado de clientes registrados. </summary>

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /customers HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/customers/{id:int}`
**Acción:** `GetById`

**Descripción funcional:** <summary> Obtiene el detalle de un cliente por identificador. </summary>

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /customers/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/customers`
**Acción:** `Create`

**Descripción funcional:** <summary> Registra un nuevo cliente. </summary>

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `request`: `SaveCustomerRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /customers HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/customers/{id:int}`
**Acción:** `Update`

**Descripción funcional:** <summary> Actualiza la información de un cliente existente. </summary>

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `request`: `SaveCustomerRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /customers/{id:int} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/customers/{id:int}`
**Acción:** `Delete`

**Descripción funcional:** <summary> Elimina un cliente por identificador. </summary>

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /customers/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## DigitalEnvelopeCertificatesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs`
- Ruta base: `nacha-security/certificates`
- Autorización de clase: `Authorize`

### GET `/nacha-security/certificates`
**Acción:** `GetAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<DigitalEnvelopeCertificateResponse>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /nacha-security/certificates HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/nacha-security/certificates`
**Acción:** `UploadAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `request`: `UploadCertificateRequest` (source: `FromForm`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<DigitalEnvelopeCertificateResponse>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /nacha-security/certificates HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/nacha-security/certificates/{id:int}`
**Acción:** `DeleteAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /nacha-security/certificates/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## FinancialInstitutionsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs`
- Ruta base: `financial-institutions`
- Autorización de clase: `Authorize`

### GET `/financial-institutions`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `includeInactive`: `bool` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /financial-institutions HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/financial-institutions`
**Acción:** `Create`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `dto`: `FinancialInstitutionDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /financial-institutions HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PATCH `/financial-institutions/{id}/status`
**Acción:** `SetStatus`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `status`: `FinancialInstitutionStatus` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PATCH /financial-institutions/{id}/status HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## InstitutionClearingHousePreferencesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/InstitutionClearingHousePreferencesController.cs`
- Ruta base: `institution-clearing-house-preferences`
- Autorización de clase: `Authorize`

### GET `/institution-clearing-house-preferences`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /institution-clearing-house-preferences HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/institution-clearing-house-preferences/{id}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /institution-clearing-house-preferences/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## JwksController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs`
- Ruta base: `/oauth2`
- Autorización de clase: `AllowAnonymous`

### GET `/oauth2/jwks`
**Acción:** `GetJwks`

**Descripción funcional:** <summary> Endpoint de la API ACH Interbank. </summary>

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `jwksService`: `IJwksServiceScoped` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /oauth2/jwks HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/oauth2/TokenClientAssertions`
**Acción:** `TokenClientAssertions`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `getToken`: `IGetTokenWithClientAssertionScoped` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /oauth2/TokenClientAssertions HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/oauth2/client-assertion`
**Acción:** `Authenticate`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `request`: `string` (source: `FromBody`)
- `getToken`: `IClientAssertionValidatorScoped` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /oauth2/client-assertion HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/oauth2/Genearte-client-assertion`
**Acción:** `GenerateClientAssertion`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `getToken`: `IGetTokenWithClientAssertionScoped` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /oauth2/Genearte-client-assertion HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## LoginLockoutSettingsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/LoginLockoutSettingsController.cs`
- Ruta base: `api/users/login-lockout`
- Autorización de clase: `Authorize`

### GET `/api/users/login-lockout`
**Acción:** `GetAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<LoginLockoutSettingsDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/users/login-lockout HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/api/users/login-lockout`
**Acción:** `SaveAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `request`: `LoginLockoutSettingsDto` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<LoginLockoutSettingsDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /api/users/login-lockout HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## MaintenanceController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/MaintenanceController.cs`
- Ruta base: `/(sin Route atributo)`
- Autorización de clase: `NotSpecified`

### POST `/seed`
**Acción:** `RunDbInitializer`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- Ninguno

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /seed HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## MenuItemsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs`
- Ruta base: `navigation/menu-items`
- Autorización de clase: `Authorize(Roles = "Admin")`

### GET `/navigation/menu-items`
**Acción:** `GetMenuItemsAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Roles = "Admin")`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<MenuItemAdminDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /navigation/menu-items HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/navigation/menu-items`
**Acción:** `CreateMenuItemAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Roles = "Admin")`

**Parámetros de entrada:**
- `request`: `SaveMenuItemRequest` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<MenuItemAdminDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /navigation/menu-items HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/navigation/menu-items/{id:int}`
**Acción:** `UpdateMenuItemAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Roles = "Admin")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `request`: `SaveMenuItemRequest` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<MenuItemAdminDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /navigation/menu-items/{id:int} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/navigation/menu-items/{id:int}`
**Acción:** `DeleteMenuItemAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Roles = "Admin")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /navigation/menu-items/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NachaController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NachaController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `AllowAnonymous`

### POST `/[controller]/header`
**Acción:** `SaveHeader`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `header`: `NachaHeader` (source: `FromBody`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/header HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NachaExportController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `NotSpecified`

### GET `/[controller]/{cycleId}`
**Acción:** `Export`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `cycleId`: `string` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/{cycleId} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/[controller]/{cycleId}/sobre-digital`
**Acción:** `ExportEncrypted`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `cycleId`: `string` (source: `inferred`)
- `forceEncryption`: `bool` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/{cycleId}/sobre-digital HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NachaRecordDefinitionsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs`
- Ruta base: `nacha-record-definitions`
- Autorización de clase: `Authorize`

### GET `/nacha-record-definitions`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<NachaRecordDefinitionDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /nacha-record-definitions HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/nacha-record-definitions/{id:int}`
**Acción:** `GetById`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<NachaRecordDefinitionDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /nacha-record-definitions/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/nacha-record-definitions`
**Acción:** `Create`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `request`: `NachaRecordDefinitionDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<NachaRecordDefinitionDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /nacha-record-definitions HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/nacha-record-definitions/{id:int}`
**Acción:** `Update`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `request`: `NachaRecordDefinitionDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /nacha-record-definitions/{id:int} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/nacha-record-definitions/{id:int}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /nacha-record-definitions/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NachaRecordLayoutsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs`
- Ruta base: `nacha-layouts`
- Autorización de clase: `Authorize`

### GET `/nacha-layouts`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<NachaRecordLayoutDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /nacha-layouts HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/nacha-layouts/{id:int}`
**Acción:** `GetById`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<NachaRecordLayoutDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /nacha-layouts/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/nacha-layouts`
**Acción:** `Create`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `request`: `NachaRecordLayoutDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<NachaRecordLayoutDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /nacha-layouts HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/nacha-layouts/{id:int}`
**Acción:** `Update`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `request`: `NachaRecordLayoutDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /nacha-layouts/{id:int} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/nacha-layouts/{id:int}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /nacha-layouts/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NachaUploadController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `NotSpecified`

### POST `/[controller]/upload`
**Acción:** `UploadNachaFile`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `request`: `NachaUploadRequest` (source: `FromForm`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/upload HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/[controller]/records`
**Acción:** `GetUploadedRecords`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `immediateOrigin`: `string?` (source: `FromQuery`)
- `immediateDestination`: `string?` (source: `FromQuery`)
- `referenceCode`: `string?` (source: `FromQuery`)
- `achCycleId`: `string?` (source: `FromQuery`)
- `fileCreationDate`: `DateTime?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IReadOnlyList<NachaUploadRecordResponse>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/records HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NavigationController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NavigationController.cs`
- Ruta base: `navigation`
- Autorización de clase: `Authorize`

### GET `/navigation/menu`
**Acción:** `GetMenuAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IList<MenuItemDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /navigation/menu HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## NavigationLogsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs`
- Ruta base: `api/navigation-logs`
- Autorización de clase: `Authorize`

### GET `/api/navigation-logs`
**Acción:** `GetNavigationLogsAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `startDate`: `DateTime?` (source: `FromQuery`)
- `endDate`: `DateTime?` (source: `FromQuery`)
- `userId`: `string?` (source: `FromQuery`)
- `route`: `string?` (source: `FromQuery`)
- `page`: `int` (source: `FromQuery`)
- `pageSize`: `int` (source: `FromQuery`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/navigation-logs HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/api/navigation-logs`
**Acción:** `AddNavigationLogAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `request`: `NavigationLogCreate` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /api/navigation-logs HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## OauthsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/OauthsController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `AllowAnonymous`

### POST `/[controller]/GenerateToken`
**Acción:** `GenerateToken`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `model`: `TokenModelClient` (source: `FromBody`)
- `generateToken`: `IGenerateToken` (source: `FromServices`)
- `validator`: `IValidator<TokenModelClient>` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/GenerateToken HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/GenerateTokenAsync`
**Acción:** `GenerateTokenAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `Assertion`: `string` (source: `FromBody`)
- `generateToken`: `IGenerateToken` (source: `FromServices`)
- `validator`: `IValidator<TokenModelClient>` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/GenerateTokenAsync HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## PasswordRulesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/PasswordRulesController.cs`
- Ruta base: `api/users/password-rules`
- Autorización de clase: `Authorize`

### GET `/api/users/password-rules`
**Acción:** `GetRulesAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<PasswordRulesDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/users/password-rules HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/api/users/password-rules`
**Acción:** `SaveRulesAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `request`: `PasswordRulesDto` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<PasswordRulesDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /api/users/password-rules HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## PermissionsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/PermissionsController.cs`
- Ruta base: `api/[controller]`
- Autorización de clase: `Authorize`

### GET `/api/[controller]`
**Acción:** `GetPermissionsAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<PermissionSummaryDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## ReturnReasonsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/ReturnReasonsController.cs`
- Ruta base: `return-reasons`
- Autorización de clase: `Authorize`

### GET `/return-reasons`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /return-reasons HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## RolesController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/RolesController.cs`
- Ruta base: `api/[controller]`
- Autorización de clase: `Authorize`

### GET `/api/[controller]`
**Acción:** `GetRolesAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<RoleSummaryDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## ServersController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/ServersController.cs`
- Ruta base: `/[controller]`
- Autorización de clase: `AllowAnonymous`

### GET `/[controller]`
**Acción:** `HandleRequest`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- `loadBalancer`: `ILoadBalancerSingleton` (source: `FromServices`)
- `httpClient`: `HttpClient` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## SobreDigitalController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `NotSpecified`

### POST `/[controller]/encrypt`
**Acción:** `Encrypt`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `file`: `IFormFile` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/encrypt HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/decrypt`
**Acción:** `Decrypt`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `file`: `IFormFile` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<DecryptResponse>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/decrypt HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]/testRSA`
**Acción:** `testRSA`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `_rsaKeyService`: `IRsaKeyProvider` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `void`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller]/testRSA HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## TaskDefinitionsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `Authorize`

### GET `/[controller]`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<IEnumerable<TaskDefinitionDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/[controller]/{id}`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanReadAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<TaskDefinitionDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]`
**Acción:** `Post`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `task`: `TaskDefinitionDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<TaskDefinitionDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller] HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/[controller]/{id}`
**Acción:** `Put`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `task`: `TaskDefinitionDto` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /[controller]/{id} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/[controller]/{id}`
**Acción:** `Delete`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize(Policy = "CanManageAch")`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /[controller]/{id} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## TestsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/TestsController.cs`
- Ruta base: `/[controller]`
- Autorización de clase: `Authorize`

### GET `/[controller]`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `data`: `string` (source: `inferred`)
- `test`: `ITestTransient` (source: `FromServices`)
- `httpContextAccessor`: `IHttpContextAccessor` (source: `FromServices`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/[controller]/Prueba`
**Acción:** `Get`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `AllowAnonymous`

**Parámetros de entrada:**
- Ninguno

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/Prueba HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## TransactionsController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs`
- Ruta base: `[controller]`
- Autorización de clase: `NotSpecified`

### GET `/[controller]`
**Acción:** `GetAll`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `achCycleId`: `string?` (source: `FromQuery`)
- `achCycleName`: `string?` (source: `FromQuery`)
- `effectiveDate`: `DateTime?` (source: `FromQuery`)
- `clearingHouseId`: `int?` (source: `FromQuery`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/[controller]`
**Acción:** `CreateTransaction`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `request`: `AchTransactionRequest` (source: `FromBody`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /[controller] HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/[controller]/{id:int}`
**Acción:** `GetById`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `NotSpecified`

**Parámetros de entrada:**
- `id`: `int` (source: `inferred`)
- `ct`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /[controller]/{id:int} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## UsersController
- Archivo: `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs`
- Ruta base: `api/[controller]`
- Autorización de clase: `Authorize`

### GET `/api/[controller]`
**Acción:** `GetUsersAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `search`: `string?` (source: `FromQuery`)
- `roleId`: `Guid?` (source: `FromQuery`)
- `page`: `int` (source: `FromQuery`)
- `pageSize`: `int` (source: `FromQuery`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<PagedResponse<UserSummaryDto>>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/[controller] HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/api/[controller]/validate-email-domain`
**Acción:** `ValidateEmailDomainAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `email`: `string` (source: `FromQuery`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<bool>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/[controller]/validate-email-domain HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### GET `/api/[controller]/{id:guid}`
**Acción:** `GetUserAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `id`: `Guid` (source: `inferred`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<UserSummaryDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
GET /api/[controller]/{id:guid} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/api/[controller]`
**Acción:** `CreateUserAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `request`: `SaveUserRequest` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<UserSummaryDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /api/[controller] HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### PUT `/api/[controller]/{id:guid}`
**Acción:** `UpdateUserAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `id`: `Guid` (source: `inferred`)
- `request`: `SaveUserRequest` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `Task<ActionResult<UserSummaryDto>>`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
PUT /api/[controller]/{id:guid} HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### POST `/api/[controller]/{id:guid}/roles`
**Acción:** `AssignRolesAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `id`: `Guid` (source: `inferred`)
- `request`: `AssignRolesRequest` (source: `FromBody`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
POST /api/[controller]/{id:guid}/roles HTTP/1.1
Host: localhost:7269
Content-Type: application/json

{ "example": true }
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

### DELETE `/api/[controller]/{id:guid}`
**Acción:** `DeactivateUserAsync`

**Descripción funcional:** Endpoint de la API ACH Interbank.

**Autorización:** `Authorize`

**Parámetros de entrada:**
- `id`: `Guid` (source: `inferred`)
- `cancellationToken`: `CancellationToken` (source: `inferred`)

**Esquema de respuesta:**
- Tipo de retorno declarado: `IActionResult`
- HTTP 200: objeto JSON (estructura depende del servicio interno).
- HTTP 400/401/500: error estándar de API.

**Ejemplo de solicitud:**
```http
DELETE /api/[controller]/{id:guid} HTTP/1.1
Host: localhost:7269
```

**Ejemplo de respuesta:**
```json
{
  "statusCode": 200,
  "success": true,
  "message": "OK",
  "data": {}
}
```

## Campos/documentación faltante o ambigua

- Endpoints sin summary XML explícita:
  - AchCyclesController.Get
  - AchCyclesController.GetExportable
  - AchCyclesController.GetById
  - AchCyclesController.Create
  - AchCyclesController.Update
  - AchCyclesController.Delete
  - AuditLogsController.GetAuditLogsAsync
  - AuthController.ForgotPassword
  - AuthController.ResetPassword
  - AuthController.RefreshSession
  - BankHolidaysController.GetAll
  - BankHolidaysController.Delete
  - BrandingController.GetBrandingAsync
  - CatalogTypesController.GetAll
  - CatalogTypesController.Create
  - CatalogTypesController.Update
  - CatalogTypesController.Delete
  - ClearingHouseSpecialDatesController.GetAll
  - ClearingHouseSpecialDatesController.Delete
  - ClearingHousesController.Get
  - ClearingHousesController.GetById
  - ClearingHousesController.GetCyclesForClearingHouse
  - CustomerThirdPartiesController.Get
  - CustomerThirdPartiesController.UpdateStatus
  - DigitalEnvelopeCertificatesController.GetAsync
  - DigitalEnvelopeCertificatesController.UploadAsync
  - DigitalEnvelopeCertificatesController.DeleteAsync
  - FinancialInstitutionsController.GetAll
  - FinancialInstitutionsController.Create
  - FinancialInstitutionsController.SetStatus
  - InstitutionClearingHousePreferencesController.GetAll
  - InstitutionClearingHousePreferencesController.Delete
  - JwksController.TokenClientAssertions
  - JwksController.Authenticate
  - JwksController.GenerateClientAssertion
  - LoginLockoutSettingsController.GetAsync
  - LoginLockoutSettingsController.SaveAsync
  - MaintenanceController.RunDbInitializer
  - MenuItemsController.GetMenuItemsAsync
  - MenuItemsController.CreateMenuItemAsync
  - MenuItemsController.UpdateMenuItemAsync
  - MenuItemsController.DeleteMenuItemAsync
  - NachaController.SaveHeader
  - NachaExportController.Export
  - NachaExportController.ExportEncrypted
  - NachaRecordDefinitionsController.GetAll
  - NachaRecordDefinitionsController.GetById
  - NachaRecordDefinitionsController.Create
  - NachaRecordDefinitionsController.Update
  - NachaRecordDefinitionsController.Delete
  - NachaRecordLayoutsController.GetAll
  - NachaRecordLayoutsController.GetById
  - NachaRecordLayoutsController.Create
  - NachaRecordLayoutsController.Update
  - NachaRecordLayoutsController.Delete
  - NachaUploadController.UploadNachaFile
  - NachaUploadController.GetUploadedRecords
  - NavigationController.GetMenuAsync
  - NavigationLogsController.GetNavigationLogsAsync
  - NavigationLogsController.AddNavigationLogAsync
  - OauthsController.GenerateToken
  - OauthsController.GenerateTokenAsync
  - PasswordRulesController.GetRulesAsync
  - PasswordRulesController.SaveRulesAsync
  - PermissionsController.GetPermissionsAsync
  - ReturnReasonsController.GetAll
  - RolesController.GetRolesAsync
  - ServersController.HandleRequest
  - SobreDigitalController.Encrypt
  - SobreDigitalController.Decrypt
  - SobreDigitalController.testRSA
  - TaskDefinitionsController.Get
  - TaskDefinitionsController.Get
  - TaskDefinitionsController.Post
  - TaskDefinitionsController.Put
  - TaskDefinitionsController.Delete
  - TestsController.Get
  - TestsController.Get
  - TransactionsController.GetAll
  - TransactionsController.CreateTransaction
  - TransactionsController.GetById
  - UsersController.GetUsersAsync
  - UsersController.ValidateEmailDomainAsync
  - UsersController.GetUserAsync
  - UsersController.CreateUserAsync
  - UsersController.UpdateUserAsync
  - UsersController.AssignRolesAsync
  - UsersController.DeactivateUserAsync
- Muchos métodos retornan `IActionResult`, por lo que el esquema exacto depende del flujo interno y no siempre es inferible estáticamente.
- Para precisión contractual completa, complementar con `[ProducesResponseType]` y modelos de respuesta tipados por acción.
