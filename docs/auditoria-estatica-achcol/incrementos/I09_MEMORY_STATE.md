# MEMORY_STATE — auditoría ACH Colombia

**Fuente portable de continuidad:** este archivo → `ESTADO_INCREMENTOS.md` → `incrementos/I09_TRANSVERSAL_REGLA_A_REGLA.md` → `PLAN_INCREMENTAL.md`. Foto congelada: `incrementos/I09_MEMORY_STATE.md`.

- **Corte 2026-11-01, cámara ACH Colombia.** ZIP completo base I08; **I01–I09 TERMINADOS = 9/10 hitos (90 %); solo resta I10 (1/10).** 0 ciclos certificados. No confundir porcentaje de hitos con cumplimiento regulatorio.
- **I09 TRANSVERSAL:** 41 reglas: 23 TRAZADAS, 12 PARCIALES, 4 NO_DEMOSTRADAS, 2 INCUMPLIMIENTOS_ESTATICOS de código. 23/23 Rxx y 32/32 Dxx presentes en el catálogo semilla; presencia no equivale a conformidad de aplicación.
- **H01 ALTA:** ventanas de devolución por ciclo original/día operativo declaradas, enforcement en ruta general no demostrado; mantenida I01–I09.
- **H02 MEDIA:** diferencia documental manual V35 (5 ciclos) / anexo siete ciclos; vigencia normativa y despliegue externos pendientes.
- **H03 ALTA:** conciliación planilla/pago/liberación/ledger y confirmación externa no demostradas.
- **H04 ALTA/PARCIAL:** ROR auditado en I09. `GenerateAsync` es artefacto de auditoría `ROR|`, `GenerateNachaAsync` es archivo NACHA; validar inyección obligatoria de política ciclo, contrato ROR ACH y validador efectivo de registro tipo 7; método local ValidateNachaRecords no invocado en esa clase. Doble generación productiva tiene comprobación por conjunto de flujos.
- **H05 MEDIA:** oportunidad R10 y base/documentación de prenotificación requieren contrastes adicionales.
- **H06 MEDIA condicional:** fecha operativa en C1 nocturno/disparadores pendiente confirmación.
- **H07 ALTA (confirmación estática):** `OrderBy(StartTime)` rompe orden operativo nocturno para R10 C1→C2/C3/C4/C5; C1→C6/C7 queda fuera de ventana ordinaria, no asumir otro incumplimiento.
- **H08 MEDIA:** enforcement PSE/SSS/DIAN por producto×ciclo no demostrado; clase general sí tiene política.
- **H09 NUEVO, ALTA, INCUMPLIMIENTO_ESTATICO de mapeo Dxx:** ACH V35 Anexo 3: D01 fecha efectiva anterior, D02 fecha efectiva inválida, D04 prenotificación distinto cero. `NachaInboundValidation.cs` emite D01 secuencia duplicada, D02 bloqueo de diez, D04 número lotes; `AchCauseCodePolicy.cs` trata Dxx genéricamente como FileRejection y `ReturnReasonConfiguration.cs` contiene 32 códigos como devolución operador. **No probado** impacto en ACH productivo/rail del parser; no aplicar reemplazo global porque CENIT STA Dxx tiene semántica distinta.
- **D-I08-01 permanente:** NACHA-M puede prepararse/transmitirse manualmente o recibirse tarde fuera de ciclos. No crear `OutsideCycle=>Reject`. Automático saliente sí comprueba ventana; manual exige habilitación; archivo tardío se conserva y observa; compensación externa requiere evidencia independiente.
- **I09 evidencia:** informe `I09_TRANSVERSAL_REGLA_A_REGLA.md`, snapshot `I09_MEMORY_STATE.md` y `MANIFIESTO_I09.md`, 4 documentos vivos actualizados. Sin modificaciones en `src/`, `tests/`, DB, frontend o arquitectura.
- **No bloqueos por ambiente chat:** .NET/DB/Quartz/MFT/SFTP/ACH externa NO_EJECUTADO; no se cuentan como conformes ni como fallas de código. Auditoría estática sí avanzó.
- **Próximo I10:** consolidar hallazgos H01–H09 (sin duplicados), clasificar incumplimientos vs no demostrados, identificar correcciones mínimas y pruebas de certificación, dictamen final; entregar ZIP completo I10 con snapshot y memory state. **Resta 1 incremento**.
