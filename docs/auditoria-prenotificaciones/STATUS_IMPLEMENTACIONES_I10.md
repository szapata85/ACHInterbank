# Estado acumulado APN-I01–I10

Corte: 2026-11-03. `EN_FUENTE` significa código incorporado; `ESTÁTICO` no significa probado funcionalmente.

| Incremento | Alcance | Código productivo | Revisión / pruebas disponibles | .NET / BD / UAT | Estado |
|---|---|---|---|---|---|
| I01 | Estados/intentos | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I02 | ACH Colombia 7 ciclos | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I03 | CENIT 5 ciclos | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I04 | Devoluciones/correlación | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I05 | Aceptación tácita | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I06 | Elegibilidad/cuenta | Sin cambios | Inspección estática | No ejecutadas | AUDITORÍA_DOCUMENTAL |
| I07 | Informe y matriz | Sin cambios | Cierre documental | No ejecutadas | CIERRE_DOCUMENTAL |
| I08 | Plazos/release end | Corregido | 6/6 pruebas Python estáticas | No ejecutadas | IMPLEMENTADO_EN_FUENTE |
| I09 | Guardias job/legado | Corregido | 8/8 pruebas Python estáticas | No ejecutadas | IMPLEMENTADO_EN_FUENTE |
| **I10** | **MFT + gateway CENIT + tardías** | **Corregido** | **10/10 Python estáticas y C# preparado** | **No ejecutadas** | **IMPLEMENTADO_EN_FUENTE, NO_HOMOLOGADO** |

**Certificación funcional acumulada:** 0/22 escenarios homologados (no se trata de fallas, sino de ausencia de ejecución sobre infraestructura).  
**Cambios en esquemas BD I10:** ninguno.  
**Riesgo mayor:** falta un contrato verificable de recepción completa de todos los archivos; no puede cerrarse solo mediante una consulta a filas existentes.
