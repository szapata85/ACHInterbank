# INFORME I06-ACH — Generación automática protegida, sin transmisión

**Fecha de corte:** 2026-11-02. **Base:** ZIP completo I05-ACH. **Cámara:** ACH Colombia únicamente. **Estado:** incorporado al código, pruebas de ejecución pendientes; **NO-GO productivo**.

## Resultado

Se conectó la generación interna autónoma a la fase existente de procesamiento de transacciones entrantes, **sin crear una segunda canalización ni programador**. El enlace está protegido mediante dos condiciones simultáneas: modo de ACH Colombia `AUTOMATICO` persistido (I01) y parámetro de despliegue `AchCol:EnableAutomaticReturnFileGeneration=true`. El ZIP entregado conserva este último parámetro en **false**.

El proceso consulta solo candidatas ACH Colombia preparadas a partir de rechazos funcionales confirmados, en lotes máximos de 10. Para cada candidata el servicio existente vuelve a comprobar evidencia, causal, ventana de cuatro ciclos, tipo, fecha valor, duplicados, R10, cámara y modo. Cuando es elegible llama al **generador existente NACHA-M** y verifica que se registró la devolución, marcando `ARCHIVO_GENERADO_SIN_ENVIO`. Se corta la ejecución del lote ante un error y no se altera el resultado funcional original del sistema central.

La vía manual sigue funcionando con **aprobación explícita**: no se convirtió en generación automática al usar `CON_APROBACION`. La ruta interna de generación automática no tiene controlador HTTP propio y no llama a cifrado, transmisión, resultado de cámara, compensación o liquidación.

## Recuperación y UX

Se reutilizó `IAchOutboundReturnArtifactService.BuildAsync` para reconstruir archivos ya generados desde la información persistida, sin consumir secuencias nuevas. El endpoint de recuperación exige permiso `P0.ReturnsGenerateFile`; comprueba cámara ACH Colombia, candidato generado, archivo registrado y exclusividad de la transacción. La pantalla de devoluciones Angular Material existente añade un botón **Descargar archivo existente**, con mensajes en español y nombre devuelto por el servidor. No se crea pantalla nueva.

## Archivos principales

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnFileRunner.cs` (nuevo)
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` y `IncomingNachaPostProcessingOrchestrator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs` y `AchColombiaAutomaticReturnSettingsService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaReturnReviewService.cs` y `Models/AchColombiaAutomaticReturnSettings.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/AchColombiaReturnReviewController.cs` y `appsettings.json`
- SPA existente de revisión, su servicio HTTP y pruebas Jasmine
- `tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs` (nuevo)
- `scripts/test/automatic_returns_i06_ach_static_check.py` (nuevo)

No se modifica ningún archivo funcional CENIT ni el modelo de base de datos.

## Validaciones realizadas y NO realizadas

| Tipo | Estado |
|---|---|
| Comprobación estática propia I06 | 28/28 PASS_STATIC |
| Regresión estática I02/I03/I05 | 18/18, 25/25, 33/33 |
| Script histórico I04 | 33/35, dos restricciones deliberadamente obsoletas desde I05 |
| Parseo TypeScript de los cuatro archivos afectados | Sin error de sintaxis |
| Grafo de referencias de proyectos fuente | Seis proyectos, sin referencias ausentes |
| Compilación .NET / xUnit | NO EJECUTADAS |
| Angular build / Jasmine | NO EJECUTADAS |
| SQL Server/PostgreSQL / pruebas de concurrencia | NO EJECUTADAS |
| Homologación bancaria y pruebas operativas 5/7 ciclos | NO EJECUTADAS |

## Riesgos operativos pendientes

1. El generador histórico modifica `ReturnedByEpr` al generar el archivo; no confundirlo con una entrega efectiva. Revisar semántica y efectos contables antes de habilitar la puerta técnica.
2. Validar composición NACHA-M con archivos reales y comparar recuperación byte a byte, porque el artefacto reconstruido usa un constructor independiente a partir de metadatos persistidos.
3. Confirmar versión vigente del calendario ACH (5 frente a 7 ciclos), migraciones previas y ajustes PostgreSQL.
4. Probar en SQL transacciones EF con reintentos, bloqueo concurrente multiinstancia y compensación ante errores del generador.
5. Probar fallos de descarga sin generar duplicados, autenticación del endpoint de recuperación y cancelaciones a mitad de lote.
6. El transporte es un flujo separado ya existente; I06 NO le entrega ni envía archivos por iniciativa propia.

## Decisión de cierre

**Se detiene en I06-ACH**, con `EnableAutomaticReturnFileGeneration=false` y estado **NO-GO**. No se declara operación en producción ni certificación de ACH Colombia. Se conserva CENIT para ruta independiente.
