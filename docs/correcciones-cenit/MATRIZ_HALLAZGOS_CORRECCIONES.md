# Matriz inmutable de los 13 hallazgos — después de F08

**Ningún HC cerrado:** no hay resultados `dotnet test` ni integración oficial ejecutados en este entorno. Ver `REGISTRO_CORRECCIONES.md` y evidencias originales I10.

| ID original | Descripción | Fase | Estado | Pendiente imprescindible |
|---|---|---|---|---|
| HC-CENIT-001 | Cola entre ciclos | F03 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | xUnit/SQL/Quartz/exportación concurrente y fechas valor interdiarias; PT-01–03/PT-20/PT-21 |
| HC-CENIT-002 | Correlación ROR C1 | F05 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Probar ciclo previo bancario/archivo raw correlacionado/evento padre con SDK y SQL; confirmar política del operador |
| HC-CENIT-003 | Inclusión neteo antes de exclusiones | F01 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Pruebas C# reales, DB/Quartz y resultados oficiales |
| HC-CENIT-004 | R23 mismo día | F04 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | xUnit, PT-07 y validación normativa de excepción mismo día |
| HC-CENIT-005 | Prioridades PPD/CCD/CTX | F01 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Pruebas C# reales, DB/Quartz y resultados oficiales |
| HC-CENIT-006 | Rechazo por liquidez simulada | F01 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Pruebas C# reales, DB/Quartz y resultados oficiales |
| HC-CENIT-007 | R06 C5 | F04 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | xUnit, PT-09 y correlación de origen C4 oficial |
| HC-CENIT-008 | ROR R74 | F05 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | xUnit/SQL/operador: R32 sobre crédito monetario con adenda campo 3, casos negativos, compatibilidad |
| HC-CENIT-009 | Plazo R23 | F04 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | xUnit, PT-08, notificación y recepción bancarias verificables |
| HC-CENIT-010 | Alfabeto NACHA | F06 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | PT-11: .NET, entrada NACHA real y ACH Colombia (Ñ/ñ), perfil publicado |
| HC-CENIT-011 | Códigos STA vs NACHA | F06 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | PT-12: .NET, fallas físicas/formales y ruta STA comparadas con operador |
| HC-CENIT-012 | ADV | F02 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Compilación/xUnit, ensayo con ADV oficial 280 y conciliación Tesorería |
| HC-CENIT-013 | NACK individual | F02 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN | Compilación/xUnit, DB transaccional, ACK y NACK reales, prenotificaciones |

**Resumen:** 13 implementados pendientes de verificación, 0 abiertos sin implementación, 0 cerrados. 13 sin cierre.

**F07:** evidencia adicional de rutas, 6 fixtures golden de 1.060 bytes y ROR sintético, 24/24 controles de integración estática, 113/113 previos; 11 casos de xUnit incorporados pero **0 ejecutados**. Los trece estados permanecen implementados/pendientes de verificación; no hay cierre por pasar controles de fuente.

## Dictamen de cierre F08

Los 13 IDs conservan estado **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN / ABIERTO**. No hay TRX, DB/Quartz, gateway de operador, pruebas de ACH Colombia funcionales ni actas certificadas; por ello **0 cerrados formalmente**. La matriz de decisiones y 22 PT pendientes se registran en `F08_MATRIZ_CIERRE_HALLAZGOS.md` y `F08_PLAN_PRUEBAS_Y_APROBACIONES.md`. F08 no modifica código de las fases previas.
