# Hallazgos APN — Corte I11

| ID | Estado tras I11 | Riesgo / evidencia |
|---|---|---|
| APN-R01 | MITIGACIÓN_PARCIAL_EN_FUENTE | Se usa hora final de liberación y barreras de ingreso, pero falta comprobante positivo de última entrega. |
| APN-R02 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | Cuatro ciclos posteriores completos ACH Colombia. I11 excluye sesiones canceladas. |
| APN-R03 | MITIGACIÓN_AMPLIADA_EN_FUENTE | Se exige transmisión y consulta NACHA, MFT, gateway CENIT; no cubre archivos aún no recibidos. |
| APN-R04 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | Devolución tardía no reescribe intentos terminales, vía canónica CENIT; faltan pruebas SQL/concurrencia. |
| APN-R05 | NO_EJECUTADO | SDK .NET, SQL Server/PostgreSQL, Quartz y UAT no disponibles. |
| APN-R06 | ABIERTO | Job Quartz 30 minutos: cuantificar SLA y latencia real. |
| APN-R07 | ABIERTO | Falta un proveedor validado de evidencia positiva de archivos de salida completos por cámara/ciclo. Contrato diseñado, no integrado. |
| APN-R08 | ABIERTO / MONITOREAR | Barreras conservadoras retienen aceptación si hay registros fallidos/ambiguos hasta resolución autorizada. |
| APN-R09 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | Límite UTC calculado erróneamente desde día UTC en vez de fecha operacional America/Bogota; se corrige ventana NACHA/MFT/CENIT y exclusión CENIT por ciclo de origen. |
| APN-R10 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | Ciclos ACH Colombia cancelados eran contados dentro de la ventana de cuatro ciclos; ahora se excluyen y se bloquea origen cancelado. |

**No se certificó que el sistema sea seguro para producción.** Todo hallazgo corregido requiere compilación y pruebas integradas para cierre final.
