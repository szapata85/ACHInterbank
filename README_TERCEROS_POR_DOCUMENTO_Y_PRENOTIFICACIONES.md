# ACHInterbank — Consulta unificada de terceros por documento del cliente

## Endpoint (nuevo, solo lectura)

```http
GET /api/third-party-accounts/by-customer-document?documentType=CC&documentNumber=1234567890&page=1&pageSize=20
Authorization: Bearer <TOKEN>
```

Autenticación obligatoria y permiso `CanReadAch`. Compatible con IIS y Docker: es parte de la misma API ASP.NET Core, sin nueva infraestructura, migración ni contenedor.

Resuelve el **cliente** por la combinación exacta de tipo y número de documento (normalizando tipo a mayúsculas y quitando espacios exteriores) y, con el `customerId` resuelto en servidor, obtiene únicamente sus **cuentas canónicas de terceros**. No consulta bancos externos ni intenta validar cuentas en tiempo real. No acepta filtros de elegibilidad suministrados por el consumidor.

## Respuesta

```json
{
  "customer": {
    "id": 123,
    "documentType": "CC",
    "documentNumber": "1234567890",
    "fullName": "Cliente Ejemplo"
  },
  "accounts": {
    "items": [
      {
        "thirdPartyAccountId": 201,
        "thirdPartyId": 200,
        "customerId": 123,
        "thirdPartyDisplayName": "Tercero Ejemplo",
        "documentType": "CC",
        "documentNumberMasked": "****9876",
        "financialInstitutionId": 1,
        "financialInstitutionName": "Banco Ejemplo",
        "accountNumberMasked": "****1234",
        "validations": [
          {
            "clearingHouseId": 1,
            "clearingHouseCode": "ACHCOL",
            "clearingHouseName": "ACH Colombia",
            "transactionNature": 2,
            "serviceType": "PPD",
            "validationType": 1,
            "status": 4,
            "isVerified": true,
            "latestAttempt": {
              "attemptId": 400,
              "attemptNumber": 1,
              "status": 5,
              "resolution": 1,
              "returnCode": null
            }
          },
          {
            "clearingHouseId": 2,
            "clearingHouseCode": "CENIT",
            "clearingHouseName": "CENIT",
            "transactionNature": 2,
            "serviceType": "PPD",
            "validationType": 1,
            "status": 6,
            "isVerified": false,
            "latestAttempt": {
              "attemptId": 401,
              "attemptNumber": 1,
              "status": 7,
              "resolution": 3,
              "returnCode": "R03"
            }
          }
        ]
      }
    ],
    "total": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

**Ejemplo ilustrativo:** el contrato contiene además campos de terceros/cuentas, fechas, política y trazabilidad definidos en `ThirdPartyAccountReadDto`; los enums se serializan como números bajo la configuración actual del API.

`validations` incluye una entrada por contexto disponible de **cámara + naturaleza de transacción + tipo de servicio**. Si la cuenta no tiene prenotificaciones registradas, `validations: []`: **no significa validación aprobada ni rechazada**, solo que no hay registro en ese lector. No se inventa estado de otra cámara.

`isVerified` indica aceptación registrada vigente (estados `Accepted=4` o `TacitlyAccepted=5` y fecha de vencimiento no superada). Los estados pendientes, rechazados, vencidos y suspendidos se muestran como `false`. **No representa autorización general para transferir**: la elegibilidad monetaria depende de la política vigente, cámara, naturaleza, fecha y tipo de servicio, evaluada por otro servicio de backend.

### Respuestas HTTP

- `200 OK`: cliente existe; devuelve página de cuentas, aunque esté vacía (`items: []`, `total: 0`).
- `400 Bad Request`: falta tipo/número de documento, `page < 1` o `pageSize` fuera de `1..100`.
- `401 Unauthorized`: sin autenticación.
- `403 Forbidden`: falta `CanReadAch`.
- `404 Not Found`: cliente no registrado con esa combinación de documento.

`page` empieza en 1, `pageSize` por defecto 20 (máximo 100); `total` permite recorrer todas las páginas sin cargar indiscriminadamente cuentas en memoria.

Las identificaciones de terceros y las cuentas bancarias se entregan **enmascaradas** mediante el lector canónico; no se incluye el número completo de cuenta. No cambia ningún endpoint anterior, ni Quartz, ni la lógica de transmisión o prenotificación. No requiere migración.

## Pruebas incluidas

- `CustomerThirdPartyAccountsByDocumentTests`: políticas de autorización, formato de ruta, validación de entrada, 404, aislamiento por `customerId`, estados por cámara, paginación y vacío.
- `ThirdPartyAccountReadShadowTests`: aceptación ACH Colombia y rechazo CENIT en el mismo registro, con `isVerified` derivado.
- `OpenApiDocumentGenerationTests`: nueva ruta incorporada al inventario contractual OpenAPI.

Desde `ACHInterbank/` en Windows con SDK .NET 10:

```cmd
dotnet build ACHInterbank.sln -c Release
dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~CustomerThirdPartyAccountsByDocumentTests|FullyQualifiedName~ThirdPartyAccountReadShadowTests|FullyQualifiedName~OpenApiDocumentGenerationTests"
```

**Límite de verificación de la entrega:** se revisó la estructura y las dependencias de código, pero no fue posible compilar ni ejecutar las pruebas en este entorno sin SDK .NET. Se requiere validación del endpoint contra SQL Server/entorno de homologación y control de acceso antes de producción.
