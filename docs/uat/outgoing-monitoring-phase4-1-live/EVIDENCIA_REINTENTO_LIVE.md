# Evidencia — reintento LIVE

## TX-C

- Raíces: 1 por motor.
- Intento inicial: 1, error técnico de transporte real.
- Backoff: respetado según `NextAttemptAtUtc` persistido.
- Reintento: 1 sobre la misma transacción.
- Intentos totales: 2.
- Respuesta final: SOAP real, interpretada y persistida.
- Respuestas exitosas: 1.
- Transacciones duplicadas: 0.
- Movimientos monetarios reales: 0.

La línea de tiempo y el detalle técnico autorizado muestran el intento inicial, el error, el intento posterior y el resultado. El monitor no incorpora acciones de reintento.
