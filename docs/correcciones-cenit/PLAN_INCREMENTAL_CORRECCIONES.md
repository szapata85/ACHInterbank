# Plan incremental de correcciones CENIT — F01–F08

Línea base única: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip` (SHA-256: `9c298cf0ae8f8215b4ab00a186c14b1bcc18be78c139b3d8584edec5fa54aeef`). Historial I01–I10 intacto. Documentos de referencia: `../auditoria-estatica-cenit/DICTAMEN_ESTATICO_FINAL_CENIT.md`, `HALLAZGOS_CONSOLIDADOS_CENIT.md`, `PLAN_CORRECCIONES_MINIMAS_CENIT.md`, `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md` y matrices I01–I10.

| Fase | Hallazgos principales | Entregable mínimo | Estado |
|---|---|---|---|
| F01 | 003, 005, 006 | Separación simulación/efecto de cámara, prioridades informativas | IMPLEMENTADA; .NET/DB/Quartz pendientes |
| F02 | 012, 013 | NACK por operación, ADV para conciliación | IMPLEMENTADA ESTÁTICAMENTE; .NET/DB/gateway pendientes |
| F03 | 001 | Ingreso real de colas por ciclo, persistencia e idempotencia | IMPLEMENTADA ESTÁTICAMENTE; pruebas de integración pendientes |
| F04 | 004, 007, 009 | R23, R06, fechas y calendarios | IMPLEMENTADA ESTÁTICAMENTE; .NET/DB/operador pendientes |
| F05 | 002, 008 | Correlación ROR C1 con raw/evento; R74 exclusiva R32 crédito monetario con adenda | IMPLEMENTADA ESTÁTICAMENTE; xUnit/DB/gateway pendientes |
| F06 | 010, 011 | NACHA-M físico CENIT por perfil y errores STA separados | IMPLEMENTADA ESTÁTICAMENTE; .NET/DB/gateway pendientes |
| F07 | Todos | Regresión transversal ACH Colombia/CENIT y conciliación | EJECUTADA ESTÁTICAMENTE: 24/24 + 113/113; .NET/SQL/Quartz/operador PENDIENTES |
| F08 | Todos | Matriz final, dictamen condicionado, plan PT-01–PT-22, evidencias y ZIP íntegro | ENTREGADA DOCUMENTALMENTE; NO-GO, 0 HC cerrados |

**Límites obligatorios:** CUD fuera de alcance; su revisión permanece manual en Tesorería. No replicar optimización ni liquidación del Banco. No modificar códigos/rutas ACH Colombia por conveniencia. **Criterio de cierre HC:** causa corregida, pruebas ejecutadas, rutas/estados documentados y ausencia de regresiones en alcance probado. F01 y F02 completadas como entregas incrementales, no como certificación ni cierre de HC.

**Cierre F08 (2026-11-02):** 8/8 entregas documentadas; 13 HC implementados pendientes de verificación y 22 PT sin ejecutar. Ver `F08_DICTAMEN_FINAL_CONDICIONADO.md`; no es homologación.
