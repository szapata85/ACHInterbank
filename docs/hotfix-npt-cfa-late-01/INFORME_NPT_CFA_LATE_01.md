# Hotfix focal NPT-CFA-LATE-01 (P1) — 2026-11-15

## Alcance y defecto reproducido

El método `IncomingNachaPostProcessingPipeline.PreserveLateEvidenceAsync` volvía a insertar `IncomingNachaIntegrationExecution` tras perder el fencing de finalización. Si el intento ya estaba guardado, la inserción reutilizaba `(EntryDetailId, AttemptNumber)` y violaba el índice único. El error impedía conservar la respuesta SOAP tardía. La implementación anterior no probaba idempotencia entre instancias.

## Cambio en fuente

Único archivo productivo modificado: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`, método `PreserveLateEvidenceAsync`.

- No crea ni actualiza `IncomingNachaIntegrationExecution`. El intento anterior permanece inmutable.
- Crea un evento `NptLateOutcomeFenced` en **`IncomingNachaProcessingEvents` ya existente**, con ID SHA-256 truncado a UUID determinista sobre la identidad semántica de la respuesta. El payload `EvidenceJson` guarda identificadores, claim token, correlación, números de intento, XML de solicitud y respuesta, hashes, códigos, resultado, estados y fechas observadas.
- La clave no incorpora las horas de recepción o descripciones variables: duplicados del mismo resultado no crean nuevas filas. Una respuesta distinta (XML/códigos/estado/resultado) obtiene una clave diferente y no sobrescribe la primera.
- `SaveChangesAsync` escribe un único evento como operación atómica. Ante `DbUpdateException`, relee la clave y solo admite colisión si existe un evento del tipo correcto con la misma huella; de lo contrario vuelve a propagar el error. No se silencian errores de FK o indisponibilidad de BD.
- La instancia vieja ya ha descartado todos sus cambios rastreados en `FinalizeClaimAsync` tras perder el fencing. El hotfix no toca `QueueStatus`, `RequiresReconciliation`, `DispatchClaimToken`, `AttemptCount` ni llama a SOAP. Por tanto, archivar la respuesta **no confirma** la transacción ni provoca otro débito.
- No se alteran contratos, parser, mapper, migraciones, WSDL/XSD, NPT-01/02/05/14, otros servicios o dependencias NuGet.

**Transaccionalidad:** una inserción es atómica en SQL Server y PostgreSQL mediante el commit implícito del comando/`SaveChangesAsync`. No hay transacción larga alrededor de SOAP. No se añadió una tabla ni índice nuevo: el PK de eventos ya funciona como árbitro de concurrencia. El JSON existente no tiene longitud máxima configurada. El XML queda en la BD bajo los controles de acceso y retención existentes, no en logs de aplicación.

## Pruebas C# incorporadas, no ejecutadas

`tests/Cfa.ACHInterbank.Tests/NptCfaLateEvidenceTests.cs` contiene seis pruebas xUnit de EF Core SQLite relacional:

1. El intento persistido real **rechaza** la duplicación por índice único, pero la respuesta tardía crea un evento exitosamente.
2. Un intento con XML y código originales no pierde esos datos al llegar otra respuesta tardía.
3. Tres entregas idénticas con timestamps distintos generan un solo evento.
4. Tres workers simultáneos intentan archivar el mismo resultado; se espera un solo evento, sin mutar cola ni ejecución.
5. Dos XML distintos en el mismo intento conservan ambos eventos y la ejecución original.
6. Un trigger SQLite induce un fallo de inserción: se verifica rollback, ejecución histórica intacta y cola en conciliación.

Las pruebas usan una **ejecución realmente guardada** en la BD SQLite del fixture y el método `FinalizeClaimAsync` de producción, no se limitan a mocks. Se desactivan FK de entidades ajenas al grafo en el fixture para aislar este caso; pruebas SQL Server/PostgreSQL y FKs reales siguen siendo obligatorias.

## Resultados reales del ambiente

| Verificación | Resultado |
|---|---|
| Guard focal nuevo: inspección de fuente + modelo independiente de SQLite/Python | **15/15 PASS**; no ejecuta EF Core |
| Regresión `npt_focal_sqlite_source_guard.py` | **26/26 PASS** |
| Regresiones históricas E2E, H01/H02, RES-H02 y NACHA ACH Colombia | **5/5 scripts**, todos código 0 (31/31, 35/35, 39/39, 27/27, 28/28 comprobaciones de fuente/modelo) |
| Regresión NPT general heredada | **41/42 FAIL**, comprobación *Axon cancel persists independently* depende de un patrón anterior (`CommitAsync`), ya reemplazado antes de este hotfix; no se cambió el verificador |
| `dotnet --info`, `dotnet restore`, `dotnet build`, `dotnet test` | Todos **exit 127**, SDK no instalado: compilación y xUnit no ejecutados |
| SQL Server / PostgreSQL reales; simuladores SOAP instrumentados; UAT | No ejecutados |

Los logs y códigos de retorno están en `evidencias/`. No se declara cierre probado por .NET; estatus **Corregido en fuente pendiente de prueba**. **NO-GO SOAP Live/productivo**.

## Autoverificación adversarial

| Riesgo | Control y evidencia | Límite |
|---|---|---|
| Unicidad `(EntryDetailId,AttemptNumber)` | Ya no hay segundo INSERT en tabla de executions; caso xUnit de colisión real y modelo SQLite | xUnit pendiente SDK |
| Worker vencido sobrescribe conciliación | `FinalizeClaimAsync` exige token/lease/estado; ante pérdida `ChangeTracker.Clear()`, evento independiente | SQL Server/PostgreSQL pendientes |
| Respuesta tarde confirmada como éxito | Evento `RevisionManual` solamente; cola no modificada | Reconciliación bancaria siempre manual |
| Repetición de evidencia por carrera o reinicio | UUID derivado de hash, PK de eventos, reconsulta exacta ante choque | Certificación multinodo pendiente |
| Dos resultados distintos pierden evidencia | Identidad incorpora XML/códigos/resultado y crea dos eventos, sin actualización de uno anterior | Revisar política operativa de acceso a XML |
| Fallo de BD deja estado parcial | Un único INSERT y propagación de error si no hay copia válida | Corte abrupto anterior a la persistencia requiere conciliación externa |
| Retransmisión monetaria | El método no invoca transporte y no quita `RequiresReconciliation`; pruebas verifican bloqueo de claim | No se ejecutó SOAP real |

**Riesgos residuales:** confirmación en runtime, comportamiento de ambos proveedores bajo colisiones y permisos de acceso a XML archivado. Si un proceso termina físicamente antes de insertar evidencia, el resultado solo puede reconstruirse mediante conciliación externa. No se ha probado homologación o Live.

## Ruta para reauditoría

Ver `MATRIZ_PRUEBAS.md`, `AUTOVERIFICACION_ADVERSARIAL.md`, `COMANDOS_REPRODUCIBLES.md`, `INVENTARIO_CAMBIOS.md`. El ZIP completo conserva estructura, historial normativo y memorias previas.
