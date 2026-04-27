# Evidencia de cierre — Fase 1A documentación Scalar/OpenAPI

**Proyecto:** ACHInterbank  
**Fecha (UTC):** 2026-04-26  
**Objetivo:** cerrar formalmente la fase 1A con compilación real y evidencia de documentación en 10 controladores críticos.

## 1) Controladores revalidados

1. `AchReturnsController`
2. `AchTraceabilityController`
3. `CenitOperationsController`
4. `CertificateManagementController`
5. `DigitalEnvelopeCertificatesController`
6. `IncomingNachaCommandCenterController`
7. `NachaExportController`
8. `NachaSecurityOperationsController`
9. `PaymentRailCapabilityRegistryController`
10. `ReportsController`

## 2) Comandos ejecutados

```bash
git status --short
git log --oneline -20

bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet build ACHInterbank.sln -c Release

rg -n "servicio documentado|expone la operación|revisar \[Authorize\]/Policy|durante operación diaria y soporte|Endpoint de la API ACH Interbank" \
  src/Cfa.ACHInterbank.Api/Controllers/{AchReturnsController.cs,AchTraceabilityController.cs,CenitOperationsController.cs,CertificateManagementController.cs,DigitalEnvelopeCertificatesController.cs,IncomingNachaCommandCenterController.cs,NachaExportController.cs,NachaSecurityOperationsController.cs,PaymentRailCapabilityRegistryController.cs,ReportsController.cs} -S

rg -n "EndpointSummary|EndpointDescription" \
  src/Cfa.ACHInterbank.Api/Controllers/{AchReturnsController.cs,AchTraceabilityController.cs,CenitOperationsController.cs,CertificateManagementController.cs,DigitalEnvelopeCertificatesController.cs,IncomingNachaCommandCenterController.cs,NachaExportController.cs,NachaSecurityOperationsController.cs,PaymentRailCapabilityRegistryController.cs,ReportsController.cs} -S
```

## 3) Resultado de setup y compilación

- Setup de entorno ejecutado correctamente con instalación de:
  - .NET SDK `10.0.203`
  - Runtime `10.0.7`
  - `dotnet-ef 10.0.7`
- `dotnet build ACHInterbank.sln -c Release`: **exitoso**.
- Estado de compilación:
  - **0 errores**
  - **9 warnings** (preexistentes de nulabilidad en capas Application/Persistence; no relacionados con cambios de documentación).

## 4) Verificación de calidad de documentación

### 4.1 Textos genéricos
La búsqueda de textos genéricos en los 10 controladores críticos no arrojó coincidencias activas, validando su eliminación en metadata OpenAPI/Scalar.

### 4.2 Presencia de metadata por ruta
Se verificó presencia de atributos `EndpointSummary` y `EndpointDescription` en los controladores revalidados, con contenido específico por servicio y contexto ACH/CENIT/NACHA-M.

## 5) Conclusión de fase 1A

Se **cierra fase 1A** de documentación Scalar/OpenAPI con evidencia verificable:

- compilación real en `Release` ejecutada con éxito,
- documentación específica por ruta presente,
- remoción de textos genéricos en metadata,
- sin cambios de lógica de negocio, rutas, contratos, permisos, Angular, criptografía ni migraciones.
