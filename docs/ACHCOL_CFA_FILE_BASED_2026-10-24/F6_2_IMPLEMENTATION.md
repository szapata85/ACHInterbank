# F6.2 — Operación, observabilidad y rollout/rollback controlado (2026-10-25)

**Estado:** `IMPLEMENTED_UNVERIFIED`. Base completa: `ACHInterbank_ACHCOL_CFA_F6_1_UNIFICADO.zip`. Entrega: `ACHInterbank_ACHCOL_CFA_F6_2_UNIFICADO.zip`. **No se habilitó `Live` ni se enviaron operaciones CFA.**

## Alcance concreto: sin sobreingeniería

Se reutilizó **GET `/incoming-nacha-command-center/observability/summary?windowHours=24`** (política existente `CommandCenterRead`). Se añadió el miembro aditivo `achColFileBased` a la respuesta actual, sin endpoint nuevo, sin tabla nueva, sin worker nuevo, sin cambios de esquema, y sin configuración adicional. La ventana de observación de este endpoint sigue limitada a **1–168 horas** y filtra filas de cola **por fecha de creación**; no representa por sí sola todo el histórico pendiente.

| Campo `achColFileBased` | Significado en la ventana | Uso operativo |
| --- | --- | --- |
| `totalItems` | Total de filas `SourceKind=AchColFileBased` | Volumen de entradas procesadas en la ventana |
| `pendingPreflight` | `Blocked` / `ACHCOL_DISPATCH_NOT_ENABLED_F33` | Falta de evaluación local F4.2 |
| `pendingContractHomologation` | `Blocked` / `ACHCOL_MAPPING_NOT_READY_F42` | Contexto elegible, **aún no autorizado para enviar** |
| `preflightRejected` | `Blocked` / `ACHCOL_PREFLIGHT_REJECTED_F42` | Revisar integridad/clasificación antes de actuar |
| `unexpectedNonBlocked` | Filas ACHCOL distintas de `Blocked` | **Alerta de seguridad: valor esperado 0** |
| `withAttempts` | Filas ACHCOL con `AttemptCount > 0` | **Alerta de conciliación: valor esperado 0** |

Los contadores usan el `queueSnapshot` que ya consultaba la pantalla: solo se proyectaron `SourceKind` y `AttemptCount` adicionales, sin consultas extras a la base y sin exponer identidades, montos, cuentas ni nombres. La respuesta existente `CycleBound` mantiene campos y agrupaciones previos.

## Cambios aplicados

1. `Application/ACH/Models/IncomingNachaCommandCenterDtos.cs`: nuevo record `IncomingNachaAchColOperationalKpiDto` y propiedad de solo lectura/init `AchColFileBased` en el DTO ya existente, sin cambiar su constructor posicional histórico.
2. `Persistence/ACH/Services/Implementation/IncomingNachaCommandCenterQueryService.cs`: cálculo de seis contadores en memoria con los datos ya recuperados de la ventana; no cambia estados ni ejecuta SOAP.
3. `tests/.../IncomingNachaCommandCenterServiceTests.cs`: casos C# de conteos para los tres bloqueos y para estado/intento anómalos; `CycleBound` queda fuera de métricas ACHCOL.
4. `tests/.../OpenApiDocumentGenerationTests.cs`: contrato esperado de respuesta de observabilidad actualizado con la propiedad aditiva.
5. `scripts/validate-achcol-f62-offline.py`: verificación estática reproducible de contratos, bloqueo y documentos. **No ejecuta pruebas C#.**
6. `F6_2_OPERATIONS_RUNBOOK.md`: operación y rollback documentados, sin nuevos servicios ni dashboards paralelos.

## Evidencia realmente ejecutada

- Auditoría Python `validate-achcol-f62-offline.py`: **PASS 10/10** en este entorno; verifica estructura/patrones estáticos, no semántica C#.
- Auditoría previa `validate-achcol-f61-offline.py`: **PASS 12/12** en este entorno; verificación offline, no compilación. Grafo de solución `validate-solution-projects.py`: **PASS 7/7**.
- Integridad ZIP y comparación con F6.1: **PASS CRC completo**, 8.126 entradas precedentes conservadas; **8.118** entradas sin cambio, **8** entradas modificadas y **3** agregadas; nuevo total **8.129**. SHA-256 del archivo entregado se informa tras actualización final de este reporte.
- **No ejecutado:** `dotnet build`, xUnit, SQL Server/PostgreSQL de verdad, CI, pruebas E2E CFA, validación XSD `xsd0/xsd1`, ensayos de rollback con datos reales. SDK 10.0.402 ausente en el contenedor.

## Guardas de despliegue y reversión

- **No activar `AchCol:AllowCycleIndependentInboundDispatch`** en un ambiente real hasta F6.3 + aprobación; su valor por defecto sigue `false`. Encenderlo hoy solo habilitaría la preparación local en F4.2 (no un transporte ACHCOL `Live`).
- El envío `Proc_Transacciones` para origen `AchColFileBased` sigue **no implementado/autorizado en Live**: el guard `CanTransmit=false` y la cola `Blocked` no son una aprobación de negocio.
- Antes de cualquier activación: build/xUnit/suite CI, migraciones F2.3/F3.1–F3.3 en **ambos** motores y pruebas de rollback sobre copia/snapshot; contrato CFA/XSD homologados, seguridad de sobres, UAT, idempotencia/multinstancia, conciliación y aprobación formal.
- Ante anomalía: mantener/apagar la feature flag ACHCOL, detener nuevas entradas del canal según procedimiento autorizado, **no** transformar filas bloqueadas a `Queued`, **no** usar `Down` de migraciones con filas ACHCOL, **no** reintentar resultados monetarios inciertos ni eliminar evidencia/auditoría. Escalar a conciliación con CFA.
- El endpoint de resumen es **read-only** y no sustituye el chequeo histórico de filas en DB, detallado en `F6_2_OPERATIONS_RUNBOOK.md`.

## Decisiones de F6.2

- **DEC-081:** Usar exclusivamente el endpoint de observabilidad ya disponible; ampliación aditiva, sin nueva infraestructura.
- **DEC-082:** Mostrar métricas de seguridad solo como conteos agregados por `SourceKind`, motivos de bloqueo y señales de intento inesperado. No generar logs ni reportes con datos bancarios.
- **DEC-083:** Un resumen limitado temporalmente **no** demuestra ausencia global de operaciones; antes de activar ejecutar consultas históricas read-only.
- **DEC-084:** Rollback operativo primario mediante flag apagado, preservación de filas/auditoría, con migraciones `Down` condicionadas y **nunca** usadas como reversión ciega.
- **DEC-085:** Mantener deuda F6.1 y contrato externo CFA pendientes; marcar F6.2 `IMPLEMENTED_UNVERIFIED`, seguir con F6.3 sin declarar producción certificada.

## Próximo paso F6.3

Ejecutar sobre ambiente habilitado la suite total y focal de C#, migraciones PostgreSQL/SQL Server, UAT por archivos `.OUT` / `.OUT.env`, colisiones, flujos CENIT/RET y homologación XSD/contrato CFA. Solicitar autorización separada antes de cualquier `Live`. Si un requisito externo falta, registrar `BLOCKED` **solo en la certificación externa**, sin falsear el resultado de las auditorías estáticas.
