# F0.1 — Baseline de paquete fuente, Git y dependencias

**Fecha:** 2026-10-24  
**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Microtarea:** `F0.1` — caracterización, sin modificaciones funcionales.

## 1. Fuente y trazabilidad

- Paquete suministrado: `ACHInterbank_ajustado.zip`.
- SHA-256 fuente: `26da6cf2dffcf939aa539f8ea9f0169fa8164fb959eaf2eb3a7ae22651b6c890`.
- Verificación de integridad del ZIP: `unzip -tq` -> `No errors detected`.
- Entradas del ZIP fuente: **8.073** (incluidos archivos de Git y directorios).
- Se descomprimió sin editar el contenido productivo y se incorporó `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/` con los tres documentos originales, más este reporte y un manifiesto de entrega.
- Rama Git extraída: `ACH-Interbank`; `HEAD`: `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`.
- El `HEAD` es **metadato del ZIP**, no certificación de que este snapshot coincide con el árbol de ese commit.
- Repositorio remoto registrado en Git (no consultado): `https://github.com/szapata85/ACHInterbank.git`.

## 2. Estado de trabajo observado ANTES de incorporar documentación

- `git status --porcelain=v1 --untracked-files=no`: **2.918** entradas (2.911 modificaciones de contenido por CRLF/LF y **7 eliminaciones** de archivos seguidos por Git).
- `git -c core.autocrlf=true status --porcelain=v1 --untracked-files=no`: **7** eliminaciones, tras normalizar la visualización del fin de línea.
- `core.filemode=false` en `.git/config`; la mayor parte de las modificaciones no son de modo sino terminaciones de línea, como muestra `git diff -- .cbmignore`.
- No se restauraron los 7 archivos ni se normalizaron los 2.911, porque el objetivo es **preservar íntegramente** el ZIP suministrado, sin alterar código fuente inadvertidamente.

**Archivos seguidos por Git ausentes en el ZIP de entrada:**

- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/AchCycleDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/BankHolidayDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/ClearingHouseCycleConfigDtos.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/ClearingHouseDtos.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/ClearingHouseSpecialDateDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/InstitutionClearingHousePreferenceDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Ach/Dtos/UpdateInstitutionClearingHousePreferenceDto.cs`

**Criterio para continuar:** El ZIP recibido constituye el snapshot de referencia de esta iniciativa; la divergencia de Git queda documentada y no se inventa un estado de Git limpio. La microtarea **F0.2 de investigación estática** puede continuar sobre este snapshot. Antes de cambios funcionales, el ejecutor debe inspeccionar las 7 ausencias, la posibilidad de compilación y el estado local del checkout de integración; nunca reconstruirlos/commitearlos automáticamente.

## 3. Dependencias y rutas localizadas

- Resolución y admisión: `IncomingNachaCycleResolver.cs`, `IncomingNachaAdmissionValidator.cs`, `IncomingNachaIngestionAppService.cs`.
- Header, secuencia y nombre: `NachaParserService.cs`, `ExternalFileNames/ExternalFileNameSupport.cs` y `ExternalFileNames/ExternalFileNameBuilder.cs`.
- Elegibilidad y cola: `IncomingNachaDispatchEligibilityPolicy.cs`, `IncomingNachaDispatchPlanner.cs`, `IncomingNachaPostProcessingPipeline.cs`.
- SOAP: `ProcTransaccionesRequestMapper.cs`, `Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs`.
- Modelo de cola: `Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs` y `Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs`.
- Migraciones: `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/` y `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/`.
- Pruebas existentes: al menos **22** archivos `IncomingNacha*Tests.cs`/`AchColExternalNamingGateTests.cs` identificados por ruta; **no ejecutados**.
- Programa concurrente: `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md`, estado `P8 IN_PROGRESS`, `P8.4 BLOCKED`; **sin alteraciones**. Pueden solaparse mappers/contratos SOAP. Revisar antes de editar esos archivos.

## 4. Huellas SHA-256 sobre código fuente extraído, sin cambios

| Archivo | SHA-256 |
| --- | --- |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaCycleResolver.cs` | `90a9870dc32bd3bcc871d6d438cded87563aef81cc5b76d1d3187d275415acdb` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs` | `e2a3bbfb321a413b12cec07589b30e837f763f478297ef5fa7a72710f616d535` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs` | `84b7494a0a04a966b9c5e9d15e208f3ec3bad0fd90e2989636782db8850ca753` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs` | `9ff9fc5635c0f057761acf96c8e5240ea2b7638bbecfccf9c0445718938add7a` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchEligibilityPolicy.cs` | `6e9f777b5a0e9f87aec6ae701c1324241230f90d49759ea71ee3897c596b862a` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs` | `b3e728696bf5b8a91be261b678a466be2c578a0d36433bf4c0591a902df33a08` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` | `1e52e87e75e1d6d7e2335ea391c7694506e6c8b07584746f48f59e6f83b41f26` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` | `6d3ad17c8867e1ce1ac6e4029776e96c93e0e6bc95288840cdc2f66bf366cba5` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/ExternalFileNameSupport.cs` | `be589bae309897f5d4547ed45df40a8efd10bb061a3201029ad47fb55db01926` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/ExternalFileNameBuilder.cs` | `dceb56075aa8862edc80335770b6471f7cdbd0201d19b76543b052b89a9d4665` |
| `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs` | `c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72` |
| `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` | `5f8d7dae04cc5a63116c0aa66c393952edc32ff0f354c3030d5cd46c3e378633` |

## 5. Validaciones / bloqueos

- Ejecutado: prueba de integridad del ZIP; inspección de Git HEAD/branch, dirty diff antes de agregar documentos, rutas y SHA-256 de fuentes; comparación de estado de otro programa.
- No ejecutado: `dotnet build`, `dotnet test`, migraciones, Docker, bases, DryRun/Live CFA. Las herramientas `dotnet` y `docker` no se encuentran en `PATH` en este entorno.
- **Sin cambios funcionales**, sin llamadas a CFA y sin despliegue.
- No se restableció ni se hizo commit sobre el estado de trabajo.
- **Resultado acotado de F0.1:** `VERIFIED` para verificación documental y captura reproducible del snapshot, **no** para integridad de build o limpieza de Git.
- **Próximo incremento:** `F0.2`, trazado detallado nombre/encabezado -> ingesta -> admisión -> parseo -> cola -> mapper -> SOAP, sin cambios productivos.
