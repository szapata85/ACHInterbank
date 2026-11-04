# MEMORY_STATE — Correcciones incrementales CENIT

**Fecha:** 2026-11-02. **Línea base histórica:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip`. **Base inmediata verificada:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F01_2026-11-02.zip` (SHA-256 `8beee25554db99142677d6c084cb08cb9cf864967e864cbc61dd6c72459084e1`, 4.614 entradas, CRC OK). **Raíz:** `ACHInterbank_ajustado/`. Memoria histórica de auditoría inalterada.

## Estado y continuidad

- **Fase actual:** F02 — respuestas oficiales individuales y ADV. **Fases implementadas como entregas:** F01, F02 (2/8; 25 % de hitos); F03–F08 pendientes.
- **Hallazgos implementados pendientes de verificación:** HC-CENIT-003, 005, 006 (F01), 012 y 013 (F02). **Cerrados con evidencia:** 0. **Abiertos por corregir:** HC-CENIT-001, 002, 004, 007, 008, 009, 010, 011 (8). **Total no cerrados:** 13.
- **Bloqueo:** solo verificación dinámica y externa. Sin .NET SDK, DB/Quartz/CENIT/Tesorería en este contenedor.
- **Código modificado F02:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs` (único archivo productivo); **tests modificados:** `tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs`; **script nuevo:** `scripts/test/cenit_f02_static_check.py`.
- **Pruebas fuente C# añadidas:** 7 Fact + 2 InlineData (9 casos); extensión de 3 tests existentes. **Ejecución real:** F02 estático 13/13 PASS, regresión F01 estática 12/12 PASS, dotnet/xUnit 0 ejecutadas, aprobadas 0, fallidas 0; SQL/gateway 0. ACH Colombia: ningún archivo original alterado fuera de los dos anteriores; regresión funcional no ejecutada.

## Decisiones vigentes

1. **CUD completamente fuera del alcance:** manual Tesorería, sin API/liquidación/asiento; ADV es solo evidencia de conciliación (Anexo NACHA-M §7.3.3, 280/ADV, códigos 81–88).
2. CENIT es único destinatario de la ruta modificada, sin cambio de catálogos ACH Colombia.
3. Rechazo individual de `FileNack` exige `TraceNo` oficial único, archivo saliente correlacionado; no modifica estado agregado del archivo. `ReturnedByOperator` y evento fuente Operator reflejan rechazo explícito; no se generan por simulación. No prevalece metadato externo de traza sobre la traza XML.
4. Archivo FileAck es aceptación técnica, no liquidación. FileNack sin traza es rechazo de archivo, no actualización selectiva de estados de transacción.
5. ADV con ciclo identificable crea evidencia persistente por detalle y control de totales; sin ciclo, conserva respuesta no aplicada. Muestras sintéticas **no son certificación**.
6. F01 dejó Quartz a la espera de resultado oficial y cola Queued; **F03 debe implementar incorporación real** y no consumir cola sin evidencia persistida.
7. No reconstruir los 13 hallazgos ni modificar identificadores; preservar historiales I01–I10.

## Riesgos y pendientes de F02

- Compilación xUnit/CI con SDK .NET 10, tests SQLite y DB transaccional real para concurrencia, reintentos, idempotencia/rollback de NACK múltiples.
- Verificar sincronización de prenotificaciones/terceros en respuestas de operador y clasificación de errores formales con ejemplos certificados. Si afecta, ajuste específico posterior sin refactor masivo.
- Certificación de códigos ADV reales, controles de lote/archivo y conciliación con Tesorería (PT-13, PT-19); formalismos físicos completos para F06.
- PT-20 regresión dinámica ACH Colombia pendiente, aunque los hashes prueban inmutabilidad de archivos originales ajenos a F02.
- Seguimiento DN-CEN-007/008 por discrepancias normativas; no inventar reglas nuevas.

## Entrega, integridad y próximo paso

**Último ZIP completo:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F02_2026-11-02.zip`. El SHA-256 final se incluye en el archivo hermano `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F02_2026-11-02.zip.sha256` y en el informe de verificación externo: no se inserta en el propio ZIP para evitar autorreferencia criptográfica. Validar SHA, CRC y manifiesto antes de usar como línea base.

**Siguiente fase F03:** HC-CENIT-001; inspeccionar cola→scheduler→ciclo destino→persistencia→estados/eventos, aplicar incorporación atómica/idempotente solo tras confirmación persistida. Sin cambios CUD ni simulación financiera.
