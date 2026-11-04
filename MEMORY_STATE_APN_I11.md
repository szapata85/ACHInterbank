# MEMORY STATE — APN-I11

**Fecha:** 2026-11-03. **Base recibida:** ZIP completo APN-I10. **Nueva base:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I11_2026-11-03.zip`.

## Reglas inmutables

- ACH Colombia: **7 ciclos operativos vigentes**, política `ACHCOL-7C-20261014`; V35 contiene cinco ciclos históricos, no sustituir el calendario actual.
- ACH Colombia: cuatro ciclos **posteriores válidos** de devolución; un ciclo `Cancelled` no cuenta.
- CENIT: **5 sesiones**; la última termina 18:45 y archivos de salida hasta las 19:15 **America/Bogota**.
- No confundir UTC con fecha operacional Colombia; para registros fechados en UTC usar ventana derivada de medianoche local posterior al plazo. Los campos `OperationalDate` son fechas operativas locales.
- Una salida `ACK`/`NoActivity`, un `AchCycle.Closed` y la tabla de recepciones vacía **no** constituyen comprobante positivo de recepción completa.
- No reescribir estados terminales de prenotificación silenciosamente. Resultado discordante/tardío pasa a revisión.
- `IMPLEMENTADO_EN_FUENTE` ≠ `CSHARP_APROBADO` ≠ `HOMOLOGADO`.
- Conservar ficheros `MEMORY.md`/`MEMORY_STATE.md`, sus versiones APN anteriores y estructura `ACHInterbank_ajustado/`.

## Historial incremental

- I01–I07: auditoría documental/estática únicamente; no cambios productivos ni homologación.
- I08: plazos completos y hora final de liberación; fuente modificada, sin pruebas .NET.
- I09: transmisión e intentos canónicos y guardias negativas de archivos NACHA; fuente modificada, sin .NET.
- I10: guardias MFT/CENIT, rechazo canónico CENIT, control de terminales; fuente modificada, sin .NET.
- **I11:** excluye ciclos cancelados del conteo ACHCOL y bloquea fuente cancelada, corrige ventana local/UTC de ingresos NACHA/MFT/CENIT, incluye CENIT negativo de sesiones posteriores sin correlación definitiva. Añade 8 pruebas C# y diseño de contrato de evidencia externa.

## Comprobaciones realizadas

- Python estática y fechas: I08 6/6, I09 8/8, I10 10/10, I11 12/12, matrices normativas 6/6 = **42/42** PASS.
- SDK .NET 10.0.403: **AUSENTE**. xUnit/compilación: **NO_EJECUTADOS**. SQL/Quartz/UAT: **NO_EJECUTADOS**.
- Las 42 pruebas Python son verificaciones estáticas/documentales, **no** 42 pruebas de ejecución del motor C#.

## Riesgos abiertos y siguiente trabajo

- R05, R06, R07, R08 abiertos. R01/R03 mitigación parcial. R02/R04/R09/R10 corregidos únicamente en fuentes y sin homologación.
- Contrato de cierre positivo en `APN_I11_CONTRATO_CIERRE_RECEPCION.md` estado **DISEÑADO / NO INTEGRADO**. No inventar watermark ni activar gate que impediría todas las aceptaciones sin productor real.
- Siguiente: APN-I12 enfocado en ejecución verificable .NET+SQL donde haya runtime, evaluación de mejoras de retención/alertas y/o incorporación del proveedor de evidencia externa solo si hay contrato confirmado. No marcar homologado por revisión estática.
