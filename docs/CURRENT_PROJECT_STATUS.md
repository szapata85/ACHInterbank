# Estado consolidado del proyecto ACHInterbank

**Corte de revisión:** 2026-09-29  
**Alcance:** código fuente, solución .NET, SPA, pruebas, migraciones, documentación interna y fuentes normativas entregadas en Sources.

## 1. Resultado ejecutivo

El problema de compilación/restore `NU1105` reportado para `Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` quedó corregido en la solución: el proyecto ya existía, la API lo referenciaba, pero faltaba en `ACHInterbank.sln`. El paquete incorpora además una validación preventiva del grafo y un script de recuperación de Visual Studio/NuGet.

La revisión normativa confirma que una parte importante del desarrollo actual está alineada con ACH Colombia V35 y con el NACHA-M CENIT vigente. Sin embargo, **el proyecto no debe declararse globalmente terminado ni productivo**: persisten dependencias externas y algunos gaps funcionales/regulatorios concretos enumerados en este documento.

## 2. Evidencia inspeccionada

- 7 proyectos `.csproj`, todos `net10.0`.
- 1 solución `ACHInterbank.sln`.
- 1.730 archivos C# bajo `src/` + `tests/`.
- 697 archivos TS/HTML/SCSS bajo la SPA.
- Migraciones PostgreSQL y proyecto separado de migraciones SQL Server.
- Copias normativas comparadas por SHA-256 contra los archivos entregados; ver `docs/normativa/SOURCE_MANIFEST.md`.
- Estado durable previo consolidado en `MEMORY.md`.
- Auditoría funcional de SPA en `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`.

La revisión actual fue estática porque el entorno de preparación no dispone de `dotnet`. No se declara build/test exitoso del paquete actual hasta ejecutarlo en un host con .NET 10.

## 3. Corrección del NU1105

### Antes

`src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` contiene:

```xml
<ProjectReference Include="..\Cfa.ACHInterbank.Persistence.Migrations.SqlServer\Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj" />
```

El proyecto físico existía y contenía las migraciones SQL Server, pero no estaba incluido en la solución. El proyecto de pruebas referencia la API, por lo que el error aparecía también en `Cfa.ACHInterbank.Tests` de forma transitiva.

### Después

`ACHInterbank.sln` incluye ahora:

```text
Cfa.ACHInterbank.Persistence.Migrations.SqlServer
  src\Cfa.ACHInterbank.Persistence.Migrations.SqlServer\Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj
```

Validación estática aplicada:

```text
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution
```

Controles añadidos:

- `scripts/validate-solution-projects.py`
- `scripts/repair-visualstudio-restore.ps1`
- `docs/BUILD_AND_TROUBLESHOOTING.md`

## 4. Código vs. documentación normativa

| Área | Fuente | Código observado | Estado |
|---|---|---|---|
| ACH Colombia — ciclos V35 | `ACH-Colombia-V35.md`, 2.4.1 | `RegulatoryCycleScheduleCatalog.AchColombiaV35`: 5 ciclos 19:01–08:30, 08:31–11:00, 11:01–14:00, 14:01–16:00, 16:01–18:00; ciclo 5 sin débito monetario | **ALINEADO** |
| ACH Colombia — MFT | V35, 2.4.2/2.4.2.1 | Administración persistente, monitoreo, retry y frontera `IAchColombiaManagedMftAdapter`; artefactos `.env` | **ALINEADO INTERNAMENTE / EXTERNO PENDIENTE** |
| ACH Colombia — GoAnywhere/SFTP/S3 | V35, 2.4.2 | El código no embebe GoAnywhere ni implementa la infraestructura SFTP/S3 de ACH; usa una frontera administrada local/aplicativa | **CORRECTO COMO BOUNDARY; HOMOLOGACIÓN EXTERNA PENDIENTE** |
| CENIT — 5 ciclos base | DSP-152 Anexo 2, 3.1 | `RegulatoryCycleScheduleCatalog.CenitDsp152` conserva 07:30–10:30, 11:00–13:00, 13:30–15:00, 15:30–17:15, 17:45–18:45 y 19:15 como límite final del ciclo 5 | **ALINEADO EN VENTANAS BASE** |
| CENIT — excepciones ciclo 1 | Manual CENIT 3.1 | Fuente admite ROR del día hábil anterior y R23 de días anteriores; la política genérica actual marca `AllowsReturn=false` y `AllowsReturnOfReturn=false` en ciclo 1 | **GAP CONFIRMADO; REQUIERE POLÍTICA CONDICIONAL** |
| CENIT — límite de salida ciclos 1–4 | DSP-152 Anexo 2, 3.1 | La fuente expresa colocación de salida hasta antes del inicio del siguiente ciclo; el modelo actual usa `OutputReleaseTime = EndTime` para ciclos 1–4 | **SEMÁNTICA A ACLARAR / NO CAMBIAR A CIEGAS** |
| CENIT NACHA-M — longitud | Manual NACHA-M 7-may-2026, sec. 3 | Motor/perfiles trabajan sobre registros de 106 caracteres | **ALINEADO** |
| CENIT NACHA-M — cardinalidad PPD/CCD/CTX | Manual NACHA-M, sec. 3.1/3.2 | `BuildCenitCardinalityPolicy`: PPD crédito prenote 0–1, PPD débito 1, CCD 1, CTX original 1–9.999 y prenote 1 | **ALINEADO** |
| CENIT — causales Rxx | Anexo A | `CenitIncomingReturnPolicy.CauseDefinitions` contiene R01...R35 aplicables y seeder añade R60–R74 para ROR | **ALINEADO EN CATÁLOGO DEDICADO** |
| CENIT — rechazos Dxx | Anexo B | Seeder dedicado separa `AchFileRejectionCode` D01–D06 del catálogo transaccional | **ALINEADO EN MODELO** |
| CENIT Gateway/PO | Manual de Usuario CENIT | Existe interfaz `ICenitGatewayTransportAdapter` y simulador local; Production rechaza habilitarlo | **BOUNDARY INTERNO; LIVE EXTERNO PENDIENTE** |
| Perfiles NACHA ordinarios | Fuente vigente por cámara | Runtime usa snapshots `PUBLISH` y perfiles separados por cámara/servicio/flujo | **INTERNAMENTE CERRADO PARA ALCANCE ORDINARIO SOPORTADO** |

### Observación importante: CENIT ciclo 1

La fuente dice explícitamente que la primera sesión normalmente no compensa devoluciones, **salvo**:

- devoluciones de devoluciones del día hábil bancario inmediatamente anterior; y
- devoluciones de días anteriores por causal R23.

La configuración actual es binaria por clase funcional (`AllowsReturn`, `AllowsReturnOfReturn`). Activarlas sin condición permitiría demasiado; dejarlas en `false` impide las excepciones. La corrección correcta requiere enriquecer la política con contexto de causal/fecha/origen, no simplemente cambiar dos booleanos. Por riesgo transaccional, este paquete **documenta el gap y no inventa una regla incompleta**.

## 5. Capacidades cerradas internamente que se preservan

Según `MEMORY.md`, se mantienen como baseline aceptado y no se reabren sin evidencia contradictoria:

- inmutabilidad de configuración NACHA publicada;
- selección efectiva y table-driven de perfiles ordinarios;
- autoridad PUBLISH inbound/outbound ordinaria;
- cardinalidad CENIT publicada;
- OPS-GAP-001;
- OPS-GAP-002 / 2A–D a nivel interno de aplicación;
- OPS-GAP-004 local runtime E2E;
- OPS-GAP-005 trazas;
- OPS-GAP-006 ciclos configurables;
- CENIT-FORMAT-NACHAM a nivel interno;
- RET-GAP-018 y RET-GAP-019.

“Cerrado internamente” no equivale a homologación externa ni release productivo.

## 6. Pendientes vigentes

### Bloqueantes externos / de release

1. **OPS-GAP-003 — CENIT Gateway/PO:** sigue bloqueado por el contrato operativo aprobado CFA↔CENIT para intercambio real. El simulador local no sustituye esa definición.
2. **ACH Colombia MFT externo:** falta despliegue/conectividad/credenciales/topología GoAnywhere-SFTP-S3 y homologación con ACH Colombia. La aplicación ya tiene el boundary y administración interna.
3. **CENIT ciclo 1 — excepciones R23/ROR:** gap regulatorio concreto descrito arriba; necesita modelado condicional y pruebas.
4. **UAT/release del artefacto exacto:** la evidencia histórica no certifica este paquete. Se requiere build, pruebas, UAT aplicable y aprobación sobre el commit/artefacto final.

### Funcionales

5. **ACHCOL-CLAIMS-DEV14:** Claims sigue pendiente; DEV14 no debe reintroducirse como Return NACHA-M.
6. **NACHA-RULE-METADATA:** programa parcial; los cierres ordinarios no cierran todos los residuales CTX/metadata.
7. **PSE-SCOPE-001:** diferido hasta contar con autoridad y certificación específica.
8. **SPA bloqueada en flujos concretos:** siguen pendientes, salvo evidencia posterior, la resolución manual de respuestas, administración persistente de status mappings, cierre/reproceso gobernado de conciliación, varias pantallas regulatorias CENIT genéricas, experiencia administrativa NACHA completa, modo diferencial homologado del simulador y acreditación completa del scheduler. Ver `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`.
9. **Interoperability UAT frontend sin endpoint backend:** `InteroperabilityApiService` conserva llamadas a `nacha-security/interoperability/*` y un `TODO(UAT)` explícito; no se encontró controlador backend equivalente. Debe resolverse como endpoint real o retirar/deshabilitar el contrato SPA antes de certificar ese flujo.

### Deuda técnica a resolver sin confundir con normativa

10. `src/Cfa.ACHInterbank.External/Connections/AuthenticationService.cs` conserva un `GetTokenAsync()` con `NotImplementedException`. No se encontraron consumidores de `IAuthenticationService` en el código productivo actual, por lo que no explica NU1105; debe eliminarse como contrato muerto o implementarse cuando exista un flujo autorizado que lo use.
11. `Cfa.ACHInterbank.Application.csproj` mezcla `net10.0` con paquetes ASP.NET Core `2.3.9`. No se demostró un fallo concreto en esta revisión, pero conviene planificar su retiro/migración para reducir riesgo de compatibilidad/transitividad.

## 7. Qué se cambió en este paquete

- `ACHInterbank.sln`: agregado el proyecto SQL Server faltante, sus configuraciones de build/nesting y eliminado un folder de solución `tests` duplicado/vacío.
- `scripts/validate-solution-projects.py`: validador preventivo de solución/referencias.
- `.github/workflows/dotnet-ci.yml`: ejecuta el validador antes de `dotnet restore` para impedir que la misma regresión llegue a CI.
- `scripts/repair-visualstudio-restore.ps1`: saneamiento seguro de cachés locales + restore/build/test.
- `README.md`: actualizado a estado y estructura reales.
- `docs/BUILD_AND_TROUBLESHOOTING.md`: guía del NU1105 y build limpio.
- `docs/CURRENT_PROJECT_STATUS.md`: este consolidado.
- `docs/DOCUMENTATION_INDEX.md`: índice canónico.
- `docs/normativa/SOURCE_MANIFEST.md`: hashes y autoridad normativa.
- `tests/Cfa.ACHInterbank.Tests/ClearingHouseCycleConfigSeederTests.cs`: nombre de prueba corregido de V32 a V35; no cambia comportamiento.
- `AGENTS.md` / `MEMORY.md`: actualizados únicamente para registrar la solución y el estado de mantenimiento.

## 8. Validación que debes ejecutar al recibir el ZIP

Con Visual Studio cerrado:

```powershell
python scripts/validate-solution-projects.py
powershell -ExecutionPolicy Bypass -File .\scripts\repair-visualstudio-restore.ps1
```

Si el restore/build pasa, abrir `ACHInterbank.sln` en Visual Studio 2026 y confirmar que `Cfa.ACHInterbank.Persistence.Migrations.SqlServer` aparece bajo Infrastructure.

## 9. Criterio para declarar terminado

No usar un porcentaje genérico como sustituto de evidencia. Para cerrar el proyecto deben quedar, como mínimo, resueltos o formalmente aceptados los bloqueantes externos, el gap condicional CENIT ciclo 1, el alcance Claims/metadata que entre al release y la UAT del artefacto exacto. Todo cierre debe mantener separados ACH Colombia y CENIT y citar su fuente aplicable.
