# MEMORY STATE — APN-I10

**Fecha:** 2026-11-03. **Base recibida:** ZIP completo APN-I09. **Nueva base acumulativa:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I10_2026-11-03.zip`.

## Invariantes del proyecto

- **ACH Colombia: 7 ciclos**, política `ACHCOL-7C-20261014`. El manual V35 describe cinco ciclos históricos y NO reemplaza el calendario operativo `Nuevos Ciclos de Proceso y Horarios.pdf`.
- **CENIT: 5 ciclos**, quinta sesión hasta 18:45 y colocación de archivos de salida hasta **19:15**.
- Los intentos de prenotificación terminales conservan evidencia append-only; una devolución tardía requiere revisión y no una modificación silenciosa.
- El cierre de `AchCycle`, un ACK, un `NoActivity` o la ausencia de filas de ingreso **no son watermark de recepción completa**.
- Conservar `MEMORY.md`, `MEMORY_STATE.md` y los Memory State previos; anexar esta versión sin sobrescribir historia.
- Incremento `IMPLEMENTADO_EN_FUENTE` no equivale a `PRUEBAS_NET_APROBADAS` ni `HOMOLOGADO`.

## Estado acumulado

- APN-I01..I07: inventario/auditoría estática-documental; sin modificaciones productivas; pruebas runtime no ejecutadas.
- APN-I08: plazo por liberación real y calendario completo; corrección en fuente, sin .NET/UAT.
- APN-I09: transmisión obligatoria y barreras de registros NACHA conocidos, serialización del job; corrección en fuente, sin .NET/UAT.
- **APN-I10:** amplía barreras a MFT y CENIT, contempla ingestiones sin cámara y fallidas, manual review tardía, protege el servicio central y enruta rechazo CENIT de prenote mediante transición canónica. No migraciones/no nuevos estados.
- Pruebas estáticas APN-I08: 6/6; APN-I09: 8/8; APN-I10: 10/10. C# preparado, **NO_EJECUTADO** (SDK ausente). SQL/Quartz/UAT **NO_EJECUTADO**.

## Riesgos residuales y próximos pasos

- R01: mitigación parcial; falta cierre externo verificable.
- R02: corregido en fuente I08, no homologado.
- R03: mitigación ampliada I09/I10; faltan archivos aún no recibidos.
- R04: barrera y sincronización en fuente I10; requiere pruebas SQL/concurrencia.
- R05: pruebas .NET/SQL/UAT pendientes.
- R06: latencia de job cada 30 min pendiente de evaluar.
- R07: **ABIERTO** — no se dispone de señal normativa/técnica positiva que demuestre recepción de TODOS los archivos esperados por cámara/ciclo.

**Próximo trabajo sugerido:** APN-I11 para homologación de I08–I10 y diseño del contrato de cierre externo con evidencia operativa real. No inventar cierres ni declarar producción lista sin pruebas.
