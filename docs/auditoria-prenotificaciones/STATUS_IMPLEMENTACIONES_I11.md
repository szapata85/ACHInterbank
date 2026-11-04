# Estado acumulado APN-I01–I11

**Corte:** 2026-11-03. `AUDITORÍA` significa revisión documental/estática; `EN FUENTE` código incorporado, **sin compilación/ejecución validada**.

| Incremento | Alcance | Código productivo | Pruebas realizadas | Estado |
|---|---|---|---|---|
| I01 | Inventario de estados | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I02 | Calendario ACH Colombia 7 ciclos | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I03 | CENIT 5 ciclos | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I04 | Respuestas y correlación | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I05 | Aceptación tácita | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I06 | Consistencia de cuentas | Sin cambios | Revisión estática | AUDITORÍA DOCUMENTAL |
| I07 | Consolidación documental | Sin cambios | Informe y matriz | CIERRE DOCUMENTAL |
| I08 | Horas de liberación y ventana completa | Corregido | Python estática 6/6 | IMPLEMENTADO EN FUENTE |
| I09 | Guards de transmisión y vencimiento | Corregido | Python estática 8/8 | IMPLEMENTADO EN FUENTE |
| I10 | MFT, CENIT, devoluciones tardías | Corregido | Python estática 10/10 | IMPLEMENTADO EN FUENTE |
| **I11** | **Ciclos cancelados + fechas operativas UTC/local + contrato externo** | **Corregido** | **Python 12/12; matrices 6/6; C# escrito** | **IMPLEMENTADO EN FUENTE / CONTRATO NO INTEGRADO** |

**Regresiones estáticas acumuladas realizadas ahora:** 42/42 PASS. **Pruebas C# ejecutadas ahora:** 0; **escenarios UAT homologados acumulados:** 0/22.

**Riesgo principal:** ausencia de comprobante externo positivo de recepción completa por cámara/ciclo; el contrato propuesto no lo suple.

**Próxima base de trabajo:** ZIP completo APN-I11, no ZIP delta ni APN-I10.
