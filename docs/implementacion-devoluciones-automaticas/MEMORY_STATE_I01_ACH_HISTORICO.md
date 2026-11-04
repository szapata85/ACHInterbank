# MEMORY_STATE — DEV-AUTO-2026-11-02

**Corte:** 2026-11-02. **Última implementación:** **I01-ACH**, solo configuración de ACH Colombia. **Base:** ZIP acumulativo I00. **Siguiente autorizado:** I02-ACH, detector pasivo específico para ACH Colombia.

## Invariantes

- Mantener la raíz y el ZIP completo; no sobrescribir `MEMORY.md` y `MEMORY_STATE.md` de la raíz ni otros estados anteriores.
- Ejecución separada por cámara conforme a `PLAN_POR_CAMARA_I01_ACH.md`; una cámara por microincremento. Código compartido solo cuando no acople habilitación.
- CENIT sigue NO INICIADO dentro de esta iniciativa. La tabla común es infraestructura; su valor predeterminado es DESACTIVADO, sin endpoint CENIT.
- Nunca generar devolución desde creación genérica. Mantener manual y proceso de devoluciones recibidas.
- `Mode` configurado no equivale a ejecución conectada: `ProcessingConnected` permanece false hasta fase posterior.
- No declarar autorizada la transmisión/compensación financiera sin pruebas de recepción, acuse, idempotencia y conciliación.
- SPA intocada en I01; para fases posteriores Angular Material, UX simple y mensajes en español.
- Falta de SDK/BD no bloquea cambios seguros, pero las pruebas pendientes siguen NO EJECUTADAS.

## Cambios I01-ACH comprobados en fuente

- `ClearingHouseConfig` amplía un campo persistente con `DESACTIVADO` por defecto; configuración EF añade token de concurrencia y CHECK de modos; migración SQL Server y snapshot.
- Servicio `AchColombiaAutomaticReturnSettingsService` resuelve por `RegulatoryCycleScheduleCatalog.AchColombiaCode` (`ACHCOL`); exige registro propio y falla cerrado ante configuración compartida.
- Endpoint único `api/ach-colombia/devoluciones-automaticas/configuracion`, GET con `P1.ConfigRead`, PUT con `P1.ConfigManage`.
- Se reutiliza auditoría genérica de `AchDbContext` y mecanismo existente de autorización; no se altera `AchReturnsService` ni `IncomingNachaPostParseProcessor`.
- Pruebas xUnit I01-ACH añadidas; no ejecutadas por SDK ausente.

## Decisiones

- DEC-DA-007: separar dos líneas de implementación ACH / CENIT, con cierres independientes; `PLAN_POR_CAMARA_I01_ACH.md` prevalece sobre la secuencia mixta de I00.
- DEC-DA-008: evitar nueva tabla; usar `ClearingHouseConfigs` por cámara y una sola columna con valor seguro; CENIT no recibe UI ni habilitación en I01-ACH.
- DEC-DA-009: exponer `ProcessingConnected=false` en contrato de lectura para evitar que una selección `AUTOMATICO` sugiera que ya se procesa automáticamente.
- DEC-DA-010: predeterminado DESACTIVADO, cambios persistidos con CAS optimista de `AutomaticReturnMode` y `AuditLogs` preexistente.

## Evidencia y limitaciones

- Grafo de solución: `SOLUTION GRAPH: OK` ejecutado con Python.
- Código, migración, snapshot y pruebas: inspección estática; validar coherencia de artefactos durante el empaquetado.
- `dotnet build`, xUnit, migración contra SQL Server, pruebas de permisos runtime y homologación: **NO_EJECUTADAS_POR_AMBIENTE**.
- No afirmar cierre de cumplimiento ni producción. Sin generador automático ni envío autorizado.

## Puertas y parada

- I00: COMPLETADO documentalmente en entrega previa.
- **I01-ACH: IMPLEMENTADO EN FUENTE / PENDIENTE DE EJECUCIÓN .NET y BD.**
- I02-ACH: NO INICIADO; detectar fallo real de aplicación sin generar archivos, con pruebas ACH exclusivas.
- I01-CENIT y sucesivos: NO INICIADOS; solo iniciar tras instrucción expresa y memoria/revisión individual de CENIT.
- **Parada al terminar I01-ACH.** El ZIP entregado es la siguiente base acumulativa.
