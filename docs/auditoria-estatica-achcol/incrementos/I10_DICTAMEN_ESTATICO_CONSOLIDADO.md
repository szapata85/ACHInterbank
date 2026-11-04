# I10 — Dictamen consolidado de auditoría estática ACH Colombia

**Corte documental:** 2026-11-01 · **Base:** ZIP completo I09 · **Resultado:** cierre de auditoría **ESTÁTICA**, no de certificación bancaria. **Alcance:** ACH Colombia (7 ciclos), sus subprocesos y controles transversales. **Código y pruebas preexistentes sin cambios en I10.**

## 1. Dictamen ejecutivo

**10/10 incrementos de auditoría terminados (100 % de hitos). Quedan 0 incrementos de este plan. Certificación de ciclos: 0/7.** Se puede **cerrar la revisión documental/estática**, pero **NO se puede declarar conformidad integral**, aprobación de homologación ni aptitud productiva con la evidencia disponible.

Los módulos muestran controles de calendario, horarios, tipos de operación, idempotencia y generación/recepción de archivos. No obstante, existen **dos contradicciones lógicas en rutas específicas verificables estáticamente**, y otras brechas cuya aplicación total no está demostrada:

1. **H07 (ALTA, contradicción estática):** el tratamiento R10 ordena los ciclos por `StartTime`, y en la agenda 7C el C1 comienza a las 19:01 del **día previo**; ordenar solo la hora ubica C1 después de C2–C7. Ejemplo C1→C2: `returnIndex < originalIndex` bloquea el caso. No se midió incidencia operativa real.
2. **H09 (ALTA, contradicción de semántica y cobertura):** los códigos D01/D02/D04 usados por el validador para errores diferentes de los definidos en el Anexo 3 ACH V35; clasificación genérica de Dxx como `FileRejection`. **Corrección de inventario de I09:** el **Anexo 3 sí contiene D33** (impreso `D3 3`), pero `ReturnReasonConfiguration.cs` termina en D32. Por tanto en el catálogo revisado están **32/33 códigos Dxx**, no 32/32 del anexo completo. Presencia de código no equivale a cobertura funcional ni a validación completa.
3. **H01 (ALTA, enforcement no demostrado):** el catálogo define la matriz `ReturnWindows` de los siete ciclos, pero esta no se transporta al snapshot activable ni se encuentra consumida por la ruta general productiva de devoluciones inspeccionada. **No afirmar** sin ensayo que todas las devoluciones inválidas se aceptan.
4. **H04/H03/H08:** ROR, liquidación/conciliación y clasificación especial PSE/SSS/DIAN requieren cierre adicional de trazabilidad y pruebas concretas.

**No se añadió código, infraestructura, arquitectura, pruebas nuevas ni migraciones.** La falta de un runtime .NET, base de datos, MFT o conexión bancaria en el entorno de revisión no impidió el dictamen estático; esos escenarios permanecen `NO_EJECUTADO`, no `APROBADO` ni `FALLIDO`.

## 2. Fuentes, alcance y reserva de interpretación

- *Manual de Servicio ACH Transferencias Interbancarias para Entidad Participante V35*, abril 2026; en particular **Anexo 3 — Causales de Devolución por Operador**, páginas **228–229** según numeración impresa, y **Anexo 9 — Causales**. El manual base conserva referencias al esquema previo de cinco ciclos.
- *Nuevos Ciclos de Proceso y Horarios.pdf*, páginas 1–3, tabla siete ciclos; incluye erratas visibles (texto «cinco (7)» y apertura «11:01pm» en C4). No enmendar la fuente: el catálogo usa 11:01 **a. m.** para C4, por ratificar documentalmente con ACH Colombia.
- Código del ZIP completo I09, ubicado en `ACHInterbank_ajustado/src/`; pruebas disponibles en `tests/`. La política `ACHCOL-7C-20261014` establece vigencia en código, **sin probar** instalación, habilitación efectiva ni aceptación externa.
- Informes I01–I09 dentro de `docs/auditoria-estatica-achcol/incrementos/`; se conservan intactos como evidencia histórica. Este I10 **prevalece** cuando corrige un dato consolidado anterior, especialmente el conteo Dxx del I09.

**Estados:** `CONTRADICCION_ESTATICA` = resultado lógico/etiqueta contrario a la fuente en una ruta de código; `BRECHA_DE_ENFORCEMENT` = regla declarada no trazada hasta validación efectiva; `PARCIAL` = hay control que no cubre todo el proceso; `NO_DEMOSTRADO` = datos insuficientes; `CONFLICTO_FUENTE` = documentos no reconciliados. Ninguna categoría sustituye certificación o prueba de producción.

## 3. Estado consolidado de hallazgos únicos

| ID | Prioridad | Estado I10 | Evidencia principal y delimitación | Cierre mínimo propuesto |
|---|---|---|---|---|
| **H01** | **Alta** | **BRECHA_DE_ENFORCEMENT** | `RegulatoryCycleScheduleCatalog.cs:114-128,178-185` define ventanas; `RequireActivatable` no incluye matriz; búsqueda de `ReturnWindows` fuera de catálogo no encuentra consumo en `src/`; `AchReturnsService.cs` comprueba reglas generales | Validar en el servicio que genera `ReturnOut` la tupla `(cámara, fecha operativa, ciclo original, día fuente, ciclo destino)` contra la tabla versionada; probar positivos y negativos de C1–C7 |
| **H02** | Media | **CONFLICTO_FUENTE** | Manual ACH V35 conserva 5 ciclos; tabla anexa 7; C4 `11:01pm` contradictoria con cierre `12:30pm`; catálogo aplica 11:01am y vigencia `2026-10-14` | Confirmar versión normativa y vigencia aplicable al ambiente; documentar corrección de errata con dueño ACH, sin sobrescribir el original |
| **H03** | Alta | **NO_DEMOSTRADO** | Scheduler/modelo de planilla-pago-liberación (`RegulatoryCycleScheduleCatalog.cs:187-237`, `AchColombiaManagedMftHandlers.cs:253-315`) no acredita cuadre externo/ledger | Evidenciar por ciclo correlación de planilla, resultado bancario, acuse final, saldo y ledger; no confundir archivo transmitido con pago confirmado |
| **H04** | Alta | **PARCIAL** | `AchReturnOfReturnFileGenerationService.cs:262-313` valida política si `_cycleTransactionPolicy != null`; método `ValidateNachaRecords` declarado en 584–591 no invocado en ese archivo; controller diferencia auditoría `ROR|` y NACHA productivo | Cerrar DI y ruta NACHA obligatoria; validar layout/adenda, ciclo, causal, referencia y no duplicidad concurrente; no bloquear artefactos de auditoría |
| **H05** | Media | **PARCIAL** | `AchReturnEligibilityPolicyComponent.cs:135-139,176-210`: R10 trata día anterior de forma especial y requiere evidencia de prenotificación/autorización | Ratificar alcance de excepción R10 frente a matriz ordinaria, calendario y causales; casos con/ sin prenotificación |
| **H06** | Media | **CONDICIONAL_NO_DEMOSTRADO** | C1 inicia 19:01 del día anterior a fecha operativa del corte; se identificaron rutas de calendario/scheduler en I02 | Rastrear disparos nocturnos sobre feriados, fines de semana, cierre diario y `ProcessingDate` explícita |
| **H07** | **Alta** | **CONTRADICCION_ESTATICA** | `AchReturnEligibilityPolicyComponent.cs:148-160`: `.OrderBy(cycle => cycle.StartTime)` y guard `returnIndex < originalIndex`; agenda 7C `C1=19:01`, `C2=07:01` | Orden por **número/secuencia de ciclo operativo versionada**, sin desplazar la fecha operativa; regresiones C1→C2/C3/C4/C5 y negaciones C1→C6/C7 |
| **H08** | Media | **BRECHA_DE_ENFORCEMENT** | `CycleTransactionPolicy.cs:56-71` distingue clase, no subproducto; descripciones PSE/SSS/DIAN en catálogo de conceptos | Control producto×ciclo exacto (C1,C3,C5,C6,C7 sí; C2,C4 no enumerados en anexo) y prueba rechazo sin afectar créditos ordinarios |
| **H09** | **Alta** | **CONTRADICCION_ESTATICA + COBERTURA INCOMPLETA** | Manual Anexo 3 págs. 228–229: D01 fecha antigua, D02 fecha inválida, D04 prenote monto ≠0, **D33 transacción no permitida para EFR**. `NachaInboundValidation.cs:333-352,899-906` reutiliza D04 para totales, D02 para relleno y D01 para secuencia; `AchCauseCodePolicy.cs:35-39` tipifica Dxx como rechazo de archivo; `ReturnReasonConfiguration.cs:64-95` D01–D32, sin D33 | Matriz única *por cámara/flujo/fase* y prueba parametrizada de códigos; agregar D33 solo donde aplique; corregir mapeos puntuales, no reemplazar Dxx globalmente ni afectar CENIT/STA |

**Alcance H09:** se comprobó que D33 no está en el **catálogo semilla revisado**, no se afirma que ninguna otra configuración del sistema pueda representarlo. La revisión estática de uso de `D33` en `src/` y `tests/` tampoco arrojó referencias literales. **Dxx de CENIT-STA tienen otra semántica** y no deben reasignarse usando el anexo ACH.

## 4. Matriz final por ciclo y regla de devolución

`P#` = ciclo del **día hábil operativo anterior**; `C#` = ciclo del **día operativo corriente**; solo muestra ventanas ordinarias de devoluciones declaradas en la tabla siete ciclos. Su vigencia concreta requiere H02.

| Ciclo destino | Corte | Orígenes de devolución elegibles | Restricción destacada |
|---|---|---|---|
| C1 | 07:00 | P4, P5, P6, P7 | C1 abre 19:01 del día anterior; vigilar fecha operativa |
| C2 | 09:00 | P5, P6, P7, C1 | R10 C1→C2 afectado por orden de hora |
| C3 | 11:00 | P6, P7, C1, C2 | PSE/SSS/DIAN enumerados; elegibilidad por producto pendiente |
| C4 | 12:30 | P7, C1, C2, C3 | PSE/SSS/DIAN no enumerados; errata hora inicial |
| C5 | 14:30 | C1, C2, C3, C4 | PSE/SSS/DIAN enumerados |
| C6 | 16:00 | C2, C3, C4, C5 | C1 queda fuera de ventana ordinaria; no inventar defecto R10 C1→C6 |
| C7 | 18:00 | C3, C4, C5, C6 | Prohibir **débito monetario ordinario nuevo**; permitir devolución débito elegible y otras clases permitidas |

Regla transversal: no mezclar **elegibilidad por causal**, **ventana origen→destino**, **tipo/servicio**, **calendario**, **adenda NACHA-M**, **unicidad**, **transmisión** y **liquidación**. Son controles distintos: pasar uno no implica pasar los demás.

## 5. NACHA-M fuera de ciclos — decisión permanente D-I08-01

**No construir `OutsideCycle ⇒ Reject` como regla global.** Se verificó código que permite rutas diferentes:

| Acción | Dictamen estático y salvaguarda mínima |
|---|---|
| Preparar/generar NACHA-M | Puede ser operación separada del envío; exigir estado, contenido, fecha valor y origen autorizado, no presuponer compensación |
| Transmitir MFT **automático** | `AchColombiaManagedMftOutboundWorkflow.cs:49-50` exige ventana automática de envío |
| Transmitir MFT **manual** | Camino separado que requiere habilitación (`ManualOutboundAllowed`); no usa la misma guarda automática; exigir rol, justificación, idempotencia y trazabilidad |
| Recepción tardía/inbound | Evento `InboundOutsideExpectedWindow` y correlación con ciclo; **no rechazar automáticamente** por marca horaria |
| Reprocesar archivo | Respetar contenido retenido, estado, hash, duplicados, fecha operativa y referencia original |
| Compensar/liquidar | Solo registrar conformidad si existe respuesta y evidencia bancaria que autorice/aplique la transacción en su ciclo/fecha; **no** se infiere de preparación o transmisión extrahoraria |

## 6. Plan mínimo de remediación y validación — posterior a este I10

**Fase A — correcciones acotadas (no implementadas):**

1. **P0 / H01:** conectar la matriz de devoluciones versionada al punto vinculante de emisión `ReturnOut` y bloquear combinaciones origen-día-destino no elegibles. No incorporar lógica duplicada por cada job.
2. **P0 / H07:** corregir el orden de C1 en R10 sin relajar ninguna causal ni ventana; añadir regresiones específicas.
3. **P0 / H09:** conciliar los **33 códigos** del Anexo 3 ACH con funciones diferenciadas por cámara/flujo; eliminar asignaciones D01/D02/D04 semánticamente incompatibles en la ruta ACH; conservar STA/CENIT; probar D33.
4. **P1 / H04-H08:** exigir validación efectiva del archivo ROR NACHA y política de ciclo; comprobar subproductos especiales por ciclo. Si ya existen guardas alternativas, documentarlas y evitar código duplicado.
5. **P1 / H06-H05-H02:** aprobar configuración de vigencia, fecha operativa nocturna y reglas especiales R10, antes de actualizar documentación operativa.
6. **P1 / H03:** completar la correlación planilla–pago–ACK–ledger y los estados no concluyentes con evidencia bancaria; no reemplazar con inferencia estática.

**Fase B — batería mínima para certificación (NO_EJECUTADA):**

| Bloque | Escenario de aceptación mínimo |
|---|---|
| Ventanas C1–C7 | 7 pruebas positivas (una fuente elegible por ciclo) + 7 negativas (fuente inelegible), con casos de día anterior y corte exacto |
| R10 | C1→C2/C3/C4/C5, con y sin base de prenotificación; C1→C6/C7 negativos y feriado/interdiario |
| C7 | Crédito y prenotificación válidos; débito monetario nuevo rechazado; devolución de débito elegible válida |
| Producto/ciclo | PSE/SSS/DIAN positivos en C1,C3,C5,C6,C7 y negativos C2,C4, según fuente vigente confirmada |
| Causales | Reglas Rxx y D01–D33 con su semántica ACH, clases válidas/no válidas, adenda y fase; confirmar Dxx de otras cámaras separadas |
| ROR | Referencia a devolución original, causal elegible, una sola emisión NACHA, layout y adenda válidos; endpoint `ROR|` solo auditoría |
| NACHA-M fuera de ventana | Preparación sin compensación, automático fuera bloqueado, manual habilitado auditado, inbound tardío no rechazado automáticamente, reintento idempotente |
| Conciliación | Planilla, pago, respuesta externa, liberación y ledger coincidentes; caso de rechazo, corte sin respuesta y reintento |

**Criterio de salida para la etapa siguiente:** código corregido con trazabilidad, regresiones ejecutadas, reconciliación de versiones documentada, y acta/evidencia externa por cámara y ciclo. No habilitar paso a producción solo con este informe.

## 7. Verificaciones realizadas para I10

- Lectura literal y contraste del manual ACH V35 para **D01, D02, D04 y D33**; detección de omisión D33 en catálogo semilla.
- Lectura de `RegulatoryCycleScheduleCatalog`, `AchReturnEligibilityPolicyComponent`, `NachaInboundValidation`, `AchCauseCodePolicy`, `ReturnReasonConfiguration`, generador ROR y flujo MFT; cruces con I02–I09.
- Conteo de la matriz siete ciclos y revisión de condición C7 débito ordinario; política de fuera de ciclo conservada.
- Validación de integridad ZIP, nombres únicos y comparativa exacta de los archivos heredados no documentales; resultado consignado en `MANIFIESTO_I10.md`.
- **NO_EJECUTADO:** dotnet build/test, pruebas de base de datos, Quartz, MFT/SFTP/servicios bancarios, liquidación real, conciliación contable externa ni homologación ACH Colombia.

## 8. Cierre y continuidad

**Plan original de auditoría estática: TERMINADO — 10/10, faltan 0.** Quedan **H01–H09** como hallazgos abiertos con diferente naturaleza; no se confunde cierre de revisión con cierre de incumplimientos. El siguiente trabajo, si se autoriza, es **remediación acotada y pruebas**, fuera de los diez incrementos cerrados. Para reanudar en otro chat, leer `MEMORY_STATE.md` y este documento; toda la evidencia está contenida en el ZIP I10.
