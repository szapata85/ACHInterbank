# Matriz de casos — Fase 4

## Precondiciones comunes

Datos sintéticos con prefijo `UAT-F4-MON-SAL-`, migraciones EF Core en bases aisladas, API y SPA Docker reales, autenticación real y ningún endpoint SOAP Live. La misma preparación determinista se ejecutó en SQL Server y PostgreSQL.

Para cada caso se ejecutó el mismo procedimiento: preparar mediante el fixture EF Core, consultar conteos y relaciones, invocar listado/detalle con el servicio o la API, verificar la representación en la SPA mediante Playwright y repetir el conjunto crítico en el segundo proveedor. Las ejecuciones posteriores del fixture y el reinicio comprobaron idempotencia y persistencia.

| ID | Objetivo y dato | Persistencia observada | API observada | SPA/evidencia automatizada | Motores | Resultado |
|---|---|---|---|---|---|---|
| UAT-01 | Ciclo futuro `01-FUTURO` | Una salida determinada, ciclo 05/08/2026, sin archivo ni intento | `Scheduled`, fecha y siguiente paso | “Asignada a un ciclo futuro” | SQL/PG | Aprobado |
| UAT-02 | Espera `02-PENDIENTE` | Intento exitoso, sin respuesta correlacionada | `PendingResponse` | “Pendiente de respuesta de la cámara compensadora” | SQL/PG | Aprobado |
| UAT-03 | Éxito `03-ACEPTADA` | Evento de aceptación único | Resultado `Accepted` | Resultado aceptado y evento en seguimiento | SQL/PG | Aprobado |
| UAT-04 | Rechazo `04-RECHAZADA` | Intento funcional R01 con descripción contextual | `Rejected`, código y causal | Rechazo humanizado; filtro R01 devuelve la raíz exacta | SQL/PG | Aprobado |
| UAT-05 | Devolución `05-DEVUELTA` | Aceptación y devolución R01 ordenadas | `Accepted` + `ReturnedLater` simultáneos | Línea de tiempo conserva ambos hechos | SQL/PG | Aprobado |
| UAT-06 | Sin archivo `06-SIN-ARCHIVO` | Cero membresías | Archivo nulo | Mensaje explícito, sin archivo ajeno | SQL/PG | Aprobado |
| UAT-07 | Falla `07-ERROR-TECNICO` | Un intento DryRun TIMEOUT, cero respuestas/movimientos | `TechnicalError` + `NotDetermined` | Error técnico, nunca rechazo | SQL/PG | Aprobado |
| UAT-08 | Reintento `08-REINTENTO` | Una raíz, intentos 1 fallido y 2 exitoso | Trazabilidad preservada | Resultado posterior sin acción operativa | SQL/PG | Aprobado |
| UAT-09 | Filtros | Consulta sobre campos persistidos | Fechas, identificador/traza, cámara, ciclo, entidad, tipo, estados, importes y código admitidos; vacíos omitidos | Formularios reactivos, búsqueda y limpieza contra servidor | SQL/PG | Aprobado |
| UAT-10 | Paginación y orden | 34 salidas UAT determinadas más el caso base; histórica desconocida excluida | Cuatro páginas de 10, total y orden estable | Paginación de servidor y retorno desde detalle | SQL/PG | Aprobado |
| UAT-11 | Archivo `11-ARCHIVO-EXACTO` | Una membresía a `.001` v1; `.002` v2 pertenece a otra raíz | Solo `.001`, sin evidencia de transmisión | Versión 1 visible; versión 2 ausente | SQL/PG | Aprobado |
| UAT-12 | Tres perfiles | Grafo de permisos y menú idempotente | 403 sin permiso; detalle técnico condicionado | Menú/ruta protegidos y 4/4 Playwright por proveedor | SQL/PG | Aprobado |
| UAT-13 | Privacidad | Payloads del fixture vacíos; cuenta sintética solo persistida en servidor | DTO enmascara `******7890`, no entrega XML | DOM, consola, red y capturas sanitizados | SQL/PG | Aprobado |

El resultado esperado de cada fila fue el comportamiento canónico descrito; el resultado obtenido coincide. Cada afirmación corresponde a fixtures sintéticos y evidencia persistida, no a hechos financieros reales.
