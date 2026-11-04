# PLAN_INCREMENTAL — auditoría estática CENIT

**Identificador:** CENIT-ESTATICA-2026-11-01 | **Línea base:** ZIP ACHCOL-I10 del 01-11-2026 | **Estado:** I01–I10 COMPLETADOS (10/10; 100 % de hitos). La fase de correcciones y certificación queda fuera de este plan.

## Método constante

En cada incremento: leer `MEMORY_STATE.md`; preservar intacta la base ACH Colombia; comparar la regla de la fuente vigente (fecha, sección, alcance, excepciones) contra **la ruta ejecutable** (API/job/servicio → aplicación → persistencia/configuración → archivo/respuesta/estado); distinguir catálogo de enforcement; verificar casos límite, fecha valor, cambio de día, duplicados y fallos de contabilización; documentar archivo, método y líneas. Marcar `Cumple estáticamente`, `Parcial`, `Incumplimiento estático demostrado`, `No demostrable` o `Requiere prueba dinámica/externa`. No confundir “existe prueba” con “fue ejecutada”. Consolidar hallazgos por causa raíz. Sin cambios productivos.

**Regla de no bloqueo temporal:** separar generación, recepción, envío automático, envío manual autorizado, encolamiento, reproceso y compensación. No introducir un veto global fuera de horario. Analizar el código real; no trasladar restricciones de ACH Colombia a CENIT.

**Prioridad:** acceptance/rejection incorrectos, devolución extemporánea, liquidación o contabilización duplicada, correlación insuficiente, pérdida de trazabilidad, normalización de fecha/hora.

| Incremento | Alcance y entregable verificable | Estado |
|---|---|---|
| I01 | Inventario completo, arquitectura/rutas candidatas, jerarquía documental, matriz inicial, control SHA-256, memoria | COMPLETADO |
| I02 | Ciclo 1 (07:30–10:30): entradas ordinarias/prenotes; R23 y ROR del día hábil previo; archivos, cutoff, colas, compensación | COMPLETADO — 24 reglas, HC-CENIT-001…003 |
| I03 | Ciclo 2 (11:00–13:00): devoluciones de C1, ROR, archivos, fecha valor, pre-notes, liquidación | COMPLETADO — 28 reglas; 1 hallazgo nuevo HC-CENIT-004, 2 causas previas ampliadas |
| I04 | Ciclo 3 (13:30–15:00): devoluciones de C2, ROR same-day, archivos, límites y liquidación | COMPLETADO — 30 reglas; HC-CENIT-005 nuevo, HC-001/003/004 ampliados |
| I05 | Ciclo 4 (15:30–17:15): últimas entradas monetarias ordinarias, devoluciones C3, optimización por insuficiencia | COMPLETADO — 34 reglas; HC-CENIT-006 nuevo; HC-001/003/004/005 ampliados |
| I06 | Ciclo 5 (17:45–18:45): entradas ordinarias prohibidas, excepciones R23/ROR/devoluciones C4, corte/salida 19:15 y optimización | COMPLETADO — 36 reglas; HC-CENIT-007 nuevo; HC-001/003/004/005/006 ampliados |
| I07 | Causales R01…R35 aplicables, R23, ROR R60–R74 según Anexo A, límites, doble devolución y causal STA vs compensación | COMPLETADO — 64 reglas; HC-008/009 nuevos, HC-002/004/007 ampliados |
| I08 | NACHA-M 2026, adendas PPD/CCD/CTX, 106 caracteres, totales, ADV, rechazo formal/fatal, envío/recepción fuera de ciclo y STA | COMPLETADO — 55 reglas, 3 hallazgos nuevos HC-CENIT-010…012; HC-001 ampliado |
| I09 | Frontera CUD manual (sin API), neteo/optimización internos no definitivos, resultados oficiales, conciliación/ADV y contingencias | COMPLETADO — 50 reglas, HC-CENIT-013 nuevo; HC-001/003/005/006/012 ampliados |
| I10 | Dictamen de código, matriz consolidada, plan mínimo correctivo, pendientes de certificación y cierre portable | PENDIENTE |

## Salida obligatoria por incremento

`IXX_REGLA_A_REGLA.md`, `IXX_MEMORY_STATE.md`, `MANIFIESTO_IXX.md`, actualización de `MEMORY_STATE.md`, `ESTADO_INCREMENTOS.md`, `REGISTRO_HALLAZGOS.md`, ZIP **completo**, verificación de integridad contra el punto de partida. I10 agrega dictamen/matriz de conformidad, hallazgos y plan de certificación. **Porcentaje = hitos documentados / 10**, nunca certificación.

## Detalles que obligan trazabilidad especial

Ciclo 1 y 5; R23; ROR hasta el segundo ciclo del día hábil siguiente o quinto del día actual según origen; calendario bancario Bogotá y fecha valor; primera a tercera insuficiencia → siguiente ciclo, cuarta y quinta → optimización/rechazo según DSP-152 2025; colas fuera de cierre; diferenciación completa del STA, operador ACH, devolución del receptor, errores fatales y avisos contables; contrato NACHA-M 07-05-2026. Los WSDL adjuntos se tratan como evidencia SOAP/CFA externa, **no** como prueba de contrato CUD/SEBRA/CENIT.

## Política de fuentes y contradicciones

Usar `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`. Las versiones se cotejan por fecha interna y alcance, no solo nombre de archivo. Desactualización, errata de extracción y contradicción sustantiva son categorías diferentes; no cerrar discrepancias con inferencias. Antes de I02 confirmar cualquier publicación oficial posterior a la consulta de I01 (la búsqueda de I01 no prueba ausencia absoluta de nuevas circulares).

**Actualización I07:** separación confirmada por política entre Rxx Return y Dxx STA (no certifica procesamiento STA), R74 ROR demasiado permisiva para padres no R32 y fecha límite R23 admitida desde evidencia aportada; ver matriz.

**Actualización I08:** revisión de físico/semántico NACHA-M y canales de archivos concluida con 55 reglas; el plan conserva I09 e I10 sin ampliarlo. Los Dxx del Anexo B son solo STA; no se modifica normativa ACH Colombia.

**Actualización I09:** 50 reglas. Se confirma alcance CUD manual/externo: ACH Interbank no requiere API CUD ni contabilización. El riesgo demostrado es que el simulador conectado a Quartz cambia estados y que el importador de NACK individuales los aplica al nivel de archivo (HC-CENIT-013). I10 debe dictaminar 13 hallazgos preservados, con recomendaciones mínimas y no crear motor de liquidación.


## Cierre I10 — 2026-11-02

Todos los incrementos I01–I10 se completaron documentalmente, con 13 hallazgos abiertos. Dictamen y matrices finales: `DICTAMEN_ESTATICO_FINAL_CENIT.md`, `MATRIZ_CUMPLIMIENTO_FINAL_CENIT.md`, `HALLAZGOS_CONSOLIDADOS_CENIT.md`, `PLAN_CORRECCIONES_MINIMAS_CENIT.md` y `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md`. La fase correctiva requiere solicitud separada. CUD es frontera externa; no integrar ni contabilizar.
