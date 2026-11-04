# Informe I08-ACH — Verificación previa a la confirmación automática

**Fecha:** 2026-11-02. **Base:** I07 ACH Colombia. **Resultado:** modificación de código, pruebas escritas y comprobación estática. **No certificado en ejecución.**

## Hallazgo y mitigación mínima

Hasta I07, la verificación SHA-256 impedía descargar una reconstrucción distinta, **pero ocurría después de guardar un archivo generado**. La generación autónoma podía persistir un artefacto cuya recuperación posterior sería imposible. Se añadió una sola comprobación pre-commit en la ruta autónoma ACH: reconstruir desde las filas persistidas dentro de la unidad de trabajo y cotejar contenido y metadatos contra los bytes que devolvió `GenerateReturnsFileAsync`.

Se elige bloquear antes de la confirmación, **no** regenerar, autocorregir un NACHA-M o añadir una tabla. No se altera el uso manual de `GenerateApprovedFileAsync` ni el algoritmo CENIT. Aun así, el comportamiento de rollback real debe verificarse fuera del entorno de chat.

## Código modificado

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs`: control de existencia del reconstructor y cotejo de la respuesta original vs artefacto reconstruido con `FixedTimeEquals`. Verificación se ejecuta antes de asignar `ARCHIVO_GENERADO_SIN_ENVIO`; sobre falla se activa la reversión de la transacción relacional existente.
- `tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs`: dos pruebas positivas reciben un artefacto coherente con bytes/nombre del generador simulado.
- Nuevo `tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI08Tests.cs`: pruebas de discrepancias, puerta sin servicio de integridad, falla de reconstrucción e independencia CENIT.
- Nuevo script de comprobación estática I08 y documentos de continuidad/aceptación; sin cambio a API, Angular, CENIT, transporte, base de datos o migraciones.

## Resultado técnico

- Fuente I08: **25/25 PASS_STATIC**, scripts I02/I03/I05/I06/I07 siguen aprobados; I04 histórico 32/35.
- 7/7 referencias de solución presentes. **No es un `dotnet build`.**
- Riesgo financiero inspeccionado, no resuelto por diseño: `ReturnedByEpr` generado en `AchReturnStateTransitionComponent`; los reportes de conciliación lo cuentan. No autorizar su uso como liquidación sin evidencia real de la cámara.
- **NO EJECUTADO:** pruebas .NET, xUnit, SQL Server/PostgreSQL, concurrencia, pruebas de bytes con generador real, homologación ACH.

## Limitaciones

El chequeo pre-commit no prueba que el archivo seguirá siendo reconstruible luego de cambios en datos fuente, ni que una transacción en memoria pueda revertirse. En prueba SQL se debe confirmar que **si falla la verificación no queda `AchReturnGenerated`, evento de estado, archivo transmitido ni candidata generada**. También se debe verificar que el flujo positivo genera un archivo byte a byte idéntico al recuperado por I07.

**Punto de parada:** I08-ACH. Bandera de generación autónoma apagada; estado productivo **NO-GO**. Siguiente: I09-ACH (ejecución de UAT/SQL y cierre o corrección puntual de discrepancias verificadas).
