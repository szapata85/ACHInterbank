# F08 — Informe de entrega final del programa de correcciones

**Fecha de corte:** 2026-11-02. **Línea base F07:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F07_2026-11-02.zip` SHA-256 `86e4ea5ccf30b1155fbc3a9477889bc611b499d326210b2f042fe5bd99b2c5f0`, 4.660 entradas, CRC íntegro y árbol cotejado con ZIP original. **Salida:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F08_2026-11-02.zip`.

## Cambios y conservación

F08 es **documental y de verificación**: **0 cambios productivos, 0 cambios de pruebas C# o infraestructura**, 0 modificaciones en archivos exclusivos ACH Colombia, WSDL, DB/migraciones o CUD. Se preservan íntegramente auditorías I01–I10 y correcciones F01–F07. Se actualizan seis documentos de continuidad y se añaden archivos F08 de dictamen, matriz de cierre, pruebas pendientes, checklist, informe, evidencia, memoria, manifiesto, parche de documentación, comparación SHA-256 y script auditor estático. Todos los datos de integridad definitivos figuran en `MANIFIESTO_F08.md`, `F08_COMPARACION_SHA256.tsv` y el informe externo de entrega.

## Resultados realmente ejecutados

| Evidencia | Resultado |
|---|---|
| F01–F06 verificación de código/fixtures | **113/113 PASS**: 12+13+17+21+26+24 |
| F07 verificación transversal de código/fixtures | **24/24 PASS** |
| F08 consistencia documental / auditoría de ZIP F07 | **17/17 PASS** |
| Total controles estáticos ejecutados | **154/154 PASS** (sin equipararlos a pruebas del código compilado) |
| Intento `run_cenit_f07_dotnet_regression.sh` | **exit 2**, SDK ausente; no se ejecutó xUnit |
| xUnit y compilación | **0 ejecutadas**, 0 aprobadas, 0 fallidas ejecutadas |
| PT-01–PT-22 de I10 | **22 pendientes**, ninguna ejecutada |
| SQL/Quartz/operador/Tesorería/ACH Colombia funcional | **NO EJECUTADAS** |

**Dictamen:** `F08_DICTAMEN_FINAL_CONDICIONADO.md`. Decisión: **NO-GO producción y homologación**; 13 hallazgos implementados pero **ABIERTOS pendientes de verificación**. Las 8/8 fases entregadas significan 100 % del plan de **entrega**, no cumplimiento. La aceptación depende de matriz PT, resultados TRX, logs de DB/Quartz, archivos del operador, confirmación de vigencia y actas.

**Ambiente .NET:** el `global.json` real es **10.0.403, latestPatch**; se aclara sin reescribir la referencia distinta 10.0.402 incluida en reportes F07.

**Reversión:** restaurar el ZIP F07 como línea base segura; alternativamente aplicar `F08_CAMBIOS.patch` en reversa a los seis documentos originales y eliminar las adiciones F08, sin afectar el código productivo.
