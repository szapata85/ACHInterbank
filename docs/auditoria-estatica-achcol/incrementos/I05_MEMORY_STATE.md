# MEMORY_STATE — auditoría ACH Colombia

**Fuente portátil de continuidad.** Leer primero este archivo, `ESTADO_INCREMENTOS.md` y `incrementos/I05_CICLO_4_REGLA_A_REGLA.md`. Foto inmutable de corte: `incrementos/I05_MEMORY_STATE.md`.

- Corte **2026-11-01**, cámara **ACHCOL exclusivamente**; CENIT se auditará por separado.
- Último ZIP íntegro entregable: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I05_2026-11-01.zip`. Base anterior preservada: `..._I04_2026-11-01.zip`; próximo incremento continuar **desde I05**, no reconstruir desde el baseline.
- **I01–I05 TERMINADOS (5/10 = 50 % de hitos), I06–I10 PENDIENTES (5). Cero ciclos certificados.** No es el porcentaje de cumplimiento normativo.
- **I05 terminado estáticamente, ciclo 4:** 17 controles: **6 trazadas, 5 parciales, 4 no demostradas, 1 incumplimiento lógico estático (extensión H07), 1 conflicto de fuente (H02)**. Apertura codificada **11:01am**, cierre **12:30**, planilla 12:30–13:00, pago 13:00–13:15, liberación 13:15–13:30. Devoluciones permitidas por matriz **P7/C1/C2/C3**.
- Política de siete ciclos `ACHCOL-7C-20261014` en código; V35 tiene cinco; vigencia externa/realidad del despliegue **sin probar**.
- **H01 ALTA abierta**: `ReturnWindows` P7/C1/C2/C3 sólo localizada en catálogo y tests; el snapshot vigente y servicio general no aplican explícitamente el conjunto elegible fuente+día. Brecha de trazabilidad de enforcement.
- **H02 MEDIA abierta, ampliada I05**: fuente de siete ciclos imprime **`11:01pm` en apertura C4** aunque cierra 12:30pm; catálogo confirmado/tests usan **11:01am**. Conservar literal y solicitar ratificación documental. Diferencia V35 (cinco) vs política 7C y fecha de vigencia externa no acreditada.
- **H03 ALTA por comprobar**: condición real de pago/compensación conciliada como requisito de liberación y cierre, hasta I09.
- **H04 pendiente I09**: ROR y causales transversales ACHCOL, no presume conformidad.
- **H05 MEDIA abierta**: R10 del día anterior se dirige a reclamaciones `AchReturnEligibilityPolicyComponent.cs:134-139`; comprobar tratamiento por causal (I09).
- **H06 MEDIA condicional**: disparador nocturno C1 puede confundir fecha local vs fecha de procesamiento; registrar en I02; no afecta al C4 diurno por sí solo.
- **H07 ALTA incumplimiento estático de la rama R10, extendido a C4**: `AchReturnEligibilityPolicyComponent.cs:148-160` usa `OrderBy(StartTime)` y coloca C1 al final; C1→C2/C3/C4 se rechaza con orden no resuelto si alcanza esa ruta. **R10 C1→C4 puede quedar inelegible por MaxCycles aun al arreglar orden**, por lo que no afirmar aceptación automática; requiere prueba de causa específica tras reparación focal.
- **H08 MEDIA no demostrado, extendido a C4**: C3 lista PSE/SSS/DIAN; C4 no; política funcional general no distingue productos. Determinar si otras capas de archivo/lote filtran por ciclo (I09).
- **Próximo I06 — ciclo 5:** recepción 12:31–14:30; planilla 14:30–15:00, pago 15:00–15:15, liberación 15:15–15:30; devoluciones C1–C4. PSE/SSS/DIAN sí figuran en C5.
- Cambios I05: **solamente siete documentos Markdown** (3 nuevos + 4 vivos). `src/`, `tests/`, frontend, configuración intactos desde I04. No desplegar fixes conjeturales.
- .NET/DB/Quartz/MFT/ACH: `NO_EJECUTADO_EN_ESTE_ENTORNO` y **no bloquea auditoría estática**. No emitir certificación financiera.
- Política de entrega: ZIP **completo** desde el precedente, `MEMORY_STATE.md` + fotografía Ixx, estado de 10 incrementos y manifiesto; cada turno indicar hecho, faltante y próximo. Sin sobreingeniería.
