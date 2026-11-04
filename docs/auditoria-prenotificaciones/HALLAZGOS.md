# Hallazgos y riesgos APN — corte I09

| ID | Estado tras I09 | Observación / acción pendiente |
|---|---|---|
| APN-R01 | MITIGACIÓN_PARCIAL_EN_FUENTE | Se recalcula el fin de liberación; sin cierre operativo positivo de ingestión, no certificar ausencia de devolución. |
| APN-R02 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | En ACH Colombia se requieren cuatro ciclos posteriores completos. |
| APN-R03 | MITIGACIÓN_PARCIAL_EN_FUENTE | Job exige transmisión, revalida deadline y bloquea archivos conocidos pendientes; falta watermark de última entrega por cámara/ciclo. |
| APN-R04 | ABIERTO | Devoluciones tardías tras aceptación tácita pueden divergir entre intento, validación y estado del tercero; requiere carrera reproducible. |
| APN-R05 | NO_EJECUTADO | SDK .NET/BD/archivos reales/UAT no disponibles; no afirmar PASS funcional. |
| APN-R06 | ABIERTO | Frecuencia del job 30 min; medir resolución respecto del SLA exigido. |
| APN-R07 | NUEVO_RIESGO_ESTATICO | No existe evidencia explícita de cierre de recolección de todos los archivos de la cámara. Los archivos aún no recibidos o sin cámara identificada no pueden bloquearse por consulta a ingresiones conocidas. |

La prioridad de R07 es potencialmente alta hasta comprobar el contrato real de recepción. Ningún riesgo se declara definitivamente resuelto sin evidencia operacional.
