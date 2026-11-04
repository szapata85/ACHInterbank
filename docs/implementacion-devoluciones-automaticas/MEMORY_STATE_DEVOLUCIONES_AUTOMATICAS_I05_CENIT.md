# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I05-CENIT

**Corte:** 2026-11-03. **Incremento:** I05-CENIT, generación local controlada NACHA-M, sin transmisión. **Base exclusiva:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I04_CENIT_2026-11-03.zip`, SHA-256 `6b6bfeb57ede872f8c0dcde8aa2cd02fd038331123c5653c17fe9fbeb2b6e23b`. **ZIP final:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I05_CENIT_2026-11-03.zip`. **Estado:** incorporado al código, no compilado/homologado. **NO-GO PRODUCTIVO.**

## Base, integridad y memorias

El ZIP I04 contiene 4.918 archivos, prueba CRC sin entradas defectuosas. Se leyeron `MEMORY.md`, `MEMORY_STATE.md`, `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md`, la memoria I04 y el plan I04. Se contrastaron controles reales de I02–I04, generador común `INachaFileBuilder`/`NachaFileGenerationCoordinator`, perfil `CenitReturnOut2026Layout`, modelos de transacciones y vinculaciones NACHA originales, seguridad, auditoría y secuencias. **No se utilizó ningún ZIP anterior como base.** Los históricos no se sobreescribieron.

## Resultado exacto

I05 añade `ICenitReturnArtifactService`, `CenitReturnArtifactService`, DTOs, `CenitReturnArtifactController`, migraciones reversibles SQL Server/PostgreSQL y pruebas xUnit. Solo un operador autenticado con `ConfigManage` solicita **POST** `/api/cenit/devoluciones-automaticas/artefactos/{id}/generar-local`, partiendo de una reserva **APROBADA_SIN_ENVIO** por otra persona. Revalida la causal funcional, el modo CENIT, ventana, fecha valor, ciclo y ausencia de devolución financiera; acredita en auditoría el preparador distinto del revisor y del generador; consulta vínculo único original NACHA-M CENIT y exige consistencia de DFI, cuenta, valor, código, secuencia y perfil PPD/CCD.

Reutiliza **solo infraestructura común**: generador físico Opción C `INachaFileBuilder` con `ClearingHouseCode=CENIT`, perfil `OFFICIAL_CENIT_SALIDA_DEVOLUCION_V2026_1_0`, versión `2026-05-07`, `PersistAudit=false`; reserva `IAchReturnTraceSequenceService`; `AchDbContext`/`AuditLogs` y DataProtection con llaves persistidas. Valida estructura fija 106 × 10, tipos 1/5/6/7/8/9 y adenda 99, secuencias y campos fuente. El contenido y SHA-256 quedan ligados a la reserva; la copia se almacena como **texto cifrado**. Estado terminal **GENERADA_SIN_ENVIO**, descarga manual protegida `ConfigManage` en `GET /api/cenit/devoluciones-automaticas/artefactos/{id}/descargar-local`, solo mientras modo/ventana sean válidos; metadatos por `ConfigRead`. Nombre interno **NO_TRANSMITIR_CENIT_I05_*.txt**. Sin identificador ni nombre externo de CENIT, sin canal bancario. No inserta `AchReturnGenerated` ni toca saldos, transacciones, reporte financiero, jobs o SPA.

**Límites explícitos:** se admite un único PPD/CCD ordinario por reserva/artefacto; CTX y otros servicios no habilitados; R06, R23, R34, devoluciones de devoluciones R60–R69, prenotificaciones y plazos extraordinarios mantienen tratamiento manual/pendiente. `AUTOMATICO` NO dispara ejecución autónoma en I05: el POST sigue siendo manual/autorizado. No se implementan envío, publicación, acuse ni conciliación hasta I06. La generación local NO demuestra archivo bancariamente enviable.

## Rutas modificadas

- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnReviewService.cs` (se expone **internamente** la validación I04, sin cambiar su API)
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`
- Snapshots de SQL Server y PostgreSQL, siempre en el esquema existente.

## Rutas nuevas

- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnArtifactService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnArtifactModels.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnArtifactService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/CenitReturnArtifactController.cs`
- Migraciones `20261103190000_CenitReturnArtifact2026.cs` (SQL Server/PostgreSQL)
- `tests/Cfa.ACHInterbank.Tests/CenitReturnArtifactI05Tests.cs`
- `scripts/test/automatic_returns_i05_cenit_static_check.py`
- Documentación y evidencias I05: este MEMORY, INFORME, EVIDENCIA, PLAN, inventario/manifiesto/verificación y resultados estáticos.

## Evidencia ejecutada / no ejecutada

- **Sí:** CRC ZIP base, estática I05 **84/84**, I01–I04 CENIT **29/29, 50/50, 57/57, 71/71**, grafo de proyectos **7/7**, regressiones estáticas ACH I02 18/18, I03 25/25, I05 33/33, I06 28/28, I07 24/24, I08 25/25; comparar hash archivo-por-archivo original/final y CRC ZIP final según evidencia.
- **Histórico abierto ACH I04:** **32/35**, NO corregido ni aprobado.
- **NO ejecutado:** SDK .NET 10, `dotnet restore/build/test` y xUnit, aplicaciones EF/migraciones SQL reales, prueba de dos operadores, integración HTTP/JWT, reconstructor real de archivos con perfil publicado, golden files bancarios, transporte, acuse, conciliación, IAM real, homologación.

## Riesgos y decisiones no resueltas

El control de concurrencia relacional, transacciones y reversión de secuencias exige pruebas SQL Server/PostgreSQL. Existe posibilidad de competencia con el flujo manual histórico CENIT (`AchReturnsGenerated`), no certificada; I06 deberá evaluar coordinación global sin alterar ACH Colombia. La persistencia de DataProtection requiere disponibilidad y restauración de claves para acceder a contenido histórico. La huella y el nombre NO_TRANSMITIR son para revisión local y **no** sustituyen naming externo único ni la homologación. La evidencia 1/5/6 puede estar ausente en registros históricos; se bloquean en lugar de fabricar datos. La configuración `DESACTIVADO` impide generar y descargar contenido nuevo desde esta ruta; NO modifica procedimientos manuales heredados.

## Estado y punto de parada

| Cámara | En código | Pendiente del plan | Productivo |
|---|---|---|---|
| CENIT | **I01–I05 (5/6)** | **I06 (1 incremento)** | **NO-GO** |
| ACH Colombia | I01–I08, sin cambios en I05 CENIT | I09-B obligatorio; I09-A si es necesario | **NO-GO** |

**Faltan 2 fases obligatorias** en ambos planes juntos (I06-CENIT e I09-B-ACH) y **0–1 fase opcional** ACH I09-A, más correcciones que revele la ejecución. **Parar en I05-CENIT.** Próximo: I06-CENIT, integración controlada, acuses, conciliación, prueba relacional y homologación; no comenzar sin orden nueva. Nunca habilitar transporte productivo por completar I05.
