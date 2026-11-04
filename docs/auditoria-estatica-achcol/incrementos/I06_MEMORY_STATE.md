# MEMORY_STATE — auditoría ACH Colombia

**Fuente portátil de continuidad.** Leer primero este archivo, `ESTADO_INCREMENTOS.md` y `incrementos/I06_CICLO_5_REGLA_A_REGLA.md`. Foto inmutable de corte: `incrementos/I06_MEMORY_STATE.md`.

- Corte **2026-11-01**, cámara **ACHCOL exclusivamente**. CENIT fuera de este plan.
- Último ZIP completo: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I06_2026-11-01.zip`. Base inmediata I05: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I05_2026-11-01.zip`. El siguiente incremento debe partir de I06, no reconstruir.
- **I01–I06 TERMINADOS (6/10 = 60 % de hitos); I07–I10 PENDIENTES (4). Cero ciclos certificados.** Porcentaje del plan estático documental, no de conformidad financiera.
- **I06 terminado estáticamente, ciclo 5:** **18 controles**: 6 TRAZADA, 6 PARCIAL, 5 NO_DEMOSTRADA, 1 INCUMPLIMIENTO_CONFIRMADO (extensión H07). Recepción **12:31–14:30**; planilla 14:30–15:00, pago 15:00–15:15, liberación 15:15–15:30; fuentes ordinarias de devoluciones **C1/C2/C3/C4 actuales**; PSE/SSS/DIAN en la fila de C5.
- Verificación ligera de fuente/código/test C5 realizada con inspección Python: **6/6** comprobaciones positivas; **no** equivale a tests .NET ni a prueba bancaria.
- Política `ACHCOL-7C-20261014` en código y seeder; despliegue/aceptación oficial **sin probar**. Manual V35 histórico cinco ciclos. **H02 MEDIA** abierta; conflicto de `11:01pm` C4 en fuente siete ciclos registrado en I05, sin sustituir la fuente.
- **H01 ALTA, ampliada C5:** `ReturnWindows` describe C1–C4 del día operativo actual, pero la política resoluble/snapshot genérico no transporta esa matriz; en ruta examinada la devolución no contrasta explícitamente combinación de origen + día + ciclo destino. Requiere enforcement en I09, sin asumir ausencia de otras validaciones no examinadas.
- **H03 ALTA, pendiente I09:** planilla, pago y liberación horarios definidos; sin confirmación externa de conciliación/liquidación.
- **H04 pendiente I09:** devolución de una devolución (ROR) y causales completas.
- **H05 MEDIA:** R10 del día anterior deriva a reclamación según `AchReturnEligibilityPolicyComponent.cs:134-139`; conservar causal y ventana separadas.
- **H06 MEDIA condicional:** disparador nocturno C1 puede confundir fecha de calendario y fecha de procesamiento; no afecta automáticamente C5.
- **H07 ALTA, extendido a C5:** `AchReturnEligibilityPolicyComponent.cs:148-160` ordena `StartTime` plano, coloca C1 último; ruta R10 C1→C5 puede fallar por orden inválido. **NO afirmar R10 C1→C5 permitida tras arreglar el orden:** quedan `MaxCycles` y condición causal.
- **H08 MEDIA, extendida C5:** PSE/SSS/DIAN expresos en C5; hay descripciones de lote, pero no se demostró el control especializado producto/ciclo en el flujo general. Contrastar contra C4 en I09.
- **Próximo I07 — C6:** recepción **14:31–16:00**, planilla 16:00–16:30, pago 16:30–16:45, liberación 16:45–17:00; devoluciones **C2/C3/C4/C5 actuales**, excluye C1 actual; operaciones especiales PSE/SSS/DIAN presentes.
- I06: **solo Markdown** (3 nuevos + 4 vivos); `src/`, `tests/`, frontend, migraciones y configuración heredados **idénticos**. No introducir arquitectura ni fixes conjeturales.
- .NET/DB/Quartz/MFT/ACH **NO_EJECUTADO_EN_ESTE_ENTORNO**: no bloquea la inspección estática, no habilita certificación.
- Entrega obligatoria en cada turno: ZIP completo desde el anterior, instantánea `Ixx_MEMORY_STATE.md`, plan, estado 10 incrementos, manifiesto, resumen de lo hecho y restante. **Sin sobreingeniería**.
