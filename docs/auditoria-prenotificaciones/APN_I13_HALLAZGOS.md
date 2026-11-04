# Registro consolidado — APN-I13

No se identificó en I13 un nuevo defecto productivo que justificara parche. Los riesgos de I12 permanecen y se clasifican sin falsos cierres.

| ID | Estado I13 | Observación |
|---|---|---|
| APN-R01 | MITIGACIÓN_PARCIAL / NO_HOMOLOGADO | Espera salida; falta comprobante auténtico de fin de recepción. |
| APN-R02 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | ACHCOL 7 ciclos, cuatro posteriores válidos. |
| APN-R03 | MITIGACIÓN_PARCIAL / NO_HOMOLOGADO | Transmisión y barreras NACHA/MFT/CENIT; sin visibilidad de archivo no entregado. |
| APN-R04 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Estados terminales; carrera SQL no ejecutada. |
| APN-R05 | ABIERTO / NO_EJECUTADO | Compilación .NET, EF Core relacional, Quartz, UAT. |
| APN-R06 | MITIGACIÓN_PARCIAL / NO_HOMOLOGADO | Contadores y saturación, sin medición runtime. |
| APN-R07 | ABIERTO EXTERNO | Constancia externa de entrega completa sin productor. |
| APN-R08 | ABIERTO OPERATIVO | Conciliación manual de ambiguos/fallidos. |
| APN-R09 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Ventanas UTC y Bogotá. |
| APN-R10 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Excluir ciclos cancelados. |
| APN-R11 | CORREGIDO_EN_FUENTE / NO_HOMOLOGADO | Revisión de plazos nulos. |
| APN-R12 | MITIGADO_EN_FUENTE / NO_HOMOLOGADO | Cursor y escaneo estable; en I13 se agregaron tests relacionales sin alterar lógica. |
