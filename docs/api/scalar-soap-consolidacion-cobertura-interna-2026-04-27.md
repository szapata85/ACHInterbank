# Consolidación final de cobertura SOAP interna (SOAP-2D)

**Fecha de consolidación:** 2026-04-27  
**Documento fuente principal:** `docs/api/scalar-integraciones-soap-revalidacion-tecnica-2026-04-26.md`  
**Documento base funcional SOAP:** `docs/api/scalar-integraciones-soap-proc-transacciones-proc-contrapartidas-2026-04-26.md`

---

## 1) Propósito de este documento

Consolidar en una sola pieza técnica el estado real de cobertura interna SOAP para:

1. Equipo de desarrollo.
2. QA.
3. Arquitectura.
4. Operación técnica.
5. Preparación del futuro cierre de contrato externo SOAP.

Este documento resume evidencia interna ya ejecutada y delimita claramente qué sigue pendiente únicamente en ambiente externo controlado.

---

## 2) Alcance cubierto en consolidación SOAP-2D

### 2.1 Flujos internos incluidos

- `Proc_Transacciones` (mapper, parser y orquestación interna).
- `Proc_Contrapartidas` parser (`ProcContrapartidasResponseParser`).
- `Proc_Contrapartidas` despacho por job (`ContrapartidaDispatchJobService`).
- Handler de ejecución por ciclo (`AchContrapartidasByCycleHandler`).

### 2.2 Fuera de alcance en esta fase

- Prueba de contrato externo SOAP con proveedor en ambiente de integración controlado.

---

## 3) Resumen de evidencia ejecutada (interna)

| Fase | Evidencia clave | Resultado consolidado |
|---|---|---|
| SOAP-2A | `ProcContrapartidasResponseParserTests` | 6/6 en verde |
| SOAP-2B / 2B-A | `ContrapartidaDispatchJobServiceTests` | 4/4 en verde |
| SOAP-2B-C | build + pruebas específicas + validación amplia + suite completa | build OK, 10/10, 20/20, 404/404 |
| SOAP-2C | `AchContrapartidasByCycleHandlerTests` + suite completa backend | 4/4 en verde, suite completa 408/408 |
| SOAP-2D | Revalidación documental final | Consolidación formal y criterios para SOAP-3 |

---

## 4) Componentes estabilizados internamente

### 4.1 `Proc_Transacciones`

- Cobertura vigente en parser, mapper y orquestador inbound.
- Sin bloqueos reportados en validación amplia interna.

### 4.2 `ProcContrapartidasResponseParser`

- Cobertura dedicada vigente con escenarios de éxito, rechazo funcional, `SOAP Fault` retryable/no retryable y resultados por ítem.

### 4.3 `ContrapartidaDispatchJobService`

- Cobertura dedicada vigente para escenarios críticos: vacío, éxito, retryable y mixto/parcial.

### 4.4 `AchContrapartidasByCycleHandler`

- Cobertura dedicada vigente para: sin ciclos, chunk configurado, continuidad ante error y límite de ciclos por corrida.
- Brecha histórica de ausencia de suite dedicada queda cerrada.

---

## 5) Defecto real detectado y corregido durante cierre interno

En SOAP-2C se detectó y corrigió un defecto real:

- **Síntoma:** EF Core no pudo traducir la evaluación de ventana de ciclo cuando `IsWithinCycleWindow(...)` se utilizaba directamente en `Where(...)`.
- **Impacto:** impedía ejecutar correctamente la suite dedicada del handler en entorno de pruebas.
- **Corrección aplicada:** obtención de ciclos candidatos y filtrado de ventana en memoria, preservando comportamiento funcional del handler.

Resultado posterior a corrección:

- `AchContrapartidasByCycleHandlerTests`: 4/4 en verde.
- Suite completa backend: 408/408 en verde.

---

## 6) Cobertura interna vs cobertura externa pendiente

| Área | Estado |
|---|---|
| Lógica y orquestación interna SOAP | Cubierta y revalidada |
| Parsers internos SOAP | Cubiertos y revalidados |
| Jobs/handlers internos de contrapartidas | Cubiertos y revalidados |
| Contrato externo SOAP (proveedor) | **Pendiente** |

Interpretación operativa:

- La estabilidad interna está evidenciada.
- La única brecha remanente para cierre técnico integral corresponde al contrato externo SOAP en ambiente controlado.

---

## 7) Criterios para habilitar paso a SOAP-3

Para pasar a SOAP-3 (fase enfocada en contrato externo/control operativo final), deben cumplirse y mantenerse:

1. Build backend Release en verde.
2. Suites internas SOAP en verde (`ProcContrapartidasResponseParserTests`, `ContrapartidaDispatchJobServiceTests`, `AchContrapartidasByCycleHandlerTests`).
3. Suite completa backend en verde.
4. Evidencia documental actualizada y trazable en `docs/api`.
5. Plan de ejecución de contrato externo SOAP definido (ambiente, datos sanitizados, criterios de aceptación y rollback).

---

## 8) Qué NO debe declararse todavía

Hasta ejecutar prueba de contrato externo SOAP en ambiente controlado, **no** debe declararse:

1. cierre total de integración SOAP extremo a extremo con proveedor externo;
2. cierre definitivo de riesgo operacional externo;
3. producción lista por contrato externo SOAP.

---

## 9) Recomendaciones concretas para siguiente iteración

1. Planificar prueba de contrato externo SOAP con casos mínimos:
   - éxito nominal;
   - rechazo funcional;
   - `SOAP Fault` técnico.
2. Definir datos de prueba anonimizados/sanitizados y checklist de no exposición.
3. Registrar evidencia de request/response contractual sin credenciales ni URLs sensibles reales.
4. Anclar criterios de salida de SOAP-3 a resultados verificables (pass/fail + matriz de incidencias).

---

## 10) Veredicto de consolidación SOAP-2D

- La cobertura **interna** SOAP queda consolidada y estabilizada para `Proc_Transacciones`, `ProcContrapartidasResponseParser`, `ContrapartidaDispatchJobService` y `AchContrapartidasByCycleHandler`.
- El defecto técnico real identificado en handler fue corregido y revalidado con pruebas automáticas y corrida backend completa.
- La brecha restante es única y explícita: **contrato externo SOAP en ambiente controlado**.
- Esta consolidación habilita transición ordenada a SOAP-3, sin declarar cierre externo ni readiness de producción por contrato externo.
