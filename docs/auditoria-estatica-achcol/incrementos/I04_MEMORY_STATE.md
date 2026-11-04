# I04_MEMORY_STATE — fotografía fija al cierre del incremento

**Fuente portátil de continuidad.** Leer primero este archivo, luego `ESTADO_INCREMENTOS.md` y `incrementos/I04_CICLO_3_REGLA_A_REGLA.md`. Fotografía inmutable: `incrementos/I04_MEMORY_STATE.md`.

- Corte: **2026-11-01**. Cámara: **ACHCOL exclusivamente**; CENIT fuera de este tramo.
- Último ZIP completo: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I04_2026-11-01.zip`.
- Base para I04: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I03_2026-11-01.zip`. Continuar siempre desde **I04** para I05.
- Plan: **4/10 incrementos terminados (40 % de hitos), 6 pendientes I05–I10**. **0 ciclos certificados**: el porcentaje no representa cumplimiento bancario.
- I04: **TERMINADO estáticamente**, ciclo 3, **16 controles = 6 trazadas, 5 parciales, 4 no demostradas, 1 incumplimiento estático** (extiende H07, no nueva falla). Se documentó C3 de 09:01–11:00 y fuentes P6/P7 (n-1) + C1/C2 (n).
- Política de proyecto `ACHCOL-7C-20261014` con 7 ciclos desde 2026-10-14 por decisión interna; histórico V35 de 5 separado. Constancia externa de vigencia y despliegue sin demostrar (H02).
- **H01 ALTA abierta**: matriz `ReturnWindows` P6/P7/C1/C2 declarada y probada como dato, no se encontró enforcement general en `AchReturnsService`/`CycleTransactionPolicy`; pendiente I09.
- **H02 MEDIA abierta**: diferencia documental de V35 cinco ciclos vs anexo siete y activación externa no demostrada.
- **H03 ALTA por comprobar**: pago de compensación confirmado + conciliación como barrera de liberación, pendiente I09.
- **H04 pendiente I09**: ROR, causales y rechazo transversal sin dictamen todavía.
- **H05 MEDIA abierta**: ruta R10 interdiaria P6/P7 redirige a reclamaciones (`AchReturnEligibilityPolicyComponent.cs:134-139`); validar contra regla específica por causal.
- **H06 MEDIA condicional**: disparador nocturno C1 usa fecha local donde podría necesitar fecha operativa de corte; I02.
- **H07 ALTA confirmada estáticamente en ruta R10 y extendida I04**: `AchReturnEligibilityPolicyComponent.cs:148-160` ordena ciclos por `StartTime`; C1 (19:01 del día anterior) queda detrás de C2–C7 y falla R10 C1→C2/C3. Requiere corrección pequeña y prueba real antes de homologar, **sin afirmación de fallo productivo observado**.
- **H08 MEDIA nuevo I04, NO_DEMOSTRADO**: la fuente permite PSE/SSS/DIAN en C3; hay descripciones registradas, pero la política genérica por tipo no hace filtro específico por producto. Rastrear flujo completo en I09, no imputar incumplimiento.
- Próximo **I05 ciclo 4**: recepción 11:01–12:30; planilla 12:30–13:00, pago 13:00–13:15, liberación 13:15–13:30; devoluciones P7/C1/C2/C3; ver bordes R10 y productos autorizados.
- Código, tests, frontend y configuración **sin cambios I01–I04**. Entregables I04: ficha `I04_CICLO_3_REGLA_A_REGLA.md`, foto `I04_MEMORY_STATE.md`, `MANIFIESTO_I04.md`; actualización de 4 documentos vivos.
- Sin ejecución .NET/DB/Quartz/MFT ni homologación (`NO_EJECUTADA_EN_ESTE_ENTORNO`). **No bloquea auditoría estática**; no calificar como aprobado.
- Reglas: sin sobreingeniería; solo evidencia con archivo/línea; no crear capas nuevas; ZIP completo por incremento; status, pendientes y hallazgos en cada entrega.
