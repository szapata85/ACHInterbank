# INFORME TÉCNICO — I03-CENIT: preparación controlada

**Fecha de corte:** 2026-11-03. **Base única:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT_2026-11-02.zip` (SHA-256 `bbb2862461466d54117a897b7a1b065cf396f8725ea396eabcec6577a93a2b69`). **Producción:** NO-GO.

## Objetivo y límite

Reservar **solo por solicitud humana explícita** una posibilidad de devolución de CENIT, a partir del identificador de transacción original y el identificador exacto de ejecución comprobada por el detector pasivo I02. Sin trabajos automáticos, candidatos por lote, aprobaciones, archivos NACHA-M, secuencias, transporte, liquidación ni conciliación. No modifica lógica funcional ACH Colombia.

## Implementación comprobable en código

- `POST /api/cenit/devoluciones-automaticas/preparaciones` (autenticado, `P1.ConfigManage`) recibe `{ "originalTransactionId": 42, "expectedExecutionId": "<GUID-de-I02>" }`. Retorna reserva sin envío o error 400/409; jamás una devolución enviada. No existe ruta PUT/DELETE/APROBAR.
- `GET /api/cenit/devoluciones-automaticas/preparaciones?maxItems=100` (autenticado, `P1.ConfigRead`) consulta, acotada a 1–100 y con filtro a cámara CENIT. El DTO omite números de cuenta, nombres y datos personales; `processingConnected=false`.
- Servicio `CenitReturnPreparationService`: valida modo CENIT `CON_APROBACION` o `AUTOMATICO`; el modo `DESACTIVADO` **impide el INSERT**. El modo `AUTOMATICO` no activa un trabajador, aunque sea seleccionado.
- Bajo aislamiento `Serializable` en BD relacional, reconsulta la última ejecución **sin limitarse a fallas**, compara el GUID con el visto en I02, ejecuta `CenitPassiveReturnDetector.Evaluate` sobre entidades reales y catálogo CENIT, acepta exclusivamente `INDICIO_CAUSAL_CENIT`, y comprueba que no se haya generado retorno previamente ni exista reserva.
- Restricción de ventana conservadora: **una sola** sesión siguiente (CENIT 1→2, 2→3, 3→4, 4→5) dentro de la misma fecha de operación y **Fecha Valor original**, source cerrado, destino abierto o programado, política activa, habilitada y en vigor, y horario según `RegulatoryCycleScheduleCatalog.GetRequired("CENIT")`. Se bloquea fin de semana, festivo registrado, corte vencido, sesión indefinida/ambigua, fecha futura o entrada monetaria no correlacionada. Si hay una ventana especial de calendario no modelada se **rechaza** la preparación, no se inventa.
- Reserva aislada en `CenitReturnPreparations` (distinta de `AchColombiaReturnPreparations`), unicidad real BD por `OriginalTransactionId` y `SourceExecutionId`, claves foráneas restrictivas a `AchTransactions` y `IncomingNachaIntegrationExecution`, auditoría compartida `AchDbContext.SaveChangesAsync`, único estado I03 `PREPARADA_SIN_ENVIO`.
- Migraciones explícitas SQL Server y PostgreSQL y snapshots EF correspondientes; no se crean nuevas capas genéricas ni tareas. Se actualiza solo el DbSet y el registro DI común. Sin cambios SPA: se expone API restringida; una pantalla completa de revisión/aprobación es I04.

## Normativa CENIT y exclusiones

DSP-152 Anexo 2 (2025), §§3.1–3.3 y 4.2/4.5: normalmente la devolución del crédito o débito procede en la sesión **inmediatamente siguiente**, conservando Fecha Valor. La excepción de rechazo por receptor de crédito, con hasta **15 días calendario**, no se confunde con la ventana ordinaria. Anexo A causales CENIT (2023): R06 y R23 exigen solicitud acreditada y condiciones, R34 decisión manual; se excluyen en I03. Las prenotificaciones y devoluciones de devoluciones (R60–R69) tienen tratamiento independiente; los rechazos STA Dxx no se convierten en devoluciones. NACHA-M CENIT (2026) no se genera aquí.

**Conservadurismo deliberado:** I03 no aborda créditos tardíos R23, entrada del día anterior, cierres extraordinarios ni eventuales excepciones de calendario. Se deben resolver con evidencia y homologación antes de preparar automáticamente esos casos; no es una negación de los procedimientos manuales vigentes.

## Riesgos y verificaciones pendientes

1. No se ejecutaron SDK .NET, xUnit ni DDL real SQL Server/PostgreSQL. Las garantías relacionales (incluidos bloqueos e índices concurrentes, zona horaria y serialización) **necesitan prueba en BD real**.
2. No hay evidencia de homologación CENIT de sesión, fecha valor o horario en producción. Las ventanas operativas modificadas exigirán adaptación controlada de política, no atajos.
3. Una reserva no autoriza ni garantiza devolución efectiva; **I04 debe revalidar todo** antes de decidir; I05/I06 separados para generar/integrar.
4. La fecha hábil se comprueba contra fines de semana y festivos registrados en la base; debe validarse completitud del calendario y posibles ampliaciones de sesiones.
5. No existe revalidación automática/revocación de reservas antiguas: quedan documentadas como pendientes, sin uso por motor. Ningún proceso consume esta tabla actualmente.
6. Regresión ACH: I04 mantiene resultado histórico 32/35 y sigue pendiente; no se corrige por tratarse de otra iniciativa.

## Pruebas en código y punto de parada

`tests/Cfa.ACHInterbank.Tests/CenitReturnPreparationI03Tests.cs` incorpora casos de modo apagado, modos habilitados sin efectos financieros, auditoría, duplicados, ejecución obsoleta, causal R06, fecha valor, cámara ajena, corte vencido, sesión cerrada, calendario festivo, devolución generada y autorización declarativa. Las pruebas **no fueron ejecutadas**. Comprobador fuente I03: 57/57; regresiones I02-CENIT 50/50, I01-CENIT 29/29. Ver `EVIDENCIA_DEVOLUCIONES_AUTOMATICAS_I03_CENIT.md`.

**Parada exacta:** I03 incorporado al código y empaquetado, sin certificación ejecutable ni autorización productiva. Siguiente I04-CENIT (revisión/aprobación y excepciones), únicamente por nueva instrucción. **NO-GO**.
