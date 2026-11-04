# MEMORY_STATE — auditoría ACH Colombia

**Fuente portable. Leer en orden:** este archivo → `ESTADO_INCREMENTOS.md` → `incrementos/I08_CICLO_7_REGLA_A_REGLA.md` → `PLAN_INCREMENTAL.md`. Snapshot: `incrementos/I08_MEMORY_STATE.md`.

- **Corte 2026-11-01; ACHCOL solamente.** Base completa: ZIP I07. **I01–I08 TERMINADOS estáticamente = 8/10 hitos (80 % del plan), faltan I09–I10 = 2/10; 0 ciclos certificados.** No extrapolar porcentaje a cumplimiento normativo.
- **I08 C7:** 23 reglas = **9 TRAZADAS, 8 PARCIALES, 6 NO_DEMOSTRADAS, 0 incumplimientos nuevos confirmados**; 14/14 comprobaciones ligeras de fuente pasaron (solo presencia textual, **NO** ejecución .NET).
- **C7 documental:** envío 16:01–18:00, cierre 18:00, planilla 18:00–18:30, pago 18:30–18:45, liberación 18:45–19:00. Devoluciones ordinarias de **C3/C4/C5/C6 del mismo día**, excluye C2; crédito, prenotificaciones, PSE/SSS/DIAN sí; **débito monetario ordinario nuevo NO**.
- **D-I08-01 PRIORITARIA (indicada por usuario):** **NACHA-M puede tener operaciones fuera de ciclos**. NO introducir veto genérico `OutsideCycle=>Reject`. Distintas acciones: generar/preparar, transmisión manual, transmisión automática, recepción tardía, procesamiento/reproceso, y compensación. La guarda de ventana de `AchColombiaManagedMftOutboundWorkflow.cs:49-50,311-329` solo aplica `Automatic`; `ManualOutboundAllowed` habilita ruta manual (`AchColombiaManagedMftImplementationSupport.cs:63-73`). El runbook dispone conservar archivos tardíos, observación `InboundOutsideExpectedWindow`, **no rechazo normativo inventado**. Esto no acredita que ACH Colombia acepte o liquide fuera de horario; pendiente corroboración de contrato/procedimiento y casos de I09.
- **H01 ALTA (C1–C7):** retornos por ciclo y día original están en `RegulatoryCycleScheduleCatalog.cs:178-185` y pruebas, pero no se demostró aplicación en ruta productiva `AchReturnsService`. En C7 C2 queda fuera.
- **H02 MEDIA:** Manual V35 histórico 5 ciclos vs política ACHCOL siete desde 2026-10-14 declarada en código; despliegue/vigencia real sin verificación.
- **H03 ALTA:** planilla, pago, liberación y conciliación integral aún no verificados mediante confirmaciones externas.
- **H04 PENDIENTE I09:** ROR / devolución de una devolución, reglas y causales.
- **H05 MEDIA:** R10 interdiario remite a reclamaciones, revisión de elegibilidad I09.
- **H06 MEDIA CONDICIONAL:** atribución de día operativo en C1 nocturno / disparadores.
- **H07 ALTA:** ordenamiento `StartTime` en R10 sitúa C1 al final, con riesgo C1→C2/C3/C4/C5; **no extender sin fundamento a C6/C7 porque C1 no es origen ordinario elegible**.
- **H08 MEDIA:** falta comprobar enforcement PSE/SSS/DIAN por ciclo, incluido C7.
- **Próximo I09:** auditar subprocesos transversales y casuística NACHA-M *fuera de ventana* (manual, auto, recepción tardía, generación, reintentos, fecha valor, rechazo/compensación, controles de autorización y auditabilidad). No sobreingeniería. I10 cierre de dictamen.
- **Entregables I08:** informe C7, `I08_MEMORY_STATE.md`, `MANIFIESTO_I08.md`, y README/PLAN/ESTADO/MEMORY_STATE vivos actualizados. **No hubo cambios en src/tests/frontend/DB/arquitectura**.
- **Límites:** .NET, DB, Quartz, MFT, SFTP, integraciones ACH y certificación **NO_EJECUTADOS** (no son bloqueantes para auditoría estática, tampoco se cuentan como aprobados).
- **Regla permanente de entrega:** en I09 usar ZIP **completo I08** como base; reporte, snapshot memoria, manifiesto y cuatro documentos vivos, zip total, verificar integridad, SHA256 herencia no modificada, informar hitos restantes y hallazgos.
