# Incremento: consulta de cliente por tipo y número de documento

Este es el **proyecto ACHInterbank completo** con el incremento integrado sobre
la versión original `ACHInterbank.zip`. Se conservaron todos los demás archivos.

## Endpoint nuevo

```http
GET /customers/by-document?documentType=CC&documentNumber=1234567890
Authorization: Bearer <TOKEN_CON_PERMISO_CanReadAch>
```

- **200 OK:** cliente encontrado. Ejemplo: `{"id":123,"documentType":"CC","documentNumber":"1234567890","fullName":"Ana Pérez"}`.
- **400 Bad Request:** falta `documentType` o `documentNumber`.
- **401 Unauthorized:** no se ha autenticado la solicitud.
- **403 Forbidden:** el usuario carece del permiso `CanReadAch`.
- **404 Not Found:** no se encontró un cliente con ambos datos.

Los datos del ejemplo son ficticios. La respuesta excluye números de cuentas bancarias.
El campo `id` corresponde al `customerId` para posteriores consultas, por ejemplo:

```http
GET /api/third-party-accounts?customerId=123
```

## Archivos del incremento

- `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs`
- `src/Cfa.ACHInterbank.Application/Customers/Dtos/CustomerDtos.cs`
- `src/Cfa.ACHInterbank.Application/Customers/Interfaces/ICustomersService.cs`
- `src/Cfa.ACHInterbank.Persistence/Customers/Services/CustomersService.cs`
- `tests/Cfa.ACHInterbank.Tests/CustomersByDocumentTests.cs` (nuevo)
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`

## Abrir, compilar y probar

Descomprime el ZIP y abre `ACHInterbank/ACHInterbank.sln`.
El repositorio solicita el SDK **.NET 10.0.403** en `global.json`.

Desde la carpeta `ACHInterbank`:

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~CustomersByDocumentTests
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~OpenApiDocumentGenerationTests
```

**Estado de validación:** el parche se aplicó sin conflictos sobre el ZIP original,
y se verificó la coincidencia de los seis archivos modificados. No se ejecutaron
compilación ni pruebas automatizadas porque este entorno no tiene instalado `dotnet`.
No requiere migración de base de datos.
