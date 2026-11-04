# INFORME I07-ACH — Recuperación íntegra y validación operativa delimitada

**Fecha:** 2026-11-02. **Ámbito:** ACH Colombia únicamente. **Base:** I06 completo. **Resultado:** código incorporado, ejecución y certificación pendientes. **Producción:** NO-GO.

## Hallazgo técnico comprobado

`AchReturnAuditMonitoringComponent.BuildReturnFileGeneratedPayload` ya guardaba `contentSha256` en el evento `ReturnFileGenerated`; sin embargo, `AchColombiaReturnReviewService.RecoverGeneratedFileAsync` reconstruía el NACHA-M y validaba nombre/ciclo/una transacción, **pero no que los bytes fueran idénticos a los generados**. El nombre del archivo por sí solo no prueba integridad.

## Corrección puntual

- Consulta el evento original con clave de idempotencia `outbound-return-v1:{id}` de ACH Colombia. El hash se obtiene del evento persistido, no del artefacto reconstruido.
- Revalida identidad, cámara, ciclo, causal, nombre, cantidades y estado de generación sin transmisión.
- Calcula SHA-256 sobre los bytes reconstruidos y usa comparación de tiempo constante; ante error devuelve conflicto controlado sin descargar. Mantiene recuperación sin nuevas secuencias.
- Solo se actualiza el mensaje informativo de las candidatas. El estado sigue siendo `ARCHIVO_GENERADO_SIN_ENVIO`. No se aplica movimiento financiero ni se altera el mecanismo de envío.

## Cambios de archivo

- Modificado `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs`: verificación original vs reconstruido.
- Modificado `tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs`: evento de auditoría real en la prueba positiva de recuperación y reutilización de datos de prueba.
- Nuevo `tests/Cfa.ACHInterbank.Tests/AchColombiaReturnRecoveryI07Tests.cs`: regresión de integridad de datos y aislamiento.
- Nuevo `scripts/test/automatic_returns_i07_ach_static_check.py` y documentación I07 en `docs/implementacion-devoluciones-automaticas/`.
- Sin cambios a SPA/Angular, CENIT, rutas HTTP, transporte, tablas o migraciones.

## Pruebas y límites

Verificadores: I07 24/24, I06 28/28, I05 33/33, I03 25/25, I02 18/18 y grafo de solución 7/7. **Son verificaciones estáticas**, no equivalen a compilar ni ejecutar xUnit. El verificador I04 marca 32/35 por reglas históricas que ya no representan los incrementos posteriores.

No se ejecutaron `.NET`, xUnit, SQL Server, PostgreSQL ni pruebas E2E por indisponibilidad de SDK .NET y bases de datos. La configuración técnica entregada permanece desactivada. **NO-GO** para activar generación automática o transporte.

## Riesgos encontrados sin modificación transversal

1. `ReturnedByEpr` se asigna al generar un archivo; los reportes pueden clasificarla como devuelta antes de recibir acuses, por lo que no sirven como prueba definitiva de liquidación. Requiere diseño explícito posterior, con regresión del resto de rutas.
2. No se garantiza identidad binaria del constructor de reconstrucción frente al artefacto histórico hasta correr golden files; I07 impedirá la descarga de uno diferente.
3. Recuperaciones antiguas sin hash/evento comprobable son bloqueadas y requieren resolución manual. No inventar hash ni asumir que el archivo fue transmitido.
4. La conciliación y el transporte siguen fuera de este incremento; ACH Colombia y CENIT tienen flujos y reglas distintos.

**Punto de parada I07-ACH:** entrega acumulativa completa, NO-GO. Continuación recomendada I08-ACH según matriz adjunta.
