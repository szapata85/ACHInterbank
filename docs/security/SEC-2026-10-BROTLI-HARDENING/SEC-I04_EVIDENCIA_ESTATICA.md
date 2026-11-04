# SEC-I04 — Evidencia técnica estática y continuidad

**Fecha:** 2026-10-27  
**ZIP padre:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_I03_SBOM_INVENTORY_COMPLETO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`  
**Microtareas ejecutadas:** `SEC-I04-A_CREDENTIALS_SEED_INVENTORY` y `SEC-I04-B_VERSIONED_PASSWORD_HASHING`.

## Decisión explícita SEC-DEC-009

`PRESERVE_ADMIN_SEED_CREDENTIAL=TRUE`: el archivo `src/Cfa.ACHInterbank.Persistence/Configuration/UserConfiguration.cs` **no se modificó**, tampoco su usuario, contraseña, SHA-256 heredado ni `HasData`. **No incluir la contraseña ni su hash en reportes, logs o pruebas**. Conservar una credencial fija supone riesgo residual: aprobar controles compensatorios antes de producción.

## Inventario de rutas afectadas

| Archivo | Uso inicial | SEC-I04 |
|---|---|---|
| `AuthService.cs` | Validación SHA-256 directa en login y generación en reset | Login compatible con PBKDF2 y SHA-256 legado mediante verificador central; reset siempre PBKDF2 |
| `UsersService.cs` | SHA-256 directo en Create/Update | Hash nuevo PBKDF2, elimina rutina local `BuildHash` |
| `UserConfiguration.cs` | Seed administrativo SHA-256 fijo | **Sin cambios byte a byte**, por decisión expresa del usuario |
| `CredentialPasswordHasher.cs` | No existía | PBKDF2-HMAC-SHA256 versionado `pbkdf2-sha256$v1$...`, 600.000 iteraciones, sal de 16 bytes, salida de 32 bytes |
| `CredentialPasswordHasherTests.cs` | No existía | Tests xUnit para hash salado, verificación, legado, malformados y password vacío |

**Comportamiento:** nuevos usuarios y cambios de contraseña/restablecimiento usarán un hash aleatorio fuerte. Autenticación con hashes heredados SHA-256 se mantiene sin reescribirlos; la semántica de bloqueo y generación de JWT no se modifica. No hay migración de datos, rehash automático ni actualización de registros semilla. Los hashes modernos caben en el límite existente de 256 caracteres.

## Validación y límites

- Comparación de bytes del `UserConfiguration.cs` original y del ZIP entregado: `PASS_STATIC`.
- Búsqueda de `HashHelper.GenerateHashSha256` y `BuildHash` en rutas AuthService y UsersService: `PASS_STATIC` (ausentes en flujos nuevos).
- Comprobación de todos los puntos de escritura `PasswordHash`: `PASS_STATIC` para flujos explícitos inspeccionados.
- Inspección sintáctica estructural básica y pruebas estáticas de presencia: `PASS_STATIC` (no equivale a compilación).
- Pruebas xUnit: `WRITTEN_NOT_EXECUTED`; SDK `dotnet` no instalado.
- `dotnet restore/build/test`, DB real, login/lockout/reset integración, migraciones y homologación: `NOT_RUN_ENVIRONMENT`.
- No se inspeccionaron credenciales reales en producción, no se registraron ni utilizaron.

## Validaciones de ambiente pendientes (EV-05)

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~CredentialPasswordHasherTests
# Añadir pruebas de autenticación e integración con BD semilla y no semilla en entorno autorizado.
```

## Restricciones y continuidad

- Riesgo pendiente: el seed administrativo mantiene SHA-256 legado y contraseña estática **por instrucción del solicitante**; no presentarlo como mitigado ni retirar el seed en SEC-I05.
- `GO_INCREMENT=YES`; `GO_DEPLOY=NO`; `GO_LIVE_MONETARY=NO`; Quartz sigue deshabilitado.
- Sin ambiente, continuar SEC-I05 pero dejar EV-05 pendiente.
- **Siguiente microtarea:** `SEC-I05-A_HARDENING_INVENTORY`.
