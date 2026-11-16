# Autoverificación focal adversarial

**Concluida sólo a nivel fuente y modelo; no runtime:**

- Doble envío Quartz → CAS relacional y estado previo al transporte; falta carrera real en ambos proveedores.
- Timeout y SOAP Fault → estado incierto y ningún reintento automático.
- `Dispatching` estancado → cuarentena y reconciliación, incluso migración de históricos.
- Rollback respuesta prenotificación → transacción EF envolvente, ejecución xUnit/crash/BD pendientes.
- Axon duplicado → claim token antes del gateway; fake test creado; carrera SQL y conteo físico pendientes.
- Cancelación después de SOAP Axon → token independiente; excepción de persistencia secundaria sigue siendo riesgo operacional.
- Correlación → cámara acotada, duplicados ambiguos manual; fecha/archivo no inventados.
- Falso éxito SOAP → Envelope/Body/operation, `RTAACH` único y catálogo + parser; XSD ausente.
- Migrations → dos ensamblados, índices y backfill; verificación de `Down` en entorno controlado pendiente.
- Pruebas de regresión de 13 CENIT, hotfixes ACH y H02 no ejecutadas por SDK ausente.

**Resultado:** NO-GO. No marcar ningún NPT corregido y validado en ejecución.
