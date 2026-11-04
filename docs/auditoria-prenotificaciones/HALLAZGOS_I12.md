# Hallazgos APN — Corte I12

| ID | Estado I12 | Evidencia / riesgo residual |
|---|---|---|
| APN-R01 | MITIGACIÓN_PARCIAL_EN_FUENTE | Hora final de liberación y barreras de ingreso; falta comprobante externo positivo de entrega completa. |
| APN-R02 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | ACH Colombia: cuatro ciclos posteriores válidos del esquema vigente de 7. |
| APN-R03 | MITIGADO_EN_FUENTE / NO_HOMOLOGADO | Transmisión + NACHA/MFT/CENIT; archivos nunca recibidos siguen invisibles. |
| APN-R04 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Devolución tardía y estados terminales; falta concurrencia SQL. |
| APN-R05 | NO_EJECUTADO | .NET 10, xUnit, SQL Server/PostgreSQL, Quartz y UAT no disponibles. |
| APN-R06 | MITIGACIÓN_PARCIAL_EN_FUENTE | Job ahora expone saturación y contadores; no se midió latencia real cada 30 minutos. |
| APN-R07 | ABIERTO (EXTERNO) | Contrato de evidencia de entrega completa diseñado I11, sin productor certificado por cámara/ciclo. |
| APN-R08 | ABIERTO / MONITOREAR | Barreras conservadoras requieren resolución manual ante archivos/rechazos ambiguos y fallidos. |
| APN-R09 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Ventanas de recepción Colombia-local vs UTC I11. |
| APN-R10 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Sesiones ACHCOL canceladas no cuentan I11. |
| **APN-R11** | **CORREGIDO_EN_FUENTE / NO_HOMOLOGADO** | En I11 el selector no recogía intentos con `ResponseDeadlineAtUtc=null`; I12 los agrega para recálculo seguro. |
| **APN-R12** | **MITIGADO_EN_FUENTE / NO_HOMOLOGADO** | En I11 el prefijo fijo `Take(BatchSize*4)` podía relegar indefinidamente otros IDs; I12 incorpora paginación `keyset` y cursor durable. Riesgo de performance y SQL pendiente de pruebas. |

**No homologado:** no declarar PASS C#/SQL/UAT sin ejecución; el cierre de recepción externa continúa pendiente.
