# I03-ACH — Preparación segura e idempotencia de devoluciones

**Cámara:** ACH Colombia exclusivamente (`ACHCOL`). **Fecha del corte:** 2026-11-02.  
**Base:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_ACHCOLOMBIA_2026-11-02.zip`.  
**Resultado:** código y migración incorporados; **sin validación de ejecución .NET ni SQL Server**. **Producción:** NO-GO.

## Resultado funcional concreto

1. El detector I02 sigue registrando rechazos funcionales confirmados provenientes de `Proc_Transacciones` y los separa de incidencias técnicas o con causal pendiente.
2. Tras persistirse el resultado del núcleo en el procesador (`IncomingNachaPostProcessingPipeline`), I03 explora de forma acotada las incidencias ACH Colombia verificadas y prepara candidatas solo si la configuración I01 es `CON_APROBACION` o `AUTOMATICO`.
3. La nueva tabla **`AchColombiaReturnPreparations`** guarda `OriginalTransactionId`, `SourceEventId`, `OriginalCycleId`, `SuggestedReturnCycleId`, `ReasonCode`, `Status` y `PreparedAtUtc`. Dos índices únicos impiden una segunda candidata del mismo original o reutilizar el mismo evento, incluso entre instancias concurrentes bajo SQL Server.
4. Se comprueba que la transacción pertenece a ACHCOL, que la incidencia proviene del detector I02, que su causa está verificada, que corresponde a una ejecución funcional exitosa en transporte pero rechazada por core, y que la causal pertenece a los catálogos correspondientes a **esa cámara y ese tipo**.
5. La ventana usa ciclos persistidos y zona de Bogotá, con secuencia por **número de ciclo**, no por hora de apertura, porque el ciclo 1 puede cruzar la medianoche. Cuando el ciclo declara la política confirmada de siete ciclos se contrasta además con `RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate.ReturnWindows`, conservado por la política confirmada. Un ciclo no disponible, cancelado o no habilitado no se inventa.
6. Si las cuatro oportunidades posteriores se encuentran vencidas con evidencia de agenda completa: un crédito monetario queda en `REVISION_TARDIA_CREDITO` **sin ciclo sugerido ni permiso de generación**; un débito o una prenotificación no genera candidata tardía. Los créditos tardíos podrán requerir el Esquema de Calidad, revisión de causales y validación manual: **I03 no lo resuelve**.
7. El modo `AUTOMATICO` en I01 no habilita el generador ni la transmisión: ambos modos activos producen únicamente `PREPARADA_SIN_ENVIO` para las candidatas que cumplan validaciones. `ProcessingConnected=false` no cambia.

## Límites deliberados para evitar sobreingeniería y efectos financieros

- **No se invoca** `AchReturnsService.GenerateReturnsFileAsync`, servicio de transporte, planilla de compensación, contabilidad, ni transiciones financieras.
- **No se cambia** el flujo de generación manual, recepción de devoluciones ni CENIT. La SPA sigue intacta.
- Se reutiliza `IncomingNachaProcessingEvents`, catálogos de causales, ciclo operativo persistido y configuraciones I01; se crea **una única tabla** porque los eventos de auditoría no permiten reservar estado de forma transaccional con unicidad propia.
- No se crea endpoint de usuario en I03. La revisión y aprobación se incorporará en I04 con Angular Material.
- La restricción única de preparaciones **no sustituye** la restricción existente de `AchReturnsGenerated`; una generación manual concurrente con la reserva se reconciliará en I04 antes de aprobar. Revalidar plazo/cámara/causa al consumo: el ciclo sugerido no es autorización de envío.
- La migración entregada es **SQL Server** (el mismo proveedor del incremento I01). La paridad PostgreSQL de I01/I03 permanece pendiente y no debe afirmarse como lista para desplegar.
- El escáner I03 es de lotes acotados; una incidencia no elegible puede quedar sin candidata. No implica éxito ni se pierde el evento I02.

## Archivos de implementación

- `src/Cfa.ACHInterbank.Domain/Models/ACH/AchColombiaReturnPreparation.cs` — registro persistente sin estado financiero.
- `src/Cfa.ACHInterbank.Persistence/Configuration/AchColombiaReturnPreparationConfiguration.cs` — dos índices únicos y relaciones restrictivas.
- `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs` — colección EF.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnPreparationService.cs` — verificación, preparación y ventana.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` — llamada después de guardar el resultado funcional, con aislamiento de fallos de preparación.
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261102230000_AchColombiaReturnPreparationI03.cs` y `AchDbContextModelSnapshot.cs` — esquema SQL Server.
- `tests/Cfa.ACHInterbank.Tests/AchColombiaReturnPreparationI03Tests.cs` — casos de crédito, débito, prenotificación, siete ciclos, modo, CENIT, antigüedad, duplicado y no generación.
- `scripts/test/automatic_returns_i03_ach_static_check.py` — validaciones estáticas reproducibles.

## Validaciones ejecutadas y pendientes

| Evidencia | Resultado |
|---|---|
| Grafo de solución (7 proyectos) | **PASS** |
| Verificador estático I02 | **18/18 PASS_STATIC** |
| Verificador estático I03 | **25/25 PASS_STATIC** |
| xUnit I03 | **NO EJECUTADO: SDK .NET no disponible** |
| `dotnet build` | **NO EJECUTADO: SDK .NET no disponible** |
| Migración SQL Server real/rollback | **NO EJECUTADO: no hay SQL Server** |
| Concurrencia SQL Server, SOAP, UAT | **NO EJECUTADO** |
| Validación real de ventanas ACH y causales en ambiente | **PENDIENTE** |

**Punto de parada:** I03-ACH finalizado en fuente, sin homologación runtime. Próximo autorizado **I04-ACH**: vista de candidatas y aprobación manual en SPA con Angular Material, permisos, vencimiento y reconciliación con generación manual. CENIT no iniciado. **NO-GO productivo.**
