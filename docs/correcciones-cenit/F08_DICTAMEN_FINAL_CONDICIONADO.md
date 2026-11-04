# F08 — Dictamen final de correcciones CENIT (condicionado)

**Corte:** 2 de noviembre de 2026. **Proyecto:** ACH Interbank `ACHInterbank_ajustado/`. **Antecedente:** auditoría estática I01–I10, específicamente `../auditoria-estatica-cenit/DICTAMEN_ESTATICO_FINAL_CENIT.md`, `HALLAZGOS_CONSOLIDADOS_CENIT.md` y `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md`. **Línea base inmediata:** ZIP de correcciones F07, ver `MANIFIESTO_F07.md`.

## Dictamen y delimitación

**DICTAMEN: IMPLEMENTACIÓN DOCUMENTADA DE LAS TRECE CORRECCIONES, CON VERIFICACIÓN Y CIERRE CONDICIONADOS. NO SE ACREDITA CUMPLIMIENTO INTEGRAL, HOMOLOGACIÓN CENIT NI APTITUD PARA PRODUCCIÓN.**

- Las 13 causas raíz de I10 figuran como **implementadas en código**, sobre base F01–F06, y revisadas estáticamente en F07.
- Se verificaron **137/137 controles estáticos/fixtures en F01–F07** (12+13+17+21+26+24+24), cuyos resultados se conservaron. F08 aporta controles adicionales de consistencia documental e integridad.
- **0 pruebas xUnit ejecutadas, 0 aprobadas, 0 fallidas ejecutadas**; no se ha compilado el proyecto aquí. El SDK .NET está ausente. No se interpretará ese cero como éxito ni fracaso de pruebas.
- **22 pruebas posteriores PT-01–PT-22 no ejecutadas**, sin resultados oficiales de SQL/Quartz/gateway/operador, homologación, CUD/Tesorería ni regresión funcional de ACH Colombia.
- **0 hallazgos cerrados formalmente**. Todos los HC-CENIT-001…HC-CENIT-013 permanecen **ABIERTOS — implementados pendientes de verificación**. Los IDs originales y sus prioridades se preservan.

## Alcance implementado / trazabilidad

| Fase | Hallazgos tratados | Evidencia y consideración |
|---|---|---|
| F01 | 003, 005, 006 | Separación del neteo y la liquidez simulados frente al efecto financiero; sin compensación definitiva interna |
| F02 | 012, 013 | NACK individual con traza; ADV 280/81–88 para conciliación, sin asientos CUD |
| F03 | 001 | Incorporación de cola en el ciclo destino previa a generación, idempotencia, límites entre fechas valor |
| F04 | 004, 007, 009 | R23 mismo día/posterior, R06 C5, calendario y evidencia de notificación; confirmar norma con operador |
| F05 | 002, 008 | ROR C1 con correlación NACHA/evento y R74 limitado a R32 de crédito monetario con adenda |
| F06 | 010, 011 | Repertorio físico CENIT por perfil; códigos NACHA-M separados de STA D01–D06 |
| F07 | 001–013 | Regresión de fuente y fixtures; casos xUnit adicionales no ejecutados |
| F08 | 001–013 | Dictamen de cierre condicionado, plan PT y evidencia de integridad; **sin nuevos cambios productivos** |

Detalle por ID: `F08_MATRIZ_CIERRE_HALLAZGOS.md`, registros F01–F07, diffs y memorias de cada fase.

## Aislamiento de servicios y riesgos

1. **ACH Colombia:** sin cambios productivos específicos F07/F08; los golden revisados estáticamente no demostraron divergencia de bytes, pero la regresión funcional .NET/DB está **pendiente** (PT-20). No declarar comportamiento íntegro certificado.
2. **CENIT:** el simulador es auxiliar; ni neteo, ni optimización, ni ACK técnico acreditan liquidación del Banco. ADV se usa para conciliación, no sustituye asiento bancario.
3. **CUD:** expresamente fuera de alcance. El proceso de Tesorería requiere soporte manual, con acta y verificación de diferencias (PT-19).
4. **Normativa:** se usan los documentos adjuntos de CENIT y el dictamen I10. Su vigencia y los perfiles/casos controvertidos deben confirmarse con el operador (PT-22); no se presumió consulta regulatoria externa.
5. **Persistencia/operación:** quedan por certificar SQL, concurrencia, fallos de commit, colas interdiarias y Quartz; los controles estáticos no reproducen esos escenarios.

## Decisión de liberación

**Apto:** recepción del código y documentos para auditoría independiente y ejecución de QA en un ambiente controlado.

**No apto:** homologación, certificación regulatoria, despliegue en producción, habilitación bancaria o cierre de hallazgos. Para levantar el bloqueo ejecutar PT-01–PT-22, corregir incidencias, validar resultados con roles designados y emitir un nuevo dictamen basado en evidencias reales.

**Valor de SDK a aprovisionar:** `global.json` exige **10.0.403** (`rollForward=latestPatch`). La mención **10.0.402** en documentos F07 es una discrepancia documental histórica que se aclara aquí; no se cambian archivos de código ni el historial F07.

Este dictamen es de **entrega condicionada**, no reemplaza el dictamen histórico I10, no afirma incidentes reales, y no otorga certificado del Banco de la República.
