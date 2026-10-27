# F2.1 — Resolución de cámara y fecha ACHCOL sin `AchCycleId` obligatorio

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-24  
**Fuente exacta:** `ACHInterbank_ACHCOL_CFA_F1_2_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED` — código y pruebas agregados, SDK .NET 10 no disponible.  
**Siguiente:** `F2.2` — admisión operativa sin ciclo ACH interno.

## Objetivo y límite funcional

Cuando el archivo físico ACH Colombia descifrado/en claro cumple `RRRRTTT.ZZZ.yyyyMMdd.N.OUT`, la cámara `ACHCOL` y la fecha del tipo 1 pueden quedar resueltas **sin consultar ni exigir una fila `AchCycle`**. La secuencia `ZZZ` y el sufijo externo `N` son diferentes; ninguno se traduce en `AchCycleId`.

Esta microtarea **NO** elimina todavía el rechazo `CYCLE_NOT_RESOLVED` de `IncomingNachaAdmissionValidator` (F2.2), **NO** modifica el modelo/cola de persistencia (F2.3/F3), y **NO** activa despacho SOAP (F3–F5). Así el sistema evita autorizar prematuramente movimientos monetarios mientras se desarrolla el desacoplamiento.

## Cambios en fuentes

| Archivo | Cambios |
|---|---|
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaCycleResolver.cs` | `IConfiguration?` opcional; rama `ACHCOL` válida y flag habilitado; retorna `IsResolved=true`, `AchCycleId=null`, `ResolutionMode=AchColFileBased`, estado `ResueltoSinCiclo`. Requiere fecha y hora tipo 1 válidas. Incluye evidencia estructurada de `fileSequence`, `externalCycleNumber`, cámara y fecha. No consulta `AchCycles` en esa rama. |
| `src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaIngestion.cs` | Agrega `IncomingNachaCycleResolutionStatus.ResueltoSinCiclo=7` **sin renumerar** los estados históricos 1–6. Los estados son persistidos como cadenas y no requieren migración de esquema por este cambio. |
| `tests/Cfa.ACHInterbank.Tests/IncomingNachaCycleResolverTests.cs` | Casos nuevos para H/008/.8, I/009/.9, H/008/.10, ningún ciclo, coexistencia con un ciclo interno, flag deshabilitado, nombre RET sin cambio y errores de hora/fecha en el header. |

## Control por configuración

`AchCol:AllowCycleIndependentInboundDispatch = true` es el flag **opt-in** definido en el ADR de F0.3. Ausente o `false` mantiene el resolver `CycleBound` anterior. La nueva rama solo aplica cuando `ClearingHouse.Code == "ACHCOL"` **y** el nombre ya descifrado/en claro pasa `AchColInboundFileNameParser.TryParse(...)`: evita afectar archivos `RET`, CENIT o nombres legacy genéricos.

**Nota de habilitación:** no agregar `true` a configuración productiva durante F2.1. Habilitarlo de forma controlada después de cerrar admisión, persistencia, idempotencia, cola, mapeo SOAP y autorizaciones requeridas; hacerlo antes provocaría bloqueos downstream no resueltos y podría alterar rutas no homologadas.

## Decisiones

- **DEC-018 (F2.1):** estado explícito `ResueltoSinCiclo` y `ResolutionMode=AchColFileBased`; `IsResolved=true` significa **contexto ACHCOL cámara/fecha resuelto**, **no** que exista una fila `AchCycle`.
- **DEC-019 (F2.1):** flag deshabilitado por omisión, contexto ACHCOL sin ciclo solo con nombre `*.OUT` canónico válido y cámara `ACHCOL`; RET, CENIT y nombres no aplicables usan el algoritmo existente.
- **DEC-020 (F2.1):** conservar fecha y hora de creación del header como requisitos válidos del contexto; tiempo inválido falla cerradamente con `ACHCOL_HEADER_FILE_CREATION_TIME_INVALID`, aunque no haya ciclos parametrizados. La correlación letra↔ZZZ sigue en la etapa previa F1.2.
- **DEC-021 (F2.1):** no permitir que `ResueltoSinCiclo` salte admission, política externa o mecanismos de seguridad; F2.2–F5 adaptarán los consumidores del nuevo estado.

## Evidencia / deudas

- Verificación estática: archivos modificados, visibilidad de rama y flag, comprobación de equilibrio de delimitadores, ausencia de cambios SQL/migraciones/SOAP, integridad del ZIP final.
- **No ejecutado:** `dotnet build`, `dotnet test` ni suites de DB/ambiente por SDK .NET 10 no instalado. Tests escritos NO equivalen a tests aprobados.
- Se requieren pruebas finales de integración, regresión CENIT/RET, BD, idempotencia y CFA autorizadas en F6, tras F1–F5. La falta de ambiente no impide desarrollar F2.2.
- **Limitación actual:** al recibir `IsResolved=true` con `AchCycleId=null`, el admission validator todavía rechaza; F2.2 corregirá eso con validaciones diferenciadas sin relajar controles generales.

## Rollback

Deshabilitar `AchCol:AllowCycleIndependentInboundDispatch`; se conserva la resolución legacy para todas las cámaras. No crear ciclos artificiales, no modificar estados previos ni borrar evidencia. Si se revierte el código después de persistir el estado `ResueltoSinCiclo`, revisar las ingestas afectadas con Operaciones antes de reprocesar.
