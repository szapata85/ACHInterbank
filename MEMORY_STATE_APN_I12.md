# MEMORY STATE — ACH INTERBANK / APN-I12

**Corte:** 2026-11-03. **Base recibida:** ZIP completo APN-I11. **Nueva base:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I12_2026-11-03.zip`.

## Reglas estables

- ACH Colombia: **7 ciclos vigentes**, no 5; política `ACHCOL-7C-20261014`, cuatro ciclos posteriores válidos y fin de liberación de archivos.
- CENIT: **5 ciclos**; última salida hasta **19:15 America/Bogota**.
- Los ciclos cancelados no cuentan; diferenciar fechas operativas Colombia de instantes UTC.
- La ausencia de archivos registrados, `ACK`, `NoActivity` o `AchCycle.Closed` no prueba entrega completa de cámara.
- No aceptar automáticamente si no se comprobó transmisión, plazo recalculado, intento canónico, ausencia de devoluciones/archivos pendientes y transición atómica.
- Nunca sobrescribir silenciosamente estados terminales ante devoluciones tardías.
- `IMPLEMENTADO_EN_FUENTE` ≠ `PRUEBAS_DOTNET_APROBADAS` ≠ `HOMOLOGADO`. No declarar éxito sin ejecución.
- Conservar `MEMORY.md`, `MEMORY_STATE.md`, todos los MEMORY_STATE_APN_Ixx, el proyecto completo y sus pruebas en cada ZIP acumulativo.

## Historia

- APN-I01–I07: inspección estática y documentación, sin cambios productivos.
- APN-I08: plazos completos + hora de liberación, C# sin homologación.
- APN-I09: comprobación de transmisión + intentos canónicos + NACHA, C# sin homologación.
- APN-I10: MFT/gateway CENIT + terminales, C# sin homologación.
- APN-I11: cancelados, ventanas UTC/Colombia y contrato externo propuesto, C# sin homologación.
- **APN-I12:** incorpora `PrenotificationCandidateScan` con reevaluación de plazos almacenados nulos; `UNION` de canónicos y legacy; páginas `keyset` y cursor rotativo persistido en `TaskParameters` existente; presupuesto `MaxCandidatesPerRun`, contadores y alertas de saturación. Ningún cambio de schema/estados/cámaras.

## Evidencia honesta I12

- Python I08 6/6, I09 8/8, I10 10/10, I11 12/12, I12 12/12, matrices 6/6: **54/54 PASS**. Solo estáticas/documentales y ejemplos independientes; no prueban EF ni C#.
- Cuatro métodos xUnit nuevos con casos `[Theory]`, **0 ejecutados**. `dotnet` no disponible: `run_apn_i12_dotnet.sh` retorna 127.
- SQL Server/PostgreSQL, Quartz, concurrencia, MFT/NACHA reales y UAT: **NO_EJECUTADOS**.
- Contrato externo de cierre de archivos APN-R07: DISEÑADO / SIN PRODUCTOR / NO INTEGRADO.

## Próximas prioridades

1. Ejecutar en .NET 10 las suites I08–I12, confirmar traducción de `UNION`/`DateTimeOffset` y el cursor en SQL Server y PostgreSQL; revisar resultados C# antes de permitir despliegue.
2. Medir backlog y tiempos del job (cada 30 min), alertas de saturación, idempotencia y concurrencia; no inferir éxito por los 54 tests Python.
3. Coordinar evidencia externa verificable de cierre de recepción por cámara/ciclo. No activar señal ficticia; requisito permanece abierto.
4. Auditar selección de pendientes y conciliación manual de ambiguos; cualquier ajuste posterior debe contar con pruebas y ZIP acumulativo.

**Siguiente incremento sugerido:** APN-I13 — pruebas integradas de concurrencia, cursor y transiciones en runtime; no prometer homologación sin entorno.
