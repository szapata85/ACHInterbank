# JOB 3 — Idempotencia DB-first de devoluciones inbound

Estado: B2 PARCIAL: cerrada a nivel de código y pruebas focalizadas; falta ejecución provider-specific.

- Identidad: `SHA-256(incoming-return-v2 | clearingHouseId | achTransactionId | originalTrace | causal)`.
- Persistencia: `AchTransactionStateEvent`, que conserva transición, estado previo, causal, cámara, payload y clave.
- Restricción: `UX_AchTransactionStateEvents_IdempotencyKey`, índice único filtrado para claves no nulas; ya existía en el modelo y migraciones SQL Server/PostgreSQL, por lo que no se creó migración nueva.
- Atomicidad: la transición y el evento funcional se guardan en el mismo `SaveChangesAsync`; una colisión única esperable se devuelve como resultado idempotente y otros `DbUpdateException` no se ocultan.
- Pruebas: replay con metadatos de transporte distintos, dos `DbContext` y restricción única en SQLite relacional. La concurrencia de proveedor específico requiere ejecución contra los harnesses SQL Server/PostgreSQL existentes.
- Huérfanas/ambiguas: permanecen sin transición y sin identidad de aplicación exitosa; B6 sigue pendiente como workflow operativo.

Fuera de alcance: lifecycle saliente, SOAP, ROR, simulador y conciliación.
