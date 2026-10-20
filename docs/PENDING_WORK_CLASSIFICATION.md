# Clasificación vigente de pendientes — ACHInterbank

Fecha de consolidación: 2026-10-09.

## Objetivo

Separar explícitamente el backlog en cuatro grupos para evitar mezclar desarrollo propio, dependencias externas y pruebas de aceptación de usuario.

Regla de trabajo para el cierre técnico: **antes de entregar a usuarios se cierran los pendientes de implementación propios que formen parte del alcance acordado; las pruebas funcionales/UAT de usuario permanecen abiertas para ejecución por los usuarios.** Las pruebas automatizadas unitarias/integración asociadas a código nuevo sí forman parte del cierre técnico y no se trasladan al usuario.

## A. Pendientes propios que requieren implementación de código

| ID | Pendiente | Estado | Qué falta en código | Criterio de salida técnico |
|---|---|---|---|---|
| NACHA-RULE-METADATA-RESIDUAL | Residuales del programa de metadata NACHA | PARCIAL / REQUIERE ACOTAR | Cerrar únicamente los residuales explícitamente demostrables del programa metadata que no estén cubiertos por perfiles ordinarios ya implementados. No reabrir slices aceptados. | Lista residual concreta + implementación + pruebas automatizadas. Si no existe residual reproducible, se reclasifica a verificación y no a desarrollo. |
| UAT-INTEROPERABILITY-CONTRACT | SPA de interoperabilidad sin backend equivalente | PENDIENTE | Resolver el contrato `nacha-security/interoperability/*`: implementar endpoints reales autorizados o retirar/deshabilitar de forma coherente la funcionalidad SPA. | No quedan llamadas SPA a endpoints inexistentes; contrato OpenAPI y permisos coherentes. |


> Evidencia del corte 2026-10-09: el grafo de solución permanece válido (7/7 proyectos). El host de preparación no dispone de `dotnet` ni Docker; el frontend tampoco pudo completar su validación por dependencias no disponibles. Por eso el frente ACH Colombia FASE 1–8 queda como **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**. No promover a `UAT_READY`, `USER_ACCEPTED`, `RELEASE_READY` ni producción sin ejecutar el control final sobre el artefacto exacto.

### Deuda técnica propia no bloqueante para entrega funcional

| ID | Pendiente | Tratamiento |
|---|---|---|
| TECH-AUTH-001 | `AuthenticationService.GetTokenAsync()` conserva `NotImplementedException` sin consumidores productivos identificados. | Eliminar contrato muerto o implementar solo cuando exista consumidor autorizado. No bloquea UAT funcional actual si continúa sin consumidores. |
| TECH-PKG-001 | `Cfa.ACHInterbank.Application` conserva referencias ASP.NET Core 2.3.9 sobre `net10.0`. | Planificar modernización; no mezclar con cierre regulatorio salvo que genere una falla reproducible. |

## B. Implementación encontrada en el código actual: dejar como verificación/UAT, no como desarrollo pendiente

La relectura del árbol actual muestra implementación posterior a varias observaciones históricas de `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`. Por ello estos ítems no deben seguir apareciendo automáticamente como “falta código” sin una falla reproducible.

| Capacidad | Evidencia estática actual | Clasificación vigente |
|---|---|---|
| ACHCOL-CLAIMS-DEV14 | Reclamaciones ACH para DEV14: agregado, persistencia, lifecycle/historial, Application/API, permisos `Claims.*`, menú y SPA implementados; DEV14 queda fuera de ReturnOut/Addenda 99/ROR. | **IMPLEMENTACIÓN COMPLETADA — VALIDACIÓN EJECUTABLE PENDIENTE**; no declarar UAT/release readiness hasta ejecutar build y suites focales sobre el artefacto exacto. |
| DEV-CENIT-CYCLE1-R23-ROR | Política contextual CENIT implementada para R23 de días anteriores y ROR del Día Hábil Bancario inmediatamente anterior; flags globales de ciclo 1 continúan cerrados; pruebas focales/regresión escritas. | IMPLEMENTADO — VALIDACIÓN AUTOMATIZADA PENDIENTE; al pasar .NET 10 mover a IMPLEMENTADO — UAT PENDIENTE |
| Revisión manual de respuestas / huérfanas | API con inicio de revisión, resolución, rechazo, auditoría y control de versión; SPA con formulario de resolución. | PRUEBA/UAT PENDIENTE |
| Administración de response status mappings | API CRUD/activar/desactivar/auditoría; SPA con crear/editar y manejo de conflicto de versión. | PRUEBA/UAT PENDIENTE |
| Conciliación operativa | API de excepciones y resolución con control de concurrencia; SPA permite resolver excepciones. | PRUEBA/UAT PENDIENTE |
| Scheduler | Quartz persistente/clusterizable más programación outbound ACH Colombia por ciclo desde `AchColombiaCycleOperationalSchedule`; reschedule sin restart implementado. | VALIDACIÓN EJECUTABLE FINAL / UAT / OPERACIÓN PENDIENTE |
| Catálogos y políticas CENIT | Backend expone return codes, file rejection codes, transaction/return/ROR/prenotification policies; SPA CENIT consume las vistas regulatorias. | PRUEBA/UAT PENDIENTE |
| Administración NACHA config | SPA/API disponen crear borrador, editar, clonar, validar, publicar, inactivar, archivar, editar secuencias/variantes/fields/rules, historial, snapshots y preview. | PRUEBA/UAT RESPONSIVE/FUNCIONAL PENDIENTE |
| CENIT NACHA-M ordinario PPD/CCD/CTX | Perfiles table-driven de entrada/salida y reglas de cardinalidad existentes. | HOMOLOGACIÓN/UAT PENDIENTE |
| MFT de ACH Colombia dentro de la aplicación | ManagedFolder y SFTP hacia infraestructura CFA, resolver efectivo, host-key, credencial protegida, publicación atómica, seguimiento, inbound OUT/RET y recuperación implementados. | VALIDACIÓN EJECUTABLE FINAL + DESPLIEGUE/HOMOLOGACIÓN EXTERNA PENDIENTES |
| ACH Colombia 7 ciclos — checkpoint | Regulación separada de programación operacional; C1 `06:30` y C7 latest-start `17:50` soportados operacionalmente; C2-C6 sin horas inventadas; candidata C1–C7 preservada. | **IMPLEMENTADO / VALIDACIÓN EJECUTABLE FINAL PENDIENTE / UAT PENDIENTE**; candidata **INACTIVE / NOT ACTIVATABLE** |

## C. Pendientes de terceros / externos — no desarrollar a ciegas

### ACH Colombia 7 ciclos — bloqueos de activación / NEEDS CLARIFICATION

Estos dos puntos son bloqueos documentales para activar la candidata; no deben resolverse por inferencia ni por código:

| ID | Clarificación requerida | Estado |
|---|---|---|
| ACHCOL-7C-EFFECTIVE-FROM | `EffectiveFrom` oficial de la política ACH Colombia de 7 ciclos. | **BLOCKED / NEEDS CLARIFICATION** |
| ACHCOL-7C-CYCLE4-START | Confirmación oficial de la hora inicial del Ciclo 4 por la inconsistencia documental `11:01pm` vs `11:01am`. | **BLOCKED / NEEDS CLARIFICATION** |

Mientras cualquiera de los dos siga abierto, `ACHCOL-7C-CANDIDATE` permanece **INACTIVE / NOT ACTIVATABLE**. No se declara producción, homologación, go-live ni aceptación de usuario. El frente permanece **IMPLEMENTADO / VALIDACIÓN EJECUTABLE FINAL PENDIENTE / UAT PENDIENTE**.

| ID | Dependencia | Estado |
|---|---|---|
| OPS-GAP-003 | Contrato operativo CFA↔CENIT Gateway/PO para intercambio real. | BLOQUEADO EXTERNO |
| ACHCOL-MFT-EXT | Topología productiva final, rutas, usuario/credencial, huella, firewall/lista blanca, conectividad CFA↔GoAnywhere/S3/Integra, observabilidad externa y homologación con ACH Colombia. | EXTERNO |
| CENIT-HOMOLOGATION | Homologación oficial de formatos/flujos CENIT y evidencia contra ambiente autorizado. | EXTERNO |
| SOAP-UAT-ENDPOINT | Endpoint UAT/mock autorizado para integraciones SOAP que hoy operan con guardrail DryRun. | EXTERNO |
| PSE-SCOPE-001 | Desarrollo diferido hasta contar con autoridad/definición y certificación específica. | DIFERIDO / EXTERNO |

Estos ítems no se convierten en “pendiente de programación” mientras falte el contrato, endpoint, credencial, vector o definición oficial requerida.

## D. Pruebas que deben quedar pendientes para entrega a usuarios

| Tipo de prueba | Estado de entrega |
|---|---|
| UAT funcional por usuarios | PENDIENTE INTENCIONALMENTE |
| Validación de pantallas y navegación con roles reales | PENDIENTE INTENCIONALMENTE |
| Flujos completos por ACH Colombia y CENIT con datos de negocio acordados | PENDIENTE INTENCIONALMENTE |
| Validación de manual review, mappings, conciliación y scheduler desde SPA | PENDIENTE INTENCIONALMENTE |
| Administración NACHA-M desde SPA y validación responsive | PENDIENTE INTENCIONALMENTE |
| Evidencia/capturas/acta de aceptación | PENDIENTE INTENCIONALMENTE |
| Homologación con servicios/ambientes externos | PENDIENTE SEGÚN DISPONIBILIDAD DEL TERCERO |

El paquete `entrega_pruebas_funcionales_usuarios/` se conserva como material para esta fase.

## Regla para próximos cambios

Cuando se cierre un pendiente del grupo A, debe moverse al grupo B o D según corresponda. No marcar como `CLOSED` una capacidad por el solo hecho de existir código: primero debe compilar y pasar sus pruebas automatizadas internas. La aceptación de usuarios se registra aparte y puede permanecer pendiente al momento de entregar el paquete para UAT.

## TPA-GOV-001 — nuevo programa funcional planificado

- Estado: `PLANNED`.
- Primera fase: `TPA0` characterization/baseline; sin cambios funcionales ni schema.
- Alcance: terceros/cuentas, onboarding silencioso single/bulk, prenotificaciones, elegibilidad contextual ACH Colombia V35/CENIT, API/UX administrativa y migración canónica incremental.
- Documento canónico: `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_IMPLEMENTATION_PLAN.md`.
- No interpretar esta inicialización como cierre técnico, UAT o release readiness.
