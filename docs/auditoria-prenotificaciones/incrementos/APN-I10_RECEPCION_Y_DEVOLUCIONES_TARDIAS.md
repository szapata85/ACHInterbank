# APN-I10 — Recepción multicanal y consistencia ante devoluciones tardías

**Proyecto:** ACH Interbank  
**Fecha de corte:** 2026-11-03  
**Base:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I09_2026-11-03.zip`  
**Estado:** `IMPLEMENTADO_EN_FUENTE / ESTATICAS_APROBADAS / CSHARP_SQL_UAT_NO_EJECUTADOS`.

## Objetivo

Reducir los riesgos APN-R03/R04/R07 sin inventar una señal de fin de recepción de archivos que las interfaces actuales no aportan. Las reglas operativas se mantienen: **ACH Colombia tiene siete ciclos** (`ACHCOL-7C-20261014`); **CENIT tiene cinco sesiones** (salida final hasta 19:15, hora de Colombia). La ausencia de un archivo registrado no demuestra por sí misma que la cámara haya terminado de entregar todos los archivos.

## Correcciones implementadas

1. **Barrera de ingestión NACHA incompleta/ambigua.** `AchTacitAcceptanceJobHandler` incluye archivos cuya cámara todavía no está identificada y estados `Bloqueado` o `Fallido`, únicamente dentro de la ventana operativa relevante (cuando falta fecha operacional se usa la fecha de subida). Una fila no resuelta no autoriza aceptación tácita.
2. **Recepción física MFT de ACH Colombia.** El job consulta `AchManagedFileTransfers` con dirección `Inbound` y estados `Received`, `InProgress`, `RetryPending`, `Uncertain`, `Failed` o `Rejected`. Esto cubre el lapso en que el archivo ya fue reclamado del canal pero todavía no existe `IncomingNachaFileIngestion`.
3. **Respuestas propias del gateway CENIT.** El job consulta `CenitChamberResponses`; retiene candidatos relacionados con rechazo individual, `NACK` de archivo exportado y respuestas negativas/ambiguas sin correlación resuelta. Un `ACK` o `NoActivity` **no se convierte** en señal positiva de recepción completa.
4. **Devolución tardía de prenotificación.** `IncomingPrenotificationReturnGuard` clasifica como `PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW` un retorno después de `AppliedTacitly` o `Certified`, preservando el intento terminal; el pipeline existente deja el caso en revisión manual.
5. **Protección transversal de cambios terminales.** `AchStateTransitionService` impide una segunda transición automática sobre prenotificaciones terminales, así como una transición desde `Pending` incompatible con un intento ya terminal. El evento idempotente idéntico se reconoce antes de esta comprobación.
6. **CENIT, rechazo por operador.** `CenitChamberResponseService` ya no modifica directamente el estado de una prenotificación; exige intento canónico vigente, enruta por `IAchStateTransitionService`, comprueba que intento y validación quedaron en `Rejected`, y agrupa las escrituras bajo una transacción serializable cuando la BD es relacional. Si faltan evidencias o el resultado es terminal, conserva la respuesta con código `CENIT_PRENOTE_RETURN_REQUIRES_MANUAL_REVIEW` sin cambiar la transacción. La ruta monetaria anterior permanece intacta.
7. **Sin cambios de base de datos ni nuevos estados.** Se reutiliza la estructura existente.

## Pruebas incorporadas

- `tests/Cfa.ACHInterbank.Tests/IncomingPrenotificationReturnGuardI04Tests.cs`: devolución tardía en ambas cámaras y legado sin intento.
- `tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs`: rechazo de prenotificación sin intento frente a estado `Pending` o `AppliedTacitly`, sin mutación.
- `tests/Cfa.ACHInterbank.Tests/PrenotificationI10TerminalStateTests.cs`: protección del servicio central, intento terminal, y ruta de rechazo CENIT que sincroniza transacción/validación/intento.
- `scripts/audit/prenotificaciones/test_apn_i10_cross_channel.py`: diez verificaciones estáticas (presencia de rutas/guardias; **no prueban funcionalidad .NET**).
- Regresión estática APN-I09 (8 pruebas) y APN-I08 (6 pruebas) ejecutada sin fallas.

## Qué NO se certificó

- No hay SDK `dotnet` en este entorno; no se ejecutaron xUnit, compilación C#, integración SQL Server/PostgreSQL, pruebas de concurrencia reales ni UAT con archivos de cámaras.
- **No se implementó un watermark de cierre de recepción.** Los datos disponibles no permiten demostrar que *todos* los archivos de salida del ciclo se recogieron. No sería legítimo deducirlo de `AchCycle.OperationalStatus == Closed`, de un `ACK`, del cierre de la sesión ni de que la tabla de ingestiones esté vacía.
- La barrera negativa puede demorar una aceptación tácita si existen ingestiones fallidas/ambiguas no resueltas. Se necesita monitoreo y proceso de resolución operativa; NO se debe desactivar sin investigación.
- En CENIT, los resultados discordantes después de un estado terminal requieren revisión humana; no se automatiza una reversión que corrompería la evidencia histórica.

## Homologación necesaria

1. Instalar/restaurar .NET compatible y ejecutar `dotnet test` de las pruebas APN y de `CenitChamberResponseLifecycleTests`.
2. Ejecutar prueba multi-DB (SQL Server y PostgreSQL) con concurrencia real Quartz vs. recepción MFT/CENIT y rollback de respuesta + cambio de estado.
3. Confirmar con propietarios de los canales MFT/CENIT si existe manifiesto o evento autorizado que garantice final de entrega por cámara/ciclo. Solo entonces diseñar la señal positiva certificada y el gate definitivo.
4. Revisar alertas/operación para archivos ambiguos, NACK, transferencias fallidas y `PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW`.
5. Medir la frecuencia de 30 minutos del job contra el SLA de visibilidad; la latencia no se declara cerrada.

## Criterio de entrega

**Código y documentación entregados; no homologado para producción.** Sin un contrato verificable de cierre externo, APN-R07 permanece abierto, aunque este incremento amplía la protección sobre fuentes de respuesta realmente implementadas.
