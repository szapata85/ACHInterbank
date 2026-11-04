# MEMORY_STATE — Correcciones incrementales CENIT (F05)

**Fecha:** 2026-11-02. **Línea base histórica:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip`. **Base inmediata verificada:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F04_2026-11-02.zip` SHA-256 `9bf9b515affe93f1a54076326ad1eb081b2fc1538c518faf06519de6afe9183a`, CRC OK, 4.637 entradas. Raíz interna `ACHInterbank_ajustado/`. Auditoría I01–I10 y memoria histórica preservadas.

## Continuidad e hitos

- **Fase actual:** F05 — devolución de devolución, correlación C1 y R74. **Fases implementadas como incrementos:** F01, F02, F03, F04, F05; **5/8 = 62,5 % de hitos** (NO cumplimiento normativo/homologación). **Siguiente F06:** HC-CENIT-010/011, NACHA-M caracteres y separación STA.
- **IMPLEMENTADOS — PENDIENTES DE VERIFICACIÓN:** HC-CENIT-001, 002, 003, 004, 005, 006, 007, 008, 009, 012, 013 (11). **ABIERTO sin implementar:** HC-CENIT-010, 011 (2). **CERRADOS:** 0. **Bloqueados por ambiente para verificación dinámica:** 11. F07 integración/regresión y F08 cierre pendientes.
- **Código F05 modificado (3):** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs`, `AchReturnOfReturnFileGenerationService.cs`, `Seeders/RegulatoryCatalogSeeder.cs`. **Pruebas C# originales modificadas (3):** `tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs`, `AchReturnOfReturnFileGenerationServiceTests.cs`, `RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests.cs`. **Nuevo script:** `scripts/test/cenit_f05_static_check.py`. Sin migraciones, nuevas entidades, contratos WSDL o paquetes.
- **Verificaciones ejecutadas:** F05 estático 26/26 PASS; F01 12/12 PASS, F02 13/13 PASS, F03 17/17 PASS, F04 21/21 PASS. Aplicación y reversión seca parche F05 6/6. Comparación hashes por archivo y CRC del ZIP F05 en manifiesto. **Pruebas xUnit:** 0 ejecutadas, 0 aprobadas, 0 fallidas ejecutadas (SDK .NET ausente). No DB/Quartz/gateway, no muestras oficiales ni certificación.
- **ACH Colombia:** clases/rutas particulares y catálogo no alterados; generador compartido restringido a `CENIT`, hashes de archivos previos no intencionalmente editados idénticos; **regresión funcional pendiente**.

## Decisiones técnicas

1. La aplicación productiva de Return In asienta evento sobre transacción original y no necesariamente crea otra `AchTransaction` de tipo Return; **no generar transacción financiera artificial**. Para ROR C1 el origen veraz es `AchTransactionStateEvent` + `IncomingNachaTransactionLink` + `EntryDetail` + adenda 99 + `NachaHeader` y calendario bancario; comparar traza, importe, vínculo parental y fecha del ciclo. `SourceReturnTransactionId` se completa solo cuando existe una transacción Return ya persistida, única y correlacionable; se comprueba su coherencia si está presente. **Nunca sustituir por ciclo de operación original**. Raw ausente/inconsistente bloquea generación.
2. R74 requiere padre **R32**, operación **crédito monetario** y adenda original 05 con campo 3 informado. Reglas en catálogo solo CENIT, validación en CreateOutgoing/IngestIncoming y otra en GenerateCenitNacha. Adenda 99 no cuenta; datos blancos/ceros no cuentan. No alterar ACH Colombia.
3. Reintentos de ROR utilizan índice único existente de evento padre y reutilización de flujo; sin API CUD, ni liquidación, ni algoritmos de optimización.
4. Revisar evidencia raw histórica: registros sin correlación suficiente se retienen/fallan cerrados; comprobar en integración si hay trazas/fechas desalineadas y decidir tratamiento con evidencia oficial. **No construir relaciones por inferencia**.
5. Persisten pendientes de F01–F04: pruebas .NET/SQL/Quartz, ADV/NACK oficiales, fechas R23 con notificación, R06 C5, colas entre fechas valor, restricciones DN-CEN-007/008 y regresión ACH Colombia.

## Entregables y siguiente fase

`F05_INFORME_CORRECCIONES.md`, `F05_MEMORY_STATE.md`, `F05_CAMBIOS.patch`, `F05_EVIDENCIA_CONTROLES_ESTATICOS.txt`, `MANIFIESTO_F05.md`, `F05_COMPARACION_SHA256.tsv`, `REGISTRO_CORRECCIONES.md`, `REGISTRO_PRUEBAS.md`, `MATRIZ_HALLAZGOS_CORRECCIONES.md`, `ESTADO_FASES.md`, `PLAN_INCREMENTAL_CORRECCIONES.md`. F01–F04 y auditoría previas intactas en ZIP. **ZIP F05:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F05_2026-11-02.zip`, su SHA-256 en `.zip.sha256` lateral para evitar circularidad en el contenido. **Siguiente fase F06:** HC-CENIT-010 (caracteres NACHA-M) y HC-CENIT-011 (STA D01–D06 vs errores NACHA), sin tocar reglas ACH Colombia.
