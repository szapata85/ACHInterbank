# HOTFIX E2E-01 / E2E-02 — ACH Interbank (2026-11-15)

**Fuente única:** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_CONTEOS_NACHA_T6_2026-11-15.zip` · SHA-256 `60a4f6e627c8dd62ea271af84c80a776c903d966a9626c25508a7cb8e6b7e8a0`. Versión completa de los cuatro componentes del ZIP de origen, sin reempaquetar o duplicar los documentos normativos.

**Estado verificable:** `IMPLEMENTADO_EN_CODIGO_PENDIENTE_COMPILACION_XUNIT_INTEGRACION_DB`. **GO / homologación: NO-GO; UAT NO EJECUTADA.** Se mantienen las correcciones anteriores I00–I13 y ACH Colombia NACHA/D01/D02/D04/R10 y ROR/C5 CENIT; no se abren E2E-03 a E2E-07.

## E2E-01 — Restricción real + recuperación idempotente

La instrucción recibida se identifica mediante ID externo (o referencia heredada) por entidad financiera originadora. La firma SHA-256 de la instrucción contiene los campos monetarios, ruta/destino lógico, cuentas, beneficiario, identificación y adendas ordenadas; no incorpora el ciclo, pues el reintento puede llegar a un ciclo posterior. La API detecta un registro preexistente **antes de modificar clientes, colas o previsualizaciones** y vuelve a comprobar dentro de la transacción EF. Si la firma coincide devuelve la transacción persistida sin crear otra, si difiere responde **409**; si la instrucción histórica no dispone de firma completa, falla cerrado con conciliación, sin adivinar identidad.

El índice **único filtrado** por `(SourceInstitutionId, TransactionExternalId)` es la autoridad de concurrencia real para PostgreSQL y SQL Server; colisiones de escritura se capturan, revierten y reconsultan. La migración aborta con error controlado si hay duplicados históricos con ID no vacío; no borra nada. Los registros históricos con ID externo vacío quedan fuera del índice; deben revisarse por referencia en caso de recibir un reintento.

## E2E-02 — Lease + corte persistido antes del SOAP

El reclamo en `ReportingContrapartida` o `Retrying` se realiza con una transición SQL condicional, un token de lease y una expiración; afecta la ruta programada y la ruta manual. El worker construye/mapea primero, y **persistentemente marca InFlight inmediatamente antes del transporte SOAP**. Al recuperar un lease vencido:

- `Preparing` (no se habilitó transporte): `RetryPending` con intento/auditoría sintética y control de máximo de intentos.
- `InFlight` o registro heredado de origen incierto: `ContrapartidaReportFailed`, `RequiresReconciliation=true`, auditoría y bloqueo de reenvío, aun si el worker original reaparece.

El scanner corre en Quartz aunque el calendario no presente ciclo operativo. Si hay múltiples huérfanas, procesa un máximo de cien por invocación y continúa en la siguiente; el operador debe monitorizar el job, ya que sin scheduler operativo no existe recuperación periódica. Se requiere conciliación externa para cualquier resultado desconocido; **no hay cancelación ni re-débito automáticos**.

### Límite temporal conocido

La separación de `Preparing` / `InFlight` es conservadora: una caída después de marcar `InFlight` pero antes de abrir red también solicita conciliación. Un transporte Live con falla/timeout o código de negocio desconocido no es reintentado automáticamente. La seguridad depende de desplegar conjuntamente aplicación + migración, operar el scheduler y no habilitar scripts externos de retransmisión fuera de esta política.

## Evidencias efectivas

- **35/35** controles estáticos focales y simulación del esquema SQL equivalente en SQLite, ejecutados; **no constituyen prueba del código C# con EF**. Ver `evidencias/E2E01_E2E02_STATIC_AND_SQLITE_MODEL.txt`.
- **Nueve** scripts existentes de regresión estática ACH/CENIT ejecutados con salida cero y registros individuales `REGRESSION_01.txt` a `REGRESSION_09.txt`.
- SDK `dotnet` **no instalado**: la orden `dotnet --info`, `dotnet build` y `dotnet test` retorna **127**, sin ejecutar compilación ni pruebas xUnit. `evidencias/DOTNET_ATTEMPTS_NOT_RUN.txt`.
- Se agregaron escenarios xUnit para idempotencia, conflicto, concurrencia con índice relacional, recuperación de lease, reinicio, conflicto de workers, corte SOAP y no reenvío; **0/escenarios nuevos ejecutados**, por el entorno.
- PostgreSQL y SQL Server físicos: **no conectados ni migrados**. Duplicados históricos reales no consultados. Integración SOAP Live y UAT **no ejecutadas**.

## Validación trazada de extremo a extremo

`HTTP → TransactionIdempotency.FindAsync → TransactionPolicyService → EF transaction → TransactionPersister + fingerprint → outbox/cola persistida → ContrapartidaDispatchJobService (claim Prepare) → MarkBeforeTransportAsync (InFlight) → SOAP → estado/auditoría persistidos → Quartz RecoverStaleAsync (sin reenvío cuando InFlight)`.

**Revisión estática:** los puntos enumerados tienen controles de código trazados. **No declarar aceptación end-to-end en ejecución** antes de compilar, ejecutar xUnit y reproducir caídas/competencia con los dos motores de base de datos y una interfaz SOAP de prueba que observe conteos de envío.

## Ejecución para la compuerta G1/G2 (no UAT)

Desde `ACHInterbank_ajustado/` en Visual Studio 2026 + .NET 10 con dependencias restauradas y bases de ensayo preparadas:

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --configuration Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --configuration Release --filter 'FullyQualifiedName~E2E01|FullyQualifiedName~E2E02|FullyQualifiedName~AchTransactionNachaTests|FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~TransactionPolicyServiceTests' --logger 'trx;LogFileName=E2E01_E2E02.trx'
dotnet test ACHInterbank.sln --configuration Release
```

Ejecutar una **consulta read-only** de duplicados en ambas bases **antes** de migrar:

```sql
SELECT SourceInstitutionId, TransactionExternalId, COUNT(*) AS Total
FROM AchTransactions
WHERE TransactionExternalId IS NOT NULL AND TransactionExternalId <> ''
GROUP BY SourceInstitutionId, TransactionExternalId
HAVING COUNT(*) > 1;
```

Con duplicados, detener y conciliar manualmente sin borrado automático. Ejecutar migraciones EF y transacciones concurrentes reales en PostgreSQL y SQL Server usando los procedimientos existentes del proyecto; validar failover antes de SOAP, después del corte `InFlight`, durante timeout y al reiniciar Quartz; comprobar que se generó **un solo ítem/dispatch** por instrucción y que los inciertos no hacen llamadas SOAP. No tocar datos de producción hasta pruebas y procedimientos de rollback aprobados.

Consulte también `DESPLIEGUE_SUPERVISION.md` y `ARCHIVOS_MODIFICADOS.md`. UAT queda para la etapa final, no para este hotfix.
