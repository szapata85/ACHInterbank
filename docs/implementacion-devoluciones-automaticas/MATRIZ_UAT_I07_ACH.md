# MATRIZ DE ACEPTACIÓN Y NO-GO — I07-ACH

**Solo ACH Colombia.** No usar esta matriz para homologar CENIT ni para autorizar una transmisión real. Las pruebas siguientes **NO fueron ejecutadas** en este entorno: se entregan como guía UAT reproducible.

| Caso | Escenario | Resultado obligatorio | Evidencia de salida | Estado |
|---|---|---|---|---|
| ACH-I07-01 | Archivo único generado y reconstruido | SHA-256 idéntico byte a byte | hash original y recuperado, trazas sin datos personales | PENDIENTE |
| ACH-I07-02 | Cambio del contenido de reconstrucción | Descarga 409, nunca entregar bytes | error controlado | PENDIENTE |
| ACH-I07-03 | Evento original ausente | Descarga 409, revisión manual | bitácora de revisión | PENDIENTE |
| ACH-I07-04 | Evento JSON inválido o hash incorrecto | Descarga 409 | id de transacción en ambiente de prueba | PENDIENTE |
| ACH-I07-05 | Falla navegador luego de generar | Segunda descarga no reserva nueva secuencia ni nuevo retorno | conteo persistido antes/después | PENDIENTE |
| ACH-I07-06 | Dos operadores generan misma candidata | Una sola reserva y un archivo; uno recibe conflicto | transacciones SQL concurrentes | PENDIENTE |
| ACH-I07-07 | Reintento de commit fallido / restart | No duplicar retorno, detectar estado incierto | eventos y registros SQL | PENDIENTE |
| ACH-I07-08 | Evento original CENIT | No accesible por endpoint ACH | respuesta de conflicto / auditoría | PENDIENTE |
| ACH-I07-09 | Modo DESACTIVADO / CON_APROBACION | No ejecuta generación autónoma | métricas de runner | PENDIENTE |
| ACH-I07-10 | Flag técnico apagado | No ejecuta generación autónoma, aun con modo AUTOMATICO | configuración y bitácoras | PENDIENTE |
| ACH-I07-11 | Ventana ACH cinco y siete ciclos | Elegibilidad según calendario confirmado de vigencia | acta de versión operativa | PENDIENTE |
| ACH-I07-12 | Estado ReturnedByEpr tras generar sin enviar | Informe no afirma liquidación real; validar semántica financiera | transacción, estado y reporte | PENDIENTE |
| ACH-I07-13 | Retransmisión o acuse cámara | No inferir envío por estado local; usar evidencias transmisión | acuses/compensación de UAT | PENDIENTE |
| ACH-I07-14 | Crédito/debito/prenotificación | Cumple causal, NACHA-M y restricciones de retorno | archivos golden y respuestas core | PENDIENTE |

## Comandos sugeridos en el ambiente de integración

```bash
dotnet build ACHInterbank.sln --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter 'FullyQualifiedName~AchColombiaAutomaticReturnI06Tests' --no-restore
```

Antes de aplicar migraciones: revisar versión e historial **SQL Server/PostgreSQL** en un entorno aislado. No modificar datos productivos para comprobar hashes. Capturar salida real, timestamp, versiones de BD, artefactos y hashes en un acta nueva; hasta entonces el estado es **NO-GO**.
