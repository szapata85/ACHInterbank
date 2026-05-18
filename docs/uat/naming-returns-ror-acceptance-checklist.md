# Checklist UAT — Naming externo devoluciones y ROR

## 1. Propósito
Este checklist define la verificación UAT funcional, técnica y normativa para naming externo en:
- devolución saliente;
- devolución de devolución (ROR) productivo;
- ROR audit-mode interno;
- flujos de rechazo/respuesta/operador cuando aplique por cámara.

El objetivo es evitar falsas aprobaciones de producción con nombres provisionales y separar explícitamente GO técnico/UAT de la decisión regulatoria-operativa final.

## 2. Estado actual
- **GO técnico**: sí (integración con `ExternalFileNamePolicy` implementada y validada por pruebas).
- **GO UAT controlado**: sí (validación funcional en ambientes de homologación/UAT).
- **NO-GO productivo**: sí (se mantiene hasta cierre normativo/operativo y firmas formales).
- `RET_...`: **provisional/fallback** para devolución saliente.
- `RORNACHA_...`: **provisional/fallback** para ROR productivo.
- `ROR_...`: **interno/no externo** para audit-mode.
- Los nombres provisionales **no constituyen aprobación normativa final**.

## 3. Alcance por cámara

### ACH Colombia
Validar por flujo:
- patrón de nombre aplicable;
- extensión esperada;
- correlación con Registro 1 (R1) cuando aplique;
- secuencia/consecutivo;
- código de entidad/origen;
- devolución saliente;
- ROR productivo;
- rechazo/respuesta/operador si aplica;
- aceptación efectiva de archivo por cámara/operador.

### CENIT
Validar por flujo:
- si existe naming específico para devolución;
- si existe naming específico para ROR;
- dependencia por ciclo/fecha/secuencia;
- relación operativa con causales Rxx;
- aceptación efectiva de archivo por cámara/operador.

### STA
Validar:
- alcance confirmado para rechazo;
- control D04/D05 cuando aplique;
- consistencia de conteo declarado;
- no extrapolar reglas STA a otros flujos sin fuente normativa explícita.

## 4. Checklist funcional

| ID | Cámara | Flujo | Validación | Evidencia requerida | Responsable | Estado | Observaciones |
|---|---|---|---|---|---|---|---|
| F-01 | ACH/CENIT | Devolución saliente | Genera archivo de salida | Archivo + log + payload API | QA Funcional | Pendiente | |
| F-02 | ACH/CENIT | ROR productivo | Genera archivo NACHA | Archivo + log + payload API | QA Funcional | Pendiente | |
| F-03 | ACH/CENIT | ROR audit-mode | Genera evidencia interna | Respuesta con `ROR|`/`FLOW|` | QA Funcional | Pendiente | |
| F-04 | ACH/CENIT | ROR audit-mode | No se envía a cámara | Trazas de integración / ausencia de envío | Operaciones | Pendiente | |
| F-05 | ACH/CENIT/STA | Naming provisional | Queda marcado como provisional | Resultado de validación (`RETURN_NAMING_PROVISIONAL`) | QA + Compliance | Pendiente | |
| F-06 | ACH/CENIT | Devolución/ROR | Se registra auditoría de generación | Registros de auditoría en BD | QA Técnico | Pendiente | |
| F-07 | ACH/CENIT | Devolución/ROR | Nombre usado queda persistido | Campo `FileName` persistido | QA Técnico | Pendiente | |
| F-08 | ACH/CENIT/STA | Naming externo | Duplicidad por nombre se detecta | Warning/resultado de validación | QA Técnico | Pendiente | |
| F-09 | ACH/CENIT | ROR productivo | Duplicidad por mismos `flowIds` se bloquea | Failure `DUPLICATE_PRODUCTIVE_GENERATION` | QA Técnico | Pendiente | |
| F-10 | ACH/CENIT | ROR productivo | Source real se conserva | `Source = nacha` o `nacha:{sourceReal}` | QA Técnico | Pendiente | |
| F-11 | ACH/CENIT/STA | Naming externo | Hard-block real bloquea generación | Failure/Excepción controlada + log | QA Técnico | Pendiente | |
| F-12 | ACH/CENIT/STA | Naming provisional | Warning provisional no bloquea UAT | Resultado Warning sin HardBlock | QA Funcional | Pendiente | |

## 5. Checklist normativo

| ID | Documento fuente | Cámara | Sección/página | Regla | Evidencia | Estado | Firma requerida |
|---|---|---|---|---|---|---|---|
| N-01 | ACH Colombia V32 | ACH | Naming/archivos (secciones aplicables) | Patrón/consecutivo/código entidad aplicables a ACH | Extracto normativo + caso UAT | Pendiente | Negocio + Compliance |
| N-02 | CENIT Anexo A Causales Devolución | CENIT | Causales Rxx | Cubre causales operativas; naming puede requerir confirmación adicional | Matriz de trazabilidad + acta | Pendiente | Operaciones + Compliance |
| N-03 | Matriz vigente naming externo | ACH/CENIT/STA | Documento completo | Fuente interna de trabajo actual para UAT | Referencia documental + resultados UAT | Pendiente | Arquitectura + QA |
| N-04 | Confirmación externa de cámara/operador | ACH/CENIT/STA | N/A | Validación formal pendiente de naming definitivo | Correo/acta oficial | Pendiente | Negocio + Operaciones + Compliance |

## 6. Checklist técnico

| ID | Componente | Clase/test | Validación | Evidencia | Estado |
|---|---|---|---|---|---|
| T-01 | Policy | `ExternalFileNamePolicy` | Resuelve nombre y aplica validación | Unit tests + logs | Vigente |
| T-02 | Validator | `ExternalFileNameValidator` | `RETURN_NAMING_PROVISIONAL` Warning y hard-block real | Unit tests | Vigente |
| T-03 | Devolución saliente | `AchReturnsService` | Usa `ReturnOut`, policy y fallback `RET_...` | Unit tests golden | Vigente |
| T-04 | ROR productivo | `AchReturnOfReturnFileGenerationService.GenerateNachaAsync` | Usa `ReturnOfReturnOut`, policy y fallback `RORNACHA_...` | Unit tests golden | Vigente |
| T-05 | Auditoría naming | `ExternalFileNameRegistry` | Registro de nombre y validación | Pruebas de persistencia | Vigente |
| T-06 | Golden tests | `AchReturnsFileByClearingHouseTests`, `AchReturnOfReturnFileGenerationServiceTests`, `ExternalFileNamePolicyPhase1Tests` | Congelan comportamiento provisional/UAT actual | Resultados `dotnet test` | Vigente |

## 7. Criterios de salida de NO-GO productivo
Para cambiar estado a GO productivo deben cumplirse todos:
1. Patrón confirmado por cámara y flujo.
2. Evidencia de archivo aceptado en UAT/homologación.
3. Secuencia/consecutivo validado.
4. Extensión validada.
5. Código entidad/origen validado.
6. Duplicidad validada.
7. Registro en auditoría validado.
8. Firma de negocio.
9. Firma de operaciones.
10. Firma de compliance/riesgo.
11. Aprobación técnica.
12. Acta UAT cerrada y aprobada.

## 8. Acta mínima sugerida
Formato mínimo por ejecución UAT:
- Fecha.
- Cámara.
- Ambiente.
- Archivo probado.
- Flujo.
- Resultado.
- Evidencia adjunta.
- Observaciones.
- Responsable negocio.
- Responsable tecnología.
- Responsable operaciones.
- Aprobación/rechazo.

## 9. Riesgos residuales
- `RET_...` no confirmado normativamente como naming externo final.
- `RORNACHA_...` no confirmado normativamente como naming externo final.
- Naming CENIT incompleto para ciertos flujos.
- Alcance STA parcial fuera de rechazo.
- Riesgo de extrapolar reglas ACH a CENIT sin fuente formal.
- Riesgo de usar audit-mode como si fuera externo.
- Riesgo de aceptar UAT técnico sin firma normativa/operativa.

- Referencia cruzada de control de layout NACHA-M por registro/cámara: `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` (sin alterar estado NO-GO productivo).

Referencia de trazabilidad consolidada: para trazabilidad requisito→norma→código→prueba→evidencia por cámara, ver `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md`. Esta referencia no cambia NO-GO productivo.


Referencia de compuertas de evidencia y aprobación humana: para clasificación de evidencia, GO UAT formal y aprobación humana, ver `docs/uat/human-signoff-evidence-classification-gates.md`. Esta referencia no cambia NO-GO productivo.
