# Evidencia SPA del resultado del core

## Implementación

La lista de transacciones permite seleccionar una fila y consulta `GET /Transactions/{id}/integration-result`. El panel **RESULTADO DEL PROCESAMIENTO EN EL CORE** presenta:

- servicio;
- resultado funcional;
- código;
- descripción;
- fecha;
- intento;
- estado de transacción;
- estado técnico cuando corresponde.

La semántica visual depende de `BusinessStatus`; no existe comparación Angular contra `R96`. El DTO no expone XML, JSON crudo, request ni response.

## Pruebas

Las pruebas del componente cubren R96 para débito y crédito, `Success`, `Rejected`, fallo técnico, `PendingCatalog`, `ManualReview`, etiquetas en español y ausencia de payload. Resultado Angular completo: 395/395.

La persistencia R96 fue consultada correctamente por API después de reiniciar el backend. No se obtuvo una aserción Playwright final del panel porque el flujo LIVE no puede repetirse después del intento monetario ya registrado. Esta evidencia visual runtime queda pendiente y bloquea GO productivo.

