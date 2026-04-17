# Validación relacional NACHA-M (SQL Server / PostgreSQL)

## PostgreSQL

```bash
psql "$POSTGRES_CONNECTION_STRING" -f database/scripts/20260417_incoming_nacha_ingestion_phase1.sql
psql "$POSTGRES_CONNECTION_STRING" -f database/scripts/20260417_incoming_nacha_dispatch_phase2.sql
```

Checks sugeridos:

```sql
-- unicidad idempotencia dispatch
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'IncomingNachaDispatchQueue';

-- conteo cardinalidades
SELECT
  (SELECT COUNT(*) FROM "IncomingNachaFileIngestions") AS ingestions,
  (SELECT COUNT(*) FROM "IncomingNachaEntryClassifications") AS classifications,
  (SELECT COUNT(*) FROM "IncomingNachaTransactionLinks") AS links,
  (SELECT COUNT(*) FROM "IncomingNachaDispatchQueue") AS queue,
  (SELECT COUNT(*) FROM "IncomingNachaIntegrationExecution") AS execs;
```

## SQL Server

```bash
sqlcmd -S "$SQLSERVER_HOST" -d "$SQLSERVER_DB" -U "$SQLSERVER_USER" -P "$SQLSERVER_PASSWORD" -i database/scripts/20260417_incoming_nacha_ingestion_phase1_sqlserver.sql
sqlcmd -S "$SQLSERVER_HOST" -d "$SQLSERVER_DB" -U "$SQLSERVER_USER" -P "$SQLSERVER_PASSWORD" -i database/scripts/20260417_incoming_nacha_dispatch_phase2_sqlserver.sql
```

Checks sugeridos:

```sql
SELECT name, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID('IncomingNachaDispatchQueue');

SELECT
  (SELECT COUNT(*) FROM IncomingNachaFileIngestions) AS ingestions,
  (SELECT COUNT(*) FROM IncomingNachaEntryClassifications) AS classifications,
  (SELECT COUNT(*) FROM IncomingNachaTransactionLinks) AS links,
  (SELECT COUNT(*) FROM IncomingNachaDispatchQueue) AS queue,
  (SELECT COUNT(*) FROM IncomingNachaIntegrationExecution) AS execs;
```

## Concurrencia mínima recomendada

1. Ejecutar dos workers del handler `IncomingNachaPostProcessing` en paralelo.
2. Verificar que no existan duplicados por `IdempotencyDispatchKey`.
3. Verificar que `AttemptCount`/`NextAttemptAtUtc` no diverjan entre workers.
