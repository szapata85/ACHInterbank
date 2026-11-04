# F2.3 — Persistencia y trazabilidad ACHCOL sin ciclo interno

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-24  
**Baseline:** `ACHInterbank_ACHCOL_CFA_F2_2_UNIFICADO.zip`  
**Resultado:** `IMPLEMENTED_UNVERIFIED` — cambios de código, migraciones y pruebas escritos, sin ejecutar .NET/DB/CFA por ausencia de SDK .NET 10.  
**Siguiente microtarea:** `F3.1` — esquema de cola NACHA y origen transaccional para PostgreSQL; no repetir las columnas de F2.3.

## Objetivo y separación semántica

Después de descifrar `.OUT.env` (cuando aplique) y validar el encabezado tipo 1, persistir de forma tipada el contexto de archivo ACH Colombia: `FileName` (nombre físico original, **incluye `.env`**), `CanonicalPlainFileName` (sin `.env`), `FileIdModifier` (`H`, `I`, etc.), `FileSequence` (`ZZZ` como entero), `ExternalCycleNumber` (`N` del nombre, positivo sin límite 7), `FileProtectionKind` (`PlainNacha`/`DigitalEnvelope`), `FileNameDate` y los campos de resolución que ya existían. **Ningún campo nuevo es una FK ni representa `AchCycleId`**. `ResolvedAchCycleId` sigue nullable y sin sustitutos ficticios.

Ejemplo: `0001283.008.20260724.10.OUT.env` → original íntegro, canónico `0001283.008.20260724.10.OUT`, identificador `H`, secuencia `8`, ciclo externo `10`, protección `DigitalEnvelope`, fecha 2026-07-24, `AchCycleId=null` bajo `AchColFileBased`.

## Cambios entregados

| Área | Archivo | Cambio |
|---|---|---|
| Dominio | `IncomingNachaIngestion.cs` | Cinco propiedades nullable adicionales en `IncomingNachaFileIngestion`, sin tocar las identidades, relaciones o enumeraciones existentes. |
| Parser de nombre | `ExternalFileNames/AchColInboundFileNameParser.cs` | Derivación determinista `FileIdModifier` desde consecutivo 001–036; nunca desde ciclo `N`. |
| Aplicación | `ExternalFileNames/AchColInboundIngestionContext.cs` **nuevo** | Materialización controlada desde un nombre ya validado; defensa frente a nombre físico distinto; no modifica estado, ciclo interno ni elegibilidad. |
| Ingesta | `IncomingNachaIngestionAppService.cs` | Guarda metadatos inmediatamente después de la correlación tras descifrado, incluso si una validación posterior impide parsear; retiene fecha ya validada cuando admisión no proporciona fecha; devuelve metadatos en rutas normal y duplicado. |
| EF Core | `IncomingNachaFileIngestionConfiguration.cs` | Longitudes 260/1/30 para cadenas nuevas. Campos numéricos opcionales; ningún vínculo nuevo a `AchCycle`. |
| API / consulta | `IncomingNachaIngestionDtos.cs`, `IncomingNachaCommandCenterDtos.cs`, `IncomingNachaCommandCenterQueryService.cs` | Metadatos expuestos en respuesta de ingesta y consulta de detalle, opcionales para históricos y otras cámaras. |
| Migraciones | Postgres `20261024190000_AchColInboundFileContextF23.cs`, SQL Server `20261024190100_AchColInboundFileContextF23.cs` | Cinco columnas opcionales **expand-first** y métodos `Down`; snapshots EF de ambos proveedores actualizados. |
| Pruebas | `AchColInboundIngestionContextTests.cs` **nuevo** | Cuatro escenarios de roundtrip SQLite (`H/008/.8`, `I/009/.9`, `H/008/.10`, `9/036/.123`), rechazo de contexto cruzado, fila legacy nullable. **Escritos, NO ejecutados.** |

## Decisiones técnicas F2.3

- **DEC-026:** `FileName` es el nombre físico de transporte ya almacenado; no crear una segunda propiedad redundante `OriginalPhysicalFileName`. `CanonicalPlainFileName` lo complementa sin sobrescribirlo.
- **DEC-027:** `FileSequence` y `ExternalCycleNumber` son columnas nullable independientes. `FileIdModifier` se deriva solo de la secuencia validada; `DetectedCycleNumber` de admisión no se sustituye ni es FK.
- **DEC-028:** La materialización ocurre **tras** `AchColInboundHeaderCorrelation.TryValidate` (sobre contenido descifrado) y **antes** de admisión/perfil/parser; guardar junto al evento de auditoría y conservar si sobreviene rechazo posterior. No guardar bytes descifrados a disco por este incremento.
- **DEC-029:** Migraciones incrementales PostgreSQL y SQL Server con paridad de columnas y snapshots; nulos para históricos y CENIT/RET, sin backfill especulativo. `DateTimeKind.Utc` al materializar `FileNameDate` ACHCOL para columna `timestamp with time zone` de Postgres.
- **DEC-030:** Exponer información de contexto solo como nuevos campos opcionales en contratos API existentes para compatibilidad; no activar el flag, no cambiar cola, SOAP, reintentos ni flujo financiero.

## Evidencia y limitaciones

**Realizado:** comparación estática del código fuente con F2.2, revisión de entidades/EF y migraciones de proveedores, inspección del uso de `IncomingNachaFileIngestionId` desde parser hacia cabeceras/lotes/detalles, preservación de metadatos en DTO de respuesta y consulta; inspección de la estructura del paquete y comprobación de integridad ZIP.

**No ejecutado:** `dotnet build`, `dotnet test`, migraciones contra PostgreSQL o SQL Server, pruebas en transporte MFT/SFTP, SOAP CFA y UAT. La paridad propuesta en C# y snapshots NO implica ejecución de migración real. Se difiere la certificación integral a F6 sin interrumpir F3–F5.

**Pendiente F3–F5:** cola, despacho y mapper aún usan `AchTransaction` y `AchCycleId`; los metadatos persistidos no habilitan por sí solos `Proc_Transacciones`. F3.1 debe modelar origen NACHA/cola sobre los cinco campos ya añadidos, conservando paridad de proveedor.

## Reversión y controles

- Desactivar `AchCol:AllowCycleIndependentInboundDispatch` detiene la nueva ruta sin pérdida de historial persistido.
- La migración `Down` suprime columnas nuevas **solo si se aprueba explícitamente revertir datos**; para rollback de aplicación se prefiere expand-first con columnas inertes.
- No reingresar transacciones monetarias por el hecho de cambiar de ruta, y no activar `Live` hasta homologación y controles de idempotencia.
