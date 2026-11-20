# Corrección CS0103: consulta de cuentas de terceros por documento

Fecha: 19 de noviembre de 2026.

## Motivo
`ThirdPartyAccountReadService.cs` referencia los enums `ThirdPartyAccountValidationType` y `ThirdPartyAccountValidationStatus` sin importar el espacio de nombres en el que están declarados.

## Cambio aplicado
Se agregó `using Cfa.ACHInterbank.Domain.Models.ACH.Enums;` en:
`src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ThirdPartyAccountReadService.cs`.

No se cambian servicios, modelos, contratos del endpoint, migraciones, ni Docker/IIS.

## Verificación en ambiente de desarrollo
Desde la raíz del repositorio ejecutar:

```powershell
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
```

**Nota:** el build no se pudo ejecutar en el entorno de preparación porque no hay SDK .NET instalado. El archivo y la referencia se comprobaron estáticamente.
