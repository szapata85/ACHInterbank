# Dictamen estático final de CENIT — ACH Interbank

**Fecha de corte:** 2026-11-02. **Auditoría:** CENIT I01–I10 sobre proyecto íntegro derivado del ZIP ACHCOL I10 y línea base incremental I09. **Resultado:** **NO SE ACREDITA CUMPLIMIENTO INTEGRAL ESTÁTICO DE CENIT** en las rutas inspeccionadas; existen **13 causas raíz abiertas de incumplimiento estático** con evidencia de código. **No se ha ejecutado certificación, integración bancaria ni validación de cuenta CUD.** Este dictamen no es homologación, ni afirma incidentes financieros reales.

## 1. Alcance y método

Se verificó arquitectura y ejecución por cámara; sesiones 1–5; crédito/débito, prenotificaciones; devoluciones ordinarias, R23, R06 y ROR R60–R74; NACHA-M 2026, adendas, códigos, ADV, STA; archivos tardíos, neteo/optimización interna, respuestas, trazabilidad, idempotencia y conciliación. Se inspeccionaron entrada API/servicio, jobs Quartz, parser, políticas, persistencia y pruebas fuente donde corresponde; se distinguió catálogo de consumo en ruta. No se alteró código productivo ni reglas ACH Colombia.

## 2. Fuentes consideradas y reserva de versión

- **DSP-152 Reglamento de CENIT, 27-feb-2025:** arts.18–19 para compensación y liquidación oficial. Fuente adjunta `docs/normativa/md/ceos_dsp_152_MAY_27_2022.md`, nombre histórico no fiable; contraste público [Circular Banco de la República](https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/ceos_dsp-152_Asunto_1_feb_27_2025.pdf).
- **Anexo 2, 27-feb-2025:** §§3.1–3.3, 4.1–4.8, 5.1–5.2, 12; [edición oficial publicada](https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/manual_dsp_cenit.pdf).
- **Anexo A y B, 28-nov-2023:** A regula causales del servicio compensación (incluida ROR R60–R74); B restringido al **STA**. No convertir D01–D06 en rechazos NACHA-M.
- **Manual de Especificaciones NACHA-M CENIT, 07-may-2026:** versión fecha interna del archivo aportado para reglas físicas/campos/ADV. **Vigencia operativa externa de esta versión no acreditada** con comunicación formal de implementación.
- **Manual CENIT-WEB, 13-jun-2025:** orientación de uso; no sustituye DSP-152. **ACH Colombia v35 (abr-2026) no es fuente normativa CENIT**.
- Registro de diferencias `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`: DN-CEN-001…008. La consulta pública de 02-nov-2026 a páginas del Banco localizó Reglamento/Anexo 2 2025; no es prueba de inexistencia absoluta de modificaciones posteriores en canales institucionales.

## 3. Recuento y calificación

- **I01–I09:** 333 filas de evaluación (66 cumple estáticamente en alcance delimitado; 193 parcial; 44 incumple estáticamente; 30 no demostrable).
- **I10:** 20 verificaciones transversales (3 cumple delimitado, 3 parcial, 13 incumplen por hallazgos conocidos, 1 no demostrable). **No son 20 reglas nuevas ni se deben sumar para inventar porcentaje certificado.**
- **Hallazgos únicos:** 13 abiertos, 0 cerrados, **0 nuevos en I10**. Cinco sesiones auditadas, diez incrementos documentados (100 % del **plan**, no del cumplimiento).
- **Evaluación global:** **incumplimiento estático demostrado en rutas específicas** y cobertura parcial en otras. No se puede declarar preparación homologada hasta subsanar defectos y ejecutar las pruebas operativas.

## 4. Riesgos principales y tratamiento mínimo

**Primera prioridad:** impedir pérdida de cola C1→C2 y otras transiciones (`HC-001`); impedir cambios definitivos de estado por neteo/liquidez **simulados** invocados por Quartz (`HC-003/005/006`); aplicar NACK por ítem y procesar evidencia ADV real sin asientos bancarios (`HC-012/013`). Estos defectos tienen riesgo de no procesamiento, rechazo indebido y conciliación incorrecta.

**Segunda línea de remediación:** cerrar elegibilidad/plazos de R23 y R06; correlación ROR padre, R74 (`HC-002/004/007/008/009`); separar alfabeto NACHA-M y catálogo STA/NACHA (`HC-010/011`). Ver acciones concretas, dependencias y aceptación en `PLAN_CORRECCIONES_MINIMAS_CENIT.md` y `HALLAZGOS_CONSOLIDADOS_CENIT.md`.

## 5. Delimitación decisiva: CUD es externo

El Banco de la República realiza **compensación multilateral y liquidación contra cuentas CUD** y ejecuta el algoritmo de optimización de su sistema (DSP-152 arts.18–19; Anexo 2 §5.2). En el modelo operativo aportado, **Tesorería verifica manualmente CUD**. ACH Interbank **no debe desarrollar API CUD, validación online de saldos, débitos, asientos bancarios ni un algoritmo sustitutivo**, salvo cambio formal de alcance. **La falta de API no es un incumplimiento.**

Sin embargo, el programador ACH Interbank llama a `CenitCycleExecutionService` y `LiquidityOptimizationService` que desde liquidez interna simulada puede producir `ReturnedByOperator` y detalles `IncludedInSettlement`. **Esos efectos de estado sí son internos y se deben corregir**: esperar/interpretar salidas oficiales del operador y conciliar evidencias, sin reclamar que una simulación ejecutó liquidación. ACK técnico, envío de archivo o posición estimada **no prueban fondos liquidados**.

## 6. Discrepancias pendientes y observaciones que no son nuevos defectos

- **DN-CEN-007:** posible tensión de alcance de devolución de prenotificaciones y ventana del quinto ciclo. Se deja sin regla inventada; requiere criterio aplicable de operador.
- **DN-CEN-008:** patrones de archivo de entrada vs salida y canal; no imponer formato outbound en todos los flujos.
- Regla de `AchOperationalReconciliationService` que cita ACH Colombia v35 incondicionalmente: **riesgo parcial**, sin caller productivo CENIT confirmado en I09; no se abre otro hallazgo sin evidencia.
- Existen controles de horario, retorno, bloque NACHA, registros y deduplicación que cumplen en rutas delimitadas; no constituyen cumplimiento integral ni mitigan por sí solos los hallazgos.

## 7. Condiciones para uso y certificación posteriores

Corregir con cambios locales; preservar ACH Colombia; ejecutar pruebas de unitarias/integración con base de datos, jobs, gateway y archivos oficiales; reconciliar manualmente con Tesorería cuando corresponda; obtener evidencias externas de aceptación/compensación por el Banco/operador. Los **22 casos** en `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md` son propuestas y están **pendientes/no ejecutados**. Reabrir este dictamen tras cambios, no cerrar hallazgos de oficio.

## 8. Trazabilidad y continuidad

Consultar `MATRIZ_CUMPLIMIENTO_FINAL_CENIT.md` (conteos por incremento y dominio), `I10_REGLA_A_REGLA.md` (20 verificaciones), `REGISTRO_HALLAZGOS.md` (IDs históricos), `HALLAZGOS_CONSOLIDADOS_CENIT.md` y `MANIFIESTO_I10.md` (integridad), más las matrices I01–I09. La auditoría queda **terminada en alcance estático**, no certificada.
