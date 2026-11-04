# MEMORY STATE — APN-I13 / ACH Interbank

**Corte:** 2026-11-03. **Base recibida:** ZIP completo APN-I12. **Base nueva:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I13_2026-11-03.zip`.

## Reglas invariables

- ACH Colombia **7 ciclos** (aunque V35 indique 5), política `ACHCOL-7C-20261014`; cuentan cuatro ciclos posteriores válidos y debe aguardarse la liberación final del archivo.
- CENIT **5 ciclos**; última liberación hasta 19:15 Bogotá.
- Los cancelados no cuentan; fechas operativas Colombia ≠ marcas UTC.
- ACK, NoActivity y ciclo marcado cerrado NO prueban que todos los archivos llegaron.
- Aceptación tácita exige transmisión verificada, plazo recalculado, intento canónico, ausencia de devoluciones/registros sin procesar y transiciones idempotentes.
- Los estados terminales no pueden sobrescribirse silenciosamente.
- **NO SOBREINGENIERÍA:** no añadir tablas, estados, procesos ni servicios sin defecto demostrado.
- `IMPLEMENTADO` / `PASS_ESTÁTICO` / `HOMOLOGADO` son categorías diferentes. 60/60 Python NO significa éxito xUnit.

## Historia mínima

- I01–I07: auditoría estática/documental.
- I08: vencimientos correctos y fin de salida, sin .NET homologado.
- I09: transmisión/NACHA/intento canónico, sin .NET homologado.
- I10: barreras MFT/CENIT y terminales, sin .NET homologado.
- I11: ciclos cancelados y zona horaria, sin .NET homologado.
- I12: selección y revisión de candidatos con vencimiento nulo, cursor persistente, sin .NET homologado.
- **I13: estabilización y pruebas mínimas; código productivo de I12 intacto.** Dos pruebas xUnit SQLite añadidas, runner C# y seis verificaciones Python.

## Evidencia y próximos pasos

- Python 60/60 PASS (54 heredadas + 6 I13). SQL Python es solo un ejemplo independiente, no EF.
- C# .NET 10 NO EJECUTADO, código 127 por SDK ausente. xUnit/EF SQL/Quartz/UAT pendientes.
- Cierre externo positivo de recepción APN-R07 sin productor, ABIERTO.
- No recomendar ampliaciones de arquitectura por APN-I14 sin fallo probado. Una vez en entorno con SDK, ejecutar la lista mínima de `APN_I13_HOMOLOGACION_MINIMA.md`; registrar TRX, correcciones exclusivamente justificadas y luego decidir aceptación.

**Uso en Sources:** subir únicamente el ZIP COMPLETO I13 como nueva base. El delta solo es apoyo para revisión; conservar memorias históricas originales del repositorio.
