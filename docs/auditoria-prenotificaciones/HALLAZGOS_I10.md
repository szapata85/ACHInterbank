# Hallazgos APN — Corte I10

| ID | Estado tras I10 | Justificación/pendiente |
|---|---|---|
| APN-R01 | MITIGACIÓN_PARCIAL_EN_FUENTE | Vencimiento por hora de liberación + barreras; sin demostración de última entrega real. |
| APN-R02 | CORREGIDO_EN_FUENTE_NO_HOMOLOGADO | Se requieren 4 ciclos posteriores completos ACH Colombia 7C (I08). |
| APN-R03 | MITIGACIÓN_AMPLIADA_EN_FUENTE | I09/I10 revalidan plazo, transmisión y fuentes NACHA/MFT/CENIT; aún no se detectan archivos no recibidos. |
| APN-R04 | BARRERA_CORREGIDA_EN_FUENTE_NO_HOMOLOGADA | Enrutamiento canónico CENIT, guardia late return/manual y bloqueo de resoluciones terminales contradictorias; falta SQL/UAT. |
| APN-R05 | NO_EJECUTADO | SDK .NET, SQL Server/PostgreSQL, concurrencia/UAT no disponibles en la ejecución actual. |
| APN-R06 | ABIERTO | Evaluar periodicidad 30 minutos vs. SLA. |
| APN-R07 | ABIERTO / ALTO POTENCIAL | No existe prueba positiva de recepción final de todos los archivos por cámara/ciclo. ACK, NoActivity y cierre de ciclo no la sustituyen. |
| APN-R08 | NUEVO RIESGO DE OPERACIÓN | Guardias conservadoras pueden retener aceptación tácita ante archivos MFT fallidos, ingestiones bloqueadas o respuestas ambiguas; requieren gestión/alertas y eventual cierre autorizado. |

No se declara ningún hallazgo definitivamente homologado con pruebas runtime.
