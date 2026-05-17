# Matriz normativa vigente — Causales por cámara y flujo

**Fecha:** 2026-05-15 (UTC)  
**Ámbito:** ACHInterbank (documental, sin cambios funcionales)  
**Decisión vigente:** GO técnico / GO UAT controlado / NO-GO productivo

## 1. Propósito
Esta matriz consolida de manera trazable las causales por:
- cámara;
- flujo;
- tipo de causal;
- estado normativo;
- implementación técnica;
- evidencia requerida para UAT y eventual salida productiva.

Cobertura funcional incluida:
- devolución saliente;
- devolución entrante;
- devolución de devolución (ROR);
- rechazo total de archivo;
- rechazo parcial de registros;
- mensajes técnicos/informativos;
- códigos internos no externos.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Existen catálogos y validaciones técnicas de causales por cámara en seed/policies.
- Falta cierre normativo firmable por causal → cámara → flujo → prueba → evidencia.
- CENIT Anexo A confirma causales Rxx, pero se requiere trazabilidad por flujo real del sistema.
- ACH Colombia requiere ratificación final por manual vigente/operador + evidencia UAT por causal.
- Dxx/Ixxx se clasifican como rechazo técnico/operacional, no como causales Rxx.
- Códigos internos no deben exponerse como causal de cámara.

## 3. Fuentes revisadas

| Documento/código | Ruta | Tipo | Cámara | Alcance | Nivel de confianza | Observaciones |
|---|---|---|---|---|---|---|
| CENIT Anexo A causales | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | fuente primaria | CENIT | Causales Rxx | Alto (normativo) | Requiere mapeo operativo por flujo |
| ACH Colombia V32 | `docs/normativa/md/ACH-Colombia-V32.md` | fuente primaria | ACH Colombia | Marco operativo/manual | Alto (normativo) | Requiere trazabilidad puntual por causal |
| Auditoría devoluciones | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` | auditoría | ACH/CENIT | Gap técnico-funcional | Alto (técnico) | Base para plan incremental |
| S1 matriz maestra | `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md` | matriz interna | ACH/CENIT/STA | Estado funcional-normativo | Alto | Mantiene NO-GO productivo |
| Go/No-Go scorecard | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` | matriz interna | ACH/CENIT/STA | Decisión oficial de readiness | Alto | GO técnico / UAT controlado |
| Matriz NACHA por registro | `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` | auditoría | ACH/CENIT | 1/5/6/7/8/9 | Alto (técnico) | Registra hardcodes y brechas |
| Matriz naming externo | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | auditoría | ACH/CENIT/STA | Naming por flujo | Alto | NO-GO productivo vigente |
| Checklist UAT NACHA | `docs/uat/nacha-records-acceptance-checklist.md` | checklist UAT | ACH/CENIT | Aceptación por registro | Medio/Alto | Pendientes por cámara |
| Checklist UAT naming returns/ROR | `docs/uat/naming-returns-ror-acceptance-checklist.md` | checklist UAT | ACH/CENIT | Naming externo/interno | Medio/Alto | Pendiente cierre formal |
| Seeder de causales/policies | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/RegulatoryCatalogSeeder.cs` | código referencia | ACH/CENIT/STA | Catálogo vigente técnico | Alto (técnico) | Fuente de implementación actual |
| Validación regulatoria | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchRegulatoryCatalogService.cs` | código referencia | ACH/CENIT | Validaciones por cámara/flujo | Alto (técnico) | Núcleo de enforcement |

## 4. Convenciones de estado
- **Confirmado normativamente:** sustentado por fuente primaria y aplicabilidad aprobada por cámara/flujo.
- **Confirmado técnicamente:** implementado y validable en código/pruebas.
- **Inferido:** deducido por comportamiento técnico, sin confirmación normativa explícita.
- **Pendiente confirmación normativa:** implementado/documentado parcialmente, sin cierre formal.
- **Interno/no externo:** código operativo interno, no transmisible a cámara.
- **Rechazo técnico:** códigos de fallo técnico/operativo (no causal regulatoria Rxx).
- **Bloqueado productivo:** no habilitable para GO productivo hasta cierre.
- **UAT warning:** permitido en UAT controlado con evidencia y advertencia.

## 5. Taxonomía de códigos

### 5.1 Causales Rxx de devolución
Códigos de devolución regulatoria usados en devolución saliente/entrante y/o ROR según cámara y policy.

### 5.2 Causales DEVxx
Códigos especiales por operador/proyecto (ej. `DEV14`). Requieren trazabilidad explícita y no extrapolación entre cámaras.

### 5.3 Códigos Dxx de rechazo
Códigos de rechazo de archivo/registro (operacional/técnico), no equivalentes a causales Rxx de devolución.

### 5.4 Códigos Ixxx/técnicos
Códigos de integración/técnicos (`I500`, `I503`, `ITIMEOUT`, `ISOAP`, `IFUNC`), no causales de devolución Rxx.

### 5.5 Códigos internos
Ejemplo: `DXX-LIQ`. **No deben enviarse como causal externa**.

## 6. Matriz normativa por cámara y flujo

| ID | Cámara | Código | Tipo | Descripción | Flujos permitidos | Estado normativo | Estado técnico | Fuente | Evidencia requerida | Riesgo | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|---|---|
| M-ACH-01 | ACH Colombia | R07 | devolución | Autorización revocada | Saliente, Entrante (según policy), UAT | Pendiente confirmación normativa | Confirmado técnicamente | ACH V32 + Seeder | Caso UAT por flujo + aceptación operador | Medio/Alto | Caracterización por flujo |
| M-ACH-02 | ACH Colombia | R10 | devolución | No autorización/prenotificación asociada | Saliente, Entrante, ROR (si policy) | Pendiente confirmación normativa | Confirmado técnicamente | ACH V32 + Seeder | Evidencia por tipo tx + ventana | Alto | Validar límites por flujo |
| M-ACH-03 | ACH Colombia | R29 | devolución | No autorizado corporativo | Saliente, Entrante, ROR (si policy) | Pendiente confirmación normativa | Confirmado técnicamente | ACH V32 + Seeder | UAT con rechazo esperado/no esperado | Alto | Pruebas por flujo/cámara |
| M-ACH-04 | ACH Colombia | R31 | devolución/ROR | Return entry permitida | Saliente, Entrante, ROR | Pendiente confirmación normativa | Confirmado técnicamente | ACH V32 + Seeder + Policy ROR | Evidencia linaje + unicidad + ventana | Alto | Cierre normativo ROR |
| M-ACH-05 | ACH Colombia | DEV14 | devolución especial | No consentimiento / operador | Saliente, Entrante (según catálogo) | Pendiente confirmación normativa | Confirmado técnicamente | Seeder + auditoría devoluciones | Acta operador + UAT específico | Alto | Formalizar fuente primaria |
| M-CEN-01 | CENIT | R01 | devolución | Fondos insuficientes | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT por flujo/cámara | Medio | Mantener trazabilidad |
| M-CEN-02 | CENIT | R02 | devolución | Cuenta cerrada | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Caso UAT + evidencia aceptación | Medio | Cerrar matriz prueba-evidencia |
| M-CEN-03 | CENIT | R03 | devolución | Cuenta no abierta/localizada | Saliente, Entrante, ROR (si policy) | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT por tipo tx | Medio | Verificar por flujo |
| M-CEN-04 | CENIT | R04 | devolución | Número de cuenta inválido | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Aceptación de cámara | Medio | Mantener separación de ACH |
| M-CEN-05 | CENIT | R06 | devolución | Solicitud originador/operador | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Evidencia de no parcialidad | Alto | Validar regla no parcial |
| M-CEN-06 | CENIT | R08 | devolución | Orden de no pago | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT por ciclo | Medio | Cerrar trazabilidad |
| M-CEN-07 | CENIT | R09 | devolución | Fondos no disponibles | Saliente, Entrante, ROR (si policy) | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT por ventana | Medio | Validación de plazo |
| M-CEN-08 | CENIT | R12 | devolución | Originador no autorizado | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Evidencia de causal en R7 | Medio | Checklist por registro 7 |
| M-CEN-09 | CENIT | R13 | devolución | Solicitud receptor | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT funcional + validación operador | Alto | Evidencia de aceptación |
| M-CEN-10 | CENIT | R14 | devolución | Muerte representante | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Caso controlado UAT | Medio | Trazabilidad evidencia |
| M-CEN-11 | CENIT | R15 | devolución | Muerte beneficiario/titular | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Evidencia UAT | Medio | Cerrar matriz prueba |
| M-CEN-12 | CENIT | R16 | devolución | Cuenta inactiva/bloqueada | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Evidencia por débito/crédito | Medio | Verificar policy por tipo |
| M-CEN-13 | CENIT | R17 | devolución | Criterio de edición | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | UAT con rechazo funcional | Medio | Trazabilidad en command center |
| M-CEN-14 | CENIT | R20 | devolución | Cuenta no transaccional | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Evidencia de validación | Medio | Mantener segregación |
| M-CEN-15 | CENIT | R23 | devolución | Entrada rechazada receptor | Saliente, Entrante | Confirmado normativamente | Confirmado técnicamente | Anexo A + Seeder | Caso UAT + aceptación cámara | Medio | Cierre de evidencia |
| M-STA-01 | STA/rechazos | D01 | rechazo total/parcial | Archivo duplicado | Rechazo total, Command center | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | Evidencia de decisión total/parcial | Medio | Modelo unificado rechazo |
| M-STA-02 | STA/rechazos | D02 | rechazo técnico | Formato/padding inválido | Rechazo total/parcial | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | Evidencia parser/validator | Alto | Trazabilidad por registro |
| M-STA-03 | STA/rechazos | D03 | rechazo técnico | Canal/transmisión incorrecta | Rechazo total | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | UAT transmisión | Medio | Definir ownership operativo |
| M-STA-04 | STA/rechazos | D04 | rechazo técnico | Inconsistencia de secuencia/conteo | Rechazo total/parcial | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | Evidencia de conteos | Alto | Prueba de caracterización |
| M-STA-05 | STA/rechazos | D05 | rechazo técnico | Hash/cuadres inválidos | Rechazo total/parcial | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | Cálculo independiente hash | Alto | Cierre checklist NACHA |
| M-STA-06 | STA/rechazos | D06 | rechazo técnico | Campo obligatorio ausente | Rechazo total/parcial | Rechazo técnico | Confirmado técnicamente | Seeder Dxx | Evidencia parser/validator | Alto | Endurecer validación semántica |
| M-STA-07 | STA/rechazos | I500 | técnico | Error integración HTTP 500 | Respuesta operador, Command center | Rechazo técnico | Confirmado técnicamente | Seeder Ixxx | Evidencia de retry/política | Medio | Homologar taxonomía errores |
| M-STA-08 | STA/rechazos | I503 | técnico | Servicio no disponible | Respuesta operador, Command center | Rechazo técnico | Confirmado técnicamente | Seeder Ixxx | Evidencia de retry | Medio | Criterio de escalamiento |
| M-STA-09 | STA/rechazos | ITIMEOUT | técnico | Timeout SOAP | Respuesta operador, Command center | Rechazo técnico | Confirmado técnicamente | Seeder Ixxx | Evidencia timeout | Medio | Definir umbral reintento |
| M-STA-10 | STA/rechazos | ISOAP | técnico | SOAP fault técnico | Respuesta operador, Command center | Rechazo técnico | Confirmado técnicamente | Seeder Ixxx | Evidencia fault classification | Medio | Clasificación homogénea |
| M-STA-11 | STA/rechazos | IFUNC | informativo/técnico | Rechazo funcional integración | Rechazo parcial, respuesta operador | Rechazo técnico | Confirmado técnicamente | Seeder Ixxx | Evidencia operacional | Medio | Separar de Rxx |
| M-INT-01 | Interno | DXX-LIQ | interno | Código interno de liquidez | Interno | Interno/no externo | Confirmado técnicamente | LiquidityOptimizationService | Evidencia de no exposición externa | Alto | Control explícito anti-salida |

## 7. Matriz de implementación actual

| Código / grupo | Cámara actual | Clase/archivo | Método | Flujo | Origen | Valida por cámara | Valida por flujo | Acción | Riesgo | Observación |
|---|---|---|---|---|---|---|---|---|---|---|
| Rxx/DEV14 | ACH/CENIT | `RegulatoryCatalogSeeder` | `BuildReturnCodes`/`BuildReturnPolicies` | Catálogo base | seed/catálogo | Sí | Parcial | Permite catálogo | Medio | Requiere cierre normativo formal |
| Dxx/Ixxx | STA/técnico | `RegulatoryCatalogSeeder` | `BuildFileRejectionCodes` | Rechazo archivo/integración | seed/catálogo | N/A | Parcial | Rechaza/warning | Medio | No mezclar con Rxx |
| Rxx/DEV14 | ACH/CENIT | `AchRegulatoryCatalogService` | `ValidateReturnCodeAsync` | Saliente/Entrante | catálogo/policy | Sí | Parcial | Permite/Rechaza | Medio/Alto | Fuerte por cámara, no matriz documental cerrada |
| Rxx/DEV14 | ACH/CENIT | `AchRegulatoryCatalogService` | `ValidateReturnPolicyAsync` | Saliente/Entrante | policy | Sí | Sí (policy) | Permite/Rechaza | Medio/Alto | Depende de políticas vigentes |
| Rxx (ROR) | ACH/CENIT | `AchRegulatoryCatalogService` | `ValidateReturnOfReturnAsync` | ROR | policy | Sí | Sí | Permite/Rechaza | Alto | Requiere evidencia UAT por cámara |
| Rxx/DEV14 | ACH/CENIT | `AchReturnsService` | `GenerateReturnsFileAsync` | Devolución saliente | request + policy | Sí | Parcial | Excepción ante no elegible | Alto | Persisten hardcodes de naming/layout documentados |
| Rxx/DEV14 | ACH/CENIT | `AchIncomingReturnIngestionService` | `IngestAsync` | Devolución entrante | NACHA/parser + policy | Sí | Sí (decisioning) | Rechaza total/parcial | Alto | Controla huérfanas y desconocidas |
| Rxx (nueva causal) | ACH/CENIT | `AchReturnOfReturnEligibilityService` | `EvaluateAsync` | ROR | request + policy | Sí | Sí | Permite/Rechaza | Alto | Valida causal original/nueva/estado/ventana |
| Rxx/DEV14 | ACH/CENIT | `AchReturnOfReturnFileGenerationService` | `GenerateAsync`/`GenerateNachaAsync` | ROR audit/prod | policy/hardcoded parcial | Parcial | Parcial | Genera/409 | Alto | Requiere cierre de naming/layout por cámara |
| Rxx/DEV14 | Global catálogo | `TransactionValidator` | `NormalizeReturnReason` | Validación de addenda | request + catálogo | No (solo existencia catálogo) | Parcial | Excepción | Alto | Riesgo de strings válidos fuera de cámara/flujo |
| Dxx/Ixxx/Rxx | N/A | `NachaParserService` / `NachaSemanticValidator` / `NachaRecordFieldValidator` | parse/validate | Incoming parser | parser/sintaxis | Parcial | Parcial | Rechaza/Warn | Alto | Sintaxis ≠ semántica normativa completa |

## 8. Brechas por cámara

### 8.1 ACH Colombia
- Códigos confirmados técnicamente: `R07`, `R10`, `R29`, `R31`, `DEV14`.
- Pendiente ratificación normativa final por causal y flujo (saliente/entrante/ROR).
- Riesgo: aceptar causal técnicamente existente pero fuera de flujo normativo esperado.
- Evidencia UAT requerida: casos por causal, por flujo, con aceptación operador/cámara.

### 8.2 CENIT
- Códigos Rxx documentados en Anexo A: `R01,R02,R03,R04,R06,R08,R09,R12,R13,R14,R15,R16,R17,R20,R23`.
- Cargados técnicamente en catálogo y policies actuales.
- Pendiente: validación fina por flujo operativo y evidencia por causal.
- Riesgo crítico: extrapolar causal/comportamiento ACH hacia CENIT sin sustento explícito.
- Evidencia UAT requerida: trazabilidad causal→registro 7→resultado de cámara.

### 8.3 STA/rechazos
- `Dxx/Ixxx` son rechazo/técnico y deben mantenerse separados de Rxx.
- Cobertura técnica existe, pero modelo unificado total/parcial aún parcial.
- Evidencia requerida: escenarios de rechazo total/parcial y taxonomía operacional.

### 8.4 Internos
- `DXX-LIQ` identificado como código interno.
- No debe exponerse a cámara como causal externa.
- Control requerido: validación explícita anti-exposición externa + auditoría de eventos.

## 9. Brechas por flujo

### 9.1 Devolución saliente
- Causales aceptadas: según request + catálogo/policy.
- Valida cámara: sí.
- Valida flujo/plazo/estado: parcial a sí (según policy y ciclo).
- Eventos/auditoría: parcial.
- Riesgo: hardcodes y diferencias de cámara no completamente cerradas normativamente.

### 9.2 Devolución entrante
- Parsea causal en addenda 7/99.
- Valida catálogo/policy por cámara.
- Gestiona huérfanas y decisión total/parcial.
- Riesgo: cierre normativo pendiente por causal/flujo y trazabilidad final.

### 9.3 ROR
- Valida causal original, nueva causal, estado, ventana y unicidad.
- Riesgo: aceptación externa final por cámara aún pendiente (NO-GO productivo).

### 9.4 Rechazo total de archivo
- Dxx aplicables según etapa.
- Regla total existe en decisioning; requiere evidencia unificada por causal técnica.

### 9.5 Rechazo parcial de registro
- Dxx/Ixxx aplican según fallos parciales.
- Requiere matriz explícita registro→causal técnica→acción.

### 9.6 Mensajes informativos/técnicos
- Ixxx cubren integración/timeout/SOAP/funcional técnico.
- Deben mantenerse separados de Rxx/DEVxx regulatorios.

### 9.7 Command center / operación manual
- Requiere trazabilidad de acciones manuales y de causal visible/no editable.
- Riesgo: edición manual sin control suficiente de cámara/flujo.

## 10. Riesgos

| Riesgo | Descripción | Cámara | Flujo | Severidad | Mitigación | Estado |
|---|---|---|---|---|---|---|
| Catálogos incompletos | Faltan cierres normativos por causal/flujo | ACH/CENIT | Todos | Alta | Matriz + pruebas de caracterización | Abierto |
| Causales compartidas incorrectamente | Riesgo de mezcla ACH/CENIT | ACH/CENIT | Saliente/Entrante/ROR | Crítica | Policy por cámara+flujo | Abierto |
| Strings libres | Código válido sintáctico fuera de contexto | ACH/CENIT | Saliente/ROR | Alta | Validación rail-flow obligatoria | Abierto |
| Hardcodes | Naming/layout/valores fijos | ACH/CENIT | Saliente/ROR | Alta | Parametrización controlada | Abierto |
| ROR con causal no permitida | Riesgo regulatorio por linaje | ACH/CENIT | ROR | Alta | Endurecer policy + evidencia UAT | Abierto |
| Rechazo total/parcial no unificado | Ambigüedad operacional | STA | Incoming | Alta | Modelo unificado de rechazo | Abierto |
| Incoming causal desconocida | Aceptación indebida/ruido operativo | ACH/CENIT | Entrante | Alta | Hard-block/warning gobernado | Abierto |
| Parser solo sintaxis | Falta semántica normativa completa | ACH/CENIT | Entrante | Alta | Reglas semánticas por cámara | Abierto |
| UI causal no elegible | Riesgo de selección inválida | ACH/CENIT | Saliente/ROR | Media/Alta | Filtrado por policy y warning UAT | Abierto |
| Falta matriz integral | Sin trazabilidad causal-fin | ACH/CENIT/STA | Todos | Crítica | Esta matriz + pruebas + actas | Abierto |
| Falta evidencia UAT | Sin cierre por causal | ACH/CENIT/STA | Todos | Alta | Checklist por causal/flujo | Abierto |
| Trazabilidad de eventos insuficiente | Difícil auditoría causa-decisión | ACH/CENIT | Entrante/ROR/manual | Alta | Eventos estructurados | Abierto |
| Internos expuestos | Código interno enviado a cámara | Interno | Externo | Crítica | Bloqueo explícito + monitoreo | Abierto |

## 11. Criterios de salida de NO-GO productivo
- [ ] Cada causal tiene cámara definida.
- [ ] Cada causal tiene flujo permitido.
- [ ] Cada causal tiene fuente normativa.
- [ ] Cada causal tiene prueba de caracterización.
- [ ] Cada causal tiene prueba de validación por cámara/flujo.
- [ ] ACH y CENIT no comparten causales sin evidencia.
- [ ] Dxx/Ixxx están separados de Rxx.
- [ ] Internos no salen a cámara.
- [ ] ROR valida causal original y nueva causal.
- [ ] Incoming rechaza/advierte causal desconocida según política.
- [ ] Outbound no permite causal fuera de flujo.
- [ ] Command center registra trazabilidad.
- [ ] UAT con evidencia por causal.
- [ ] Firma negocio.
- [ ] Firma operaciones.
- [ ] Firma compliance/riesgo.
- [ ] Aprobación técnica.

## 12. Plan de remediación

| # | Commit propuesto | Objetivo | Alcance | Archivos probables | Pruebas | Riesgo | Rollback |
|---|---|---|---|---|---|---|---|
| 1 | `docs(causales): add cause-code normative matrix by rail and flow` | Consolidar línea base documental | Auditoría/docs | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` + referencias | Revisión documental cruzada | Bajo | Revert commit doc |
| 2 | `test(causales): add characterization tests for cause-code usage` | Congelar comportamiento actual | Tests backend | `tests/...` causales/flows | `dotnet test` focalizado | Medio | Revert tests |
| 3 | `refactor(causales): introduce rail-flow cause policy` | Política unificada rail-flow | Application/Persistence | servicios/policies/catalog | tests unitarios/regresión | Medio/Alto | Revert refactor |
| 4 | `feat(causales): validate outbound return causes by rail and flow` | Endurecer salida | `AchReturnsService` | service/controller/tests | suite returns | Alto | Feature flag / revert |
| 5 | `feat(causales): validate incoming return causes by rail and flow` | Endurecer entrada | ingest/parser/policy | services/tests incoming | suite incoming | Alto | Revert + warning mode |
| 6 | `feat(causales): validate ROR causes and lineage policy` | Endurecer ROR | eligibility/orchestrator/generation | services/tests ROR | suite ROR | Alto | Revert controlado |
| 7 | `feat(causales): unify file rejection cause handling` | Modelo unificado Dxx/Ixxx | parser/ingestion/ops | services/events/tests | suite rejection | Medio/Alto | Revert módulo |
| 8 | `docs(uat): add cause-code acceptance checklist` | Evidencia UAT por causal | docs/uat | checklist causal | revisión QA/compliance | Bajo | Revert doc |

## 13. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- **Opción recomendada:** **B** — matriz/catálogo unificada en modo **warning UAT** antes de hard-block productivo.


- Checklist UAT de causales por cámara/flujo: `docs/uat/cause-code-acceptance-checklist.md` (control de aceptación UAT; mantiene NO-GO productivo hasta cierre firmado).

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

