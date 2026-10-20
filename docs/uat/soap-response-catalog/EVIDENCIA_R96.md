# Evidencia R96

Fecha: 2026-07-16. Entorno local controlado; datos sintéticos y evidencia anonimizada.

## Resultado persistido

Una única invocación real local de `Proc_Contrapartidas` produjo y persistió:

- `ResponseCode = R96`;
- descripción: “Débito aplicado correctamente”;
- `ResponseCatalogId` presente;
- `TransportStatus = Succeeded`;
- `BusinessStatus = Success`;
- `RetryAllowed = false`;
- `RequiresManualReview = false`;
- un único intento y una única ejecución vinculados a la transacción sintética.

La resolución fue realizada por el catálogo existente. No se modificaron tabla, bootstrap, resolutor, seed ni migraciones R96.

## Recuperación segura posterior al dispatch

La primera corrida Playwright completó el dispatch y falló después, al interpretar como UTF-8 la salida CP850 del `sqlcmd` legacy de Windows. Conforme a la regla de una llamada por TransactionId, no se repitió el movimiento. El helper ahora decodifica UTF-16, UTF-8 válido o CP850 configurable.

El mismo escenario se reanudó sólo para consultas y quedó verde:

- correlación por `TransactionId`, `DispatchAttemptId`, `DispatchItemId`, `DispatchBatchId` y `ResponseCatalogId`;
- base, API y panel Angular validados;
- Playwright read-only aprobado;
- segundo dispatch rechazado con el código de duplicidad antes del transporte;
- conteo de intentos conservado en uno;
- log WCF sin cambios durante el gate de duplicidad.

Tras reiniciar la API, R96, la descripción, el catálogo, los estados y el intento continuaron consultables sin reinterpretar XML y sin nuevo dispatch.

## Pruebas finales

| Validación | Resultado |
| --- | --- |
| Backend dirigido | 28/28 |
| Backend completo | 1.828 aprobadas, 1 omitida diagnóstica |
| Angular | 395/395 |
| Playwright smoke | 1/1 |
| Playwright LIVE inicial | dispatch completado; falla posterior de decodificación |
| Playwright reanudado read-only | 1/1 |
| Playwright tras reinicio read-only | 1/1 |

No se documentan identificadores, cuentas, payloads, tokens ni credenciales.
