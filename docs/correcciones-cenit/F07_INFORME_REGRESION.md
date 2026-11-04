# F07 — Regresión transversal CENIT y ACH Colombia

**Fecha:** 2026-11-02. **Línea base inmediata:** ZIP F06 SHA-256 `a43539f83b8b6cdc5e36cbadf5d9334dc578bd88ddf2d22cd5acf088ffc8b76d` y 4.651 entradas. **Cobertura del plan:** 7/8 fases de entrega (87,5 %), NO cumplimiento normativo.

## Alcance y cambios reales

Se revisaron F01–F06 y los 13 hallazgos de I10. No apareció un defecto productivo nuevo demostrable mediante verificaciones disponibles sin .NET, de modo que **F07 no cambia archivos productivos**: modifica solo una clase de pruebas xUnit existente (`CenitChamberResponseLifecycleTests.cs`) y agrega otra (`CenitF07CrossPhaseRegressionTests.cs`), un script estático/fixtures y un runner de regresión .NET preparado para CI.

## Regresión transversal incorporada

| Ruta | Hallazgos y flujo | Evidencia disponible | Pendiente |
|---|---|---|---|
| Ventana/optimización y prioridades | HC-003/005/006: Quartz no ejecuta liquidación simulada; SEC informativo | F01 estático, F07 control 1/2 | .NET, DB, respuesta real |
| Respuestas oficiales y conciliación | HC-012/013: NACK individual, ACK técnico, ADV 280/81-88 en ambos órdenes | 2 xUnit nuevos sin ejecutar, F02/F07 fuente | gateway oficial, SQL, Tesorería |
| Encolamiento/exportación | HC-001: consumo tras incorporación al ciclo/lote con transacción serializable | F03 estático, F07 control 5/6 | SQLite/SQL, Quartz/concurrencia, interdiaria |
| Devoluciones | HC-004/007/009: R23, R06, notificación y calendario | F04 estático y 6 xUnit transversales nuevos | ejecutar xUnit/SQL y verificar fechas reales |
| ROR | HC-002/008: correlación padre/raw C1; R74/R32 | F05 estático y fixture sintético tipo 99 | operador/SQL, causa R74 real |
| NACHA/STA vs ACH | HC-010/011: alfabeto por cámara y dominio de errores | F06 estático, 3 xUnit transversales nuevos, 6 golden byte-validos | parser real y comparación STA auténtica |

**Verificaciones EJECUTADAS:** F07 `24/24` PASS (integridad de fixtures, conexiones de código y cobertura prevista), F01–F06 `113/113` PASS (controles estáticos heredados). Los golden ACH Colombia/CENIT existentes no se modificaron. **Verificaciones NO ejecutadas:** `dotnet test`, compilación, base de datos, Quartz, archivos reales de operador, pruebas E2E de conciliación y ACH Colombia. Se intentó ejecutar el runner y devolvió exit=2 por SDK inexistente (no equivale a fallos xUnit).

## Resultado y criterio de no cierre

13 hallazgos **implementados pendientes de verificación**; 0 sin implementar; 0 cerrados formalmente. F07 es un incremento de **pruebas y evidencia estática**, no una integración funcional certificada. No se incorporaron APIs CUD, modificaciones de contabilidad, migraciones, WSDL, componentes exclusivos ACH Colombia ni políticas externas no documentadas.

## Riesgos pendientes para F08

1. Ejecutar `.NET 10.0.402` y la suite completa con `scripts/test/run_cenit_f07_dotnet_regression.sh`, conservar TRX y resultados. Puede haber errores de compilación o pruebas que este entorno no puede detectar.
2. Ejercitar estados de cámara con mensajes ACK, NACK, ADV 280 reales del operador; comprobar persistencia, idempotencia y conciliación sin imputación de CUD.
3. Ensayar SQL Server/PostgreSQL, Quartz concurrente, colas entre fechas valor, calendarios con festivos, ROR C1 y R74; revisar fechas límite 15 días en condiciones reales.
4. Comparar NACHA-M CENIT con STA D01–D06 y ACH Colombia V35; perfiles CENIT dinámicos no identificables sin autoridad siguen siendo caso de revisión.
5. F08 debe emitir dictamen **condicionado** mientras esos bloqueos persistan, sin falso cierre ni homologación.

**Reversión:** `F07_CAMBIOS.patch` para la única prueba original modificada y restauración del ZIP F06. El resto son nuevos archivos de pruebas, scripts y docs.
