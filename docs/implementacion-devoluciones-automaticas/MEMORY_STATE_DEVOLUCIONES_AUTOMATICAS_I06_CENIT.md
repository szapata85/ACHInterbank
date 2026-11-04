# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I06-CENIT

**Corte:** 2026-11-03. **Incremento trabajado:** I06-CENIT (integración/conciliación: **subconjunto seguro y diagnóstico**, no ejecución bancaria). **Base EXCLUSIVA:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I05_CENIT_2026-11-03.zip`, SHA-256 `bddb26b63c1546edfdc03e72373a273823563e378d40435a751f80fd6f0c606c`. **ZIP final esperado:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_CENIT_2026-11-03.zip`. **Estado funcional integral I06:** PARCIAL, BLOQUEADO para transporte real y conciliación financiera. **Producción:** NO-GO.

## Lectura efectiva de la base

Se verificaron integridad CRC de 4.934 entradas ZIP y estructura real de siete proyectos. Se leyeron `MEMORY.md`, `MEMORY_STATE.md`, `AGENTS.md`, memoria y plan I05 de esta iniciativa. Se contrastaron los servicios I03–I05, perfiles NACHA-M CENIT, controlador I05, `CenitReturnPreparation`, `AchReturnGenerated`, `AchFileExport`, `AchFileExportTransaction`, `CenitChamberResponse`, `AchDbContext` y permisos. No se recreó un proyecto ni se usaron ZIP históricos.

## Problema y solución I06 implementada

El artefacto I05 es un archivo local `NO_TRANSMITIR_CENIT_I05_<id>.txt`, cifrado, sin nombre externo homologado, **sin `AchFileExportId`, sin transmisión y sin relación atribuible con acuses**. Vincularlo a una respuesta CENIT histórica implicaría confirmar un envío inexistente. Por eso I06 incorpora **conciliación diagnóstica de solo lectura**, sin activar el transporte.

- `ICenitReturnIntegrationReadService` y `CenitReturnIntegrationReadService` consultan hasta 100 reservas CENIT, con indicador de ventana limitada y selección por cámara en BD.
- Verifican integridad **sin divulgar bytes ni descifrar en una respuesta HTTP**: propósito DataProtection idéntico a I05, hash SHA-256, nombre interno `NO_TRANSMITIR`, perfil oficial CENIT, diez registros de 106 caracteres 1/5/6/7/8/9, adenda 99, causal, secuencias y monto.
- Consultan devoluciones existentes por la ruta manual (`AchReturnsGenerated`), trazas presentes en otras exportaciones **CENIT** y respuestas existentes de la cámara, sin adjudicarlas al artefacto I05.
- Reportan **siempre** `TransmissionEnabled=false`, `ChamberAcknowledgementAttributed=false`, `FinanciallyReconciled=false`; los valores no son opciones de configuración del usuario.
- Acceso `GET /api/cenit/devoluciones-automaticas/conciliacion[/{id}]` con `[Authorize]` y `P1Policies.ConfigRead`, `Cache-Control: no-store, private`. No hay POST, jobs, nuevos estados, transportadores, migraciones, cambios de saldos ni exportaciones.
- Diagnóstico funciona aun después de expirar la ventana original o desactivar el modo, pues **ver un artefacto histórico no lo reautoriza**; solo reporta hallazgos.

## Rutas nuevas

- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnIntegrationReadService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnIntegrationModels.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnIntegrationReadService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/CenitReturnIntegrationController.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnIntegrationI06Tests.cs`
- `scripts/test/automatic_returns_i06_cenit_static_check.py`
- Documentación I06: este MEMORY, informe, evidencia, plan, ejecución estática, manifiesto/inventario e integridad final.

**Única ruta de código heredado modificada:** `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs` (registrar el servicio scoped I06). **No hay migraciones ni snapshots nuevos o modificados; no se tocó SPA, reglas de ACH Colombia ni generación I05.**

## Evidencias reales

- CRC ZIP I05: sin fallas, 4.934 entradas; SHA de origen verificado.
- `python scripts/validate-solution-projects.py`: PASS para 7 proyectos/7 cargados/referencias.
- I06: **62/62 comprobaciones estáticas PASS**. Regresión CENIT I01–I05: **29/29, 50/50, 57/57, 71/71, 84/84**.
- ACH Colombia I02/I03/I05/I06/I07/I08: **18/18, 25/25, 33/33, 28/28, 24/24, 25/25**. **I04-ACH 32/35: abierto, no aprobado y no corregido.**
- **NO EJECUTADOS:** `dotnet restore/build/test`, pruebas xUnit I01–I06, HTTP autenticado, EF SQL Server/PostgreSQL, concurrencia, archivos golden bancarios, restitución de llaves, transporte CENIT/MFT/SEBRA, acuse atribuible, liquidación/conciliación, homologación externa. SDK .NET 10 ausente en este entorno. Comprobaciones estáticas ≠ funcionales.

## Riesgos y decisiones abiertas que NO deben inventarse

1. El nombre y modificador ID del I05 **no están homologados**; no realizar transporte ni crear `AchFileExport` de oficio.
2. Los ACK/NACK existentes en `CenitChamberResponse` dependen de otras exportaciones; aunque compartan transacción o traza, **no son recibos del archivo I05**. El diagnóstico deliberadamente los clasifica como no atribuibles.
3. La ruta manual CENIT puede generar una devolución de la misma operación. La consulta I06 **detecta** señales pero **no garantiza exclusión transaccional cruzada**; se requiere diseño/proceso validado antes de enviar.
4. Si las llaves DataProtection se pierden o el contenido es alterado, se reporta bloqueo; nunca se publica el texto cifrado o plano desde I06.
5. Verificación post-ventana no equivale a autorización extemporánea. R06/R23/R34, prenotificaciones, CTX y devolución de devolución permanecen fuera del flujo ordinario.
6. La respuesta diagnostica artefactos I05; NO representa conciliación del universo de transferencias o posición de compensación CENIT.

## Estado y punto exacto de parada

| Cámara | Avance en código | Trabajo todavía obligatorio | Productivo |
|---|---|---|---|
| CENIT | I01–I05 incorporados. **I06 diagnóstico incorporado, integración efectiva no cerrada.** | **1 cierre funcional I06:** pruebas runtime/SQL, contrato de naming, transporte protegido/homologado, acuse atribuible, conciliación bancaria y certificación | NO-GO |
| ACH Colombia | I01–I08 incorporados sin cambios funcionales | **I09-B** obligatorio; I09-A condicional para preparar el entorno | NO-GO |

**Quedan 2 frentes obligatorios de cierre entre cámaras** (I06-CENIT funcional e I09-B-ACH), hasta 1 frente condicional (I09-A-ACH). Son frentes de validación/homologación, no una promesa de dos incrementos de código suficientes. Cualquier fallo real puede requerir correcciones adicionales. **Punto de parada: I06-CENIT, diagnóstico y pruebas en fuente incorporados.** Continuación recomendada: ejecución y homologación del pendiente I06 en ambiente controlado antes de implementar una ruta de envío o conciliación efectiva. No abrir I07 ni reactivar ACH sin instrucción.
