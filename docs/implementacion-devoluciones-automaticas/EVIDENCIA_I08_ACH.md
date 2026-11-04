# Evidencia I08-ACH — Comprobaciones de fuente y pendientes

- Revisión código sobre ZIP I07; modificación limitada a generación automática ACH y fixtures de prueba.
- Script I08: **25/25 PASS_STATIC** (fuente, no ejecución .NET).
- Revisión scripts previos: I02 18/18, I03 25/25, I05 33/33, I06 28/28, I07 24/24; I04 32/35 por controles históricos obsoletos.
- Solución: **7/7 referencias a proyectos existentes**, no implica compilación.
- El parámetro técnico permanece **false**; no se agregaron rutas de transporte ni SPA ni CENIT.
- `dotnet`: no instalado. **No se ejecutó** .NET, xUnit, SQLite, SQL Server, PostgreSQL ni la conciliación con evidencia externa.
- Integridad, diferencias exactas contra ZIP I07 y huella del ZIP I08 se registrarán en `MANIFIESTO_I08_ACH.json` y archivo SHA-256.

## Alcance de pruebas escritas (sin ejecutar)

- `I08_AutomaticGenerationDoesNotApproveDifferentArtifact` con cinco escenarios.
- `I08_AutomaticGenerationRequiresIntegrityServiceEvenWithBothGatesEnabled`.
- `I08_AutomaticGenerationStopsIfRebuildingFails`.
- `I08_CenitNeverCallsAutomaticReturnGeneratorOrRebuilder`.
- Dos fixtures positivos I06 actualizados.

## Observación de integridad financiera

Un evento `ReturnFileGenerated` y un estado `ReturnedByEpr` **no constituyen** prueba de transmisión/aceptación/compensación/abono. Los informes actuales cuentan transacciones en dicho estado, sin comprobar acuse de ACH. No se cambió su lógica transversal; se marca como bloqueo de salida a producción para I09.
