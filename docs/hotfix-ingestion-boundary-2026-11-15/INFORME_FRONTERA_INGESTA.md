# Corrección focal de frontera de ingesta ACH Colombia — 15/11/2026

**Fuente única:** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_FINAL_ACH_DATES_R10_2026-11-15.zip` · SHA-256 `8c5c776758573b32f781088e516c5ab63789ebea362103164c7935f2af04fdfc`

**Estado:** `CODIGO_CORREGIDO_PRUEBAS_ESCRITAS_PENDIENTE_COMPILACION_XUNIT`. No constituye homologación, ni cierre formal de I01–I13, ni autorización de producción.

## Defecto confirmado

`IncomingNachaAdmissionValidator.ValidateAsync` rechazaba el archivo completo con `EFFECTIVE_DATE_MISMATCH` cuando **el primer** lote T5 tenía fecha efectiva distinta de la fecha operativa y bloqueaba `ACHCOL_EFFECTIVE_DATE_INVALID` para fecha T5 inválida o vacía, antes de que `NachaParserService` pudiera atribuir D01/D02 **por transacción T6**. La frontera omitía la diferencia normativa ACH Colombia V35, numeral 2.1.3.1 y anexo 3 (devolución por operador) versus rechazo físico fatal. También se detectó que el parser de la ruta ACHCOL `ResueltoSinCiclo` buscaba un `AchCycle` interno inexistente o infería uno, perdiendo la fecha operativa ya resuelta por la ingesta.

## Ajuste real y delimitado

1. **`IncomingNachaAdmissionValidator.ValidateAsync`**: consulta la cámara resuelta en la DB y solo aplica la excepción semántica si está activa, identificada como `ACHCOL` y la resolución es válida/no ambigua. ACHCOL permite que la fecha T5 incorrecta (pasada, futura, inválida o vacía) llegue al parser por lote; no se declara aceptada la transacción. CENIT y demás rutas conservan `EFFECTIVE_DATE_MISMATCH`. Se preservan `HEADER_DATE_MISMATCH`, correlación nombre/T1, tamaño, hora, calendario, autorización de cámara y controles de ciclo.
2. **`NachaParserService.ParseAndSaveDetailedAsync` y `ParseFileHeaderLinq`**: para ACHCOL con cámara y fecha operativa resueltas y **sin** `AchCycle` interno, impide inferir otro ciclo; compara la fecha de T1 y la cámara con el contexto y falla en cerrado `ACHCOL_OPERATIONAL_DATE_UNRESOLVED` si difieren. Aplica fecha autorizada al cálculo D01/D02 por lote y a sus T6. Mantiene intacto el procesamiento CENIT con ciclo.
3. **`NachaInboundValidation.GetAchColombiaEffectiveDateFailure`**: sin modificación; se reutiliza para D01 (fecha anterior), D02 (vacía, inválida o futura) sin tocar D04 o los catálogos CENIT.

## Pruebas agregadas (código xUnit, NO EJECUTADAS)

| Archivo | Casos |
|---|---|
| `IncomingNachaAdmissionAchColFileBasedTests.cs` | T5 anterior, posterior, inválido, vacío; archivo válido; varios lotes sin rechazo por fecha efectiva inicial; guardas de nombre, hora, festivo e identificación de cámara existentes |
| `IncomingNachaTraceabilityCoreTests.cs` | Diferencia de fecha T5: CENIT sigue rechazando el archivo; ACHCOL con ciclo configurado permite evaluación posterior del lote |
| `AchTransactionNachaTests.cs` | NACHA físico 106 bytes: fecha válida y cámara ACHCOL sin ciclo, D01 anterior y D02 futuro/vacío/inválido por T6; fecha operativa contradictoria rechazada técnicamente; pruebas anteriores preservadas |

## Evidencia ejecutada (solo fuente)

- Verificador **nuevo** `scripts/test/achcol_incoming_date_admission_boundary_static_check.py`: **30/30 PASS_STATIC**.
- Verificador **anterior** `predeploy_three_hotfixes_static_check.py`: **41/41 PASS_STATIC**.
- Verificador `cenit_post_i13_ror_c5_static_check.py`: **20/20 PASS_STATIC**.
- Verificadores históricos `cenit_i01..i10`, `i12`, `i13`: **12/12 scripts**, 391 comprobaciones estáticas previas, sin fallos.
- Evidencia exacta en `evidencias/*.log` y `regresion_estatica.json`. Los nombres de pruebas xUnit en el código no representan resultados runtime.

## Pendiente y riesgo residual

- `dotnet restore`, `dotnet build` y `dotnet test`: **NO EJECUTADOS** (SDK/compilador .NET no instalado); compilar y correr pruebas con `.NET 10` en Visual Studio/CI antes de desplegar.
- Ejecutar una ingesta real con **varios lotes** y archivos físicos de cada perfil publicado, comprobar que la devolución por operador se persiste/procesa correctamente y que los controles físicos siguen deteniendo los archivos fatales; integrar DB/Quartz y escenarios de concurrencia.
- Verificar retorno físico/salida contra operador y homologación por el equipo autorizado. **UAT** queda exclusivamente para la etapa final.
- Los **13 correctivos CENIT**, HC-CENIT-002 calendario C5/ROR, HOTFIX-02 R10 y otros hotfixes se preservan sin modificaciones de código. 0/13 casos CENIT formalmente cerrados. **NO-GO producción**.

**Siguiente actividad:** G1–G3 recorrido técnico: compilar, ejecutar xUnit focal y total con reporte TRX, comprobar archivos NACHA físicos por cámara y calendario; G4 UAT final posterior. No tocar I14.
