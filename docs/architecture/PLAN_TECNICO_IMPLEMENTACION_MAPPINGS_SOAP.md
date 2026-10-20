# Plan tecnico de implementacion de mappings SOAP

Fecha: 2026-06-26  
Estado: plan condicionado, sin implementacion  
Productivo: NO-GO

## 1. Resumen ejecutivo

Esta Fase 4 no implementa cambios. Define el plan tecnico para convertir decisiones funcionales aprobadas en cambios controlados de mappings SOAP.

Conclusion principal: no se encontro evidencia interna suficiente de aprobacion funcional para modificar campos monetarios, campos de parametrizacion o direccion request/response en `Proc_Contrapartidas` y `Proc_Transacciones`. Por tanto, el proyecto no esta listo para una implementacion funcional de mappings que cambie seeds, readiness, catalogo o reglas publicadas.

Si el equipo funcional/proveedor aprueba decisiones, este documento define como implementarlas por fases, con separacion estricta entre cambios no monetarios, parametrizacion, decisiones monetarias sensibles, readiness, seeds, tests, runtime Docker/SQL Server 2025 y UI.

## 2. Estado de fases anteriores

### Fase 1

`/integraciones/mappings` ya reconoce fuentes tecnicas validas como `Transaction`, `Cycle`, `ClearingHouse`, `Batch`, `Constant` y `DifferentialResponse`. No requiere cambios en esta fase.

### Fase 2

Readiness backend ya no debe retornar `OK` con placeholders criticos. Estados actuales esperados:

- `OK`
- `READY_WITH_WARNINGS`
- `FUNCTIONAL_MAPPING_PLACEHOLDER`
- `REGISTRAR_WSDL_CONTRACT_INVALID`

### Fase 3

Las decisiones funcionales bloqueantes quedaron documentadas en `docs/architecture/DECISIONES_FUNCIONALES_MAPPINGS_SOAP.md`. Ese documento no aprueba valores; identifica preguntas, riesgos y evidencia requerida.

## 3. Decisiones funcionales aprobadas encontradas

No se encontro aprobacion funcional de negocio/proveedor para los campos monetarios o parametrizados pendientes.

Decisiones tecnicas ya resueltas por evidencia interna:

| Decision | Evidencia interna | Estado |
|---|---|---|
| `PLValidarUsuarioBV` esta fuera de alcance funcional. | Matriz integral, documento de decisiones, tests de catalogo. | Resuelta tecnicamente. |
| `RegistrarRespuestaTransaccion` usa exactamente 7 parametros WSDL. | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper`, `IntegrationMappingReadinessService`, tests de readiness/bootstrap. | Resuelta tecnicamente. |
| `RegistrarRespuestaTransaccion` no acepta `ANS*` vigentes. | Bootstrap archiva/inactiva contrato no-WSDL; readiness valida contrato. | Resuelta tecnicamente. |
| `ANS*` pertenecen solo a `Proc_Contrapartidas` y no bloquean readiness. | Catalogo `Required=false`, tests de readiness. | Resuelta tecnicamente. |
| `/integraciones/soap-settings` esta separado de `/integraciones/mappings`. | `INTEGRACIONES_SOAP_SETTINGS_VS_MAPPINGS.md`. | Resuelta arquitectonicamente. |

Estas decisiones tecnicas no autorizan modificar semantica monetaria ni parametrizacion de negocio.

## 4. Decisiones pendientes

Pendientes criticas:

- `OFMONDEB`
- `OFMONCRE`
- `OFDD`
- `OFCTA`
- `OFST`
- `OFIDREVER`
- `TREG`
- `CONV`
- `PROD`
- `IDCAMCOMPE`
- `RTAACH`
- `RTALOC`

Pendientes altas/condicionales:

- `OFLIBRE`
- `OFLIBRE1`
- `DIRECCIONIP`
- `OFDIRECCIONIP`
- `OFIDCAMCOMPE`
- `DISCRE`
- `IREVER`
- `LIBRE`
- `LIBRE1`
- `causal`
- `descripcionCausal`

## 5. Decisiones que bloquean implementacion

No debe implementarse cambio funcional sobre mappings publicados o seeds mientras falten respuestas para:

1. Semantica de monto real en `OFMONDEB`/`OFMONCRE`.
2. Valor/regla de `OFDD`.
3. Fuente correcta de `OFCTA`.
4. Direccion real de `RTAACH`/`RTALOC`.
5. Parametrizacion oficial de `TREG`, `CONV`, `PROD` e `IDCAMCOMPE`.
6. Regla de reversos para `OFIDREVER` e `IREVER`.
7. Clasificacion de campos libres como fuente concreta, reservado u opcional.

## 6. Matriz tecnica de implementacion

| Servicio | Parametro | Decision funcional | Evidencia | Cambio tecnico requerido | Archivos probables | Tests requeridos | Impacto readiness | Impacto seed | Impacto UI | Riesgo | Estado |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Proc_Contrapartidas | `OFMONDEB` | Pendiente | Fase 3 exige definicion monto real. | Ninguno hasta aprobacion; luego ajustar fuente/regla monetaria aprobada. | `IntegrationMappingBootstrapper`, tests readiness/mapping, resolver funcional si aplica. | Contrato monetario debito; placeholder debe fallar si reaparece. | Puede pasar de `FUNCTIONAL_MAPPING_PLACEHOLDER` a `OK` solo con regla aprobada. | Alto impacto. | Sin cambio inicial. | Critico monetario. | Bloqueante. |
| Proc_Contrapartidas | `OFMONCRE` | Pendiente | Fuente `transaction.amount` con default `0`; semantica no aprobada. | Ninguno hasta aprobacion; luego eliminar/controlar default. | `IntegrationMappingBootstrapper`, readiness tests. | Regla debito/credito por naturaleza. | Warning o bloqueo segun decision. | Alto impacto. | Sin cambio inicial. | Critico monetario. | Bloqueante. |
| Proc_Contrapartidas | `OFDD` | Pendiente | Catalogo sugiere `D/C`; seed usa `C`. | Ninguno hasta aprobacion de valor/fuente. | `IntegrationMappingBootstrapper`, readiness tests. | Constante homologada o fuente derivada. | Placeholder/constante no homologada debe fallar. | Alto impacto. | Sin cambio inicial. | Critico monetario. | Bloqueante. |
| Proc_Contrapartidas | `OFCTA` | Pendiente | Mapping usa `transaction.originatingdfi`; posible DFI no cuenta. | Ninguno hasta diccionario de datos. | `IntegrationMappingBootstrapper`, source catalog si fuente no existe. | Fuente cuenta origen aprobada. | Debe seguir NotReady si queda ambiguo. | Medio/alto. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Contrapartidas | `OFST` | Pendiente | Seed cae en `SEED`. | Definir tabla/constante homologada o reservar. | Bootstrap/readiness/tests. | Estados oficiales. | Bloquea hasta homologacion. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Contrapartidas | `OFIDREVER` | Pendiente | Seed `1`; reverso no definido. | Definir normal/reverso/no aplica. | Bootstrap/readiness/tests. | Reverso normal vs reverso real. | Bloquea si constante no homologada. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Contrapartidas | `OFLIBRE` | Pendiente | Seed `SEED`. | Clasificar reservado o fuente. | Bootstrap/readiness/tests. | Reservado/opcional o fuente aprobada. | Bloquea si required funcional; warning si reservado. | Medio. | Posible estado reservado. | Medio. | Pendiente. |
| Proc_Contrapartidas | `OFLIBRE1` | Pendiente | Seed `1`. | Clasificar reservado o fuente. | Bootstrap/readiness/tests. | Igual `OFLIBRE`. | Bloqueo o warning segun decision. | Medio. | Posible estado reservado. | Medio. | Pendiente. |
| Proc_Contrapartidas | `OFDIRECCIONIP` | Parcial tecnica, no funcional | Readiness lo puede tratar como warning con `0.0.0.0`. | Si seguridad/proveedor aprueba, documentar constante tecnica; si no, definir fuente. | Readiness tests, docs; bootstrap solo con aprobacion. | Warning no bloqueante o fuente IP. | `READY_WITH_WARNINGS` o `OK`. | Bajo/medio. | Mostrar warning si se expone readiness. | Medio auditoria. | No bloqueante si proveedor acepta. |
| Proc_Contrapartidas | `OFIDCAMCOMPE` | Parcial tecnica, no funcional | Fuente `clearinghouse.id` con default `1`. | Eliminar/controlar default si se aprueba fuente. | Bootstrap/readiness/tests. | Multicamara ACH/CENIT. | Warning por default o `OK` sin default. | Medio. | Sin cambio inicial. | Alto en multicamara. | Pendiente. |
| Proc_Transacciones | `TREG` | Pendiente | Catalogo ejemplo `6`; no hay aprobacion funcional. | Si se aprueba `6`, convertir a constante homologada y testear. | Bootstrap/readiness/tests. | Constante oficial. | `OK` solo si valor homologado. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Transacciones | `CONV` | Pendiente | Seed `SEED`. | Definir fuente/tabla/convenio. | Bootstrap, posiblemente source catalog; tests. | Convenio aprobado. | Bloquea hasta fuente/constante homologada. | Medio/alto. | Posible fuente visible. | Alto. | Bloqueante. |
| Proc_Transacciones | `PROD` | Pendiente | Seed `SEED`; `ACH` no aprobado. | Definir producto/fuente. | Bootstrap, tests. | Producto aprobado. | Bloquea o warning segun decision. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Transacciones | `IDCAMCOMPE` | Pendiente | Seed `1`. | Definir fuente de camara. | Bootstrap/readiness/tests. | ACH/CENIT/multicamara. | Bloquea si constante generica. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Transacciones | `DIRECCIONIP` | Pendiente | Seed `SEED`. | Definir politica IP. | Bootstrap/readiness/tests/docs. | IP aprobada. | Warning o bloqueo segun proveedor. | Medio. | Posible warning. | Medio. | Pendiente. |
| Proc_Transacciones | `DISCRE` | Pendiente | Seed `SEED`. | Clasificar reservado/opcional o fuente. | Bootstrap/readiness/tests. | Definicion de uso. | Bloqueo o warning segun decision. | Medio. | Posible estado reservado. | Medio. | Pendiente. |
| Proc_Transacciones | `IREVER` | Pendiente | Seed `1`. | Definir reverso normal/reverso. | Bootstrap/readiness/tests. | Regla reversos. | Bloquea si no homologado. | Medio. | Sin cambio inicial. | Alto. | Bloqueante. |
| Proc_Transacciones | `LIBRE` | Pendiente | Seed `SEED`. | Clasificar reservado/opcional o fuente. | Bootstrap/readiness/tests. | Definicion funcional. | Bloqueo o warning. | Medio. | Posible estado reservado. | Medio. | Pendiente. |
| Proc_Transacciones | `LIBRE1` | Pendiente | Fuente `fileControls.blockCount`; semantica dudosa. | Mantener warning o cambiar con aprobacion. | Bootstrap/readiness/tests. | Confirmacion de semantica. | Warning si no bloquea. | Medio. | Posible warning. | Medio. | Pendiente. |
| Proc_Transacciones | `RTAACH` | Pendiente | Parser lo trata como respuesta; catalogo input required. | Confirmar direccion; luego mover/reclasificar o mantener. | `IntegrationCatalogBootstrapper`, readiness/tests, mapper/parser tests. | WSDL oficial o ejemplo SOAP. | Puede pasar a opcional/reservado o output. | Alto. | UI debe reflejar direccion. | Alto contrato. | Bloqueante. |
| Proc_Transacciones | `RTALOC` | Pendiente | Igual `RTAACH`. | Igual `RTAACH`. | Igual `RTAACH`. | Igual `RTAACH`. | Igual `RTAACH`. | Alto. | UI debe reflejar direccion. | Alto contrato. | Bloqueante. |
| RegistrarRespuestaTransaccion | `causal` | Condicional pendiente fina | Fuente `DifferentialResponse`; no monetario. | Solo ajustar si negocio define obligatoriedad. | Bootstrap/readiness/trace tests si cambia. | Causal aprobacion/rechazo. | Warning/OK segun condicion. | Bajo. | UI sin cambio. | Medio. | No bloquea WSCFAACH. |
| RegistrarRespuestaTransaccion | `descripcionCausal` | Condicional pendiente fina | Fuente `DifferentialResponse`; no monetario. | Igual `causal`. | Igual `causal`. | Descripcion externa/normalizada. | Warning/OK segun condicion. | Bajo. | UI sin cambio. | Medio. | No bloquea WSCFAACH. |
| RegistrarRespuestaTransaccion | 7 WSDL base | Resuelta tecnicamente | Tests y bootstrap actuales. | Ninguno. | No tocar salvo regresion. | Regresion 7 parametros, sin `ANS*`. | Mantener `OK`. | Ninguno. | UI debe seguir mostrando 7. | Bajo. | Resuelto. |
| Proc_Contrapartidas | `ANS*` | Resuelta tecnicamente como opcional/reservado | Catalogo `Required=false`, tests. | Ninguno salvo texto docs/UI futuro. | Tests bootstrap/readiness. | No bloquear readiness. | Sin bloqueo. | Ninguno. | Mostrar opcional/reservado. | Bajo. | Resuelto tecnico. |
| PLValidarUsuarioBV | N/A | Fuera de alcance | Docs/tests. | Ninguno. | No tocar. | No catalogado. | No aparece. | Ninguno. | No aparece. | Bajo. | Fuera de alcance. |

## 7. Plan por fases

### Fase 4A - Aplicar decisiones sin riesgo monetario

Objetivo: implementar solo decisiones ya aprobadas que no alteren montos ni naturaleza debito/credito.

Estado actual: no hay aprobaciones funcionales suficientes para iniciar codigo. La fase puede iniciar solo como preparacion de evidencias.

Archivos permitidos cuando exista aprobacion:

- `IntegrationMappingReadinessService.cs`
- `IntegrationMappingBootstrapper.cs`
- tests backend focalizados
- documentacion

Archivos prohibidos:

- logica monetaria SOAP
- WSDL
- Docker
- Angular salvo ajuste visual menor aprobado
- migraciones

Tests requeridos:

- readiness warning/OK segun decision aprobada
- no regresion de Registrar
- `PLValidarUsuarioBV` excluido

Runtime:

- no obligatorio hasta que exista cambio de seed/readiness

Rollback:

- revertir commit de fase; no ejecutar migraciones

Veredicto esperado:

- No iniciar implementacion de codigo hasta contar con aprobacion.

### Fase 4B - Aplicar parametrizacion funcional

Objetivo: implementar decisiones aprobadas para `TREG`, `CONV`, `PROD`, `IDCAMCOMPE`, `DIRECCIONIP`, `DISCRE`, `LIBRE`, `LIBRE1`.

Condicion de entrada:

- matriz de parametrizacion aprobada por negocio/proveedor
- definicion de si cada valor es fuente, constante homologada o reservado

Archivos probables:

- `IntegrationCatalogBootstrapper.cs` si cambia direccion/required/catalogo
- `IntegrationMappingBootstrapper.cs` si cambian reglas seed
- `IntegrationMappingReadinessService.cs` si cambian reglas OK/warning
- tests backend de bootstrap/readiness/mapping

Archivos prohibidos:

- logica monetaria SOAP
- Docker
- migraciones salvo requerimiento formal, que debe tratarse aparte

Tests:

- `ProcTransaccionesReadiness_ShouldBeOk_WhenApprovedFunctionalParametersAreSeeded`
- placeholders siguen fallando
- multicamara ACH/CENIT si aplica

Rollback:

- archivar mapping set seed nuevo y restaurar version anterior; no borrar historia

### Fase 4C - Aplicar decisiones monetarias sensibles

Objetivo: implementar solo reglas aprobadas para `OFMONDEB`, `OFMONCRE`, `OFDD`, `OFCTA`.

Condicion de entrada:

- aprobacion escrita de negocio/proveedor
- ejemplos SOAP esperados
- pruebas de no movimiento monetario real

Archivos probables:

- `IntegrationMappingBootstrapper.cs`
- `ProcContrapartidasFunctionalMappingResolver.cs` solo si resolver necesita reflejar decision aprobada
- tests backend de contrato monetario y readiness

Archivos prohibidos:

- clientes SOAP fisicos
- ejecucion SOAP real
- NACHA-M
- Docker

Tests:

- debito originado por CFA genera payload esperado
- campo no aplicable no usa placeholder
- readiness falla ante reintroduccion de `0`, `1`, `SEED` no homologado

Rollback:

- revertir mapping seed/politica readiness; conservar historia de MappingSet

### Fase 4D - Ajustar readiness y validation segun decisiones aprobadas

Objetivo: alinear readiness y validacion de publicacion con la politica funcional aprobada.

Archivos probables:

- `IntegrationMappingReadinessService.cs`
- `IntegrationMappingValidationService.cs` solo si publish debe impedir nuevos placeholders
- tests readiness/validation

Regla:

- no bloquear publish en esta fase si no esta explicitamente aprobado
- si se endurece publish, debe ser fase separada con tests de UX/API

### Fase 4E - Ajustar seeds/bootstrap idempotentes

Objetivo: que base limpia y base existente queden consistentes sin pisar configuracion manual.

Archivos probables:

- `IntegrationMappingBootstrapper.cs`
- `IntegrationCatalogBootstrapper.cs` si cambia required/direccion/catalogo
- tests `IntegrationBootstrapperTests`

Reglas:

- no borrar historia
- no pisar mapping manual publicado sin politica explicita
- crear nueva version seed solo cuando mapping publicado sea seed/controlado

### Fase 4F - Ajustar tests backend

Objetivo: cubrir cada decision aprobada y sus regresiones.

Tests requeridos:

- readiness OK para decisiones aprobadas
- readiness warning para campos tecnicos aceptados
- readiness failure para placeholders reintroducidos
- Registrar 7 WSDL sin `ANS*`
- Contrapartidas conserva `ANS*` opcionales/reservados
- `PLValidarUsuarioBV` no catalogado
- no llamadas SOAP reales

### Fase 4G - Validar Docker/SQL Server 2025 limpio

Objetivo: validar runtime solo despues de cambios de seed/readiness.

Comandos esperados:

```powershell
$env:DATABASE_APPLY_MIGRATIONS="true"
docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml build achinterbank-api achinterbank-spa
docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml up -d
curl -i http://localhost:843/health/ready
curl -i -X POST http://localhost:843/Maintenance/seed
```

Validaciones:

- SQL Server 2025 healthy
- API ready
- seed 200
- BD con mappings aprobados
- readiness por servicio segun decision aprobada

### Fase 4H - Validar UI

Objetivo: confirmar que `/integraciones/mappings` refleja estados correctos y que `/integraciones/soap-settings` no cambia.

Condicion:

- solo ejecutar si hubo cambio backend observable o estado nuevo para UI

Tests/validacion:

- Playwright en mappings
- no cambio en soap-settings

## 8. Fase recomendada para iniciar

No iniciar implementacion de codigo.

Fase recomendada inmediata: `4A.0 - Preparacion de aprobaciones`, que consiste en obtener evidencia funcional/proveedor para los campos bloqueantes listados en Fase 3.

Si el equipo exige avanzar con codigo, el unico alcance seguro seria no monetario y tecnicamente ya resuelto: reforzar tests/documentacion de no regresion de `RegistrarRespuestaTransaccion`, `ANS*` opcionales en `Proc_Contrapartidas` y exclusion de `PLValidarUsuarioBV`. Eso no resuelve los blockers de negocio y no debe presentarse como cierre funcional.

## 9. Cambios tecnicos por componente

| Componente | Cambio futuro posible | Condicion | Riesgo |
|---|---|---|---|
| `IntegrationCatalogBootstrapper` | Cambiar `Required`, direccion o textos de parametros. | Solo con WSDL/proveedor. | Alto si altera contrato. |
| `IntegrationMappingBootstrapper` | Reemplazar placeholders por fuentes/constantes aprobadas. | Decision funcional aprobada. | Alto en campos monetarios. |
| `IntegrationMappingReadinessService` | Actualizar allowlist de constantes homologadas y warnings. | Decision aprobada. | Medio si queda laxa. |
| `IntegrationMappingValidationService` | Impedir publish de placeholders. | Politica explicita. | Medio/alto por impacto UX/API. |
| `IntegrationMappingTraceWriter` | Agregar trazabilidad de decision/fallback. | Si se requiere auditoria de aprobaciones. | Bajo/medio. |
| Tests backend | Cubrir cada decision y regresiones. | Siempre que haya cambio. | Bajo. |
| Angular | Mostrar nuevos estados si backend los expone. | Solo si necesario. | Medio. |
| Migraciones | No recomendadas. | Solo si se agrega metadata persistida aprobada. | Alto. |
| Docker | No tocar. | N/A. | N/A. |

## 10. Impacto en seeds/bootstrap

No modificar seeds/bootstrap sin decisiones aprobadas.

Cuando existan decisiones:

- preferir cambios idempotentes en bootstrap antes que migraciones;
- no borrar MappingSets historicos;
- no sobrescribir mappings manuales publicados sin politica explicita;
- crear o activar reglas solo para parametros con decision aprobada;
- mantener `RegistrarRespuestaTransaccion` con 7 WSDL;
- mantener `ANS*` solo en `Proc_Contrapartidas` como opcionales/reservados.

## 11. Impacto en readiness

Readiness debe seguir fallando para:

- `SEED`
- `TEST`
- `0`/`1` como cobertura funcional no homologada
- `REF-1`
- `ACH` sin politica
- `000010070`
- `900123456`
- `constant.value`
- defaults sin fuente funcional o sin aprobacion

Readiness puede pasar a `READY_WITH_WARNINGS` solo con decision explicita de warning no bloqueante.

Readiness puede pasar a `OK` solo si:

- hay fuente funcional confiable; o
- hay constante homologada con evidencia; y
- no existe fallback peligroso.

## 12. Impacto en validation/publish

Hoy `IntegrationMappingValidationService` valida estructura, tipos, transformaciones y reglas publicables. No debe endurecerse publish en esta fase sin aprobacion, porque podria bloquear flujos manuales existentes.

Plan futuro:

1. Primero endurecer readiness.
2. Luego documentar politica de publish.
3. Finalmente, si se aprueba, impedir publicacion de placeholders criticos desde validation.

## 13. Impacto en trazabilidad

Fase futura puede enriquecer traza para:

- indicar si se uso default;
- indicar si una regla esta homologada, warning o pendiente;
- registrar version de MappingSet y decision aprobada;
- detectar reaparicion de placeholders en ejecuciones dry-run.

No se recomienda cambiar trazabilidad antes de aprobar reglas funcionales, salvo tests de no regresion.

## 14. Impacto en UI

No se requiere cambio UI en esta fase.

Futuro:

- `/integraciones/mappings` podria mostrar `Pendiente de definicion funcional`, `Warning tecnico` u `Opcional/reservado` si backend expone esos estados.
- `/integraciones/soap-settings` no debe cambiar ni mezclarse con matriz de campos.

## 15. Impacto en Docker/runtime

No se requiere cambio Docker.

Despues de cambios de seed/readiness, validar SQL Server 2025 limpio con:

- `DATABASE_APPLY_MIGRATIONS=true`
- `/Maintenance/seed`
- health ready
- consultas BD
- endpoint `/integraciones/mappings`

No ejecutar SOAP real.

## 16. Tests requeridos

Tests backend futuros por fase:

- `RegistrarRespuestaTransaccion` conserva 7 parametros WSDL.
- `RegistrarRespuestaTransaccion` no contiene `ANS*`.
- `PLValidarUsuarioBV` no esta catalogado.
- `Proc_Contrapartidas` conserva `ANS*` opcionales/reservados.
- Readiness falla con placeholders criticos.
- Readiness OK con constantes homologadas aprobadas.
- Readiness warning con constantes tecnicas aprobadas.
- Seeds son idempotentes en base limpia.
- Seeds no pisan mapping manual publicado sin politica.
- MappingSet publicado tiene reglas esperadas tras decision aprobada.
- Trace registra campos y defaults sin exponer datos sensibles.

Tests UI solo si se toca Angular:

- `/integraciones/mappings` muestra estados correctos.
- `/integraciones/soap-settings` no cambia.

## 17. Validaciones SQL Server 2025

Cuando exista implementacion:

1. Levantar con SQL Server 2025 y migraciones habilitadas despues de volumen limpio.
2. Ejecutar `/Maintenance/seed`.
3. Consultar:
   - `IntegrationMethods`
   - `IntegrationMethodParameters`
   - `IntegrationMappingSets`
   - `IntegrationMappingRules`
4. Validar:
   - Registrar 7 WSDL sin `ANS*`
   - Proc_Contrapartidas 22 parametros con `ANS*` reservados
   - Proc_Transacciones 27 parametros
   - `PLValidarUsuarioBV` ausente
   - readiness segun decisiones aprobadas

## 18. Riesgos

| Riesgo | Severidad | Mitigacion |
|---|---|---|
| Implementar monto sin aprobacion | Critica | Bloquear Fase 4C hasta evidencia. |
| Convertir placeholder en constante oficial por suposicion | Alta | Requerir evidencia por parametro. |
| Cambiar direccion `RTAACH`/`RTALOC` sin WSDL | Alta | Exigir WSDL/ejemplo SOAP. |
| Pisar mapping manual publicado | Alta | Politica explicita y tests. |
| Endurecer publish sin UI/API preparada | Media/alta | Fase separada para validation/publish. |
| Introducir migracion innecesaria | Alta | Preferir seed/readiness in-memory. |
| Mezclar soap-settings con mappings | Media | Mantener separacion documentada. |

## 19. Preguntas pendientes

Las preguntas pendientes son las de Fase 3. Las mas bloqueantes:

1. Donde viaja monto real en `Proc_Contrapartidas`: `OFMONDEB`, `OFMONCRE` o ambos?
2. Que valor debe tener `OFDD`?
3. Que fuente exacta debe alimentar `OFCTA`?
4. `RTAACH` y `RTALOC` son request o response?
5. Cuales son valores/fuentes aprobadas para `TREG`, `CONV`, `PROD`, `IDCAMCOMPE`?
6. Que semantica tienen `OFIDREVER` e `IREVER`?
7. Cuales campos libres son reservados y cuales obligatorios?

## 20. Criterios de aceptacion

Para considerar lista una futura implementacion:

- Cada parametro modificado tiene decision aprobada y evidencia.
- No se toca logica monetaria sin Fase 4C separada.
- No se toca WSDL.
- No se toca Docker.
- No se catalogan operaciones excluidas.
- Seeds idempotentes no pisan manuales.
- Readiness no retorna `OK` con placeholders criticos.
- Registrar conserva 7 WSDL sin `ANS*`.
- Proc_Contrapartidas conserva `ANS*` opcionales/reservados.
- Tests backend pasan.
- SQL Server 2025 limpio valida seed/readiness.
- UI mappings refleja estado correcto si aplica.
- soap-settings permanece separado.

## 21. Veredicto

No listo para implementacion: faltan decisiones funcionales criticas.

La implementacion de negocio debe esperar aprobacion de proveedor/core y negocio para campos monetarios, parametrizados y de direccion request/response. El unico trabajo seguro inmediato es preparar aprobaciones y, como mucho, reforzar documentacion/tests de no regresion tecnica ya resuelta.

## 22. No implementar

Este documento no autoriza cambios de codigo. No modificar:

- `.cs`
- `.ts`
- `.html`
- `.scss`
- `.spec.ts`
- tests
- compose
- Dockerfiles
- migraciones
- seeds
- WSDL

