# MEMORY_STATE — DEVOLUCIONES AUTOMÁTICAS — I04-ACH

**Fecha del corte:** 2026-11-02. **Base completa:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_ACHCOLOMBIA_2026-11-02.zip`. **Incremento incorporado:** I04-ACH, revisión/aprobación interna de candidatas. **Siguiente:** I05-ACH. **Estado productivo:** **NO-GO**. **CENIT:** ruta independiente, sin cambios funcionales de esta iniciativa.

## Invariantes que deben conservarse

1. **No confundir aprobación con devolución financiera:** `APROBADA_SIN_ENVIO` solo habilita una etapa posterior; no existe archivo `.RET` de esta acción, no se envía a cámara, no hay compensación ni asiento contable. `DESCARTADA` es terminal en I04. I03 solo prepara; I02 detecta; I01 parametriza.
2. **No tocar CENIT:** la API I04 vive bajo `api/ach-colombia/devoluciones-automaticas/candidatas`; filtra `ACHCOL` y no cambia rutinas ni reglas CENIT. No asumir reglas idénticas entre cámaras.
3. **Mantener configuración:** `DESACTIVADO` bloquea aprobar; `CON_APROBACION` o `AUTOMATICO` habilitan consulta y posible aprobación, pero `ProcessingConnected=false` permanece. Descarta incluso si luego se deshabilita el modo.
4. **Verificar antes de aprobar:** I04 revalida cámara, estado de candidata, configuración, ausencia de registro en `AchReturnsGenerated`, fuente `AchColApplicationFailureDetected` I02 y JSON `CORE_FUNCIONAL`, causal activa en catálogos ACH, fecha valor/ciclos mediante `AchColombiaReturnPreparationWindow.Evaluate`, y coincidencia exacta con el ciclo sugerido.
5. **Crédito tardío / R10:** `REVISION_TARDIA_CREDITO` se muestra y no es aprobable en I04; causal R10 queda sin aprobación por falta de evidencia adicional verificable. El usuario puede descartar, no aprobar por excepción implícita.
6. **Concurrencia y auditoría:** la propiedad `Status` es `IsConcurrencyToken()`. `SaveChangesAsync` permite registrar `AuditLogs` (usuario, fecha, anterior/posterior) y rechaza actualizaciones simultáneas con conflicto HTTP 409. No hay migración de esquema nueva para I04: cambia el mapeo de concurrencia y snapshot SQL Server, no columnas.
7. **Compatibilidad manual:** se consulta `AchReturnsGenerated` para bloquear aprobación de candidatas ya generadas manualmente. Persiste una posible carrera distribuida entre esta consulta y posteriores procesos de generación: **revalidar siempre dentro de I05**; nunca afirmar idempotencia financiera transversal I04/manual.
8. **Permisos reutilizados:** lectura `P0.ReturnsRead`, resolución `P0.ReturnsGenerateFile`; SPA oculta acciones sin derecho. No nuevos permisos ni rutas SPA; se agrega solo una sección a «Devoluciones» existente con Angular Material.
9. **Preservar memoria histórica:** raíz `MEMORY.md` y `MEMORY_STATE.md` sin sobrescribir. Esta memoria más `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` son la continuidad de la iniciativa. I03 histórico en `MEMORY_STATE_I03_ACH.md`.
10. **Ambiente no bloquea código, pero sí certificación:** sin .NET SDK, SQL Server, PostgreSQL ni dependencias Angular instaladas no es posible aprobar compilación, xUnit, Karma ni UAT. Las verificaciones estáticas no sustituyen ejecución.

## Archivos cambiados en I04

- API: `AchColombiaReturnReviewController.cs`, modelos `AchColombiaReturnReviewModels.cs`, contrato `IAchColombiaReturnReviewService.cs`.
- Persistencia: `AchColombiaReturnReviewService.cs`, registro DI, mapeo del token `Status`, snapshot EF SQL Server.
- SPA: `ach-colombia-return-review.component.{ts,html,scss}`, servicio `ach-colombia-return-review-api.service.ts`, inserción mínima en `ach-returns-management.component.{ts,html}`, actualización de prueba padre.
- Pruebas: `AchColombiaReturnReviewI04Tests.cs`, dos nuevas especificaciones Jasmine; `scripts/test/automatic_returns_i04_ach_static_check.py`.
- Documentos: este estado, informe, evidencia, manifiesto y README de entrega I04.

## Pruebas y estado

- Comprobaciones estáticas: I02 **18/18**, I03 **25/25**, I04 **35/35** `PASS_STATIC`.
- Grafo solución: **7/7** referencias válidas.
- Sintaxis TypeScript de seis archivos: **6/6** sin errores de parser (no equivale a compilación Angular).
- .NET build/xUnit: `NO_EJECUTADO_SIN_SDK`.
- Angular/Karma/Playwright: `NO_EJECUTADO_SIN_DEPENDENCIAS`.
- SQL Server/PostgreSQL, pruebas concurrencia, homologación de ciclos: `PENDIENTE`. **La paridad de migraciones PostgreSQL I01/I03 aún está pendiente.**

## Punto de parada

**I04-ACH cerrado en código/documentación, sin pruebas de ejecución.** El siguiente I05-ACH deberá consumir **solo candidatas aprobadas y otra vez verificadas**, con validaciones de cámara, ventana, causal, permiso, duplicados y separación de la generación manual. La generación automática/archivo no se habilitó en I04. No iniciar CENIT de forma implícita. **Usar el ZIP completo I04 como siguiente base.**
