# FinancialIntegrity multimotor CI

## Alcance

`FinancialPersistenceMigrationTests` valida con bases aisladas SQL Server y PostgreSQL la migración `EnforceFinancialIntegrity`, conservación exacta de datos/relaciones, rollback y rechazo de importes fuera de escala.

## Modos de ejecución

- CI: `FINANCIAL_INTEGRITY_REQUIRE_DATABASES=true`. La ausencia o fallo de cualquiera de las dos conexiones produce fallo de prueba.
- Local: sin esa variable y sin conexiones, las cuatro pruebas de base se omiten explícitamente mediante `FinancialIntegrityFact`; nunca retornan como aprobadas. Las pruebas de configuración permanecen ejecutables.

## Workflow

`.github/workflows/financial-integrity-multidb.yml` levanta SQL Server y PostgreSQL con imágenes fijadas por digest, espera healthchecks y puertos, ejecuta la categoría `FinancialIntegrity` y publica `financial-integrity-multidb-results` con TRX, lista de pruebas y resumen técnico. `FINANCIAL_INTEGRITY_EVIDENCE_PATH` registra conexión, recurso aislado, migración Up, invariancia, rollback, prueba fuera de escala y limpieza por proveedor.

## Hallazgos del JOB 2

- La etiqueta `Configuración de ciclos` es la etiqueta vigente en `MenuItemConfiguration` y `CyclesMenuSeeder`; el snapshot SQL Server sincronizado la refleja. No se modificó funcionalidad de menú.
- `SyncSqlServerFinancialIntegrityModel` conserva `Up`/`Down` vacíos porque sincroniza el snapshot SQL Server tras el cambio de modelo sin emitir DDL. Se mantiene para que `has-pending-model-changes` permanezca limpio y no se altera la secuencia histórica.

## Evidencia local

Con ambos contenedores locales disponibles, la categoría ejecutó 8 pruebas: 8 aprobadas, 0 omitidas y 0 fallidas. La evidencia por proveedor se conserva en el TRX y en el resumen generado por `FINANCIAL_INTEGRITY_EVIDENCE_PATH`. La ausencia de conexiones en modo local produjo 4 omisiones explícitas y 4 pruebas de configuración aprobadas; en modo obligatorio sin conexiones produjo 4 fallos explícitos.
