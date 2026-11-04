# MEMORY_STATE — auditoría ACH Colombia

**Fuente portable de continuidad (leer al reanudar).** Orden: `MEMORY_STATE.md` → `ESTADO_INCREMENTOS.md` → `incrementos/I07_CICLO_6_REGLA_A_REGLA.md` → `PLAN_INCREMENTAL.md`. Foto congelada: `incrementos/I07_MEMORY_STATE.md`.

- **Corte 2026-11-01; solo ACHCOL.** No mezclar reglas CENIT.
- **ZIP entregado I07:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I07_2026-11-01.zip` (base íntegra I06). **I01–I07 TERMINADOS: 7/10 (70 % de hitos). Faltan I08–I10: 3/10. 0 ciclos certificados.** Esto mide incrementos revisados, NO conformidad.
- **I07 auditó C6:** 19 reglas = **6 TRAZADA, 7 PARCIAL, 6 NO_DEMOSTRADA, 0 incumplimientos nuevos confirmados**. Ventanas C6: envío 14:31–16:00, planilla 16:00–16:30, pago 16:30–16:45, salida 16:45–17:00. Devoluciones ordinarias de **C2/C3/C4/C5 del día actual**, **excluye C1**. Admite nuevos créditos/débitos/prenotificaciones y PSE/SSS/DIAN.
- **Chequeos fuente I07:** 7/7 positivos sobre texto código/test; **no** ejecución de pruebas .NET.
- **H01 ALTA:** matriz de ventanas definida en `RegulatoryCycleScheduleCatalog.cs` pero no demostrada en la ruta productiva general `AchReturnsService`/`CycleTransactionPolicy`. Para C6 la negativa C1 está en test de catálogo, **no** en enforcement demostrado. Investigar al consolidar I09.
- **H02 MEDIA:** manual histórico V35 cinco ciclos vs política siete `ACHCOL-7C-20261014` declarada en código. Activación/despliegue externo no comprobados. Inconsistencia literal del horario C4 en PDF siete ciclos tratada en I05, sin alterar fuente.
- **H03 ALTA:** horas planilla/pago/liberación configuradas, pero confirmación externa y conciliación end-to-end no comprobadas. I09.
- **H04 PENDIENTE:** devolución de una devolución/ROR y causales completas, I09.
- **H05 MEDIA:** tratamiento R10 para devolución de día operativo anterior remite a reclamaciones según `AchReturnEligibilityPolicyComponent.cs:134-139`, evaluar transversal I09.
- **H06 MEDIA CONDICIONAL:** C1 nocturno, posible confusión fecha civil/operativa en disparadores, sin extender a C6.
- **H07 ALTA:** ordenamiento `StartTime` en R10 deja C1 último, defecto para oportunidades que involucran C1. **C1→C6 debe rechazarse como devolución ordinaria por ventana C6**: no inventar una incidencia R10 C6 sin evidencia. Para C2–C5 revisar `MaxCycles` propio de R10 (I09).
- **H08 MEDIA:** PSE/SSS/DIAN C6 explícitos en anexo; descripción de lote encontrada, no enforcement producto/ciclo demostrado. I09.
- **Siguiente I08:** C7 entrada 16:01–18:00, planilla 18:00–18:30, pago 18:30–18:45, liberación 18:45–19:00; devoluciones C3/C4/C5/C6 del día actual, excluye C2, **prohibido débito monetario ordinario**; crédito/prenotificaciones/PSE/SSS/DIAN sí enumerados.
- **Integridad / código:** solo 3 documentos nuevos (I07 informe, foto memoria, manifiesto) y 4 vivos actualizados. `src/`, `tests/`, frontend, DB, migraciones, configuraciones y demás archivos heredados intactos.
- **No hay ambiente bancario ejecutado:** .NET/DB/Quartz/MFT/ACH **NO_EJECUTADO**. No bloquear auditoría estática; no declarar pruebas aprobadas.
- **Regla permanente de entrega:** usar ZIP I07 como base para I08; agregar informe, snapshot memoria, manifiesto, actualizar cuatro docs vivos, zip completo, verificar integridad y archivos no modificados, informar estado de 10 hitos y los restantes. **Sin sobreingeniería**.
