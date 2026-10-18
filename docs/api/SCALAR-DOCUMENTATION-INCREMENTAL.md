# Plan incremental de afinación de documentación Scalar / OpenAPI

> **Proyecto:** ACHInterbank  
> **Objetivo:** convertir Scalar en un portal de integración autosuficiente, claro y verificable, sin cambiar la lógica de negocio ni romper contratos existentes.  
> **Modo de ejecución:** incremental mediante ChatGPT Chat, en unidades pequeñas, reanudables y con límites explícitos para evitar ejecuciones largas o bloqueadas.

---

## 0. Cómo usar este archivo

Este archivo es el **estado único de ejecución** del trabajo de documentación Scalar. Debe viajar dentro del repositorio y actualizarse en cada sesión.

### Regla principal

En cada conversación de ChatGPT se ejecuta **una sola unidad de trabajo** (`SC-xxx`) y se actualiza este mismo archivo antes de entregar el resultado.

No se debe intentar completar toda la documentación en una sola conversación.

### Ubicación recomendada en el repositorio

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

### Qué debe hacer una sesión nueva

1. Leer este archivo completo.
2. Revisar la sección **Estado de reanudación**.
3. Tomar únicamente la unidad indicada en `Próxima unidad` o la siguiente marcada `READY`.
4. Ejecutarla respetando los límites de sesión.
5. Validar solo el alcance afectado.
6. Actualizar este archivo.
7. Entregar un resumen corto de cambios, validaciones y siguiente unidad.
8. Generar un nuevo ZIP únicamente al final de la unidad, si se está trabajando sobre un ZIP.

---

# 1. Estado de reanudación

> Esta sección debe quedar siempre actualizada al terminar una sesión.

| Campo | Valor |
|---|---|
| Estado global | `VERIFY — SC-900 cerró la auditoría final estática de Scalar/OpenAPI: 416/416 operaciones públicas clasificadas (322 CONTRACTUAL_QG + 36 PENDING_RESERVED_SCOPE + 50 INTENTIONAL_EXCLUSION + 8 APIEXPLORER_IGNORED), QG-1/QG-2 = 322/322 e idénticos, UNEXPECTED = 0 y QG-3 DESACTIVADO. Gates SC-503..508, aliases SC-502 y retiro SC-507 preservados. Falta únicamente ejecutar OpenApiDocumentGenerationTests y build Release en un entorno con dotnet.` |
| Última unidad completada | `SC-900` (`VERIFY` — auditoría final y cierre completados estáticamente; validación ejecutable .NET pendiente) |
| Unidades en verificación | `SC-002` — prueba OpenAPI pendiente; `SC-003` — tests dirigidos pendientes; `SC-004` — estrategia de `operationId` implementada y prueba OpenAPI pendiente; `SC-005` — taxonomía oficial de tags implementada y prueba OpenAPI pendiente; `SC-006` — metadata de headers transversales implementada y prueba OpenAPI pendiente; `SC-007` — metadata global del documento implementada y prueba OpenAPI pendiente; `SC-008` — QG-1 incremental implementado y tests dirigidos pendientes; `SC-090` — checkpoint estático de Fase A completado y validación ejecutable pendiente; `SC-101` — generación XML habilitada en proyectos con contratos públicos y tests dirigidos pendientes; `SC-102` — patrón de ejemplos OpenAPI implementado y tests dirigidos pendientes; `SC-103` — patrón de responses y descripciones explícitas implementado y tests dirigidos pendientes; `SC-104` — Auth documentado con QG-1/QG-2 incremental y prueba OpenAPI pendiente; `SC-105` — cámaras y ciclos documentados con QG-1/QG-2 ampliados y prueba OpenAPI pendiente; `SC-106` — catálogos de riel de pago y perfiles NACHA-M documentados con QG-1/QG-2 ampliados y prueba OpenAPI pendiente; `SC-107` — Quick Start agregado a la metadata global y prueba OpenAPI pendiente; `SC-190` — checkpoint estático de Fase B completado y validación ejecutable pendiente; `SC-201` — cinco operaciones core de TransactionsController documentadas y QG-1/QG-2 ampliados; `SC-202` — cuatro operaciones bulk/integración restantes de TransactionsController documentadas y QG-1/QG-2 ampliados; `SC-203` — seis operaciones de BulkIngestionController documentadas, responses saneados, DTO XML y QG-1/QG-2 ampliados; `SC-204` — tres operaciones de AchTraceabilityController documentadas, responses saneados, DTO XML y QG-1/QG-2 ampliados; `SC-205` — tres operaciones del flujo base de devoluciones documentadas en AchReturnsController y ReturnReasonsController, con catálogo, generación de archivo, DTO XML y QG-1/QG-2 ampliados; `SC-206` — once operaciones de devolución de devolución y respuestas documentadas en AchReturnOfReturnController y AchResponsesController, con responses reales, DTO XML y QG-1/QG-2 ampliados; `SC-207` — catorce operaciones de políticas y reglas transaccionales documentadas en ClearingHouseTransactionPoliciesController y ClearingHouseTransactionRulesController, con status reales, DTO XML y QG-1/QG-2 ampliados; `SC-208` — nueve operaciones de políticas y reglas transaccionales documentadas en CfaOperationalTransactionRestrictionsController y PaymentRailCapabilityRegistryController, con status reales, DTO XML y QG-1/QG-2 ampliados a 80 operaciones; `SC-209` — una operación de TransactionPrerequisitePolicyController documentada con response 200 tipado y QG-1/QG-2 ampliados a 81 operaciones; `SC-290` — checkpoint estático de Fase C completado con QG-1/QG-2 preservados y validación ejecutable pendiente; `SC-301` — cuatro consultas Incoming NACHA del grupo 1 documentadas contractualmente, DTO XML completados y QG-2 ampliado de 77 a 81 sin cambiar QG-1; `SC-302` — trece operaciones restantes de IncomingNachaCommandCenterController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 81 a 94; `SC-303` — diez operaciones legacy de definiciones/layouts NACHA documentadas con mutaciones retiradas 410, XML docs y QG-1/QG-2 ampliados de 94 a 104; `SC-304` — catorce operaciones de AchResponseOperationsController documentadas contractualmente con mappings, auditoría, huérfanos y reproceso, XML docs y QG-1/QG-2 ampliados de 104 a 118; `SC-305` — siete operaciones de CenitOperationsController documentadas contractualmente, DTO XML y QG-1/QG-2 ampliados de 118 a 125; `SC-306` — quince operaciones efectivas de certificados documentadas contractualmente con QG-1/QG-2 ampliados de 125 a 140; `SC-307` — tres operaciones efectivas de SobreDigitalController documentadas contractualmente con QG-1/QG-2 ampliados de 140 a 143; `SC-308` — ocho operaciones de NachaSecurityOperationsController documentadas contractualmente con responses reales, DTO XML y QG-1/QG-2 ampliados de 143 a 151; `SC-309` — cuatro operaciones de NachaExportController/NachaUploadController documentadas contractualmente con content types, nombres de descarga/upload y responses reales, DTO XML y QG-1/QG-2 ampliados de 151 a 155; `SC-390` — checkpoint estático de Fase D completado con 155 operaciones únicas en QG-1/QG-2, 78 operaciones de Fase D presentes en ambos gates y sin regresiones detectadas; `SC-401` — nueve operaciones de transferencia/ejecución de AchColombiaFileExchangeController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 155 a 164; `SC-402` — doce operaciones de IntegrationMappingSetsController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 164 a 176; `SC-403` — tres operaciones de SoapIntegrationSettingsController/UatContrapartidasController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 176 a 179; `SC-404` — trece operaciones de SchedulerController documentadas contractualmente, DTO/enums XML completados y QG-1/QG-2 ampliados de 179 a 192; `SC-405` — cinco operaciones de TaskDefinitionsController documentadas contractualmente, TaskDefinitionDto/TaskParameterDto/enums XML completados y QG-1/QG-2 ampliados de 192 a 197; `SC-406` — doce operaciones autorizadas de ReportsController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 197 a 209; `SC-407` — ocho operaciones restantes de ReportsController documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 209 a 217; `SC-408` — nueve operaciones de administración/catálogos documentadas contractualmente en IntegrationCatalogController y AchColombiaFileExchangeController, DTO XML completados y QG-1/QG-2 ampliados de 217 a 226; `SC-409` — once operaciones GET de lectura de configuración NACHA documentadas contractualmente en NachaConfigProfilesController y NachaConfigProfilesReadOnlyController, DTO/read models XML completados y QG-1/QG-2 ampliados de 226 a 237; `SC-410` — tres acciones de AuditLogsController/NavigationLogsController y dos Minimal APIs health documentadas contractualmente, DTO XML completados y QG-1/QG-2 ampliados de 237 a 242; `SC-490` — Checkpoint Fase E completado estáticamente con 87 operaciones de SC-401..SC-410 presentes en QG-1/QG-2, 242 operaciones únicas globales y conjuntos idénticos; `SC-501` — inventario transversal de routing completado con 52 grupos alias / 104 rutas, 0 duplicados exactos, 0 colisiones de template, 10 canónicas definidas y 42 pendientes de decisión; `SC-502` — limpieza de ALIAS-033..042 completada: 10 aliases `/api` sin uso eliminados, 0 deprecated nuevos, 2 `[Obsolete]` retirados, QG-1/QG-2 preservados en 242; `SC-503` — auditoría de 431 parámetros OpenAPI completada sobre 242 operaciones, con 8 descripciones añadidas, 0 parámetros/metadata inválida agregados y nuevo gate QG-2; `SC-504` — auditoría de 476 pares operación/status completada sobre las mismas 242 operaciones, sin responses faltantes/especulativos ni correcciones de schema/media type, y con gate transversal de status exactos añadido; `SC-505` — auditoría de examples completada sobre 10 METHOD + PATH críticos definidos por decisión humana, con 6 request examples y 4 response examples contractuales agregados sin ampliar routing/QG; `SC-506` — auditoría de DTO/schemas ambiguos completada sobre 290 contratos schema alcanzables, con 31 descripciones de propiedades corregidas en 3 familias y gate dirigido agregado. En todos los casos la validación ejecutable pendiente se debe a que el entorno no dispone de `dotnet`; `SC-507` — los tres artefactos estáticos obsoletos fueron retirados por falta de consumidor vigente y desincronización con el contrato de 242 operaciones; validación ejecutable .NET no disponible; `SC-508` — 92 operaciones resueltas (80 PUBLIC_CONTRACT + 12 INTENTIONAL_EXCLUSION), QG-1/QG-2 322/322 y gate final sin UNEXPECTED; ejecución .NET pendiente; `SC-900` — auditoría final estática cerrada, 416/416 clasificadas y validación .NET final pendiente.` |
| Próxima unidad | `Ninguna` — no se abre trabajo posterior; la iniciativa Scalar queda en `VERIFY` hasta la validación final con .NET. |
| Último artefacto base | `ACHInterbank-v124-scalar-r900-SC900.zip` |
| Build completo | `NO EJECUTADO EN SC-900 — dotnet --info devolvió exit 127 (dotnet no disponible). No se simuló build Release.` |
| Tests completos | `NO EJECUTADOS EN SC-900 — dotnet no está disponible (exit 127). OpenApiDocumentGenerationTests requiere ejecución posterior con .NET.` |
| Validación dirigida | `SC-900: auditoría estática final coherente. QG-1/QG-2 = 322/322 con el mismo METHOD + PATH set; 416/416 clasificadas = 322 + 36 + 50 + 8; UNEXPECTED = 0. SC-503=431 parámetros, SC-504=476 operation/status, SC-505 examples críticos, SC-506 schemas y SC-508 cobertura final permanecen conectados al test OpenAPI. 10 aliases SC-502 ausentes; 42 grupos CANÓNICA PENDIENTE y 36 reservas intactos; artefactos SC-507 ausentes. validate-solution-projects.py = SOLUTION GRAPH: OK; merge markers = 0.` |
| Bloqueadores conocidos | `Sin inconsistencias de cierre detectadas. Única limitación: dotnet no está disponible, por lo que SC-900 y la iniciativa Scalar permanecen VERIFY hasta ejecutar OpenApiDocumentGenerationTests y build Release.` |
| Última actualización | `2026-10-08` |

### Archivos modificados en la última unidad

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

### Archivos revisados en SC-001

```text
src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs
src/Cfa.ACHInterbank.Api/OpenApi/ScalarOperationDocumentationTransformer.cs
src/Cfa.ACHInterbank.Api/OpenApi/OpenApiSecurityMetadataTransformer.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj  (configuración de proyecto; no es archivo de código)
```

### Estado final de la iniciativa Scalar

```text
SC-900 = VERIFY — auditoría final estática completada sin inconsistencias.
Cobertura final: 416/416 clasificadas = 322 CONTRACTUAL_QG + 36 PENDING_RESERVED_SCOPE + 50 INTENTIONAL_EXCLUSION + 8 APIEXPLORER_IGNORED; UNEXPECTED = 0.
QG-1 = 322; QG-2 = 322; conjuntos METHOD + PATH idénticos; QG-3 = DESACTIVADO.
SC-503/504/505/506/508 permanecen vigentes; SC-502 y SC-507 preservados.
Las 36 reservas y los 42 grupos CANÓNICA PENDIENTE permanecen fuera del cierre contractual.
dotnet --info = exit 127; no se simularon tests/build. validate-solution-projects.py = SOLUTION GRAPH: OK.
No hay siguiente unidad abierta. La iniciativa no se declara DONE hasta ejecutar la validación .NET final.
```

---

# 2. Principios no negociables

1. **La documentación debe describir comportamiento real.** No se permite inventar comportamiento funcional a partir del nombre del método, controller o ruta.
2. **Scalar no es la fuente de verdad.** La fuente de verdad es metadata OpenAPI mantenible en código y sus contratos.
3. **El transformer global no debe sustituir documentación funcional explícita.** Solo puede aplicar metadatos determinísticos y convenciones transversales.
4. **No se cambia lógica de negocio dentro de este plan.** Si se detecta un problema funcional, se registra como deuda separada.
5. **No se normalizan contratos de error silenciosamente.** Primero se documenta el comportamiento actual. Cualquier cambio contractual requiere iniciativa independiente.
6. **No se eliminan rutas legacy en esta iniciativa.** Se identifican, documentan y marcan como deprecadas cuando corresponda.
7. **No se deben agregar textos genéricos para “cumplir cobertura”.** Una descripción que no aporta significado es equivalente a no documentar.
8. **Cada endpoint nuevo debe poder fallar CI si no cumple el estándar documental.**
9. **Los ejemplos deben ser ficticios pero técnicamente válidos.** Nunca incluir secretos, tokens reales, certificados reales, cuentas reales o datos personales reales.
10. **Este Markdown es el único tracker incremental.** Evitar crear un nuevo documento `scalar-*.md` por cada pequeña sesión salvo que exista una necesidad de evidencia formal independiente.

## 2.1 Política permanente para código, fuentes normativas y continuidad

### Prioridad de evidencia

Para documentar un comportamiento relevante se debe distinguir siempre entre:

1. **Comportamiento observado en el código:** lo que ACHInterbank implementa realmente.
2. **Regla de ACH Colombia:** lo que establece la documentación de esa cámara, cuando aplique.
3. **Regla de CENIT:** lo que establece la documentación de esa cámara, cuando aplique.
4. **Interpretación técnica:** conclusión obtenida al relacionar código y fuente normativa, identificada expresamente como interpretación y no como hecho independiente.

La documentación funcional debe partir del comportamiento real del código. Los documentos normativos y operativos pueden complementar la explicación cuando aporten contexto necesario, pero nunca sustituyen la comprobación de la implementación.

Nunca se debe presentar como implementada una regla solo porque aparece en un manual, presentar como normativa una conducta deducida únicamente del código, ni usar documentación de una cámara como prueba de comportamiento de la otra.

### Separación obligatoria entre ACH Colombia y CENIT

ACH Colombia y CENIT se tratan como **cámaras independientes**. Antes de usar una fuente normativa debe determinarse si el comportamiento corresponde a ACH Colombia, CENIT, ambas cámaras o lógica interna de ACHInterbank independiente de la cámara.

`NACHA-M` no se tratará como una especificación universal sin contexto. ACH Colombia posee lineamientos propios relacionados con NACHA-M y CENIT posee sus propias especificaciones. Se evitarán formulaciones ambiguas como “Según NACHA-M...” o “El formato NACHA-M exige...”. Cuando existan diferencias o pueda existir ambigüedad, la documentación deberá identificar expresamente la cámara y el documento de origen. Una regla solo podrá declararse común a ambas cámaras cuando haya sido comprobada contra fuentes correspondientes de las dos.

Los ciclos y horarios de ACH Colombia y las sesiones de CENIT deben mantenerse separados. No se utilizarán ciclos, horarios, causales, estructuras, restricciones o validaciones de una cámara para explicar la otra.

Cuando una operación soporte ambas cámaras, la documentación futura deberá distinguir, cuando aplique, entre comportamiento común y particularidades de ACH Colombia y CENIT. La información realmente común no debe duplicarse, pero comportamientos con nombres parecidos tampoco deben unificarse sin verificación.

### Fuentes normativas disponibles para unidades futuras

Las unidades posteriores podrán consultar, entre otras fuentes disponibles en el proyecto:

- **ACH Colombia:** Manual de Servicio ACH Transferencias Interbancarias para Entidad Participante; documentación de ciclos y horarios de ACH Colombia; secciones y anexos relacionados con NACHA-M disponibles en el proyecto.
- **CENIT:** Reglamento CENIT; Manual Operativo CENIT; Manual de Operación CENIT-WEB; Manual de Especificaciones Formato NACHA-M CENIT; Anexo A de causales de devolución; Anexo B de causales de rechazo; y demás documentos CENIT disponibles en el proyecto.

La documentación de una cámara no debe utilizarse como autoridad normativa de la otra.

### Uso de fragmentos normativos

Se pueden utilizar fragmentos breves de documentos normativos cuando ayuden realmente a explicar una operación. Deben conservar fielmente el significado, identificar claramente la cámara y el documento de origen, usarse solo cuando agreguen valor y evitar bloques extensos innecesarios. Scalar debe explicar primero el comportamiento de ACHInterbank y después relacionarlo, cuando aporte contexto, con la norma aplicable; no debe convertirse en una reproducción extensa de los manuales.

### Discrepancias entre código y norma

Si el código y una fuente normativa parecen diferir, no se debe corregir silenciosamente ninguna de las dos partes ni asumir cuál tiene la razón. La discrepancia debe registrarse indicando la cámara, el documento y el archivo o componente de código involucrado, dejando la decisión para la unidad correspondiente.

### Continuidad y validaciones pendientes

Una validación pendiente **exclusivamente por limitaciones del entorno** —por ejemplo, ausencia de `dotnet`, infraestructura, servicios externos o ambiente real— no bloquea el avance incremental por sí sola. La unidad puede quedar en `VERIFY` mientras la siguiente queda en `READY`, y la prueba pendiente puede recuperarse posteriormente en un ambiente adecuado o en un checkpoint acumulado.

Solo se debe detener el avance cuando exista evidencia concreta de un error de compilación causado por el cambio, incompatibilidad contractual, ambigüedad funcional que impida implementar correctamente la siguiente unidad o una modificación que pueda propagar un defecto conocido.

---

# 3. Reglas anti-bloqueo para ChatGPT Chat

Estas reglas existen para evitar que una conversación intente procesar demasiado y termine detenida, exceda tiempo de herramientas o produzca cambios difíciles de revisar.

## 3.1 Límites máximos por sesión

Una sesión normal debe respetar **todos** estos límites:

- Máximo **2 controladores** por sesión.
- Máximo **15 operaciones HTTP** por sesión.
- Máximo **8 archivos de código modificados**, sin contar este tracker.
- Máximo **1 objetivo técnico principal** por sesión.
- No ejecutar build del frontend.
- No ejecutar pruebas E2E del SPA salvo que la unidad lo pida explícitamente.
- No reconstruir el inventario completo de la solución en cada sesión.
- No volver a analizar archivos ya cerrados si no hay una regresión que lo justifique.

Si una unidad supera cualquiera de estos límites, debe dividirse antes de implementarla.

## 3.2 Límites de comandos

- Preferir búsquedas dirigidas (`rg`, lectura de archivos específicos) en lugar de escaneos repetidos de todo el repositorio.
- Si un comando de validación tarda más de **120 segundos**, cancelarlo y registrar la validación como `PENDIENTE` en vez de esperar indefinidamente.
- `dotnet test` completo solo se ejecuta en checkpoints (`SC-090`, `SC-190`, `SC-290`, `SC-390`, `SC-900`) o cuando el usuario lo solicite.
- En sesiones normales usar tests dirigidos por clase/proyecto cuando sea posible.
- No ejecutar una compilación completa repetidamente si no hubo cambios que lo justifiquen.
- No comprimir/descomprimir el mismo ZIP múltiples veces en una sola sesión. Extraer una vez, trabajar y comprimir una vez al final.

## 3.3 Regla de salida temprana

La sesión debe detenerse y devolver un estado parcial controlado cuando ocurra cualquiera de estas condiciones:

- Aparece una ambigüedad de negocio que no puede resolverse leyendo el código y documentación del repositorio.
- Para documentar correctamente sería necesario cambiar el contrato funcional.
- Se requieren más de 2 controladores o 15 operaciones para cerrar la unidad.
- Una dependencia de infraestructura impide implementar correctamente la unidad, no solo ejecutar una validación posterior.
- Un build/test dirigido falla por una causa ajena a la unidad y no puede diagnosticarse rápidamente.
- Se alcanza un bloqueo técnico que exigiría una investigación distinta.

En esos casos:

1. No improvisar.
2. Marcar la unidad `BLOCKED` o `VERIFY`.
3. Registrar exactamente qué falta.
4. Proponer la unidad de desbloqueo más pequeña posible.

---

# 4. Estados permitidos

Cada unidad de trabajo utiliza uno de estos estados:

| Estado | Significado |
|---|---|
| `TODO` | Aún no preparada para ejecución |
| `READY` | Puede ejecutarse en la siguiente sesión |
| `IN_PROGRESS` | Está siendo trabajada en la sesión actual |
| `VERIFY` | Cambios hechos, falta una validación disponible fuera de la sesión |
| `BLOCKED` | Existe una dependencia o decisión pendiente |
| `DONE` | Implementada y validada según su criterio de salida |
| `SKIPPED` | Se decidió explícitamente no realizarla, con justificación |

No marcar `DONE` únicamente porque el código fue editado.

---

# 5. Definition of Done por operación OpenAPI

Una operación pública se considera documentalmente terminada cuando, según corresponda, cumple lo siguiente:

- [ ] `Summary` específico, breve y orientado a intención.
- [ ] `Description` funcional explícito, no generado por heurística.
- [ ] `operationId` estable y único.
- [ ] Tag funcional válido.
- [ ] Seguridad/autenticación representada correctamente.
- [ ] Policy/permisos explicados de acuerdo con la autorización real.
- [ ] Parámetros de ruta documentados.
- [ ] Parámetros query documentados, incluyendo formato/rango/default cuando aplique.
- [ ] Headers especiales documentados.
- [ ] Request body documentado.
- [ ] DTO y propiedades relevantes entendibles en schema.
- [ ] Response de éxito documentado.
- [ ] Status codes relevantes documentados.
- [ ] Content types correctos.
- [ ] Ejemplo de request cuando aporta valor.
- [ ] Ejemplo de response cuando aporta valor.
- [ ] Side effects o implicaciones operativas descritas cuando existan.
- [ ] Idempotencia descrita cuando aplique.
- [ ] Relación ACH/CENIT/NACHA-M descrita únicamente si es real y relevante.
- [ ] Alias/ruta legacy identificado y marcado como deprecated cuando corresponda.

## Texto no aceptable como documentación final

Ejemplos que deben ser detectados como incompletos:

```text
Endpoint de la API ACH Interbank.
Operación del módulo X.
Consulta información del sistema.
DOCUMENTATION INCOMPLETE
```

También es inválida cualquier descripción funcional “deducida” automáticamente del nombre de la ruta.

---

# 6. Estándar de redacción humanizada

## Summary

Debe contestar **qué consigue el consumidor**.

**Bueno:**

```text
Consulta el ciclo ACH activo para una cámara de compensación.
```

**Malo:**

```text
GET de AchCycles.
```

## Description

Usar prosa corta y directa. Cuando aplique debe incluir:

1. Qué hace.
2. Cuándo usarla.
3. Qué efecto produce.
4. Requisitos de seguridad importantes.
5. Restricciones relevantes.
6. Relación con el flujo operativo.

Evitar descripciones infladas con información que ya aparece de forma estructurada en OpenAPI.

## Parámetros

No repetir el nombre del parámetro. Explicar su semántica.

**Bueno:**

```text
Identificador interno de la cámara de compensación configurada en ACHInterbank.
Debe corresponder a una cámara activa.
```

## Fechas y horas

Siempre especificar, cuando aplique:

- formato;
- si representa fecha calendario u operacional;
- zona horaria o UTC;
- restricciones de rango.

## Importes

Cuando aplique especificar:

- moneda;
- unidad;
- precisión;
- restricciones de signo/rango.

---

# 7. Arquitectura documental objetivo

```text
Código + atributos / XML docs / metadata determinística
                        |
                        v
               Documento OpenAPI
                        |
                        v
                      Scalar
                        |
          +-------------+-------------+
          |                           |
          v                           v
   Consumo humano               SDK / tooling
```

La documentación auxiliar debe derivarse de esta fuente o quedar claramente identificada como evidencia histórica.

---

# 8. Backlog incremental

## Fase A — Fundaciones y quality gate mínimo

| ID | Estado | Alcance | Límite recomendado | Criterio de salida |
|---|---|---|---|---|
| `SC-001` | `DONE` | Auditar configuración actual de Scalar/OpenAPI y definir metadata que seguirá siendo responsabilidad de transformers | 4 archivos | Lista cerrada de responsabilidades del transformer y cambios propuestos |
| `SC-002` | `VERIFY` | Retirar `ScalarOperationDocumentationTransformer` y eliminar inferencias funcionales/riesgo/precauciones inventadas | 3 archivos | Implementación completada; prueba dirigida pendiente por ausencia de `dotnet` |
| `SC-003` | `VERIFY` | Crear detector de documentación incompleta y placeholders | 3 archivos | Detector implementado; tests dirigidos pendientes por ausencia de `dotnet` |
| `SC-004` | `VERIFY` | Definir estrategia estable de `operationId` | 3 archivos | Implementación completada; prueba dirigida pendiente por ausencia de `dotnet` |
| `SC-005` | `VERIFY` | Definir taxonomía oficial de tags de integración | 3 archivos | Implementación completada; prueba dirigida pendiente por ausencia de `dotnet` |
| `SC-006` | `VERIFY` | Documentar headers transversales (`Authorization`, `Idempotency-Key`, `X-Correlation-ID`) mediante metadata OpenAPI | 4 archivos | Implementación completada; prueba dirigida pendiente por ausencia de `dotnet` |
| `SC-007` | `VERIFY` | Afinar información global del documento OpenAPI/Scalar: título, descripción, convenciones y seguridad | 4 archivos | Implementación completada; prueba dirigida pendiente por ausencia de `dotnet` |
| `SC-008` | `VERIFY` | Extender test OpenAPI con quality gate mínimo: summary, description no genérica, operationId, tags | 4 archivos | QG-1 incremental implementado; tests dirigidos pendientes por ausencia de `dotnet` |
| `SC-090` | `VERIFY` | Checkpoint Fase A | Sin cambios funcionales | Checkpoint estático completado; build/tests pendientes por ausencia de `dotnet`; no bloquea Fase B |

### Resultado de SC-001 — auditoría de responsabilidades OpenAPI/Scalar

**Estado:** `DONE`  
**Alcance ejecutado:** solo infraestructura OpenAPI/Scalar. No se revisaron ni documentaron controladores.

#### Configuración observada

- `AddOpenApi("v1")` registra actualmente tres transformaciones: `ScalarOperationDocumentationTransformer`, `OpenApiSecurityMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer`.
- El documento se publica en `/openapi/{documentName}.json` con caché de salida y acceso anónimo. Scalar también se publica con acceso anónimo; `Agent`, `MCP` y las herramientas de desarrollo están deshabilitadas.
- La respuesta OpenAPI se comprime de forma dirigida y las rutas `/openapi` y `/scalar` quedan fuera de `WafMiddleware`, `RequestResponseLoggingMiddleware` y `TokenJwtMiddleware`. Estas decisiones pertenecen a configuración de hospedaje, no a transformadores de documentación.
- El proyecto genera archivo XML de documentación y referencia `Microsoft.AspNetCore.OpenApi`, `Microsoft.OpenApi` y `Scalar.AspNetCore`.
- La prueba `OpenApiDocumentGenerationTests.GenerateAndSerialize_ShouldCompleteWithBearerAndEnrichedDocumentation` confirma por diseño actual que el documento espera el esquema Bearer, seguridad por operación y texto enriquecido fabricado por el transformer. Esa expectativa deberá ajustarse en SC-002 para dejar de exigir texto heurístico.

#### Lista cerrada de responsabilidades que sí corresponden a transformadores

1. **Definir el esquema de seguridad Bearer a nivel de documento**, de forma mecánica y única, a partir de la configuración técnica conocida (`OpenApiBearerSecuritySchemeTransformer`).
2. **Reflejar seguridad por operación desde metadata explícita del endpoint**, sin redactar interpretación funcional: `[AllowAnonymous]` elimina el requisito de seguridad y `[Authorize]` agrega el requisito Bearer (`OpenApiSecurityMetadataTransformer`).
3. **No se aprueba ninguna otra responsabilidad para transformadores en SC-001.** `operationId`, tags, headers transversales, deprecación y otras convenciones quedan sujetos a sus unidades específicas; no deben anticiparse en SC-002.

La regla general queda fijada así: un transformer puede copiar o normalizar metadata transversal cuando el origen sea explícito, verificable y determinístico; no puede crear prosa funcional ni completar vacíos mediante heurísticas.

#### Responsabilidades actuales que deben salir de `ScalarOperationDocumentationTransformer` en SC-002

Deben eliminarse del transformer la generación o sustitución de `Summary` y `Description`, junto con todas las inferencias usadas para construirlas:

- clasificación de dominio por nombre de ruta o controller;
- propósito derivado de método HTTP, nombre de acción y ruta;
- contexto de uso supuesto;
- perfil consumidor supuesto;
- texto de permiso/autorización incrustado en la descripción, especialmente el supuesto de token válido cuando no existe `[Authorize]` explícito;
- clasificación `solo consulta` / `modifica información` por verbo HTTP;
- afirmación de que una operación genera auditoría;
- lista textual de códigos de respuesta y, en particular, códigos de respuesta inventados cuando OpenAPI no los contiene;
- riesgo operativo deducido de palabras de la ruta o del verbo HTTP;
- relación ACH/CENIT/NACHA-M inferida por ruta;
- precauciones operativas o de desarrollo inventadas;
- reemplazo de documentación vacía/genérica por texto automático;
- helpers de humanización, dominio, riesgo, propósito, uso, consumidor, relación de negocio y precaución que existan únicamente para esas heurísticas.

`IsGenericText` expresa una necesidad válida de control de calidad, pero **no debe seguir dentro de un transformer que rellena documentación**. La detección de vacíos/placeholders pertenece a `SC-003`.

#### Cambios concretos propuestos para SC-002 — no implementados en esta unidad

1. Retirar `ScalarOperationDocumentationTransformer` del registro de `AddOpenApi`.
2. Eliminar la clase/archivo `ScalarOperationDocumentationTransformer.cs` si, después de retirar las heurísticas, no queda una responsabilidad determinística real. No trasladar sus heurísticas a otro helper o transformer.
3. Mantener separados `OpenApiSecurityMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer`; no hacer una refactorización general de seguridad en SC-002.
4. Ajustar `OpenApiDocumentGenerationTests` para dejar de exigir frases como `ACH/CENIT/NACHA-M` o `Precauciones` generadas por heurística y conservar las verificaciones estructurales de serialización, referencias y seguridad.
5. Añadir una verificación negativa y dirigida de que el OpenAPI generado ya no contiene el andamiaje fabricado (`Qué hace:`, `Cuándo se usa:`, `Riesgo operativo:`, `Precauciones para desarrollo u operación:`), sin exigir todavía que todos los endpoints tengan documentación funcional completa.
6. No agregar `Summary`/`Description` a controladores en SC-002. Los vacíos resultantes se harán visibles y se gestionarán desde `SC-003` y las unidades de documentación explícita posteriores.

#### Ambigüedad registrada

El alcance efectivo de `TokenJwtMiddleware` sobre acciones sin `[Authorize]` no se resolvió en SC-001 porque el límite de revisión se cerró en cuatro archivos de código. Por lo tanto, la frase actual `requiere token válido (sin política explícita en atributo)` **no se considera metadata determinística** y debe desaparecer en SC-002. Esta ambigüedad no bloquea SC-002 porque la decisión de separación no depende de asumir su comportamiento.

#### Validación de SC-001

- Revisión estática dirigida de cuatro archivos de código y del `.csproj` de la API.
- Se intentó ejecutar únicamente `OpenApiDocumentGenerationTests.GenerateAndSerialize_ShouldCompleteWithBearerAndEnrichedDocumentation`.
- El intento no pudo iniciar porque el entorno de ejecución no tiene `dotnet` instalado (`dotnet: command not found`). No se ejecutaron frontend, E2E, build completo ni suite completa.
- El criterio de salida de SC-001 se considera cumplido porque esta unidad es una auditoría de responsabilidades y deja una separación cerrada y cambios concretos para SC-002; la validación runtime permanece pendiente por limitación del entorno.

### Resultado de SC-002 — retiro de documentación heurística

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente infraestructura OpenAPI y su prueba dirigida. No se modificaron ni documentaron controladores o endpoints como parte de la implementación.

#### Implementación realizada

1. Se retiró `ScalarOperationDocumentationTransformer` del registro de `AddOpenApi`.
2. Se eliminó `ScalarOperationDocumentationTransformer.cs` porque, una vez retiradas las heurísticas, no quedaba en esa clase ninguna responsabilidad transversal y determinística aprobada en SC-001.
3. No se trasladó su lógica a otro transformer, helper, extensión, servicio, atributo ni convención automática.
4. `OpenApiSecurityMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer` se conservaron sin refactorización y siguen limitados a metadata de seguridad explícita y determinística.
5. `OpenApiDocumentGenerationTests` dejó de exigir `Summary` o `Description` enriquecidos por heurística, mantuvo las verificaciones estructurales, del esquema Bearer y del requisito de seguridad por operación, y ahora comprueba sobre la operación representativa que no aparezcan los marcadores `Qué hace:`, `Cuándo se usa:`, `Riesgo operativo:` ni `Precauciones para desarrollo u operación:` generados por el transformer retirado.
6. No se agregó documentación funcional manual, ejemplos, referencias normativas, ciclos, horarios, causales ni explicaciones de ACH Colombia, CENIT o NACHA-M en el OpenAPI.

#### Validación de SC-002

- La implementación y el diff fueron revisados de forma estática.
- La prueba dirigida prevista es `OpenApiDocumentGenerationTests.GenerateAndSerialize_ShouldCompleteWithBearerAndWithoutHeuristicDocumentation`.
- No pudo ejecutarse porque el entorno no dispone de `dotnet` (`dotnet: command not found`). No se simulan resultados ni se afirma que la prueba haya pasado.
- La validación queda `PENDIENTE` exclusivamente por limitación ambiental; por política de continuidad, SC-002 queda en `VERIFY` y **no bloquea** SC-003.
- No se ejecutaron frontend, E2E, build completo ni suite completa.

#### Observación mantenida sin investigar

Permanece abierta la ambigüedad sobre el alcance efectivo de `TokenJwtMiddleware` en acciones sin `[Authorize]`. SC-002 no la investigó ni la resolvió y no depende de ella.

### Resultado de SC-003 — detector de documentación incompleta y placeholders

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente quality gate dentro del proyecto de tests. No se modificó la generación OpenAPI en runtime y no se revisaron ni documentaron controladores o endpoints.

#### Implementación realizada

1. Se creó `OpenApiDocumentationCompletenessDetector`, reutilizable desde tests y sin registro en la aplicación. Recibe una `OpenApiOperation` y reporta por separado si `Summary` o `Description` están incompletos.
2. El detector considera incompleto un texto inexistente, vacío o compuesto solo por espacios.
3. También reconoce de forma determinística los placeholders `Endpoint de la API ACH Interbank.`, `Consulta información del sistema.`, `DOCUMENTATION INCOMPLETE` y el patrón genérico `Operación del módulo X.`, normalizando únicamente mayúsculas/minúsculas y espacios. No incorpora heurísticas lingüísticas de propósito o negocio.
4. Se añadieron tests unitarios que distinguen explícitamente documentación ausente/vacía, placeholders y documentación específica aceptable. Estos tests no recorren todavía todas las operaciones de la API ni convierten el detector en un quality gate global; esa aplicación general permanece para `SC-008`.
5. No se agregaron `Summary`, `Description`, ejemplos ni contenido funcional a endpoints para satisfacer los tests.

#### Validación de SC-003

- La validación dirigida prevista es `OpenApiDocumentationCompletenessDetectorTests`.
- No pudo ejecutarse porque el entorno no dispone de `dotnet` (`dotnet: command not found`). No se simulan resultados ni se afirma que los tests hayan pasado.
- La implementación y el diff se revisaron de forma estática. La validación queda `PENDIENTE` exclusivamente por limitación ambiental; por política de continuidad, SC-003 queda en `VERIFY` y **no bloquea** SC-004.
- No se ejecutaron frontend, E2E, build completo ni suite completa.

### Resultado de SC-004 — estrategia estable de `operationId`

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente metadata técnica OpenAPI y su prueba dirigida. No se modificaron ni documentaron controladores o endpoints.

#### Convención adoptada

1. Se agregó `OpenApiOperationIdTransformer` como transformer de operación y se registró en `AddOpenApi` antes del transformer de seguridad. Su responsabilidad exclusiva es asegurar `operationId` cuando la operación no trae uno explícito no vacío.
2. Todo `operationId` explícito no vacío se conserva exactamente y no se reemplaza. La ruta nombrada `Users.GetById` queda como caso representativo protegido por `OpenApiDocumentGenerationTests`.
3. Para acciones MVC con `ControllerActionDescriptor`, el formato automático queda cerrado como `Controller_Action_HTTP_Route`, usando `ControllerName`, `ActionName`, método HTTP y ruta relativa real de `ApiDescription`.
4. La ruta se normaliza de forma puramente técnica: se elimina query string si existiera, se recortan `/` extremos, los parámetros como `{id}` o `{id:guid}` se reducen al nombre estable `id`, los separadores y caracteres no alfanuméricos se convierten en `_`, y los `_` resultantes se colapsan por la sustitución. Una ruta raíz usa el componente estable `Root`.
5. Cuando no existe `ControllerActionDescriptor`, el fallback usa únicamente `HTTP_Route`. Si el método HTTP no estuviera disponible, se usa el marcador técnico determinístico `UnknownMethod`; no se inventa un nombre funcional.
6. La ruta forma parte del identificador para distinguir casos reales donde `ControllerName` y `ActionName` no bastan. `TaskDefinitionsController` expone dos acciones `Get` por `GET` con rutas distintas; la convención produce `TaskDefinitions_Get_GET_TaskDefinitions` y `TaskDefinitions_Get_GET_TaskDefinitions_id`.
7. No se usan `Guid`, timestamps, contadores globales, orden de enumeración ni sufijos incrementales. No existe resolución de colisiones dependiente del orden de procesamiento.
8. La búsqueda dirigida de nombres de ruta explícitos encontró un único `operationId` explícito de controller (`Users.GetById`), por lo que no se identificó una colisión explícita en la revisión estática disponible. La unicidad total sobre el documento real queda cubierta por la prueba dirigida pendiente.

#### Prueba OpenAPI ajustada

`OpenApiDocumentGenerationTests` ahora recorre todas las operaciones HTTP publicadas en el documento generado y verifica que:

- cada operación tenga `operationId` no vacío;
- no existan `operationId` duplicados con comparación `StringComparer.Ordinal`;
- las dos acciones `Get` de `TaskDefinitionsController` reciban IDs distintos por la ruta;
- `Users.GetById` se conserve como `operationId` explícito;
- continúen vigentes las validaciones de esquema Bearer, seguridad por operación, referencias locales/resolubles y ausencia de documentación heurística de SC-002.

SC-003 no se convirtió en quality gate global de `Summary`/`Description`; esa activación sigue reservada para SC-008.

#### Validación de SC-004

- Se confirmó que el entorno sigue sin disponer de `dotnet` (`command -v dotnet` no devuelve ejecutable).
- La prueba dirigida afectada es `OpenApiDocumentGenerationTests`; no pudo ejecutarse y queda `PENDIENTE`. No se simulan resultados ni se afirma que haya pasado.
- Se revisó estáticamente el diff, el registro del transformer, la construcción determinística del identificador y las fuentes explícitas de nombres de ruta. No se identificaron colisiones según esta revisión disponible, pero la comprobación sobre el OpenAPI generado permanece pendiente.
- No se ejecutaron frontend, E2E, build completo ni suite completa.
- La limitación es exclusivamente ambiental: SC-004 queda en `VERIFY`, no `BLOCKED`, y SC-005 queda en `READY`.

### Resultado de SC-005 — taxonomía oficial de tags de integración

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente metadata técnica de agrupamiento OpenAPI y su prueba dirigida. No se modificaron controllers, contratos HTTP, seguridad, lógica de negocio, `Summary` ni `Description`.

#### Taxonomía oficial cerrada

La lista exacta de tags funcionales queda fijada en 16 valores:

1. `Autenticación y acceso`
2. `Usuarios y permisos`
3. `Cámaras y ciclos`
4. `Transacciones interbancarias`
5. `Devoluciones y respuestas`
6. `Trazabilidad y conciliación`
7. `Políticas y reglas transaccionales`
8. `NACHA-M`
9. `CENIT`
10. `ACH Colombia`
11. `Seguridad y certificados`
12. `Integraciones`
13. `Operación y programación`
14. `Reportes`
15. `Auditoría y observabilidad`
16. `Administración y catálogos`

#### Estrategia de asignación

1. Se agregó `OpenApiIntegrationTagsTransformer`, registrado después de `OpenApiOperationIdTransformer` y antes de `OpenApiSecurityMetadataTransformer`. Su única responsabilidad es sustituir el agrupamiento técnico automático por un tag funcional primario.
2. La fuente técnica de identidad es `ControllerActionDescriptor.ControllerName`. La clasificación se resuelve únicamente mediante un `Dictionary<string, string>` cerrado con comparación ordinal exacta. No existen reglas `Contains`, `StartsWith`, regex ni inferencias por ruta, acción, verbo HTTP o coincidencia parcial.
3. Cada controller público actualmente conocido tiene exactamente una asignación primaria. El transformer reemplaza los tags automáticos del controller por ese único tag funcional.
4. Si una futura operación basada en controller no aparece en la tabla, el transformer no inventa un fallback y elimina el tag técnico automático. El quality gate de `OpenApiDocumentGenerationTests` exige un tag oficial y hará visible esa falta de clasificación cuando pueda ejecutarse.
5. La revisión estática de metadata encontró 69 controllers con acciones HTTP no excluidos de ApiExplorer y todos quedaron asignados. `ServersController` y `TestsController` declaran `[ApiExplorerSettings(IgnoreApi = true)]` y no forman parte de la tabla. No quedaron controllers públicos con clasificación pendiente en SC-005.

#### Asignación explícita controller → tag

- **Autenticación y acceso:** `Auth`, `Jwks`, `Oauths`.
- **Usuarios y permisos:** `LoginLockoutSettings`, `PasswordRules`, `Permissions`, `Roles`, `Users`.
- **Cámaras y ciclos:** `AchCycles`, `BankHolidays`, `ClearingHouseCycleConfigs`, `ClearingHouseSpecialDates`, `ClearingHouses`, `InstitutionClearingHousePreferences`.
- **Transacciones interbancarias:** `BulkIngestion`, `Prenotifications`, `Transactions`.
- **Devoluciones y respuestas:** `AchClaims`, `AchOutboundReturnTransport`, `AchResponseOperations`, `AchResponses`, `AchReturnOfReturn`, `AchReturns`, `ReturnReasons`.
- **Trazabilidad y conciliación:** `AchReconciliation`, `AchTraceability`, `OutgoingTransactionMonitoring`.
- **Políticas y reglas transaccionales:** `CfaOperationalTransactionRestrictions`, `ClearingHouseTransactionPolicies`, `ClearingHouseTransactionRules`, `PaymentRailCapabilityRegistry`, `TransactionPrerequisitePolicy`.
- **NACHA-M:** `IncomingNachaCommandCenter`, `Nacha`, `NachaConfigProfiles`, `NachaConfigProfilesReadOnly`, `NachaExport`, `NachaInboundSimulator`, `NachaOperationalReadiness`, `NachaRecordDefinitions`, `NachaRecordLayouts`, `NachaSecurityOperations`, `NachaSoapUatConsole`, `NachaUpload`.
- **CENIT:** `CenitOperations`.
- **ACH Colombia:** `AchColombiaFileExchange`.
- **Seguridad y certificados:** `CertificateManagement`, `DigitalEnvelopeCertificates`, `SobreDigital`.
- **Integraciones:** `IntegrationCatalog`, `IntegrationMappingSets`, `SoapIntegrationSettings`, `UatContrapartidas`.
- **Operación y programación:** `Maintenance`, `Scheduler`, `TaskDefinitions`.
- **Reportes:** `Reports`.
- **Auditoría y observabilidad:** `AuditLogs`, `AuthLogs`, `NavigationLogs`.
- **Administración y catálogos:** `Branding`, `CatalogTypes`, `CompanyEntryDescriptions`, `CustomerThirdParties`, `Customers`, `FinancialInstitutions`, `MenuItems`, `Navigation`, `RegulatoryCatalogs`.

#### Separación ACH Colombia / CENIT / NACHA-M

- `AchColombiaFileExchangeController` se identifica de forma explícita como `ACH Colombia`. No se extrapola ese tag a operaciones transversales.
- `CenitOperationsController` se identifica de forma explícita como `CENIT`. No se usa CENIT como sinónimo de NACHA-M.
- Los controllers de formato/configuración/operación NACHA-M no exclusivos de una cámara se agrupan bajo `NACHA-M`; los controllers cuya navegación primaria es seguridad/certificados permanecen en `Seguridad y certificados`. No existe un tag combinado `ACH/CENIT/NACHA-M`.
- `TransactionsController` queda en `Transacciones interbancarias` y la prueba comprueba que no reciba por error `ACH Colombia` ni `CENIT`.

#### Prueba OpenAPI ajustada

`OpenApiDocumentGenerationTests` conserva las validaciones anteriores y añade comprobaciones de que:

- toda operación HTTP generada tenga tag;
- cada operación use exactamente un tag funcional primario en el estado actual;
- ningún tag esté vacío y todos pertenezcan a la taxonomía oficial;
- `GET /api/ach-colombia/file-exchange/transfers` use `ACH Colombia`;
- `GET /api/cenit/chamber-responses` use `CENIT`;
- `POST /Nacha/header` use `NACHA-M`;
- `POST /Transactions` use `Transacciones interbancarias` y no reciba tags de cámara;
- permanezcan las validaciones de `operationId`, unicidad, esquema Bearer, seguridad por operación, referencias locales/resolubles y ausencia de documentación heurística.

SC-003 no se activa todavía como control global de `Summary`/`Description`; esa aplicación sigue reservada para SC-008.

#### Validación de SC-005

- Se confirmó nuevamente que el entorno no dispone de `dotnet` (`command -v dotnet` no devuelve ejecutable).
- La prueba dirigida afectada es `OpenApiDocumentGenerationTests`; no pudo ejecutarse y queda `PENDIENTE`. No se simulan resultados ni se afirma que haya pasado.
- Se realizó revisión estática dirigida de la tabla de 69 controllers con acciones HTTP no excluidos de ApiExplorer, del registro del transformer y de los casos representativos ACH Colombia, CENIT, NACHA-M y transacción general. No se identificaron controllers públicos sin asignación en la revisión disponible.
- No se ejecutaron frontend, E2E, build completo ni suite completa.
- La limitación es exclusivamente ambiental: SC-005 queda en `VERIFY`, no `BLOCKED`, y SC-006 queda en `READY`.

### Resultado de SC-006 — headers transversales desde evidencia explícita del contrato HTTP

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente metadata OpenAPI determinística para headers ya consumidos por acciones públicas y su prueba dirigida. No se modificaron controllers, firmas, rutas, DTO, autenticación, autorización ni lógica de negocio.

#### Estrategia adoptada

1. Se agregó `OpenApiTransversalHeadersTransformer`, registrado después de `OpenApiIntegrationTagsTransformer` y antes de `OpenApiSecurityMetadataTransformer`. Su única responsabilidad es agregar metadata de headers comprobados para operaciones identificadas exactamente por `ControllerActionDescriptor.ControllerName` + `ActionName`.
2. La asignación es una tabla cerrada y auditable. No usa `Contains`, `StartsWith`, regex, verbo HTTP, ruta, tag ni pertenencia general a un controller como heurística.
3. Antes de agregar un header, el transformer conserva cualquier parámetro existente con el mismo nombre usando comparación `OrdinalIgnoreCase`; no reemplaza metadata preexistente y su aplicación es idempotente respecto a duplicados.
4. Los headers agregados usan `in: header`, `required: false`, schema `string` y una descripción breve. No se inventan formato, longitud, regex ni otras restricciones no declaradas por el contrato actual.
5. `Authorization` **no** se representa como `OpenApiParameter`. Permanece modelado exclusivamente por `components.securitySchemes.Bearer` y por el requisito de seguridad de cada operación derivado de `[Authorize]` / `[AllowAnonymous]` mediante los transformers existentes.

#### Operaciones con `Idempotency-Key`

La búsqueda dirigida del literal `Idempotency-Key` en controllers confirmó dos puntos de lectura y sus únicos consumidores públicos actuales:

- `AchColombiaFileExchangeController`: `ExecuteOutbound`, `ExecuteInbound`, `Retry`.
- `AchClaimsController`: `CreateDev14`, `RecordProgress`, `Resolve`, `Expire`.

`AchColombiaFileExchangeController.Reprocess` se mantiene como caso negativo: pertenece al mismo controller pero no llama al helper que lee el header, por lo que no recibe `Idempotency-Key` en OpenAPI.

#### Operaciones con `X-Correlation-ID`

La búsqueda dirigida del literal `X-Correlation-ID` confirmó tres puntos de lectura y los consumidores públicos actuales:

- `AchResponseOperationsController`: `CreateMapping`, `UpdateMapping`, `ActivateMapping`, `DeactivateMapping`, `CreateOrphan`, `BeginReview`, `ResolveOrphan`, `Reprocess`.
- `OutgoingTransactionMonitoringController`: `Search`, `GetDetail` (la auditoría interna etiqueta esta última como `Detail`).
- `SchedulerController`: `Pause`, `Resume`, `UpdateSchedule`.

`SchedulerController.Execute` es el caso negativo principal: usa directamente `HttpContext.TraceIdentifier` y no llama a `SetAuditScope`, por lo que no se documenta `X-Correlation-ID` por simple pertenencia al controller.

#### Prueba OpenAPI ajustada

`OpenApiDocumentGenerationTests` conserva las validaciones de unidades anteriores y añade comprobaciones de que:

- una operación protegida conserva requisito Bearer y una operación `[AllowAnonymous]` no lo tiene;
- ninguna operación publica un parámetro header llamado `Authorization`;
- `ExecuteOutbound` y `CreateDev14` exponen `Idempotency-Key`, mientras `AchColombiaFileExchange.Reprocess` no lo expone;
- `AchResponseOperations.CreateMapping`, `OutgoingTransactionMonitoring.Search` y `Scheduler.Pause` exponen `X-Correlation-ID`, mientras `Scheduler.Execute` no lo expone;
- los headers comprobados son `header`, opcionales, de schema `string`, con descripción no vacía;
- no existen parámetros header duplicados por nombre con comparación case-insensitive;
- siguen vigentes `operationId`, unicidad, tags oficiales, esquema Bearer, seguridad por operación, referencias resolubles y ausencia de documentación heurística.

SC-003 no se activa todavía como quality gate global de `Summary`/`Description`; esa aplicación continúa reservada para SC-008.

#### Validación de SC-006

- Se confirmó que el entorno sigue sin disponer de `dotnet` (`command -v dotnet` no devuelve ejecutable).
- La prueba dirigida afectada es `OpenApiDocumentGenerationTests`; no pudo ejecutarse y queda `PENDIENTE`. No se simulan resultados ni se afirma que haya pasado.
- Se realizó revisión estática dirigida de las lecturas literales de `Idempotency-Key` y `X-Correlation-ID`, de sus llamadas desde las acciones enumeradas, del registro del transformer y de los casos negativos. No se investigó `TokenJwtMiddleware`.
- No se ejecutaron frontend, E2E, build completo ni suite completa.
- La limitación es exclusivamente ambiental: SC-006 queda en `VERIFY`, no `BLOCKED`, y SC-007 queda en `READY`.

### Resultado de SC-007 — metadata global del documento OpenAPI/Scalar

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente metadata global del documento OpenAPI y su prueba dirigida. No se modificaron controllers, operaciones individuales, contratos HTTP, seguridad, lógica de negocio ni configuración visual de Scalar.

#### Metadata global adoptada

1. Se agregó `OpenApiDocumentMetadataTransformer` y se registró mediante `AddDocumentTransformer` en `AddOpenApi("v1")`, antes de `OpenApiBearerSecuritySchemeTransformer`. Su responsabilidad exclusiva es definir metadata global para el documento.
2. El título queda fijado en `ACHInterbank API` y la versión en `v1`.
3. La descripción global presenta ACHInterbank como el contrato HTTP implementado por la aplicación y aclara que OpenAPI, mostrado mediante Scalar, describe ese contrato sin convertir a Scalar en fuente normativa. La documentación funcional específica continúa perteneciendo a cada operación.
4. ACH Colombia y CENIT se presentan expresamente como cámaras independientes. La portada aclara que una operación puede pertenecer a una cámara, ser transversal a ambas o corresponder a lógica interna de ACHInterbank, y que las reglas de una cámara no se extrapolan automáticamente a la otra. No se incorporan ciclos, horarios, sesiones, causales, límites ni procedimientos operativos.
5. `NACHA-M` se presenta como un concepto que debe interpretarse con contexto: ACH Colombia y CENIT poseen lineamientos propios y las operaciones específicas indicarán el contexto aplicable cuando corresponda. No se define globalmente como exclusivo de ninguna de las dos cámaras.
6. La seguridad global se explica exclusivamente mediante el esquema `Bearer JWT` y la metadata de autorización publicada por operación. `Authorization` continúa representado por el modelo de seguridad OpenAPI y no como parámetro header. La ambigüedad pendiente de `TokenJwtMiddleware` no se investigó ni se usó para ampliar afirmaciones de seguridad.
7. La portada indica que la navegación usa tags funcionales y que las operaciones publican un `operationId` estable y único. No expone la fórmula interna definida en SC-004.
8. `Idempotency-Key` y `X-Correlation-ID` se explican como headers que aparecen únicamente en las operaciones que los consumen y se aclara que no son obligatorios globalmente. La tabla de asignación de SC-006 no se modificó.
9. No se agregaron `Contact`, `License`, `TermsOfService`, `ExternalDocs` ni `Servers`, ni se modificó la configuración visual de Scalar. `Agent`, `MCP` y las herramientas de desarrollo permanecen en su configuración previa.

#### Prueba OpenAPI ajustada

`OpenApiDocumentGenerationTests` conserva todas las verificaciones de unidades anteriores y añade comprobaciones de que:

- `info.title` sea exactamente `ACHInterbank API`;
- `info.version` sea exactamente `v1`;
- `info.description` exista, no esté vacía y no contenga los placeholders documentales definidos previamente;
- la descripción contenga lenguaje español estable del contrato y mencione de forma diferenciada `ACH Colombia`, `CENIT` y `NACHA-M`;
- la descripción declare que ACH Colombia y CENIT son cámaras independientes y que las reglas de una no se aplican automáticamente a la otra;
- la descripción explique `Bearer JWT`, tags funcionales y `operationId`;
- `Idempotency-Key` y `X-Correlation-ID` aparezcan como convenciones selectivas y no como headers obligatorios globales;
- siga sin existir un parámetro OpenAPI `Authorization`, además de mantenerse las verificaciones previas de `operationId`, unicidad, tags, Bearer, seguridad por operación, headers transversales, referencias resolubles y ausencia de documentación heurística.

SC-003 no se activa todavía como quality gate global de `Summary`/`Description`; esa aplicación permanece reservada para SC-008.

#### Validación de SC-007

- Se confirmó que el entorno sigue sin disponer de `dotnet` (`command -v dotnet` no devuelve ejecutable).
- La prueba dirigida afectada es `OpenApiDocumentGenerationTests`; no pudo ejecutarse y queda `PENDIENTE`. No se simulan resultados ni se afirma que haya pasado.
- Se realizó revisión estática del transformer, su registro, las nuevas aserciones y el diff completo de SC-007. No se revisaron controllers ni se consultó documentación normativa.
- No se ejecutaron frontend, E2E, build completo ni suite completa.
- La limitación es exclusivamente ambiental: SC-007 queda en `VERIFY`, no `BLOCKED`, y SC-008 queda en `READY`.

### Resultado de SC-008 — quality gate mínimo QG-1

**Estado:** `VERIFY`  
**Alcance ejecutado:** exclusivamente tests de calidad documental. No se modificó runtime, controllers, transformers, contratos HTTP ni documentación funcional existente.

#### Implementación de QG-1

1. `OpenApiDocumentGenerationTests` incorpora un allowlist explícito por `METHOD + PATH`. La pertenencia a QG-1 no se infiere por controller, tag, ruta parcial, `operationId` ni por la mera presencia de `Summary`/`Description`.
2. Para cada operación del allowlist se reutiliza `OpenApiDocumentationCompletenessDetector` de SC-003. QG-1 exige `Summary` y `Description` sin ausencia, vacío ni placeholders reconocidos por ese detector; también exige `operationId` no vacío y exactamente un tag perteneciente a `OfficialIntegrationTags`.
3. El helper reutilizable `AssertQG1`/`AssertQG1Operation` identifica el método y path en sus mensajes de fallo, de modo que las unidades funcionales posteriores solo necesitan agregar deliberadamente una entrada al allowlist al cerrar documentalmente una operación.
4. Se agregó una prueba negativa dirigida que demuestra que una operación con documentación específica pasa, mientras un `Summary` placeholder y una `Description` vacía hacen fallar QG-1. No se duplicó la lista de placeholders.
5. `Summary` y `Description` **no** se validan globalmente para todas las operaciones públicas. Las validaciones globales previas de `operationId`, unicidad, tags, seguridad, headers y referencias se conservan sin cambios.
6. La inclusión inicial en QG-1 protege integridad documental básica; no constituye aprobación semántica o normativa definitiva del contenido funcional existente.

#### Allowlist inicial QG-1

```text
GET /incoming-nacha-command-center/ingestions/{ingestionId}/validations
GET /incoming-nacha-command-center/ingestions/{ingestionId}/batches
GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions
GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas
```

Las cuatro operaciones ya publicaban simultáneamente `[EndpointSummary]` y `[EndpointDescription]` en `IncomingNachaCommandCenterController`. SC-008 no modificó esos atributos ni evaluó su completitud normativa profunda.

#### Criterio para incorporar operaciones futuras

Una operación entra a QG-1 únicamente por decisión explícita de una unidad de documentación que la deja terminada para el nivel aplicable. La unidad debe agregar su `METHOD + PATH` exacto al allowlist y conservar las mismas aserciones reutilizables; no se permiten mecanismos automáticos de descubrimiento de pertenencia.

#### Validación de SC-008

- Se confirmó que el entorno no dispone de `dotnet` (`dotnet: command not found`).
- Las pruebas dirigidas previstas son `OpenApiDocumentationCompletenessDetectorTests` y `OpenApiDocumentGenerationTests`; ambas quedan `PENDIENTES`. No se simulan resultados ni se afirma que hayan pasado.
- Se realizó revisión estática del allowlist, el helper QG-1, la prueba negativa y el diff. No se modificaron controllers ni transformers de runtime, no se agregó contenido normativo y no se investigó `TokenJwtMiddleware`.
- No se ejecutaron frontend, E2E, build completo ni suite completa.
- La limitación es exclusivamente ambiental: SC-008 queda en `VERIFY`, no `BLOCKED`, y SC-090 queda en `READY` como próxima unidad.

### Resultado de SC-090 — checkpoint de Fase A

**Estado:** `VERIFY`  
**Tipo de checkpoint:** revisión estática completada; validación ejecutable pendiente por ausencia de `dotnet`.  
**Implementación de Fase A:** `COMPLETADA`.  
**Bloqueo para Fase B:** `NO`.

#### Resultado de la auditoría acumulada

1. **SC-002 — retiro de heurísticas:** `ScalarOperationDocumentationTransformer.cs` no existe y no hay referencias activas a esa clase en `src` o `tests`. Los transformers OpenAPI de runtime no escriben `operation.Summary` ni `operation.Description`; no se encontró traslado de la generación automática de propósito, riesgo, recomendaciones o semántica ACH/CENIT/NACHA-M a otro componente.
2. **SC-003 — detector documental:** `OpenApiDocumentationCompletenessDetector` permanece únicamente en `tests/Cfa.ACHInterbank.Tests`. Conserva detección de ausencia, vacío, espacios y los placeholders aprobados, sin modificar el OpenAPI en runtime.
3. **SC-004 — `operationId`:** `OpenApiOperationIdTransformer` continúa registrado. Conserva IDs explícitos no vacíos y mantiene la convención cerrada `Controller_Action_HTTP_RouteNormalizada`, con fallback técnico `HTTP_RouteNormalizada` cuando no existe `ControllerActionDescriptor`. No se modificó la estrategia.
4. **SC-005 — tags:** `OpenApiIntegrationTagsTransformer` continúa registrado con la taxonomía cerrada de 16 tags y una tabla explícita controller → tag. La revisión dirigida no encontró `Contains`, `StartsWith`, regex funcionales, análisis de ruta, método HTTP ni `ActionName` usados para inferir clasificación.
5. **SC-006 — headers:** `OpenApiTransversalHeadersTransformer` continúa registrado. La tabla mantiene 7 acciones explícitas con `Idempotency-Key` y 13 con `X-Correlation-ID`; `Authorization` no se agrega como parámetro. El transformer conserva parámetros preexistentes y evita duplicados mediante comparación case-insensitive.
6. **SC-007 — metadata global:** `OpenApiDocumentMetadataTransformer` continúa registrado con `Title = ACHInterbank API` y `Version = v1`. La descripción mantiene separados ACH Colombia y CENIT, trata NACHA-M con contexto, explica Bearer JWT y declara `Idempotency-Key` / `X-Correlation-ID` como selectivos. No agrega `Contact`, `License`, `TermsOfService`, `ExternalDocs` ni `Servers`.
7. **Seguridad:** `OpenApiSecurityMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer` continúan registrados. SC-090 no investigó `TokenJwtMiddleware` ni cambió autenticación, autorización o seguridad OpenAPI.
8. **SC-008 — QG-1:** el quality gate continúa reutilizando `OpenApiDocumentationCompletenessDetector` y el allowlist explícito `METHOD + PATH`. No existe descubrimiento automático por controller, tag, ruta parcial u `operationId`, y `Summary`/`Description` no son requisitos globales.

#### Baseline QG-1 confirmado

```text
GET /incoming-nacha-command-center/ingestions/{ingestionId}/validations
GET /incoming-nacha-command-center/ingestions/{ingestionId}/batches
GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions
GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas
```

El baseline coincide con SC-008 y no cambió sin una unidad que lo justificara.

#### Registro `AddOpenApi("v1")` confirmado

Se conserva el registro acumulado y el orden existente, sin cambios en SC-090:

```text
OpenApiOperationIdTransformer
OpenApiIntegrationTagsTransformer
OpenApiTransversalHeadersTransformer
OpenApiSecurityMetadataTransformer
OpenApiDocumentMetadataTransformer
OpenApiBearerSecuritySchemeTransformer
```

#### Pruebas acumuladas revisadas estáticamente

`OpenApiDocumentGenerationTests` conserva verificaciones de estructura OpenAPI, metadata global, `operationId` presente y único, conservación de ID explícito, tags oficiales y casos ACH Colombia/CENIT/NACHA-M, Bearer, seguridad por operación, ausencia de `Authorization` como parámetro, `Idempotency-Key`, `X-Correlation-ID`, casos negativos de headers, referencias locales resolubles, ausencia de documentación heurística retirada y QG-1 para el allowlist.

`OpenApiDocumentationCompletenessDetectorTests` conserva cobertura de documentación ausente/vacía, placeholders con normalización de mayúsculas/minúsculas y espacios, y documentación específica válida.

#### Validación ejecutable

Se comprobó una sola vez la disponibilidad de `dotnet` mediante `command -v dotnet`; no se encontró ejecutable.

- **Build:** `PENDIENTE — dotnet no disponible`.
- **Tests dirigidos (`OpenApiDocumentationCompletenessDetectorTests`, `OpenApiDocumentGenerationTests`):** `PENDIENTES — dotnet no disponible`.
- **Suite completa backend/solución:** `PENDIENTE — dotnet no disponible`.
- No se intentó instalar `dotnet`, no se simularon compilaciones o tests y no se ejecutaron frontend ni E2E.

#### Regresiones y continuidad

No se detectaron regresiones técnicas en la revisión estática dirigida de la infraestructura documental de Fase A. No fue necesaria ninguna corrección de código en SC-090. La ausencia de `dotnet` es una limitación de validación y no un defecto técnico; por lo tanto, SC-090 queda en `VERIFY`, SC-101 queda en `READY` y Fase B puede comenzar.

## Fase B — Contratos, DTO y experiencia de onboarding

| ID | Estado | Alcance | Límite recomendado | Criterio de salida |
|---|---|---|---|---|
| `SC-101` | `VERIFY` | Revisar generación de XML docs en proyectos con DTO/contratos públicos | 5 archivos | Implementación completada; tests dirigidos pendientes por ausencia de `dotnet` |
| `SC-102` | `VERIFY` | Definir patrón para ejemplos OpenAPI request/response | 4 archivos | Mecanismo reutilizable implementado; tests dirigidos pendientes por ausencia de `dotnet` |
| `SC-103` | `VERIFY` | Definir patrón para responses y `ProblemDetails` sin cambiar contratos | 4 archivos | Patrón implementado; tests dirigidos pendientes por ausencia de `dotnet` |
| `SC-104` | `VERIFY` | AuthController + DTO asociados | 2 controllers / 15 ops máx. | Implementación completada; OpenApiDocumentGenerationTests pendiente por ausencia de `dotnet` |
| `SC-105` | `VERIFY` | ClearingHouses + AchCycles (primer lote) | 2 controllers / 15 ops máx. | Implementación completada; OpenApiDocumentGenerationTests pendiente por ausencia de `dotnet` |
| `SC-106` | `VERIFY` | Catálogos necesarios para primer request | 2 controllers / 15 ops máx. | Implementación completada; OpenApiDocumentGenerationTests pendiente por ausencia de `dotnet` |
| `SC-107` | `VERIFY` | Crear sección Quick Start real en descripción global Scalar | 4 archivos | Implementación completada; OpenApiDocumentGenerationTests pendiente por ausencia de `dotnet` |
| `SC-190` | `VERIFY` | Checkpoint Fase B | — | Checkpoint estático completado; validación ejecutable pendiente por ausencia de `dotnet`; no bloquea Fase C |

### Resultado de SC-101 — generación XML en proyectos con contratos públicos

**Estado:** `VERIFY`  
**Alcance ejecutado:** infraestructura de documentación de schemas mediante configuración de proyectos y prueba de portabilidad. No se modificaron controllers, DTO, contratos HTTP ni runtime OpenAPI.

#### Proyectos revisados y fuentes confirmadas

La revisión dirigida tomó como muestra `AuthController`, `ClearingHousesController` y `AchCyclesController` y confirmó:

- `Cfa.ACHInterbank.Api` ya genera documentación XML y conserva su `DocumentationFile` relativo. Este proyecto contiene los controllers y su metadata documental de operación; no requirió cambios.
- `Cfa.ACHInterbank.Application` define contratos públicos usados directamente por `AuthController`, entre ellos `LoginRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest` y `AuthResult`.
- `Cfa.ACHInterbank.Domain` define contratos públicos usados directamente por `ClearingHousesController` y `AchCyclesController`, entre ellos `ClearingHouseAdminQuery`, `CreateClearingHouseRequest`, `UpdateClearingHouseRequest`, `ChangeClearingHouseStatusRequest` y `AchCycleRequest`.
- En esta muestra no apareció evidencia de otro proyecto que deba modificarse para cumplir SC-101.

#### Configuración aplicada

1. `Cfa.ACHInterbank.Application.csproj` y `Cfa.ACHInterbank.Domain.csproj` habilitan ahora `<GenerateDocumentationFile>True</GenerateDocumentationFile>` sin definir una ruta manual para `DocumentationFile`, por lo que el SDK conserva la salida estándar.
2. Ambos proyectos agregan `<NoWarn>$(NoWarn);CS1591</NoWarn>` de forma acumulativa. Esto evita convertir la migración incremental de comentarios XML en una obligación inmediata de documentar todos los miembros públicos y no reemplaza warnings heredados.
3. `Cfa.ACHInterbank.Api.csproj` ya tenía generación XML habilitada y una ruta relativa bajo `bin\$(Configuration)\$(TargetFramework)`; se revisó y no se modificó.
4. Se mantiene el mecanismo nativo de .NET 10 / `Microsoft.AspNetCore.OpenApi`. No se creó ningún schema transformer, no se leen XML por reflection y no se cargan archivos XML manualmente en runtime.

#### Prueba de configuración

`ProjectConfigurationPortabilityTests` incorpora una prueba reutilizable parametrizada para `Cfa.ACHInterbank.Api`, `Cfa.ACHInterbank.Application` y `Cfa.ACHInterbank.Domain`. La prueba verifica que:

- exista `GenerateDocumentationFile` y su valor sea verdadero;
- si aparece `DocumentationFile`, no sea una ruta absoluta de Windows, incluyendo rutas con unidad o UNC;
- no se obliga a los proyectos de biblioteca a declarar explícitamente `DocumentationFile`.

#### Validación de SC-101

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `ProjectConfigurationPortabilityTests`: `PENDIENTE — dotnet no disponible`.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática de los tres `.csproj`, de los contratos usados por los controllers de muestra y del test modificado. No se intentó instalar `dotnet`, no se simuló ningún resultado y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-101 queda en `VERIFY`, no `BLOCKED`, y SC-102 queda en `READY`.

### Resultado de SC-102 — patrón reutilizable para ejemplos OpenAPI request/response

**Estado:** `VERIFY`  
**Alcance ejecutado:** infraestructura explícita de ejemplos OpenAPI y pruebas dirigidas del mecanismo. No se modificaron controllers productivos, DTO, contratos HTTP ni quality gates.

#### Patrón adoptado

1. Se creó `IOpenApiExampleProvider` con `GetExample()`, cuya única responsabilidad es devolver el objeto ficticio y determinístico que se serializará como ejemplo. Los providers del patrón no dependen de base de datos, servicios externos, reloj, aleatoriedad, configuración sensible ni secretos.
2. `OpenApiRequestExampleAttribute` asocia deliberadamente un provider a una acción y permite indicar `ContentType`; el valor por defecto es `application/json`. Solo se admite una metadata de request por método.
3. `OpenApiResponseExampleAttribute` declara explícitamente `StatusCode`, provider y `ContentType` (por defecto `application/json`) y permite múltiples instancias para responses diferentes. El status code nunca se deduce ni se crea desde el transformer.
4. Se agregó `OpenApiExamplesTransformer` como `IOpenApiOperationTransformer`, registrado después de `OpenApiTransversalHeadersTransformer` y antes de `OpenApiSecurityMetadataTransformer`. Lee únicamente atributos explícitos desde `ControllerActionDescriptor.MethodInfo`; no infiere ejemplos desde controller, acción, ruta, verbo, tag ni nombres de propiedades.
5. La serialización utiliza `Microsoft.AspNetCore.Mvc.JsonOptions` resuelto desde DI, es decir, las opciones JSON reales usadas por los controllers de la API. El objeto entregado por el provider se convierte con `System.Text.Json` a `JsonNode`, que es el tipo de ejemplo esperado por `Microsoft.OpenApi`.
6. El transformer solo asigna `Example` cuando ya existen el `requestBody`/response, el status code y el media type indicados. No crea contratos, responses, content types ni schemas faltantes.
7. Si un media type ya contiene `Example` o una colección `Examples`, se conserva sin sobrescritura. La aplicación del mecanismo es idempotente respecto al ejemplo existente.
8. Un provider que no implementa `IOpenApiExampleProvider`, que no puede construirse de forma determinística mediante constructor público sin parámetros o que devuelve `null` provoca un `InvalidOperationException` explícito; no se realiza cast silencioso ni se fabrica un fallback.
9. Los ejemplos usados para verificar el patrón son exclusivamente ficticios (`usuario.demo`, `persona@example.test`, `11111111-1111-1111-1111-111111111111`) y viven en el proyecto de tests. No se aplicó metadata de ejemplo a `AuthController`, `ClearingHousesController`, `AchCyclesController` ni a ningún otro endpoint productivo.

#### Pruebas dirigidas

`OpenApiExamplesTransformerTests` usa un controller exclusivamente de tests y comprueba que:

- metadata explícita de request genera `Example` con nombres `camelCase` según la configuración JSON real y no modifica el objeto entregado por el provider;
- metadata explícita de response se aplica al status code y `ContentType` declarados;
- no se crean `requestBody`, responses ni media types inexistentes;
- un `Example` preexistente se conserva y no es reemplazado;
- un provider incompatible produce un error claro que identifica `IOpenApiExampleProvider`;
- todos los datos de las pruebas son ficticios y determinísticos.

`OpenApiDocumentGenerationTests` no se modificó en SC-102; se conserva como prueba de regresión acumulada del documento productivo. QG-1 tampoco se modificó y los ejemplos no son obligatorios globalmente; esa exigencia corresponde al nivel QG-3 en las unidades posteriores.

#### Validación de SC-102

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiExamplesTransformerTests`: `PENDIENTE — dotnet no disponible`.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática del registro, de la metadata, del transformer y de las pruebas. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-102 queda en `VERIFY`, no `BLOCKED`, y SC-103 queda en `READY`.

### Resultado de SC-103 — patrón para responses y `ProblemDetails` sin cambiar contratos

**Estado:** `VERIFY`  
**Alcance ejecutado:** infraestructura explícita para describir responses OpenAPI ya existentes y pruebas dirigidas del mecanismo. No se modificaron controllers productivos, DTO, contratos HTTP, `BaseResponseModel`, `ResponseApiService`, quality gates ni contenido normativo.

#### Patrón oficial de responses

1. Los status codes, schemas y content types permanecen bajo metadata contractual estándar de ASP.NET Core, principalmente `ProducesResponseType` y metadata equivalente consumida por `Microsoft.AspNetCore.OpenApi`. Los transformers no fabrican esas partes del contrato.
2. Para una response con cuerpo tipado, las unidades funcionales posteriores deberán declarar el tipo real mediante metadata estándar. Para una response sin cuerpo, por ejemplo `204 No Content`, se declara únicamente el status correspondiente y no se inventa schema.
3. `ProblemDetails` se documentará únicamente cuando el código de la operación realmente produzca ese contrato. No se crea un modelo alternativo ni se asume que todos los `4xx`/`5xx` comparten el mismo envelope.
4. La revisión dirigida confirmó que `ResponseApiService.Response(...)` devuelve `BaseResponseModel`. Por tanto, cuando una operación use ese servicio como contrato real, OpenAPI deberá conservar `BaseResponseModel` y sus propiedades actuales (`StatusCode`, `Sucess`, `Message`, `Data`) en lugar de convertirlo a `ProblemDetails`. SC-103 no modifica ese modelo ni normaliza sus nombres.
5. Los contratos heterogéneos existentes —incluidas respuestas vacías, strings, objetos anónimos o tipos concretos— deberán documentarse según el comportamiento real o registrarse como limitación en su unidad funcional; SC-103 no los transforma.
6. No se infieren automáticamente `401`, `403` desde `[Authorize]` ni `500` desde middleware global. El requisito Bearer sigue siendo metadata de seguridad, no una afirmación sobre el body de error. `TokenJwtMiddleware` permanece sin investigar.

#### Metadata explícita para descripciones

1. Se creó `OpenApiResponseDescriptionAttribute`, repetible por método, con `StatusCode` y `Description` explícitos. El atributo solo describe; no crea status codes, schemas ni content types.
2. Se creó y registró `OpenApiResponseDescriptionsTransformer` después de `OpenApiExamplesTransformer` y antes de `OpenApiSecurityMetadataTransformer`. Lee exclusivamente metadata del `MethodInfo` asociado al `ControllerActionDescriptor`.
3. El transformer exige que el status declarado exista previamente en `operation.Responses`. Si no existe, lanza `InvalidOperationException` identificando `Controller.Action`, status code y que la metadata referencia una response inexistente. No falla silenciosamente ni agrega una response.
4. La regla de preservación es determinística: la descripción explícita puede reemplazar únicamente `null`, texto en blanco o la frase HTTP estándar exacta del status (`ReasonPhrases.GetReasonPhrase`). Cualquier descripción no vacía distinta de esa frase estándar se considera metadata previa deliberada y se conserva. No se aplican heurísticas lingüísticas.
5. El transformer no lee ni modifica `Content`, schemas, ejemplos ni media types. `OpenApiExamplesTransformer` de SC-102 permanece independiente y continúa limitado a examples sobre contratos existentes.

#### Pruebas dirigidas

`OpenApiResponseDescriptionsTransformerTests` usa un controller exclusivo del proyecto de tests y cubre que:

- una response `400` existente recibe la descripción explícita y conserva su schema `ProblemDetails`;
- una acción puede describir independientemente `200`, `400` y `404`;
- una response `204` mantiene ausencia de content types mientras recibe descripción;
- metadata para `409` cuando ese status no existe provoca un error claro y determinístico;
- el transformer no crea status codes adicionales;
- una descripción específica preexistente no se sobrescribe;
- una operación sin `OpenApiResponseDescriptionAttribute` permanece intacta.

QG-1 no se modificó y QG-2 no se activa globalmente en SC-103.

#### Validación de SC-103

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiResponseDescriptionsTransformerTests`: `PENDIENTE — dotnet no disponible`.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática de la metadata, el transformer, su registro, las pruebas y la separación de responsabilidades respecto de SC-102. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-103 queda en `VERIFY`, no `BLOCKED`, y SC-104 queda en `READY`.

### Resultado de SC-104 — `AuthController` + DTO asociados

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de las cuatro operaciones actuales de autenticación, documentación XML de sus contratos inmediatos, ejemplos ficticios y activación incremental de QG-2 únicamente para Auth. No se modificó lógica de negocio, seguridad, rutas, status codes ni contratos HTTP.

#### Operaciones documentadas

| Operación | Summary | Seguridad | Request | Responses documentadas |
|---|---|---|---|---|
| `POST /Auth/login` | `Iniciar sesión` | `AllowAnonymous` | `LoginRequest` | `200`, `401`, `500` con `BaseResponseModel` |
| `POST /Auth/forgot-password` | `Solicitar recuperación de contraseña` | `AllowAnonymous` | `ForgotPasswordRequest` | `200`, `400` con `BaseResponseModel` |
| `POST /Auth/reset-password` | `Restablecer contraseña` | `AllowAnonymous` | `ResetPasswordRequest` | `200`, `400` con `BaseResponseModel` |
| `POST /Auth/refresh` | `Renovar sesión autenticada` | Bearer por `[Authorize]` | Sin request body; identidad desde claims `sub` o `uid` del JWT actual | `200`, `401` con `BaseResponseModel` |

Las `EndpointDescription` explican el comportamiento observado: login autentica `Username` + `Password` y registra el intento; forgot-password responde de forma deliberadamente genérica cuando el correo tiene valor para no revelar existencia de cuenta; reset-password valida token/contraseña/confirmación y consume el token al completar el cambio; refresh no recibe un refresh token separado y renueva la sesión desde la identidad del Bearer actual.

#### Contrato de responses y limitación de `Data`

1. Las cuatro operaciones usan el contrato real `BaseResponseModel`; SC-104 no crea `ProblemDetails`, wrappers genéricos ni modelos alternativos.
2. Se conserva exactamente el nombre contractual `Sucess`, junto con `StatusCode`, `Message` y `Data`.
3. `BaseResponseModel.Data` continúa definido como `dynamic`. En runtime, los `200` de login y refresh colocan un `AuthResult` en `Data`; por esa definición dinámica, el schema OpenAPI del envelope puede no expresar estructuralmente el tipo anidado `AuthResult`.
4. Esta limitación no bloquea SC-104: los ejemplos explícitos muestran la forma concreta de `Data`. Tipar `BaseResponseModel.Data` o introducir `BaseResponseModel<T>` sería un cambio contractual separado y queda fuera de esta iniciativa documental.
5. Los `200` de forgot-password y reset-password usan `Result<string>.Success(...)`, por lo que `Data` es el mensaje string producido por el flujo real.

#### DTO y ejemplos

Se agregaron comentarios XML específicos, sin restricciones inventadas, a:

- `LoginRequest.Username` y `LoginRequest.Password`;
- `ForgotPasswordRequest.Email`;
- `ResetPasswordRequest.Token`, `NewPassword` y `ConfirmPassword`;
- `AuthResult.Success`, `Message`, `Token`, `ExpiresAt`, `Username`, `FullName`, `Roles` y `Permissions`.

`AuthOpenApiExamples.cs` incorpora providers determinísticos mediante el patrón de SC-102 para:

- request de login;
- response `200` de autenticación, reutilizado por login y refresh;
- request y response `200` de forgot-password;
- request y response `200` de reset-password.

Los datos son exclusivamente ficticios (`usuario.demo`, `persona@example.test`, `token-recuperacion-ficticio`, `jwt-ficticio-no-utilizable`) y la expiración del ejemplo es fija. No se usa reloj, aleatoriedad, tokens reales ni datos personales reales.

#### Quality gates incrementales

1. QG-1 conserva sus cuatro operaciones previas y agrega deliberadamente, por `METHOD + PATH`, `POST /Auth/login`, `POST /Auth/forgot-password`, `POST /Auth/reset-password` y `POST /Auth/refresh`. Para ellas continúa exigiendo Summary, Description, `operationId` y tag oficial `Autenticación y acceso`.
2. QG-2 se activa por primera vez de forma incremental únicamente para esas cuatro operaciones mediante un allowlist explícito `METHOD + PATH`; no descubre operaciones automáticamente.
3. QG-2 valida seguridad real, request body y schema esperado, descripciones XML de propiedades de request, response principal `200`, los status codes conocidos de cada acción, descripciones explícitas de responses y ausencia accidental de `Authorization`, `Idempotency-Key` y `X-Correlation-ID` como parámetros de Auth.
4. También se comprueba que login/forgot/reset publican sus request bodies, refresh no publica request body, y los responses documentados conservan `BaseResponseModel`.
5. Los ejemplos se validan de forma dirigida para login request/response, forgot-password request, reset-password request y refresh response. QG-3 no se globaliza.

#### Validación de SC-104

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- `OpenApiExamplesTransformerTests` no se modificó en SC-104 y no fue ejecutado, porque no apareció una necesidad técnica adicional que justificara ampliar la validación solicitada.
- Se realizó revisión estática dirigida de `AuthController`, `IAuthService`, `AuthService`, `ResponseApiService`, `BaseResponseModel`, los cuatro DTO de autenticación, providers de ejemplos y test OpenAPI acumulado. No se intentó instalar `dotnet` ni se simularon resultados.
- La limitación es exclusivamente ambiental: SC-104 queda en `VERIFY`, no `BLOCKED`, y SC-105 queda en `READY`.

### Resultado de SC-105 — `ClearingHousesController` + `AchCyclesController` (primer lote)

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de 15 acciones genéricas de cámaras y ciclos, documentación XML de DTO y parámetros inmediatos, responses tipados según el contrato actual y ampliación incremental de QG-1/QG-2. No se modificaron servicios, rutas, aliases `/api`, policies, lógica de readiness, resolución de configuraciones ni contratos HTTP.

#### Operaciones documentadas

| Operación | Summary | Response principal | Otros status documentados |
|---|---|---|---|
| `GET /clearing-houses` | `Listar cámaras compensadoras` | `200 PaginatedResult<ClearingHouseDto>` | — |
| `GET /clearing-houses/operational` | `Listar cámaras operativas` | `200` colección `ClearingHouseDto` | — |
| `GET /clearing-houses/{id}` | `Consultar cámara compensadora` | `200 ClearingHouseDto` | `404 ProblemDetails` |
| `GET /clearing-houses/{id}/readiness` | `Consultar readiness de cámara` | `200 ClearingHouseReadinessDto` | `404 ProblemDetails` |
| `POST /clearing-houses` | `Crear cámara compensadora` | `201 ClearingHouseDto` | `400/409 ProblemDetails` |
| `PUT /clearing-houses/{id}` | `Actualizar cámara compensadora` | `200 ClearingHouseDto` | `400/404/409 ProblemDetails` |
| `PATCH /clearing-houses/{id}/status` | `Cambiar estado de cámara compensadora` | `200 ClearingHouseDto` | `400/404 ProblemDetails` |
| `GET /clearing-houses/{id}/cycles` | `Listar ciclos de una cámara` | `200` colección `AchCycleDto` | — |
| `GET /ach-cycles` | `Listar ciclos ACH` | `200` colección `AchCycleDto` | — |
| `GET /ach-cycles/exportable` | `Listar ciclos exportables` | `200` colección `AchCycleExportDto` | — |
| `GET /ach-cycles/{id}` | `Consultar ciclo ACH` | `200 AchCycleDto` | `404` sin body tipado |
| `POST /ach-cycles` | `Crear ciclo ACH` | `201 AchCycleDto` | `400 ProblemDetails` |
| `PUT /ach-cycles/{id}` | `Actualizar ciclo ACH` | `200 AchCycleDto` | `400/404 ProblemDetails` |
| `POST /ach-cycles/repair-configuration-links` | `Reparar vínculos de configuración de ciclos` | `200 AchCycleConfigurationLinkRepairResult` | `409 ProblemDetails` |
| `DELETE /ach-cycles/{id}` | `Eliminar ciclo ACH` | `204` sin cuerpo | — |

Los endpoints `GET /clearing-houses/payment-rail-options` y `GET /clearing-houses/nacha-profiles` no se documentaron en esta unidad y quedan diferidos expresamente a SC-106.

#### Comportamiento funcional reflejado

- El listado administrativo de cámaras conserva paginación y filtros por búsqueda/estado; `operational` se limita a cámaras activas cuyo readiness está cumplido.
- Readiness expone `IsReady` y `MissingRequirements`; las cámaras nuevas se crean inactivas y la activación vuelve a exigir readiness.
- `ExpectedUpdatedAt` se documenta como control opcional de concurrencia de la actualización de cámara.
- La consulta de ciclos de una cámara admite `processingDate` opcional y la usa como rango exacto de `ProcessingDate`.
- `GET /ach-cycles` documenta el filtro por `clearingHouseId`, el rango por `ProcessingDate`, el uso de `processingDate` como respaldo de límites faltantes y la normalización de rangos invertidos.
- `GET /ach-cycles/exportable` aclara que `startDate`/`endDate` se aplican a `EffectiveEntryDate` de transacciones elegibles, no a `ProcessingDate` del ciclo.
- Create/update de ciclos reflejan la resolución de configuración canónica y que nombre/ventana finales provienen de esa configuración.
- Repair documenta el vínculo de ciclos históricos sin configuración y el `409 ProblemDetails` cuando existen asociaciones ambiguas; delete documenta únicamente `204` sin body.

#### DTO y parámetros

Se agregaron comentarios XML a los contratos solicitados: `ClearingHouseDto`, `ClearingHouseAdminQuery`, `CreateClearingHouseRequest`, `UpdateClearingHouseRequest`, `ChangeClearingHouseStatusRequest`, `ClearingHouseReadinessDto`, `PaginationRequest`, `AchCycleDto`, `AchCycleRequest`, `AchCycleExportDto`, `AchCycleConfigurationLinkRepairItem` y `AchCycleConfigurationLinkRepairResult`. También se documentaron por XML los parámetros path/query de las 15 acciones. No se agregaron `DataAnnotations` ni se alteró la estructura de los DTO.

#### Quality gates incrementales

1. QG-1 conserva el baseline previo y Auth, y agrega deliberadamente las 15 rutas de SC-105 identificadas por `METHOD + PATH`, usando las rutas representativas sin alias `/api`. El tag esperado es `Cámaras y ciclos`.
2. QG-2 conserva Auth y se amplía solo a las 15 operaciones de SC-105. Valida Bearer, tag, parámetros documentados, request body/schema cuando aplica, documentación XML de propiedades de request, response principal y status explícitos, descripciones de responses, contrato `ProblemDetails` solo en los status indicados y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID`.
3. QG-2 comprueba explícitamente que `GET /ach-cycles/{id}` conserva `404` sin `ProblemDetails` inventado y que `DELETE /ach-cycles/{id}` conserva `204` sin cuerpo.
4. No se agregaron examples en SC-105 y QG-3 permanece sin globalizar.

#### Separación de cámaras

La documentación de estas operaciones se mantiene genérica por cámara. No se incorporaron cantidades de ciclos, horarios, causales ni reglas NACHA-M específicas de ACH Colombia o CENIT. Las decisiones DEC-010 a DEC-014 permanecen intactas y ambas cámaras siguen tratándose como contextos independientes.

#### Validación de SC-105

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de ambos controllers, DTO, servicios inmediatos y test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-105 queda en `VERIFY`, no `BLOCKED`, y SC-106 queda en `READY`.

### Resultado de SC-106 — catálogos necesarios para primer request

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de los dos catálogos diferidos desde SC-105, documentación XML de sus DTO y ampliación incremental de QG-1/QG-2. No se modificaron servicios, lógica de selección, rutas, aliases `/api`, policies, transformers ni contratos HTTP.

#### Operaciones documentadas

| Operación | Summary | Response principal |
|---|---|---|
| `GET /clearing-houses/payment-rail-options` | `Consultar opciones de riel de pago` | `200 IReadOnlyList<ClearingHousePaymentRailOptionDto>` |
| `GET /clearing-houses/nacha-profiles` | `Consultar perfiles NACHA-M disponibles` | `200 IReadOnlyList<ClearingHouseNachaProfileOptionDto>` |

Los aliases existentes bajo `/api/clearing-houses/...` se conservaron sin cambios y esta unidad no declara todavía cuál ruta es canónica.

#### Significado de los catálogos

- `payment-rail-options` refleja la selección construida por `ClearingHouseService`: solo estrategias operativas administrativamente seleccionables, excluyendo `PaymentRailCodes.Unknown`, eliminando duplicados por código y publicando `Code` + `Name`. `Code` es el valor que puede usarse como `PaymentRailCode` al configurar una cámara.
- `nacha-profiles` devuelve únicamente perfiles con estado publicado. `Id` es el valor utilizable como `NachaProfileId`; `ClearingHouseCode` conserva la cámara asociada; `IsPublished` refleja el estado publicado y `IsCurrent` indica si el perfil está vigente en la fecha actual según su rango efectivo.
- `clearingHouseCode` es opcional. Cuando se proporciona, el servicio limita los perfiles que corresponden a esa cámara según su lógica actual; cuando se omite, devuelve los perfiles publicados disponibles. No se inventaron valores permitidos ni se afirmó que un perfil sea válido para todas las cámaras.

#### DTO documentados

Se agregaron comentarios XML específicos a:

- `ClearingHousePaymentRailOptionDto.Code` y `.Name`;
- `ClearingHouseNachaProfileOptionDto.Id`, `.Code`, `.Name`, `.ClearingHouseCode`, `.IsPublished` e `.IsCurrent`.

No se modificaron tipos, propiedades ni estructura contractual.

#### Responses y seguridad

- Ambas operaciones publican únicamente su `200` tipado y una descripción explícita mediante `OpenApiResponseDescriptionAttribute`.
- No se agregaron `400`, `404`, `500` ni `ProblemDetails` porque no forman parte del contrato explícito de estas acciones.
- Ambas conservan `FineGrainedPermissions.ClearingHouses.View` y el requisito Bearer derivado de `[Authorize]`.
- No se agregaron parámetros `Authorization`, `Idempotency-Key` ni `X-Correlation-ID`.

#### Quality gates incrementales

1. QG-1 conserva todo el allowlist anterior y agrega deliberadamente `GET /clearing-houses/payment-rail-options` y `GET /clearing-houses/nacha-profiles`, usando las rutas representativas sin alias `/api`. El tag esperado continúa siendo `Cámaras y ciclos`.
2. QG-2 conserva Auth y SC-105 y se amplía únicamente con estos dos catálogos. Comprueba Bearer, tag, response `200`, schema de colección correcto, descripción de response, documentación XML de ambos DTO, documentación y opcionalidad de `clearingHouseCode`, y ausencia de los tres headers transversales no aplicables.
3. No se agregaron examples y QG-3 permanece sin activar.

#### Separación de cámaras

Estos endpoints se documentan como catálogos internos de ACHInterbank. `ClearingHouseCode` preserva el contexto de cámara y NACHA-M no se presenta como una definición normativa universal. No se incorporaron ciclos, horarios, causales ni reglas de ACH Colombia extrapoladas a CENIT o viceversa.

#### Validación de SC-106

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de `ClearingHousesController`, `ClearingHouseDtos.cs`, la implementación de `ClearingHouseService` necesaria para confirmar el comportamiento de ambos catálogos y el test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-106 queda en `VERIFY`, no `BLOCKED`, y SC-107 queda en `READY`.


### Resultado de SC-107 — Quick Start real en la descripción global Scalar

**Estado:** `VERIFY`  
**Alcance ejecutado:** ampliación exclusiva de la metadata global OpenAPI y de su prueba dirigida. No se modificaron controllers, DTO, quality gates, contratos HTTP ni transformers distintos de `OpenApiDocumentMetadataTransformer`.

#### Flujo de onboarding agregado

La sección `### Quick Start` guía a un integrador por este recorrido, usando solo contratos ya documentados:

1. `POST /Auth/login` con `LoginRequest` y un ejemplo JSON ficticio (`usuario.demo` / `ClaveDemo-123!`).
2. Lectura del JWT desde `data.token` dentro de la response `200` real basada en `BaseResponseModel`.
3. Uso de `Authorization: Bearer <token>` como representación del esquema de seguridad OpenAPI, sin convertir `Authorization` en parámetro header.
4. Primera consulta protegida mediante `GET /clearing-houses`.
5. Descubrimiento previo de `PaymentRailCode` con `GET /clearing-houses/payment-rail-options` y de `NachaProfileId` con `GET /clearing-houses/nacha-profiles?clearingHouseCode=<codigo-camara>`. El texto indica expresamente que los códigos deben descubrirse y no inventarse.
6. Continuidad hacia `GET /clearing-houses/{id}/cycles` o `GET /ach-cycles?clearingHouseId={id}`.

El Quick Start usa rutas relativas. No se agregaron `Servers`, hostname, `localhost`, URLs de producción, tokens utilizables, identificadores reales ni códigos reales de cámaras o perfiles. Tampoco se agregaron nuevos providers/examples OpenAPI.

#### Separación de cámaras y seguridad

La sección nueva conserva la metadata global previa: ACH Colombia y CENIT continúan diferenciados, NACHA-M sigue interpretándose con contexto de cámara y no se introducen ciclos, horarios, causales ni reglas operativas específicas. Las operaciones protegidas se presentan mediante Bearer JWT y pueden exigir las autorizaciones o políticas configuradas en su contrato; no se inventan responses `401`/`403` ni se investiga `TokenJwtMiddleware`.

#### Prueba dirigida

`OpenApiDocumentGenerationTests` conserva todas las verificaciones previas y agrega aserciones para comprobar que la descripción global contiene `Quick Start`, `POST /Auth/login`, `data.token`, `Authorization: Bearer <token>`, `GET /clearing-houses`, ambos catálogos, `PaymentRailCode`, `NachaProfileId` y las rutas de continuidad hacia ciclos. También protege que la portada no incruste un JWT con prefijo habitual, no contenga URLs `http://`/`https://` y que el documento no publique `servers`. QG-1 y QG-2 no se modificaron.

#### Validación de SC-107

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de `OpenApiDocumentMetadataTransformer`, del bloque de assertions acumuladas y del diff de la unidad. No se intentó instalar `dotnet` ni se simuló ningún resultado.
- La limitación es exclusivamente ambiental: SC-107 queda en `VERIFY`, no `BLOCKED`, y SC-190 queda en `READY`.

### Resultado de SC-190 — checkpoint de Fase B

**Estado:** `VERIFY`  
**Tipo de checkpoint:** revisión estática completada; validación ejecutable pendiente por ausencia de `dotnet`.  
**Implementación de Fase B:** `COMPLETADA`.  
**Validación ejecutable:** `PENDIENTE`.  
**Bloqueo para Fase C:** `NO`.

#### Resultado de la revisión acumulada de Fase B

1. **SC-101 — XML docs:** `Cfa.ACHInterbank.Api`, `Cfa.ACHInterbank.Application` y `Cfa.ACHInterbank.Domain` mantienen `GenerateDocumentationFile = true`. Application y Domain conservan `NoWarn` acumulativo con `CS1591`; el `DocumentationFile` explícito del proyecto API sigue siendo relativo. No se encontró carga manual de XML, `IncludeXmlComments`, lectura XML en runtime ni schema transformer dedicado a copiar documentación.
2. **SC-102 — ejemplos:** `IOpenApiExampleProvider`, `OpenApiRequestExampleAttribute` y `OpenApiResponseExampleAttribute` permanecen como metadata explícita. `OpenApiExamplesTransformer` sigue usando las opciones JSON reales de MVC, solo aplica ejemplos sobre request/response y media type ya existentes, conserva `Example`/`Examples` previos y no inventa contratos.
3. **SC-103 — responses:** `ProducesResponseType` y la metadata estándar continúan siendo la fuente de status/schema/content type. `OpenApiResponseDescriptionsTransformer` solo aplica descripciones a responses existentes, falla de forma explícita si la metadata apunta a un status inexistente y no crea schemas, content types ni responses. `ProblemDetails` no se aplica globalmente y `BaseResponseModel` permanece diferenciado.
4. **SC-104 — Auth:** las cuatro operaciones (`login`, `forgot-password`, `reset-password`, `refresh`) mantienen documentación explícita. Login/forgot/reset conservan `[AllowAnonymous]`; refresh conserva `[Authorize]` y Bearer. `BaseResponseModel.Data` sigue siendo `dynamic`. Los providers de ejemplo mantienen datos ficticios y determinísticos. QG-1 y QG-2 de Auth continúan presentes.
5. **SC-105 — cámaras y ciclos:** las 15 operaciones aprobadas continúan documentadas con sus contratos actuales. `ProblemDetails` solo figura en los status tipados definidos por la unidad; `GET /ach-cycles/{id}` conserva `404` sin `ProblemDetails` inventado y delete conserva `204` sin body. Los aliases de controller bajo `/api` siguen declarados y no se mezclaron reglas específicas de ACH Colombia y CENIT.
6. **SC-106 — catálogos:** `payment-rail-options` y `nacha-profiles` permanecen documentados. `Code` se presenta como valor utilizable para `PaymentRailCode`, `Id` como valor utilizable para `NachaProfileId` y `clearingHouseCode` mantiene el contexto de cámara. No se convirtió NACHA-M en una definición universal de una sola cámara.
7. **SC-107 — Quick Start:** la portada conserva el recorrido `POST /Auth/login` → `data.token` → `Authorization: Bearer <token>` → `GET /clearing-houses` → catálogos → consultas de ciclos. Usa rutas relativas y datos ficticios; no publica `Servers`, hosts, URLs de producción, tokens utilizables, IDs reales ni códigos reales.

#### Quality gates confirmados

- QG-1 conserva un allowlist explícito de **25 operaciones** por `METHOD + PATH`: 4 del baseline de SC-008, 4 de Auth, 15 de SC-105 y 2 catálogos de SC-106. No existe descubrimiento automático de pertenencia.
- QG-2 conserva allowlists explícitos separados para **4 operaciones de Auth** y **17 operaciones de cámaras/ciclos/catálogos**. La pertenencia continúa definida por `METHOD + PATH` en los datos de prueba, no por controller, tag, `operationId` ni presencia de documentación.
- `Summary` y `Description` no son obligatorios globalmente; se exigen únicamente en QG-1 para las operaciones incorporadas deliberadamente.
- QG-3 no está activado globalmente.

#### Infraestructura de Fase A confirmada

La revisión dirigida confirma que permanecen registrados y sin redefinición de sus responsabilidades `OpenApiOperationIdTransformer`, `OpenApiIntegrationTagsTransformer`, `OpenApiTransversalHeadersTransformer`, `OpenApiSecurityMetadataTransformer`, `OpenApiDocumentMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer`. Además, los transformers de Fase B `OpenApiExamplesTransformer` y `OpenApiResponseDescriptionsTransformer` continúan registrados sin alterar las convenciones de `operationId`, taxonomía, headers o seguridad.

La metadata global sigue separando ACH Colombia y CENIT y presenta NACHA-M con contexto de cámara. `Authorization` continúa modelado por el esquema Bearer y no como parámetro. SC-190 no investigó `TokenJwtMiddleware`.

#### Pruebas acumuladas revisadas estáticamente

Se confirmó que permanecen en el repositorio las pruebas dirigidas de configuración XML, detector documental, ejemplos, descriptions de responses y generación OpenAPI. `OpenApiDocumentGenerationTests` conserva las verificaciones acumuladas de metadata global/Quick Start, `operationId`, tags, Bearer/security, headers transversales, referencias, ausencia de documentación heurística, QG-1 y QG-2.

#### Validación ejecutable

Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.

- **Build backend/solución:** `PENDIENTE — dotnet no disponible`.
- **ProjectConfigurationPortabilityTests:** `PENDIENTE — dotnet no disponible`.
- **OpenApiDocumentationCompletenessDetectorTests:** `PENDIENTE — dotnet no disponible`.
- **OpenApiExamplesTransformerTests:** `PENDIENTE — dotnet no disponible`.
- **OpenApiResponseDescriptionsTransformerTests:** `PENDIENTE — dotnet no disponible`.
- **OpenApiDocumentGenerationTests:** `PENDIENTE — dotnet no disponible`.
- **Suite completa backend/solución:** `PENDIENTE — dotnet no disponible`.
- No se intentó instalar `dotnet`, no se simularon compilaciones ni tests y no se ejecutaron frontend ni E2E.

#### Regresiones y continuidad

No se detectaron regresiones técnicas en la revisión estática dirigida de Fase B. No fue necesaria ninguna corrección de código en SC-190. La ausencia de `dotnet` sigue siendo una limitación de validación y no un defecto técnico; por tanto, SC-190 queda en `VERIFY`, SC-201 queda en `READY` y Fase C puede comenzar.

## Fase C — Core ACH transaccional

> Cada unidad debe cerrarse en máximo 2 controllers y 15 operaciones. Si un controller excede el límite, dividirlo por grupo de acciones.

| ID | Estado | Alcance |
|---|---|---|
| `SC-201` | `VERIFY` | `TransactionsController` — grupo 1 |
| `SC-202` | `VERIFY` | `TransactionsController` — grupo 2 |
| `SC-203` | `VERIFY` | `BulkIngestionController` |
| `SC-204` | `VERIFY` | Trazabilidad ACH y consultas relacionadas |
| `SC-205` | `VERIFY` | Devoluciones ACH — grupo 1 |
| `SC-206` | `VERIFY` | Devolución de devolución / respuestas — grupo 2 |
| `SC-207` | `VERIFY` | Políticas/reglas transaccionales — grupo 1 |
| `SC-208` | `VERIFY` | Políticas/reglas transaccionales — grupo 2 (`CfaOperationalTransactionRestrictionsController` + `PaymentRailCapabilityRegistryController`) |
| `SC-209` | `VERIFY` | Políticas/reglas transaccionales — grupo 3 (`TransactionPrerequisitePolicyController`) |
| `SC-290` | `READY` | Checkpoint Fase C |

### Resultado de SC-201 — `TransactionsController` grupo 1

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de cinco operaciones core de `TransactionsController`, saneamiento de responses publicados, comentarios XML de contratos inmediatos y ampliación incremental de QG-1/QG-2. No se modificaron servicios, políticas transaccionales, rutas, aliases `/api`, persistencia, seguridad ni transformers.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `GET /Transactions` | `Listar transacciones` | `200 IEnumerable<AchTransactionListDto>` |
| `GET /Transactions/company-entry-descriptions` | `Consultar Company Entry Descriptions` | `200 IEnumerable<CompanyEntryDescriptionDto>` |
| `GET /Transactions/policies/preview` | `Previsualizar política transaccional` | `200 TransactionPolicyPreview` |
| `POST /Transactions` | `Registrar transacción individual` | `201 AchTransaction`, `400` validación/regla capturada, `500` error interno capturado |
| `GET /Transactions/{id}` | `Consultar transacción` | `200 AchTransaction`, `404` respuesta actual de no encontrado |

Se retiraron de estas cinco acciones los `ProducesResponseType` especulativos no respaldados por su implementación. En particular, `POST /Transactions` ya no publica `409`: `ArgumentException` e `InvalidOperationException` son capturados por la acción y se responden actualmente como `400`. Tampoco se inventaron `401`/`403` desde `[Authorize]` ni `500` para acciones que no lo capturan explícitamente.

#### Comportamiento funcional reflejado

- El listado combina filtros opcionales por `achCycleId`, `achCycleName`, parte de fecha de `EffectiveEntryDate` y `clearingHouseId`.
- `company-entry-descriptions` devuelve el catálogo activo configurado por ACHInterbank y no se presenta como catálogo normativo universal de una cámara.
- `policies/preview` evalúa la política con los datos suministrados sin persistir una transacción. `CanSubmit` representa el resultado de esa evaluación previa y no garantiza que un registro o procesamiento posterior finalice correctamente.
- La creación individual exige `TransactionExternalId` o `Reference` legado, valida cuentas, originador, tipos de persona `PN`/`PJ` y receptor cuando corresponde, y devuelve `201` al crear. La revisión dirigida precisó que las monetarias requieren `Amount > 0` y que, aunque la validación inicial del controller no aplica esa regla a prenotificaciones, `TransactionValidator` exige `Amount == 0` para una prenotificación.
- La consulta por id devuelve la entidad encontrada o el `404` actual con mensaje anónimo; no se inventó `ProblemDetails`.

Las cuatro acciones `POST /Transactions/bulk/submit`, `POST /Transactions/bulk`, `GET /Transactions/{id}/integration-result` y `GET /Transactions/{id}/integration-readiness` permanecen fuera del alcance y quedan reservadas a SC-202.

#### DTO y parámetros documentados

Se agregaron comentarios XML específicos a `AchTransactionRequest`, `AchTransactionListDto`, `CompanyEntryDescriptionDto`, `TransactionPolicyPreviewRequest`, `TransactionPolicyPreview`, `AddendaDto` y a propiedades API relevantes de `AchTransaction`. `TransactionExternalId` se presenta como identificador operativo/idempotencia canónico; `Reference` se mantiene identificado como campo legado/transicional de respaldo. No se cambiaron tipos, nullability, enums, estructura ni `DataAnnotations`.

También se documentaron los parámetros query/path de las cinco acciones, incluidos los filtros del listado y los campos de evaluación de política. No se inventaron formatos o longitudes en la metadata de las operaciones.

#### Quality gates incrementales

1. QG-1 conserva todo su allowlist anterior y agrega deliberadamente las cinco rutas representativas de SC-201 por `METHOD + PATH`, usando rutas sin alias `/api`. El tag esperado es `Transacciones interbancarias`.
2. QG-2 conserva Auth y cámaras/ciclos/catálogos y se amplía solo a estas cinco operaciones. Verifica Bearer, tag, parámetros documentados, ausencia de request body en los GET, `AchTransactionRequest` y su documentación XML en el POST, response principal y status explícitos, descripciones de response, schemas tipados conocidos y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID`.
3. Para los errores heterogéneos actuales de creación y el `404` anónimo por id, QG-2 valida el status y su descripción sin inventar un schema `ProblemDetails`.
4. Se protege además la documentación XML del `AddendaDto` usado por el request. No se agregaron examples y QG-3 permanece sin activar.

#### Separación de cámaras

Las cinco operaciones se documentan como transacciones interbancarias generales. No se atribuyeron automáticamente a ACH Colombia o CENIT y no se incorporaron horarios, cantidades de ciclos, causales ni reglas NACHA-M específicas. Las decisiones DEC-010 a DEC-014 permanecen intactas.

#### Validación de SC-201

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida del controller, `IAchTransactionService`, `AchTransactionService`, `TransactionPolicyService`, `TransactionValidator`, contratos inmediatos y test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-201 queda en `VERIFY`, no `BLOCKED`, y SC-202 queda en `READY`.

### Resultado de SC-202 — `TransactionsController` grupo 2

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de las cuatro acciones bulk/integración restantes de `TransactionsController`, comentarios XML de sus contratos inmediatos y ampliación incremental de QG-1/QG-2. No se modificaron servicios, validadores, persistencia, lógica bulk, integración externa, estados, rutas, aliases `/api`, policies, seguridad ni transformers.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `POST /Transactions/bulk/submit` | `Procesar ingestión masiva` | `200 BulkIngestionResponse`, `400` validación global/origen no soportado, `500` error interno capturado |
| `POST /Transactions/bulk` | `Registrar transacciones en lote` | `200 BulkAchTransactionResponse`, `400` validación global del lote, `500` error interno capturado |
| `GET /Transactions/{id}/integration-result` | `Consultar resultado de integración` | `200 TransactionIntegrationResultDto`, `404` transacción inexistente, `503` servicio de consulta no disponible sin body tipado |
| `GET /Transactions/{id}/integration-readiness` | `Consultar readiness de integración` | `200 TransactionIntegrationReadinessResult`, `404` transacción inexistente, `503` servicio de readiness no disponible |

Se retiraron de estas cuatro acciones los status especulativos que no estaban respaldados por la implementación: `401`/`403` documentales derivados solo de autorización, `409` en `POST /Transactions/bulk` y `500` en las consultas de integración. Ninguna de estas cuatro operaciones se documentó con `ProblemDetails` porque su código actual no devuelve ese contrato.

#### Diferencia real entre `bulk/submit` y `bulk`

- `POST /Transactions/bulk/submit` es una capa de ingestión que selecciona `SourceType` y `ProcessingMode`. En modo `Synchronous` solo admite `InlineTransactions` y delega al registro masivo; un `SourceType` distinto produce `400`. En modo `AsynchronousJob` la implementación actual responde `200` con `Status = NOT_IMPLEMENTED`, `JobId = null` y no despacha un job.
- `POST /Transactions/bulk` procesa directamente un `BulkAchTransactionRequest`. Valida primero la referencia y la colección global; después procesa cada ítem con las validaciones transaccionales existentes.
- Los `ArgumentException`/`InvalidOperationException` capturados por ítem se registran en `ItemResults` como `ITEM_VALIDATION_FAILED`, mientras los ítems válidos se confirman por bloques. Por ello el resultado puede contener éxitos y fallos simultáneos y el procesamiento no es atómico a nivel del lote completo.
- `TransactionExternalId` continúa siendo el identificador operativo/idempotencia preferido. Cuando está ausente, el servicio usa `Reference` legado como respaldo para resolver el identificador operativo y detectar duplicados.

#### Consultas de integración

- `integration-result` primero comprueba que la transacción exista. Si existe pero no hay intentos de integración registrados, devuelve `200` con `Latest = null` y `History` vacío; el `404` corresponde a transacción inexistente, no a historial vacío. El contrato combina intentos registrados por los flujos de integración y los ordena del más reciente al más antiguo sin exponer payloads de transporte.
- `integration-readiness` resuelve la operación técnica correspondiente y evalúa su configuración de mappings. El contrato expone `IsSupported`, `Reason` y un objeto `Readiness` con mappings faltantes/inactivos, errores, advertencias, fallback y `CanBuildPayload`. Esta consulta no transmite la transacción ni confirma aceptación, procesamiento o compensación posterior.
- Ambas acciones responden `503` únicamente cuando el servicio opcional correspondiente no está registrado en el controller. SC-202 no inventó un body tipado para esas respuestas.

#### DTO y parámetros documentados

Se agregaron comentarios XML a `BulkIngestionRequest`, `BulkIngestionResponse`, `BulkIngestionSourceType`, `BulkIngestionProcessingMode`, `BulkAchTransactionRequest`, `BulkAchTransactionItemRequest`, `BulkAchTransactionResponse`, `BulkAchTransactionItemResult`, `TransactionIntegrationResultDto`, `TransactionIntegrationResultItemDto`, `TransactionIntegrationReadinessResult` e `IntegrationMappingReadinessResult`. No se cambiaron tipos, nullability, enums, estructura ni `DataAnnotations`.

También se documentó por XML el parámetro `id` de las dos consultas de integración y los request bodies de las acciones bulk. Los campos reservados para archivos, tracking o reintentos en `BulkIngestionRequest` se describen como no consumidos por el flujo actual, sin presentar funcionalidad futura como implementada.

#### Particularidad de cámara observada

La revisión dirigida de `AchBulkTransactionService` y `BatchResolver` confirmó una rama específica de **CENIT**: cuando el ciclo resuelto pertenece a CENIT y está fuera de su ventana operativa, `TransactionBatchContext.MustQueueForTargetCycle` queda activo y el servicio bulk encola la transacción si `ICenitCycleQueueService` está disponible. La documentación identifica esta conducta como específica de CENIT y no la extrapola a ACH Colombia ni a otras cámaras. No se incorporaron horarios, cantidades de ciclos, causales ni reglas normativas NACHA-M.

#### Quality gates incrementales

1. QG-1 conserva todo el allowlist anterior y agrega deliberadamente las cuatro rutas representativas de SC-202 por `METHOD + PATH`, sin aliases `/api`. El tag esperado continúa siendo `Transacciones interbancarias`.
2. QG-2 conserva Auth, cámaras/ciclos/catálogos y SC-201, y se amplía solo a estas cuatro operaciones. Verifica Bearer, tag, parámetro `id` donde aplica, request body/schema de ambas acciones bulk, documentación XML de los schemas bulk, response principal y status explícitos, descripciones de response y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID` porque ninguna acción de este grupo pertenece a la tabla SC-006.
3. Las pruebas protegen además que no reaparezcan `401`, `403`, `409` o `500` especulativos en las operaciones donde SC-202 los retiró y que los bodies heterogéneos actuales no se conviertan en `ProblemDetails`.
4. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-202

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de `TransactionsController`, `AchBulkIngestionService`, `AchBulkTransactionService`, `TransactionIntegrationResultService`, `TransactionIntegrationReadinessService`, `BatchResolver`, los DTO inmediatos y el test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutó suite completa.
- La limitación es exclusivamente ambiental: SC-202 queda en `VERIFY`, no `BLOCKED`, y SC-203 queda en `READY`.

### Resultado de SC-203 — `BulkIngestionController`

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de las seis operaciones de `BulkIngestionController`, saneamiento de responses publicados, comentarios XML de los DTO expuestos y ampliación incremental de QG-1/QG-2. No se modificaron servicios, parsers, validadores, dispatcher/jobs, persistencia, estados, retry/cancel, contratos, rutas, policies ni transformers.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `POST /api/transactions/bulk-ingestion/upload` | `Cargar archivo de transacciones masivas` | `200 BulkFileUploadResponse`, `400` archivo ausente/vacío/tamaño/formato o validación estructural capturada, `500` error interno capturado |
| `GET /api/transactions/bulk-ingestion/{batchId}` | `Consultar estado de un lote` | `200 BulkBatchStatusDto`, `404` lote inexistente con el body actual no tipado |
| `GET /api/transactions/bulk-ingestion/{batchId}/items` | `Consultar ítems de un lote` | `200 BulkBatchItemsPageDto`, `404` lote inexistente |
| `GET /api/transactions/bulk-ingestion/{batchId}/summary` | `Consultar resumen de procesamiento` | `200 BulkBatchProcessingSummaryDto`, `404` lote inexistente |
| `POST /api/transactions/bulk-ingestion/{batchId}/retry` | `Reintentar procesamiento de un lote` | `200 RetryBatchResponse`, `400` estado/máximo/sin ítems elegibles, `404` lote inexistente |
| `POST /api/transactions/bulk-ingestion/{batchId}/cancel` | `Cancelar lote de ingestión` | `200` cancelación aceptada con body anónimo actual, `400` lote existente no cancelable, `404` lote inexistente |

Se retiraron de estas seis acciones los `ProducesResponseType` especulativos no respaldados por el controller: `401`/`403` contractuales derivados solo de autorización, `400`/`500` de la consulta de ítems, `500` de las otras consultas y `409`/`500` de retry/cancel. Ningún error de este grupo se documentó como `ProblemDetails` porque las acciones actuales devuelven bodies anónimos de mensaje o respuestas sin contrato tipado.

#### Upload por archivo

- La operación consume `multipart/form-data`; `File` es obligatorio y la acción limita la carga a 20 MB. `BatchReference` y `ClientRequestId` son opcionales.
- La resolución de formato admite JSON, CSV y Excel para extensiones `.xlsx`/`.xlsm`; también considera los content types reconocidos por el servicio.
- El parser produce registros y `BulkFileStructuralValidator` valida estructuralmente cada uno. Los inválidos se persisten como `StructuralError`; los válidos quedan `Ready`.
- Si existe al menos un registro válido, el lote pasa a `Queued`, se crea el intento inicial y se despacha procesamiento. Cuando hay registros y ninguno es válido, el lote queda `Failed`; un archivo parseado sin registros no se presentó como equivalente a ese caso.
- La deduplicación específica del upload solo se aplica cuando `ClientRequestId` está informado: el servicio calcula SHA-256 y devuelve el lote más reciente que coincida simultáneamente en `ClientRequestId` y hash. No se documentó ni agregó un header `Idempotency-Key`.
- El controller convierte `NotSupportedException` y `ArgumentException` capturadas en `400`, y cualquier otra excepción capturada en `500`.

#### Consultas y paginación

- `GET /{batchId}` expone el estado consolidado y los contadores persistidos del lote.
- `GET /{batchId}/items` usa `page=1` por defecto y normaliza cualquier valor menor a 1; usa `pageSize=100` por defecto y lo limita al rango `1..500`; `status` es un filtro opcional exacto. Los ítems se ordenan por `ItemIndex`.
- `GET /{batchId}/summary` devuelve el estado consolidado y el historial de intentos, ordenado por `AttemptNumber` descendente.

#### Retry

- `Scope=FailedOnly` selecciona únicamente ítems `ProcessingError`.
- `Scope=Full` selecciona todos los ítems salvo `StructuralError`; no se afirma que los errores estructurales se corrijan automáticamente.
- Los ítems elegibles vuelven a `Ready`, reciben el mensaje de reintento y limpian `TransactionId`; después se crea un nuevo intento, el lote pasa a `Retrying` y se despacha procesamiento.
- El máximo actual es de 3 reintentos. La acción responde `400` si el lote ya está `Queued`, `Processing` o `Retrying`, si alcanzó el máximo o si el alcance no encuentra ítems elegibles; `404` corresponde a lote inexistente.

#### Cancelación

`RequestCancellationAsync` devuelve éxito únicamente para un lote existente que no esté `Completed`, `Failed` ni `Cancelled`. En ese caso marca el lote como `Cancelled`, actualiza `LastJobMessage` y `ProcessingFinishedAtUtc`, y la acción responde `200`. SC-203 no afirma que esto cancele físicamente un job Quartz, worker o backplane ya en ejecución porque esa conducta no está demostrada por el código revisado.

#### DTO y XML docs

Se documentaron únicamente los contratos expuestos por estas seis operaciones: `BulkFileUploadForm`, `BulkFileUploadResponse`, `BulkBatchStatusDto`, `BulkBatchItemDto`, `BulkBatchItemsPageDto`, `BulkBatchAttemptDto`, `BulkBatchProcessingSummaryDto`, `RetryBatchRequest` y `RetryBatchResponse`. Se describieron propiedades y significado operacional sin cambiar tipos, enums, nullability, estructura ni agregar `DataAnnotations`.

#### Seguridad y contexto de cámara

Se conservaron las policies `BulkIngestionUpload`, `BulkIngestionRead`, `BulkIngestionRetry` y `BulkIngestionCancel`; las seis operaciones permanecen bajo Bearer por `[Authorize]`. El tag continúa siendo `Transacciones interbancarias` por la taxonomía existente. No se agregaron parámetros `Authorization`, `Idempotency-Key` ni `X-Correlation-ID`. Este controller se documentó como ingestión interna de ACHInterbank; no se incorporaron horarios, ciclos, causales ni reglas NACHA-M de ACH Colombia o CENIT.

#### Quality gates incrementales

1. QG-1 conserva todos los allowlists anteriores y agrega deliberadamente estas seis rutas por `METHOD + PATH`; el total pasa de 34 a 40 operaciones.
2. QG-2 conserva Auth, cámaras/ciclos/catálogos y Transactions previos, y agrega únicamente estas seis operaciones. Verifica Bearer, tag, parámetros documentados, `multipart/form-data` y schema del upload, XML de requests/responses, status y descripciones reales, ausencia de los tres headers no aplicables y que los bodies de error heterogéneos no se conviertan en `ProblemDetails`.
3. QG-2 protege también la documentación de `BulkBatchItemDto` y `BulkBatchAttemptDto`, incluidos indirectamente en las respuestas paginada y de resumen.
4. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-203

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de `BulkIngestionController`, `AchBulkFileIngestionService`, parsers JSON/CSV/Excel, `BulkFileStructuralValidator`, `AchBulkBatchQueryService`, `AchBulkBatchRetryService`, `BulkIngestionLifecycleService`, `AchBulkBatchProcessingService`, los DTO expuestos y el test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutaron otras suites.
- La limitación es exclusivamente ambiental: SC-203 queda en `VERIFY`, no `BLOCKED`, y SC-204 queda en `READY`.

### Resultado de SC-204 — `AchTraceabilityController`

**Estado:** `VERIFY`  
**Alcance ejecutado:** documentación funcional explícita de las tres operaciones de `AchTraceabilityController`, saneamiento de responses publicados, comentarios XML de los contratos expuestos y ampliación incremental de QG-1/QG-2. No se modificaron servicios, transiciones de estado, persistencia, rutas, policies, seguridad ni transformers.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `POST /api/ach-traceability/sol02/{transactionId}/certify` | `Certificar transacción con SOL02` | `200` body anónimo actual con identificador/estado/fecha, `400` transición a Certified no permitida, `404` transacción inexistente |
| `GET /api/ach-traceability/transactions/{transactionId}` | `Consultar trazabilidad de una transacción` | `200 AchTraceabilityDetailDto`, `404` transacción inexistente con body anónimo actual |
| `GET /api/ach-traceability/report` | `Consultar reporte de trazabilidad` | `200 IReadOnlyList<AchTraceabilityReportRowDto>` |

Se retiraron de estas operaciones los `ProducesResponseType` especulativos `401`/`403`/`500` que estaban publicados solo por convención; además se retiró el `400` especulativo del reporte, que no ejecuta validación propia ni captura ese status. No se agregó `ProblemDetails` a los bodies anónimos existentes.

#### Certificación SOL02

- `CertifySol02Async` solicita una transición al estado `Certified` con fuente `Claims` y `reasonCode = "SOL02"`.
- `CertificationReference` y `Notes` son opcionales y se serializan dentro del payload JSON del evento de estado; no se inventaron reglas de obligatoriedad, formato o longitud.
- El servicio de transición permite llegar a `Certified` desde `Pending`, `ReturnedByEpr` y `AppliedTacitly`; una transición no permitida produce `InvalidOperationException`, que la acción convierte en `400`.
- Una transacción inexistente produce `KeyNotFoundException`, que la acción convierte en `404`.
- El `200` conserva el body anónimo actual con `message`, `transactionId`, `state` y `stateChangedAtUtc`; no se creó un contrato nuevo solo para documentarlo.

#### Consultas de trazabilidad

- La consulta individual devuelve datos de transacción, ciclo/cámara, instituciones, estado, último archivo de exportación `NACHA` del ciclo, última devolución generada para la transacción y eventos de estado ordenados por `CreatedAt` ascendente.
- `ReturnReasonCode` usa el valor de la transacción cuando existe y, de lo contrario, el de la devolución generada más reciente asociada.
- El reporte filtra opcionalmente `StateChangedAtUtc` con límites inclusivos `fromUtc`/`toUtc`, el estado actual y uno o varios `achCycleId` separados por comas; los resultados se ordenan por `StateChangedAtUtc` descendente.
- No se agregó paginación, límites de rango ni reglas de cámara que el código no implementa.

#### DTO y XML docs

Se documentaron únicamente los contratos expuestos por estas tres operaciones: `Sol02CertificationRequest`, `AchTraceabilityEventDto`, `AchTraceabilityDetailDto` y `AchTraceabilityReportRowDto`. Se describieron propiedades y significado operacional sin cambiar tipos, enums, nullability, serialización, estructura ni agregar `DataAnnotations`.

#### Seguridad y taxonomía

Se conservaron `P0Policies.TraceabilityCertifySol02` y `P0Policies.TraceabilityRead`; las tres operaciones permanecen bajo Bearer por `[Authorize]`. El tag continúa siendo `Trazabilidad y conciliación` por la taxonomía existente. No se agregaron parámetros `Authorization`, `Idempotency-Key` ni `X-Correlation-ID`.

#### Quality gates incrementales

1. QG-1 conserva todos los allowlists anteriores y agrega deliberadamente estas tres rutas por `METHOD + PATH`; el total pasa de 40 a 43 operaciones.
2. QG-2 conserva todos los grupos anteriores y agrega únicamente estas tres operaciones. Verifica Bearer, tag, parámetros y request de SOL02, documentación XML de schemas, status/descripciones reales y ausencia de los tres headers no aplicables.
3. QG-2 protege además que los errores anónimos actuales no se conviertan en `ProblemDetails` y que no reaparezcan `401`/`403`/`500` especulativos ni el `400` retirado del reporte.
4. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-204

- Se comprobó una sola vez la disponibilidad de `dotnet`; no se encontró ejecutable.
- `OpenApiDocumentGenerationTests`: `PENDIENTE — dotnet no disponible`.
- `dotnet build ACHInterbank.sln -c Release`: `PENDIENTE — dotnet no disponible`.
- Se realizó revisión estática dirigida de `AchTraceabilityController`, `IAchTraceabilityService`, `AchTraceabilityService`, `AchStateTransitionService`, DTOs expuestos, policies existentes y el test OpenAPI modificado. No se intentó instalar `dotnet`, no se simularon resultados y no se ejecutaron otras suites.
- La limitación es exclusivamente ambiental: SC-204 queda en `VERIFY`, no `BLOCKED`, y SC-205 queda en `READY` sin haber sido ejecutado.

### Resultado de SC-205 — Devoluciones ACH — grupo 1

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Devoluciones ACH — grupo 1`.  
**Alcance ejecutado:** documentación contractual del flujo base de consulta, generación de archivo y catálogo de causales mediante `AchReturnsController` y `ReturnReasonsController`. No se modificaron servicios, persistencia, validadores, políticas de devolución, reglas ACH/CENIT/NACHA-M, serialización, rutas ni transformers. `SC-206` no fue ejecutado.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `GET /ach-returns/cycles/{cycleId}/transactions` | `Consultar transacciones de un ciclo para devolución` | `200 IEnumerable<ReturnEligibleTransactionDto>` |
| `POST /ach-returns/generate-file` | `Generar archivo de devoluciones` | `200` descarga del archivo actual, `409 ProblemDetails` por devolución ya generada |
| `GET /return-reasons` | `Consultar causales de devolución` | `200 IEnumerable<ReturnReasonDto>` |

No se agregaron `400`, `401`, `403`, `404` ni `500` por convención. En `GenerateFile`, el único error traducido explícitamente por el controller es `AchReturnAlreadyGeneratedException`, que conserva `409 ProblemDetails` con `errorCode` y `transactionIds`. Las demás validaciones del servicio no fueron convertidas en nuevos contratos HTTP dentro de SC-205.

#### Comportamiento documentado

- `GetTransactionsByCycleAsync` valida internamente la existencia del ciclo, consulta sus transacciones ordenadas por `Id` y marca `IsEligible=false` únicamente cuando ya existe un `AchReturnGenerated` para la transacción; `ValidationMessage` explica ese caso. La documentación no extrapola esa bandera como validación completa de todas las reglas de generación.
- `GenerateReturnsFileAsync` recibe `CycleId`, `Items` y `ReturnCycleId` opcional; cuando `ReturnCycleId` está vacío usa `CycleId`. El servicio valida la selección, persiste las devoluciones generadas y transiciona las transacciones procesadas a `ReturnedByEpr` antes de devolver el archivo descargable. No se documentaron como status contractuales las `InvalidOperationException` no capturadas por el controller.
- `ReturnReasonService` devuelve el catálogo ordenado por `Code`. Con `onlyForReturn=true` filtra `IsForReturn` y excluye `DEV14`; con el valor predeterminado `false` devuelve el catálogo completo. Esta descripción refleja el código actual sin convertirla en una regla normativa externa.

#### DTO y XML docs

Se documentaron los contratos expuestos por estas tres operaciones: `ReturnEligibleTransactionDto`, `ReturnSelectionItemDto`, `GenerateReturnsFileRequest`, `CenitIncomingReturnOperationalEvidence` y `ReturnReasonDto`. También se aclaró la semántica de los enums ya existentes `AchColombiaR10ReturnBasis` y `CenitPrenotificationDirection`, sin cambiar sus valores. No se modificaron tipos, nullability, nombres JSON, defaults, estructura, serialización ni se agregaron `DataAnnotations`. `GenerateReturnsFileResponse` permanece como contrato interno del servicio y no se presentó como body HTTP porque la acción devuelve un archivo.

#### Seguridad y taxonomía

- `GET /ach-returns/cycles/{cycleId}/transactions`: `P0Policies.ReturnsRead`.
- `POST /ach-returns/generate-file`: `P0Policies.ReturnsGenerateFile`.
- `GET /return-reasons`: policy existente `CanReadAch`.
- Las tres operaciones permanecen bajo Bearer por `[Authorize]` y usan el tag existente `Devoluciones y respuestas`.
- No se agregaron parámetros `Authorization`, `Idempotency-Key` ni `X-Correlation-ID`.

#### Quality gates incrementales

1. QG-1 conserva las 43 operaciones heredadas y agrega únicamente las tres rutas de SC-205; el total pasa a **46 operaciones**.
2. QG-2 agrega solo estas tres operaciones y verifica tag, Bearer, parámetros path/query, request `GenerateReturnsFileRequest`, XML de schemas, responses/descripciones reales y ausencia de headers no consumidos.
3. QG-2 protege que `GET` por ciclo y catálogo no publiquen `400/401/403/404/500`, y que la generación no publique `400/401/403/500`; el `409` se conserva porque está implementado y probado por `AchReturnsControllerConflictTests`.
4. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-205

- Se comprobó la disponibilidad de `dotnet`; no se encontró ejecutable.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: `PENDIENTE — dotnet no disponible`.
- `dotnet build ACHInterbank.sln -c Release`: `PENDIENTE — dotnet no disponible`.
- Se realizó inspección estática dirigida de los dos controllers, `IAchReturnsService`, `AchReturnsService`, `IReturnReasonService`, `ReturnReasonService`, DTOs expuestos, policies, transformers aplicables y `OpenApiDocumentGenerationTests`.
- La limitación es exclusivamente ambiental: SC-205 queda en `VERIFY`, no `BLOCKED`; SC-203 y SC-204 conservan `VERIFY`; SC-206 queda en `READY` sin ejecutarse.


### Resultado de SC-206 — Devolución de devolución / respuestas — grupo 2

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Devolución de devolución / respuestas — grupo 2`.  
**Alcance ejecutado:** documentación contractual explícita de las cuatro acciones de `AchReturnOfReturnController` y las siete acciones de `AchResponsesController`. La selección respeta la regla de Fase C de máximo dos controllers y quince operaciones por unidad. No se modificaron servicios, repositorios, validadores, mappers, persistencia, rutas, policies, reglas ACH/CENIT/NACHA-M, serialización ni transformers. `SC-207` no fue ejecutado.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `POST /ach-returns/return-of-return/cenit/out` | `Crear devolución de devolución saliente CENIT` | `200 CenitReturnOfReturnResult`, `400` body actual no tipado, `409` body actual heterogéneo/no tipado |
| `POST /ach-returns/return-of-return/evaluate` | `Evaluar devolución de devolución` | `200 AchReturnOfReturnEligibilityResult`, `400` body actual no tipado |
| `POST /ach-returns/return-of-return/generate-audit-file` | `Generar archivo de auditoría de devolución de devolución` | `200` descarga `text/plain`, `400` body actual no tipado, `409` body actual no tipado |
| `POST /ach-returns/return-of-return/generate-nacha-file` | `Generar archivo NACHA de devolución de devolución` | `200` descarga `text/plain`, `400` body actual no tipado, `409` body actual no tipado |
| `POST /api/ach/responses/process` | `Procesar respuesta ACH` | `200 ProcesarRespuestaAchResponse`, `400 ValidationProblemDetails`, `422 ProcesarRespuestaAchResponse` |
| `POST /api/ach/responses/notifications/send` | `Enviar notificación de respuesta ACH` | `200 NotificarRespuestaAchResponse`, `400 ValidationProblemDetails`, `404` sin body |
| `GET /api/ach/responses` | `Consultar respuestas ACH` | `200 PagedResponse<AchResponseListItemResponse>` |
| `GET /api/ach/responses/dashboard` | `Consultar resumen de respuestas ACH` | `200 AchResponseDashboardResponse`, `400 ProblemDetails` |
| `GET /api/ach/responses/{id}` | `Consultar detalle de respuesta ACH` | `200 AchResponseDetailResponse`, `404` sin body |
| `GET /api/ach/responses/{id}/notification-attempts` | `Consultar intentos de notificación de una respuesta ACH` | `200 IReadOnlyList<AchResponseNotificationAttemptResponse>` |
| `GET /api/ach/response-status-mappings` | `Consultar mappings de estado de respuestas ACH` | `200 IReadOnlyList<AchResponseStatusMappingResponse>`, `400 ProblemDetails` |

#### Contratos XML documentados

- Requests Return Of Return: `CreateCenitReturnOfReturnOutRequest`, `EvaluateReturnOfReturnRequest`, `GenerateReturnOfReturnAuditFileRequest`.
- Responses Return Of Return: `CenitReturnOfReturnResult`, `AchReturnOfReturnEligibilityResult`, `AchReturnOfReturnEligibilityFailure`.
- Respuestas ACH: `ProcesarRespuestaAchRequest`, `ProcesarRespuestaAchResponse`, `NotificarRespuestaAchRequest`, `NotificarRespuestaAchResponse`, `AchResponseSearchRequest`, `AchResponseDashboardRequest`, `AchResponseDashboardResponse`, `AchResponseListItemResponse`, `PagedResponse<T>`, `AchResponseDetailResponse`, `AchResponseNotificationAttemptResponse`, `AchResponseStatusMappingResponse`.
- No se cambiaron tipos, nullability, nombres JSON, defaults, enums, serialización ni validadores.

#### Comportamiento documentado

- `Evaluate` mantiene la ineligibilidad como resultado funcional `200`: `IsEligible=false` y `Failures` describen el resultado; solo la solicitud estructuralmente inválida devuelve `400`.
- Las dos generaciones Return Of Return requieren al menos un `FlowId`; responden `409` cuando el servicio no devuelve archivo generado, contenido y nombre utilizables.
- `CreateCenitOut` requiere evento padre, causal y ciclo; responde `409` tanto cuando el servicio opcional no está disponible como cuando el servicio devuelve `IsSuccessful=false`.
- `Process` valida el contrato con `ProcesarRespuestaAchRequestValidator`; una solicitud válida devuelve `200` si `Procesada=true` y `422` si `Procesada=false`.
- `SendNotification` devuelve `200` cuando el intento fue encontrado incluso si el resultado registra error funcional o técnico; `Encontrada=false` produce `404` sin body.
- `Search` normaliza `PageNumber <= 0` a `1`; `PageSize <= 0` a `10` y limita valores mayores de `100` a `100`. Los resultados se ordenan por `FechaRecepcion` descendente.
- `Dashboard` y `GetMappings` aceptan `Prenota` o `Transaccion` sin distinguir mayúsculas/minúsculas; otro valor no vacío devuelve `400 ProblemDetails` antes de consultar el repositorio.

#### Quality gates incrementales

1. QG-1 conserva las **46 operaciones heredadas** y agrega exclusivamente las once operaciones de SC-206; el total pasa a **57 operaciones**.
2. QG-2 reutiliza el bloque de `Devoluciones y respuestas` y agrega solo las operaciones de SC-206. Valida tag funcional, Bearer, parámetros path/query, request bodies reales, XML de schemas, response DTOs/status/descripciones y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID` como parámetros en estas operaciones.
3. QG-2 agrega shapes específicos únicamente para comprobar el `ValidationProblemDetails` real de los dos POST validados y la página `PagedResponse<AchResponseListItemResponse>` sin relajar las comprobaciones existentes.
4. QG-2 agrega checks negativos de status para impedir que reaparezcan `401/403/404/409/422/500` u otros códigos especulativos donde el controller no los publica actualmente.
5. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-206

- Se comprobó la disponibilidad de `dotnet`; no se encontró ejecutable.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: `PENDIENTE — dotnet no disponible`.
- `dotnet build ACHInterbank.sln -c Release`: `PENDIENTE — dotnet no disponible`.
- Se realizó inspección estática dirigida de `AchReturnOfReturnController`, `AchResponsesController`, validators, mappers, `AchResponseRepository`, repositorio de intentos, contratos expuestos, policies, transformer de tags y `OpenApiDocumentGenerationTests`.
- Se revisó el diff completo contra `ACHInterbank-v124-scalar-r205-SC205.zip`; solo los archivos registrados para SC-206 pueden permanecer modificados.
- La ausencia de `dotnet` es una limitación exclusivamente ambiental: SC-206 queda en `VERIFY`, no `BLOCKED`; SC-207 queda en `READY` sin ejecutarse.

### Resultado de SC-207 — Políticas/reglas transaccionales — grupo 1

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Políticas/reglas transaccionales — grupo 1`.  
**Alcance ejecutado:** documentación contractual explícita de las ocho acciones de `ClearingHouseTransactionPoliciesController` y las seis acciones de `ClearingHouseTransactionRulesController`. La selección agrupa los dos controllers que comparten `IClearingHouseTransactionRuleService` y respeta el límite de Fase C de máximo dos controllers y quince operaciones por unidad. No se modificaron servicios, repositorios, persistencia, rutas, policies, enums, reglas ACH/CENIT/NACHA-M, serialización, validadores ni transformers. `SC-208` no fue ejecutado.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `GET /api/clearing-houses/{clearingHouseId}/transaction-policies` | `Consultar versiones de política transaccional por cámara` | `200 IReadOnlyList<ClearingHouseTransactionRuleDto>`, `422` body actual no tipado |
| `GET /api/clearing-houses/{clearingHouseId}/transaction-policies/current` | `Consultar política transaccional vigente` | `200 ClearingHouseTransactionRuleDto`, `404` sin body, `422` body actual no tipado |
| `GET /api/clearing-houses/{clearingHouseId}/transaction-policies/{id}` | `Consultar versión de política transaccional` | `200 ClearingHouseTransactionRuleDto`, `404` sin body, `422` body actual no tipado |
| `POST /api/clearing-houses/{clearingHouseId}/transaction-policies` | `Crear versión de política transaccional` | `201 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `PATCH /api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/metadata` | `Actualizar metadata de política transaccional` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `POST /api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/close` | `Cerrar versión de política transaccional` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `POST /api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/activate` | `Activar versión de política transaccional` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `POST /api/clearing-houses/{clearingHouseId}/transaction-policies/preview` | `Previsualizar prerequisitos transaccionales por cámara` | `200 TransactionPrerequisitePreviewResponse`, `422` body actual no tipado |
| `GET /api/clearing-house-transaction-rules` | `Consultar reglas transaccionales por cámara` | `200 IReadOnlyList<ClearingHouseTransactionRuleDto>` |
| `GET /api/clearing-house-transaction-rules/{id}` | `Consultar regla transaccional` | `200 ClearingHouseTransactionRuleDto`, `404` sin body |
| `POST /api/clearing-house-transaction-rules` | `Crear regla transaccional por cámara` | `201 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `PUT /api/clearing-house-transaction-rules/{id}` | `Actualizar regla transaccional por cámara` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `PATCH /api/clearing-house-transaction-rules/{id}/activate` | `Activar regla transaccional` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |
| `PATCH /api/clearing-house-transaction-rules/{id}/deactivate` | `Desactivar regla transaccional` | `200 ClearingHouseTransactionRuleDto`, `422` body actual no tipado |

#### Contratos XML documentados

- `ClearingHouseTransactionRuleDto`.
- `CreateClearingHouseTransactionRuleRequest`.
- `UpdateClearingHouseTransactionRuleRequest`.
- `TransactionPrerequisitePreviewRequest`.
- `TransactionPrerequisitePreviewResponse`.
- `CreateClearingHouseTransactionPolicyVersionRequest`.
- `UpdateClearingHouseTransactionPolicyMetadataRequest`.
- `CloseClearingHouseTransactionPolicyVersionRequest`.
- No se cambiaron firmas, tipos, nullability, enums, nombres JSON, defaults, serialización, `DataAnnotations` ni validadores.

#### Comportamiento documentado

- La API canónica anidada por cámara lista todas las versiones registradas; `transactionType` es opcional y solo Credit/Debit admiten política canónica. La consulta `current` usa `effectiveAt` o la fecha UTC actual, exige una única versión activa aplicable y devuelve `404` cuando no existe coincidencia.
- La creación canónica deriva los campos de compatibilidad desde `TransactionType` y `PrenotificationMode`, valida metadata y vigencia, puede cerrar la versión anterior al día previo al nuevo inicio y rechaza fechas iniciales duplicadas o solapamientos mediante `InvalidOperationException`, que el controller traduce a `422`.
- La actualización `metadata` no cambia la decisión funcional. `close` fija `EffectiveTo` sin permitir que anteceda al inicio ni alcance la siguiente versión. `activate` rechaza solapamientos activos.
- `preview` usa el `clearingHouseId` de la ruta aunque el body contenga otro valor. Un tipo no aplicable y la ausencia de regla son resultados funcionales `200`; solo una ambigüedad de múltiples reglas vigentes se traduce a `422`.
- En la API de reglas, `includeInactive=false` excluye inactivas. Un `transactionNature` que no puede convertirse a `TransactionNature` no produce error: el servicio actual simplemente no aplica ese filtro.
- En `Update`, un cambio funcional de modo/plazo/vigencia exige una fecha inicial posterior y crea una nueva versión; sin cambio funcional se actualizan metadata y campos de validación del receptor. Los errores capturados por `InvalidOperationException` permanecen en `422`, incluido id inexistente para las operaciones de escritura.

#### Seguridad y taxonomía

- Las catorce operaciones conservan `[Authorize]` y las policies existentes `P1Policies.ConfigRead` o `P1Policies.ConfigManage` según la acción.
- El transformer existente publica Bearer y el tag `Políticas y reglas transaccionales`.
- No se agregaron `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.

#### Quality gates incrementales

1. QG-1 conserva las **57 operaciones heredadas** y agrega exclusivamente las catorce operaciones de SC-207; el total pasa a **71 operaciones**.
2. QG-2 agrega un bloque limitado a estas catorce operaciones y valida tag funcional, Bearer, parámetros path/query, request bodies reales, XML de schemas, response DTO/status/descripciones y ausencia de headers no consumidos.
3. QG-2 protege los status reales y la ausencia de `400/401/403/409/500` especulativos; también evita `404` o `422` donde el controller no los publica. Los `422` documentados corresponden al catch explícito de `InvalidOperationException` y no se presentan como `ProblemDetails`.
4. Se conservan todos los allowlists anteriores. No se agregaron examples y QG-3 permanece sin activar.

#### Validación de SC-207

- Se comprobó la disponibilidad de `dotnet`; no se encontró ejecutable.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: `PENDIENTE — dotnet no disponible`.
- `dotnet build ACHInterbank.sln -c Release`: `PENDIENTE — dotnet no disponible`.
- Se realizó inspección estática dirigida de ambos controllers, `IClearingHouseTransactionRuleService`, `ClearingHouseTransactionRuleService`, `ITransactionPrerequisitePolicyService`, `TransactionPrerequisitePolicyService`, contratos expuestos, policies, transformer de tags y `OpenApiDocumentGenerationTests`.
- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` con 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- La revisión estructural de los cuatro archivos C# modificados confirmó delimitadores balanceados; las firmas/lógica productiva de los controllers y las firmas de DTO permanecen iguales al baseline al excluir únicamente metadata/documentación XML.
- QG-1 conserva las 57 operaciones heredadas sin eliminaciones y agrega exactamente las 14 operaciones de SC-207; QG-2 cubre exactamente ese mismo delta y QG-3 no fue modificado.
- Se revisó el diff completo por hashes contra `ACHInterbank-v124-scalar-r206-SC206.zip`: 5 archivos modificados, 0 agregados y 0 eliminados.
- La ausencia de `dotnet` es una limitación exclusivamente ambiental: SC-207 queda en `VERIFY`, no `BLOCKED`; SC-208 queda en `READY` sin ejecutarse.

### Resultado de SC-208 — Políticas/reglas transaccionales — grupo 2

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Políticas/reglas transaccionales — grupo 2`.  
**Alcance ejecutado:** documentación contractual de `CfaOperationalTransactionRestrictionsController` y `PaymentRailCapabilityRegistryController`, nueve operaciones en total. Después de SC-207 quedaban tres controllers clasificados bajo `Políticas y reglas transaccionales`; la regla permanente de máximo dos controllers por sesión obliga a dividir el remanente. `TransactionPrerequisitePolicyController` queda íntegramente reservado a `SC-209` y no fue modificado. No se cambiaron servicios, persistencia, rutas, policies, reglas ACH/CENIT/NACHA-M, validadores, serialización ni transformers.

#### Operaciones documentadas

| Operación | Summary | Responses documentados |
|---|---|---|
| `GET /api/cfa-operational-transaction-restrictions` | `Consultar restricciones operacionales CFA` | `200 IReadOnlyList<CfaOperationalTransactionRestrictionDto>` |
| `GET /api/cfa-operational-transaction-restrictions/{id}` | `Consultar restricción operacional CFA` | `200 CfaOperationalTransactionRestrictionDto`, `404` sin body |
| `POST /api/cfa-operational-transaction-restrictions` | `Crear restricción operacional CFA` | `201 CfaOperationalTransactionRestrictionDto`, `422 CfaOperationalTransactionRestrictionApiError` |
| `PUT /api/cfa-operational-transaction-restrictions/{id}` | `Actualizar restricción operacional CFA` | `200 CfaOperationalTransactionRestrictionDto`, `404` sin body, `409 CfaOperationalTransactionRestrictionApiError`, `422 CfaOperationalTransactionRestrictionApiError` |
| `PATCH /api/cfa-operational-transaction-restrictions/{id}/activate` | `Activar restricción operacional CFA` | `200 CfaOperationalTransactionRestrictionDto`, `404` sin body, `409 CfaOperationalTransactionRestrictionApiError`, `422 CfaOperationalTransactionRestrictionApiError` |
| `PATCH /api/cfa-operational-transaction-restrictions/{id}/deactivate` | `Desactivar restricción operacional CFA` | `200 CfaOperationalTransactionRestrictionDto`, `404` sin body, `409 CfaOperationalTransactionRestrictionApiError`, `422 CfaOperationalTransactionRestrictionApiError` |
| `GET /api/payment-rails/capability-registry/rails` | `Consultar rieles del registro de capacidades` | `200 IReadOnlyList<PaymentRailRegistryRailItem>` |
| `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities` | `Consultar capacidades efectivas de un riel` | `200 IReadOnlyList<PaymentRailCapabilityRegistryItem>`, `400` body anónimo actual |
| `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}` | `Consultar capacidad efectiva de un riel` | `200 PaymentRailCapabilityRegistryItem`, `400` body anónimo actual, `404` body anónimo actual |

#### Contratos XML documentados

- `CfaOperationalTransactionRestrictionDto`.
- `CreateCfaOperationalTransactionRestrictionRequest`.
- `UpdateCfaOperationalTransactionRestrictionRequest`.
- `ChangeCfaOperationalTransactionRestrictionStatusRequest`.
- `CfaOperationalTransactionRestrictionApiError`.
- `PaymentRailRegistryRailItem`.
- `PaymentRailCapabilityRegistryItem`.
- `PaymentRailCapabilityRegistryState` y sus valores existentes.
- No se cambiaron firmas, tipos, nullability, enums, valores de enums, nombres JSON, defaults, serialización, `DataAnnotations` ni validadores.

#### Comportamiento documentado

- Las restricciones operacionales CFA pueden tener alcance global, directo por cámara o por configuración versionada de ciclo. La consulta administrativa aplica filtros opcionales y no resuelve ni combina límites.
- Para creación y actualización se documentaron las validaciones reales del servicio: alcance de cámara/ciclo, enums definidos, límites numéricos no negativos y rango de vigencia válido. `ExpectedRowVersion` es el token Base64 usado para concurrencia en update/activate/deactivate.
- El controller traduce `CfaOperationalTransactionRestrictionValidationException` a `422 CfaOperationalTransactionRestrictionApiError` y `CfaOperationalTransactionRestrictionConcurrencyException` a `409 CfaOperationalTransactionRestrictionApiError`. Create no documenta 409 porque su flujo actual no usa la ruta de concurrencia del servicio.
- Capability Registry publica un catálogo de rieles y calcula capacidades efectivas por riel. Para una capacidad prioriza un override activo/vigente; cuando no existe, deriva el estado desde la estrategia operacional. `asOfUtc` es opcional y el servicio usa UTC actual cuando se omite.
- `railCode` inválido se captura como `400`; `capabilityCode` fuera del catálogo o no disponible se responde como `404`. Esos errores usan los cuerpos anónimos existentes, por lo que no se inventó un schema contractual.
- Se eliminaron de `PaymentRailCapabilityRegistryController` los `ProducesResponseType` especulativos `401`, `403` y `500` que no son devueltos por las acciones.
- No se documentaron reglas normativas de ACH Colombia o CENIT ni se interpretó Capability Registry como habilitación regulatoria.

#### Seguridad y taxonomía

- Las seis operaciones CFA conservan `[Authorize]` y `FineGrainedPermissions.OperationalRestrictions.Read` o `.Manage` según la acción.
- Las tres consultas de Capability Registry conservan `[Authorize]` y `FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry`.
- El transformer existente publica Bearer y el tag `Políticas y reglas transaccionales` para las nueve operaciones.
- No se agregaron `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.

#### Quality gates incrementales

1. QG-1 conserva las **71 operaciones heredadas** y agrega exclusivamente las nueve operaciones de SC-208; el total pasa a **80 operaciones**.
2. QG-2 amplía el allowlist de políticas/reglas únicamente con esas nueve operaciones y valida Bearer, tag, parámetros path/query, request bodies reales, XML de schemas, responses/status/descripciones y ausencia de headers no consumidos.
3. QG-2 protege la ausencia de status especulativos. Para CFA mantiene 404 solo donde el controller lo devuelve, 409 solo en operaciones con concurrencia y 422 solo en operaciones que capturan validación. Para Capability Registry conserva 400/404 únicamente en las acciones que los producen y elimina 401/403/500 especulativos.
4. QG-3 permanece desactivado y no se agregaron examples.

#### Validación de SC-208

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Se realizó revisión estática de controllers, servicios llamados, DTOs, policies, transformer de tags y `OpenApiDocumentGenerationTests`; QG-1 pasa de 71 a 80 operaciones con un delta exacto de nueve.
- Se revisó el diff por contenido contra `ACHInterbank-v124-scalar-r207-SC207.zip`; únicamente pueden permanecer modificados los seis archivos registrados para SC-208.
- La ausencia de `dotnet` es una limitación exclusivamente ambiental: SC-208 queda en `VERIFY`, no `BLOCKED`; SC-209 queda en `READY` sin ejecutarse.

### Resultado de SC-209 — Políticas/reglas transaccionales — grupo 3

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Políticas/reglas transaccionales — grupo 3 (TransactionPrerequisitePolicyController)`.  
**Alcance ejecutado:** documentación contractual exclusiva de `TransactionPrerequisitePolicyController`, una operación en total. No se modificaron otros controllers, servicios, persistencia, reglas de negocio, rutas, autorización, validadores, serialización, transformers ni contratos funcionales.

#### Operación documentada

| Operación | Summary | Responses documentados |
|---|---|---|
| `POST /api/transaction-prerequisite-policy/preview` | `Previsualizar prerequisitos transaccionales` | `200 TransactionPrerequisitePreviewResponse` |

#### Contratos XML cubiertos

- `TransactionPrerequisitePreviewRequest`.
- `TransactionPrerequisitePreviewResponse`.
- Ambos contratos ya estaban documentados en `ClearingHouseTransactionRuleDtos.cs` por SC-207 y se reutilizan sin cambios; no fue necesario modificar DTOs en SC-209.
- No se cambiaron firmas, tipos, nullability, enums, valores de enums, nombres JSON, defaults, serialización, `DataAnnotations` ni validadores.

#### Comportamiento documentado

- La operación evalúa la política con `ClearingHouseId`, `TransactionType`, `EffectiveEntryDate` y `AppliesToNachaExport` del body y no persiste cambios.
- Los tipos que no resuelven a Credit o Debit devuelven `200` con `Decision = NOT_APPLICABLE`.
- La ausencia de una regla vigente coincidente devuelve `200` con `Decision = RULE_NOT_CONFIGURED`.
- Cuando existe una regla aplicable, el response expone el modo de prenotificación y la configuración de validación de identificación del receptor almacenados en esa regla.
- No se publican `400/401/403/404/409/422/500` por convención. Conforme al patrón aprobado en SC-103, no se infiere `500` desde `GlobalExceptionMiddleware`.

#### Seguridad y taxonomía

- La operación conserva `[Authorize]` y `[Authorize(Policy = "CanReadAch")]`.
- El transformer existente publica Bearer y el tag `Políticas y reglas transaccionales`.
- No se agregaron `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.

#### Quality gates incrementales

1. QG-1 conserva las **80 operaciones heredadas** y agrega exclusivamente `POST /api/transaction-prerequisite-policy/preview`; el total pasa a **81 operaciones**.
2. QG-2 amplía el allowlist de políticas/reglas únicamente con esta operación y valida Bearer, tag, request body `TransactionPrerequisitePreviewRequest`, XML de sus propiedades, response `200 TransactionPrerequisitePreviewResponse`, XML del response y descripción explícita.
3. QG-2 protege la ausencia de `400/401/403/404/409/422/500` especulativos y de parámetros `Authorization`, `Idempotency-Key` y `X-Correlation-ID`.
4. QG-3 permanece desactivado y no se agregaron examples.

#### Validación de SC-209

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Se realizó revisión estática dirigida del controller, `ITransactionPrerequisitePolicyService`, `TransactionPrerequisitePolicyService`, contratos expuestos, policy, transformer de tags y `OpenApiDocumentGenerationTests`.
- QG-1 pasa de 80 a 81 operaciones con un delta exacto de una operación.
- La ausencia de `dotnet` es una limitación exclusivamente ambiental: SC-209 queda en `VERIFY`, no `BLOCKED`.
- El plan no define `SC-210`; la siguiente unidad planificada es `SC-290`, que queda `READY` sin ejecutarse.

### Resultado de SC-290 — checkpoint de Fase C

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Checkpoint Fase C`.  
**Tipo de unidad:** checkpoint acumulado; no documenta endpoints nuevos ni modifica contratos OpenAPI.  
**Implementación de Fase C:** `COMPLETADA`.  
**Validación ejecutable:** `PENDIENTE` exclusivamente por ausencia de `dotnet`.  
**Bloqueo para Fase D:** `NO`.

#### Alcance y métricas del checkpoint

1. Se revisó el estado acumulado de SC-201 a SC-209 sin reabrir ni reinterpretar sus contratos funcionales.
2. QG-1 conserva **81 operaciones únicas**, sin duplicados ni eliminaciones. Fase C incorporó **56** de esas operaciones.
3. QG-2 conserva **77 operaciones únicas** en total. Las **56 operaciones de Fase C** están cubiertas también por QG-2; no se añadieron excepciones ni allowlists en SC-290.
4. Los 12 controllers documentados en Fase C conservan **56 `EndpointSummary`** y **56 `EndpointDescription`**, uno por cada operación de la fase.
5. QG-3 permanece desactivado. SC-290 no agrega examples, status codes, schemas, headers ni metadata nueva.
6. No fue necesaria ninguna corrección de código ni de documentación funcional; el único archivo modificado por el checkpoint es este tracker.

#### Validación de SC-290

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- Suite completa backend prevista para checkpoints: no ejecutada por ausencia de `dotnet`; no se simuló ningún resultado.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Revisión estática de `OpenApiDocumentGenerationTests`: QG-1 = 81 únicas; QG-2 = 77 únicas; las 56 operaciones de Fase C están en ambos gates; no existen símbolos QG-3 activos en la prueba.
- Revisión estructural de 35 archivos C# de Fase C/configuración OpenAPI: delimitadores balanceados y sin marcadores de merge.

La ausencia de `dotnet` es una limitación ambiental, no un bloqueo de implementación. SC-290 queda en `VERIFY`. El plan no define SC-210 ni SC-291. La siguiente unidad real es `SC-301`, que queda `READY` sin ejecutarse.

### Resultado de SC-301 — Incoming NACHA Command Center — grupo 1

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Incoming NACHA Command Center — grupo 1`.  
**Controller:** `IncomingNachaCommandCenterController`.  
**Criterio de partición aplicado:** las cuatro operaciones que SC-008 había incorporado explícitamente a QG-1 y que seguían siendo las únicas de QG-1 no cubiertas por QG-2. Las demás operaciones del controller quedan fuera de SC-301 y reservadas a SC-302.

#### Operaciones documentadas

1. `GET /incoming-nacha-command-center/ingestions/{ingestionId}/validations`
   - `200` → colección `IncomingNachaValidationDto`.
   - `404` → sin cuerpo cuando la ingesta no existe.
2. `GET /incoming-nacha-command-center/ingestions/{ingestionId}/batches`
   - `200` → `IncomingNachaPageResult<IncomingNachaBatchDto>`.
   - `Page` se normaliza a mínimo 1 y `PageSize` se limita a 1..200; filtros y ordenamiento documentados según el servicio.
3. `GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions`
   - `200` → `IncomingNachaPageResult<IncomingNachaTransactionDto>`.
   - Paginación, filtros y ordenamiento se describen según `IncomingNachaCommandCenterService`.
4. `GET /incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas`
   - `200` → colección `IncomingNachaAddendaDto`.
   - La consulta exige coincidencia de ingesta y detalle; devuelve colección vacía cuando no hay adendas y conserva el enmascaramiento aplicado por el servicio.

No se documentaron `400/401/403/409/422/500` por convención. Las cuatro operaciones conservan `P1Policies.CommandCenterRead`, seguridad Bearer y tag canónico `NACHA-M`.

#### DTOs/schemas documentados

- `IncomingNachaPageResult<T>`
- `IncomingNachaValidationDto`
- `IncomingNachaBatchQuery`
- `IncomingNachaBatchDto`
- `IncomingNachaTransactionQuery`
- `IncomingNachaTransactionDto`
- `IncomingNachaAddendaDto`

Solo se agregaron/mejoraron XML docs; no se modificaron tipos, nullability, defaults, serialización, enums, DataAnnotations ni validadores.

#### Quality gates de SC-301

1. **QG-1:** sin cambios. Las cuatro operaciones ya pertenecían al allowlist desde SC-008. Delta `0`; total `81` operaciones únicas.
2. **QG-2:** se agregan exactamente las cuatro operaciones de SC-301. Se validan Bearer, tag `NACHA-M`, parámetros reales, ausencia de request body, schemas/XML docs, responses y descripciones explícitas, además de la ausencia de `Authorization`, `Idempotency-Key`, `X-Correlation-ID` y status especulativos. Delta `+4`; total `81` operaciones únicas.
3. **QG-3:** permanece desactivado. No se agregaron examples.

#### Validación de SC-301

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Inspección estática dirigida del controller, DTOs, servicio, policies, tag transformer y quality gates: sin cambios funcionales y con delta QG esperado.

La ausencia de `dotnet` es una limitación ambiental, no un bloqueo de implementación. SC-301 queda en `VERIFY`. La siguiente unidad real es `SC-302`, que queda `READY` sin ejecutarse.

### Resultado de SC-302 — Incoming NACHA Command Center — grupo 2

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Incoming NACHA Command Center — grupo 2`.  
**Controller:** `IncomingNachaCommandCenterController`.  
**Criterio de partición aplicado:** las 13 operaciones restantes del controller que SC-301 dejó expresamente fuera del grupo 1 y reservó a SC-302. No se documentaron controllers ni rutas de unidades posteriores.

#### Operaciones documentadas

1. `GET /incoming-nacha-command-center/observability/summary`
   - `200` → `IncomingNachaObservabilitySummaryDto`.
   - `windowHours` se normaliza por el servicio al rango `1..168`.
2. `GET /incoming-nacha-command-center/ingestions`
   - `200` → `IncomingNachaPageResult<IncomingNachaIngestionListItemDto>`.
   - `Page` se normaliza a mínimo `1` y `PageSize` a `1..200`; filtros y ordenamiento reflejan el servicio actual.
3. `GET /incoming-nacha-command-center/ingestions/{ingestionId}`
   - `200` → `IncomingNachaIngestionDetailDto`.
   - `404` → sin cuerpo cuando no existe la ingesta.
4. `GET /incoming-nacha-command-center/orphans`
   - `200` → `IncomingNachaPageResult<IncomingNachaOrphanDto>`.
   - Lista vínculos no finales; `Page` se normaliza a mínimo `1` y `PageSize` a `1..100`.
5. `GET /incoming-nacha-command-center/orphans/{linkId}`
   - `200` → `IncomingNachaOrphanDto`.
   - `404` → sin cuerpo cuando no existe el vínculo.
6. `GET /incoming-nacha-command-center/orphans/{linkId}/candidates`
   - `200` → colección `IncomingNachaOrphanCandidateDto`.
   - `404` → sin cuerpo cuando no existe el vínculo.
7. `POST /incoming-nacha-command-center/orphans/{linkId}/resolve`
   - `200` → `IncomingNachaOrphanManualResolutionResult` cuando se aplica la relación o es replay equivalente.
   - `400` → body anónimo actual para solicitud/evidencia inválida.
   - `404` → body anónimo actual cuando falta vínculo o transacción seleccionada.
   - `409` → body anónimo actual para estados/decisiones no resolubles.
8. `GET /incoming-nacha-command-center/queue`
   - `200` → `IncomingNachaPageResult<IncomingNachaQueueListItemDto>`.
   - `Page` se normaliza a mínimo `1` y `PageSize` a `1..200`.
9. `GET /incoming-nacha-command-center/queue/{queueId}`
   - `200` → `IncomingNachaQueueDetailDto`.
   - `404` → sin cuerpo cuando no existe el ítem.
10. `POST /incoming-nacha-command-center/queue/{queueId}/retry`
    - `200` → `IncomingNachaManualActionResultDto`.
    - `409` → body anónimo actual para validaciones, ítem inexistente o transición no permitida.
11. `POST /incoming-nacha-command-center/queue/{queueId}/unblock`
    - `200` → `IncomingNachaManualActionResultDto`.
    - `409` → body anónimo actual para validaciones, ítem inexistente o transición no permitida.
12. `POST /incoming-nacha-command-center/queue/{queueId}/requeue`
    - `200` → `IncomingNachaManualActionResultDto`.
    - `409` → body anónimo actual para validaciones, ítem inexistente o transición no permitida.
13. `POST /incoming-nacha-command-center/queue/{queueId}/mark-failed-final`
    - `200` → `IncomingNachaManualActionResultDto`.
    - `409` → body anónimo actual para validaciones, ítem inexistente o transición no permitida.

No se documentaron `401/403/500` por convención. Tampoco se inventaron `ProblemDetails` para cuerpos anónimos. Se conservaron las policies reales: `P1Policies.CommandCenterRead`, `P1Policies.CommandCenterRetry`, `P1Policies.CommandCenterUnblock`, `P1Policies.CommandCenterRequeue`, `P1Policies.CommandCenterMarkFailedFinal` y `CanManageAch` en la resolución manual. Todas las operaciones conservan seguridad Bearer y tag canónico `NACHA-M`.

#### DTOs/schemas documentados

- `IncomingNachaIngestionQuery`
- `IncomingNachaIngestionListItemDto`
- `IncomingNachaIngestionDetailDto`
- `IncomingNachaFileSummaryDto`
- `IncomingNachaQueueQuery`
- `IncomingNachaQueueListItemDto`
- `IncomingNachaQueueDetailDto`
- `IncomingNachaEntryClassificationDto`
- `IncomingNachaIntegrationExecutionDto`
- `IncomingNachaOrphanQuery`
- `IncomingNachaOrphanDto`
- `IncomingNachaOrphanCandidateDto`
- `IncomingNachaOrphanResolveRequestDto`
- `IncomingNachaProcessingEventDto`
- `IncomingNachaManualActionRequest`
- `IncomingNachaManualActionResultDto`
- `IncomingNachaObservabilitySummaryDto`
- `IncomingNachaPipelineHealthDto`
- `IncomingNachaKpiCountDto`
- `IncomingNachaClearingCycleKpiDto`
- `IncomingNachaTopErrorDto`
- `IncomingNachaTimelinePointDto`
- `IncomingNachaAllowedActionsDto`
- `IncomingNachaOrphanManualResolutionResult`
- `IncomingNachaAdmissionIssue`

Solo se agregaron/mejoraron XML docs; no se modificaron tipos, nullability, defaults, serialización, enums, DataAnnotations ni validadores.

#### Quality gates de SC-302

1. **QG-1:** se agregan exactamente las 13 operaciones de SC-302. Delta `+13`; total `94` operaciones únicas.
2. **QG-2:** se agregan exactamente las mismas 13 operaciones. Se validan Bearer, tag `NACHA-M`, parámetros reales, request body en las cinco operaciones POST, schemas/XML docs, responses y descripciones explícitas, y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID` como parámetros. Los guards de status impiden reintroducir respuestas especulativas. Delta `+13`; total `94` operaciones únicas.
3. **QG-3:** permanece desactivado. No se agregaron examples.

#### Validación de SC-302

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Inspección estática dirigida: rutas/verbos/policies y cuerpos de acción preservados; cambios de DTO limitados a XML docs; QG-1 = 94 únicas y QG-2 = 94 únicas, sin duplicados.

La ausencia de `dotnet` es una limitación ambiental, no un bloqueo de implementación. SC-302 queda en `VERIFY`. La siguiente unidad real del plan es `SC-303 — Definiciones/layouts/configuración NACHA`, que queda `READY` sin ejecutarse.

### Delimitación de SC-303 — Definiciones/layouts/configuración NACHA

**Estado de delimitación:** `READY`  
**Objetivo macro original del plan:** `Definiciones/layouts/configuración NACHA`.  
**Naturaleza de esta sesión:** planificación/tracker exclusivamente; no implementa SC-303, no modifica OpenAPI y no ejecuta ninguna unidad posterior.

#### Regla de partición aplicable

`DEC-002` del **tracker Scalar/OpenAPI** establece literalmente: **“Se trabajará como máximo 2 controllers o 15 endpoints por sesión.”**  
La sección 3.1 refuerza que una sesión normal debe respetar **todos** estos límites y ordena: **“Si una unidad supera cualquiera de estos límites, debe dividirse antes de implementarla.”**  
Para Fase C el tracker ya dejó además el precedente operativo: **“Si un controller excede el límite, dividirlo por grupo de acciones.”**

Existe también un `DEC-002` homónimo en `docs/uat/SECURITY_COMPLIANCE_DECISION_MATRIX.md` (“Aprobar canal seguro de intercambio”). Esa decisión pertenece al paquete UAT de Seguridad/Compliance y **no** gobierna la partición del plan Scalar; para SC-303 la autoridad de partición es el `DEC-002` de este tracker.

`DEC-001` impide resolver la partición mediante heurísticas de nombre/ruta. Por tanto, la delimitación siguiente usa evidencia estructural del código y no solo afinidad nominal.

#### Inventario estático verificado

| Superficie | Operaciones lógicas | Rutas/acciones | Contratos directos principales | Evidencia de cohesión | QG-1 / QG-2 actual |
|---|---:|---|---|---|---|
| `NachaRecordDefinitionsController` | 5 | `GET /nacha-record-definitions`; `GET /nacha-record-definitions/{id}`; `POST /nacha-record-definitions`; `PUT /nacha-record-definitions/{id}`; `DELETE /nacha-record-definitions/{id}`. El controller conserva alias `/api/nacha-record-definitions`. | `NachaRecordDefinitionDto`, `NachaRecordSourceType` | Controller `[Obsolete]`; declara explícitamente que es diagnóstico/legacy y que la configuración oficial usa `nacha-config profiles`. Usa `INachaRecordDefinitionAppService`. | `0 / 0` |
| `NachaRecordLayoutsController` | 5 | `GET /nacha-layouts`; `GET /nacha-layouts/{id}`; `POST /nacha-layouts`; `PUT /nacha-layouts/{id}`; `DELETE /nacha-layouts/{id}`. El controller conserva alias `/api/nacha-layouts`. | `NachaRecordLayoutDto`, `NachaRecordFieldDto` | Mismo patrón `[Obsolete]` y misma declaración de reemplazo por `nacha-config profiles`; CRUD/diagnóstico legacy mediante `INachaRecordLayoutAppService`. | `0 / 0` |
| `NachaConfigProfilesController` | 17 | 5 consultas (`perfiles`, `catalogos-filtro`, detalle, historial, snapshots) y 12 operaciones de creación/edición/transición/preview (`POST/PUT`). | Familia `NachaConfig*` de `NachaConfigAdminDtos.cs`, incluido `NachaConfigProfileDetailDto`, requests de draft/update/clone/transición/secuencia/layout/field/rule, resultados de validación/publicación/preview y `NachaConfigApiErrorDto`. | Superficie oficial de configuración; usa `P1Policies.ConfigRead/ConfigManage` y seis servicios especializados de query, command, validación, publicación, historia y preview. Un solo controller ya excede el máximo de 15 operaciones. | `0 / 0` |
| `NachaConfigProfilesReadOnlyController` | 6 | `GET dashboard`; `GET` perfiles; `GET {id}`; `GET by-code/{profileCode}`; `GET {id}/variants`; `GET {id}/fields`. | `NachaConfigProfilesDashboardReadModel`, `NachaConfigProfileReadModel`, `NachaConfigProfileDetailReadModel`, `NachaConfigProfileVariantReadModel`, `NachaConfigProfileFieldReadModel`. | Read model oficial separado, solo lectura, con `P1Policies.NachaRead` y `INachaConfigProfileReadModelService`. | `0 / 0` |

La búsqueda dirigida de rutas/controllers confirma que estas cuatro son las superficies cuyo root contractual corresponde directamente a `nacha-record-definitions`, `nacha-layouts`, `nacha-config` o `api/ach/nacha/config-profiles`. Otros controllers NACHA pertenecen a objetivos diferentes ya nombrados en Fase D —por ejemplo seguridad, uploads/downloads, rechazo/respuesta o CENIT— y no se incorporan a SC-303.

#### Alcance exacto asignado a SC-303

SC-303 queda **READY** para una implementación posterior limitada exclusivamente a:

1. `NachaRecordDefinitionsController` — 5 operaciones lógicas.
2. `NachaRecordLayoutsController` — 5 operaciones lógicas.

**Total de SC-303: 2 controllers y 10 operaciones**, dentro de DEC-002.

Contratos/schemas directos incluidos en ese alcance:

- `NachaRecordDefinitionDto`
- `NachaRecordSourceType`
- `NachaRecordLayoutDto`
- `NachaRecordFieldDto`

Los aliases `/api/nacha-record-definitions` y `/api/nacha-layouts` siguen siendo parte del routing existente, pero no se contabilizan como acciones adicionales para DEC-002, de acuerdo con el criterio histórico del tracker que cuenta acciones lógicas y deja el inventario general de aliases a `SC-501`. La futura implementación deberá conservar ambos aliases sin crear ni retirar rutas.

**Criterio verificable de cierre de SC-303 cuando se implemente:**

- documentar únicamente las 10 acciones anteriores con comportamiento real;
- preservar su carácter legacy/deprecated y la configuración oficial separada;
- documentar los cuatro contratos directos solo mediante XML docs si hace falta, sin cambiar tipos/serialización/validación;
- incorporar deliberadamente las 10 rutas canónicas al alcance de QG-1/QG-2 conforme a DEC-020/DEC-025, sin tocar operaciones ajenas;
- mantener QG-3/examples sin cambios salvo mandato explícito posterior;
- no modificar servicios, persistencia, reglas NACHA, autorización ni comportamiento productivo.

#### Exclusiones explícitas de SC-303

Quedan fuera de SC-303:

- todo `NachaConfigProfilesController`;
- todo `NachaConfigProfilesReadOnlyController`;
- cualquier otro controller NACHA;
- servicios, persistencia, validadores y layouts productivos;
- `SC-304` y cualquier unidad posterior;
- activación de QG-3 y examples.

#### Reserva de la configuración oficial NACHA sin inventar unidades

El plan actual no contiene una unidad posterior específica para completar la configuración oficial NACHA antes de `SC-304`, y `SC-304` ya tiene el objetivo distinto `Operaciones de rechazo/respuesta de archivos`. Por tanto, **no se crean IDs nuevos ni se reutiliza SC-304**.

La reserva de trabajo queda expresada únicamente como partición de planificación, todavía **sin IDs y no ejecutable**:

1. **Reserva configuración — lectura:** los 5 `GET` de `NachaConfigProfilesController` + las 6 operaciones de `NachaConfigProfilesReadOnlyController` = **11 operaciones, 2 controllers**.
2. **Reserva configuración — gestión/mutación:** las 12 operaciones no-GET restantes de `NachaConfigProfilesController` = **12 operaciones, 1 controller**.

Ambos grupos cumplen DEC-002 y responden a una separación demostrable read/query frente a command/mutation. Antes de ejecutar cualquiera de ellos, una sesión de planificación deberá asignarles unidades reales dentro del plan. Esta reserva no cambia el propósito ni el estado de `SC-304`.

#### Quality gates y examples de esta sesión de delimitación

- **QG-1:** delta `0`; permanece en `94` operaciones únicas.
- **QG-2:** delta `0`; permanece en `94` operaciones únicas.
- **QG-3:** permanece `DESACTIVADO`.
- **Examples:** `0`; no se agregaron.
- No se modificó `OpenApiDocumentGenerationTests.cs`.

#### Validación de la delimitación

- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Recuento estático de `Qg1Operations`: `94` entradas y `94` pares `METHOD + PATH` únicos.
- Recuento estático de los arrays QG-2: `94` operaciones en total (`17 + 4 + 17 + 9 + 6 + 3 + 14 + 24`), sin modificar el test.
- Búsqueda de QG-3 en `OpenApiDocumentGenerationTests.cs`: no existe gate QG-3 activo.
- Comparación por SHA-256 contra `ACHInterbank-v124-scalar-r303-SC303.zip`: únicamente este tracker cambia; `0` archivos agregados y `0` eliminados.
- No se ejecutaron `dotnet test` ni `dotnet build` porque esta sesión no modifica C#.

#### Estado de continuidad

SC-303 queda `READY` **sin haberse implementado**.  
`SC-304 — Operaciones de rechazo/respuesta de archivos` permanece `TODO`, conserva su objetivo original y no fue ejecutado. La reserva de configuración oficial NACHA no tiene IDs de unidad y no puede ejecutarse hasta una decisión explícita de planificación.

### Implementación SC-303 — Definiciones/layouts NACHA legacy

**Estado:** `VERIFY` — implementación documental completa; validación ejecutable .NET pendiente por ausencia de `dotnet`.

**Alcance ejecutado:** exclusivamente `NachaRecordDefinitionsController` y `NachaRecordLayoutsController`, 5 acciones lógicas por controller, 10 en total. Se conservaron los aliases `/api/nacha-record-definitions` y `/api/nacha-layouts` sin agregarlos como unidades adicionales de QG.

**Operaciones documentadas:**

- `GET /nacha-record-definitions` → `200 IEnumerable<NachaRecordDefinitionDto>`.
- `GET /nacha-record-definitions/{id}` → `200 NachaRecordDefinitionDto` / `404` sin body.
- `POST /nacha-record-definitions` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.
- `PUT /nacha-record-definitions/{id}` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.
- `DELETE /nacha-record-definitions/{id}` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.
- `GET /nacha-layouts` → `200 IEnumerable<NachaRecordLayoutDto>`.
- `GET /nacha-layouts/{id}` → `200 NachaRecordLayoutDto` / `404` sin body.
- `POST /nacha-layouts` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.
- `PUT /nacha-layouts/{id}` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.
- `DELETE /nacha-layouts/{id}` → `410` con body anónimo `{ codigo, mensaje }`; la acción no invoca el servicio.

**Contratos/schemas documentados mediante XML docs:** `NachaRecordDefinitionDto`, `NachaRecordSourceType`, `NachaRecordLayoutDto`, `NachaRecordFieldDto`. No se cambiaron tipos, nullability, valores de enum, defaults, serialización, validadores, DataAnnotations ni comportamiento.

**Quality gates:** las 10 rutas canónicas no estaban presentes en QG-1 ni QG-2. QG-1 agrega exactamente 10 (`94 -> 104`) y QG-2 agrega exactamente 10 (`94 -> 104`), con Bearer, tag `NACHA-M`, parámetros/body reales, schemas XML, responses/descripciones y guards contra headers/status especulativos. QG-3 permanece `DESACTIVADO`; examples nuevos = `0`.

**Archivos modificados:**

- `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/ACH/Dtos/NachaRecordDefinitionDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/ACH/Dtos/NachaRecordLayoutDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/ACH/Dtos/NachaRecordFieldDto.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/NachaRecordSourceType.cs`
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`

**Validación:** `dotnet --info` exit `127` (`dotnet: command not found`); no se ejecutaron ni simularon `OpenApiDocumentGenerationTests` ni `dotnet build`. `python scripts/validate-solution-projects.py` exit `0` (`SOLUTION GRAPH: OK`). Inspección estática: delimitadores balanceados, rutas/verbs/policies preservados, cuerpos de acción productivos sin cambios, QG-1 = 104 únicas y QG-2 = 104 únicas sin duplicados.

**Exclusiones preservadas:** no se modificaron `NachaConfigProfilesController`, `NachaConfigProfilesReadOnlyController`, servicios, persistencia, validadores, layouts productivos, parsing/generación NACHA, configuración oficial, QG-3 ni examples. Las reservas de configuración oficial continúan sin IDs ejecutables.

**Continuidad:** `SC-304 — Operaciones de rechazo/respuesta de archivos` queda `BLOCKED` hasta que el plan delimite explícitamente su alcance. No fue ejecutada.

### Resultado de SC-304 — `Operaciones de rechazo/respuesta de archivos`

**Estado:** `VERIFY`  
**Alcance autorizado:** exclusivamente `AchResponseOperationsController`, 1 controller y 14 operaciones reales. **CUMPLE DEC-002** (máximo 2 controllers o 15 endpoints). La delimitación resuelve expresamente el bloqueo previo por falta de asignación y no modifica el título funcional de SC-304.

#### Delimitación autorizada y exclusiones

SC-304 incluye únicamente las 14 acciones de `AchResponseOperationsController`:

1. `GET /api/ach/response-status-mappings/{id}`
2. `POST /api/ach/response-status-mappings`
3. `PUT /api/ach/response-status-mappings/{id}`
4. `POST /api/ach/response-status-mappings/{id}/activate`
5. `POST /api/ach/response-status-mappings/{id}/deactivate`
6. `GET /api/ach/response-status-mappings/{id}/audit`
7. `GET /api/ach/responses/{id}/audit`
8. `GET /api/ach/responses/orphans`
9. `POST /api/ach/responses/{id}/orphan`
10. `POST /api/ach/responses/orphans/{id}/review/start`
11. `POST /api/ach/responses/orphans/{id}/resolve`
12. `POST /api/ach/responses/{id}/reprocess`
13. `GET /api/ach/responses/{id}/reprocess-attempts`
14. `GET /api/ach/responses/{id}/reprocess-attempts/{attemptId}`

No existen aliases de ruta adicionales en este controller. Quedan expresamente fuera `AchOutboundReturnTransportController`, `RegulatoryCatalogsController`, cualquier otro controller, SC-305, SC-309, Reportes, catálogos no incluidos y la configuración NACHA reservada. No se asignan IDs al trabajo remanente.

#### Contratos y documentación aplicada

Se agregaron `EndpointSummary`, `EndpointDescription`, parámetros XML, `ProducesResponseType` y descripciones explícitas de responses según el comportamiento actual. Se documentaron mediante XML docs, sin alterar tipos, nullability, defaults, nombres JSON, validación ni serialización:

- Requests: `AchResponseMappingWriteRequest`, `VersionedReasonRequest`, `CreateOrphanRequest`, `ResolveOrphanRequest`, `ReprocessResponseRequest`.
- Responses: `AchResponseMappingModel`, `AchResponseAuditModel`, `AchResponseOrphanModel`, `AchResponseReprocessModel`.

El tag se conserva por la taxonomía existente como `Devoluciones y respuestas`. Las 14 operaciones conservan Bearer mediante la metadata `[Authorize]`. Las operaciones de escritura que realmente consumen correlación publican el header opcional `X-Correlation-ID` por el transformer existente; no se publica `Authorization` como parámetro ni `Idempotency-Key`. No se agregaron 401/403 por autorización ni otros status especulativos.

#### Responses reales documentadas

- `GET /api/ach/response-status-mappings/{id}` → `200 AchResponseMappingModel`, `404 ProblemDetails`.
- `POST /api/ach/response-status-mappings` → `201 AchResponseMappingModel`, `400 ProblemDetails`, `409 ProblemDetails`.
- `PUT /api/ach/response-status-mappings/{id}` → `200 AchResponseMappingModel`, `400/404/409 ProblemDetails`.
- `POST .../{id}/activate` → `200 AchResponseMappingModel`, `400/404/409 ProblemDetails`.
- `POST .../{id}/deactivate` → `200 AchResponseMappingModel`, `400/404/409 ProblemDetails`.
- `GET .../{id}/audit` (mapping) → `200 IReadOnlyList<AchResponseAuditModel>`.
- `GET /api/ach/responses/{id}/audit` → `200 IReadOnlyList<AchResponseAuditModel>`.
- `GET /api/ach/responses/orphans` → `200 IReadOnlyList<AchResponseOrphanModel>`.
- `POST /api/ach/responses/{id}/orphan` → `200 AchResponseOrphanModel`, `400/404/409 ProblemDetails`.
- `POST /api/ach/responses/orphans/{id}/review/start` → `200 AchResponseOrphanModel`, `400/404/409 ProblemDetails`.
- `POST /api/ach/responses/orphans/{id}/resolve` → `200 AchResponseOrphanModel`, `400/404/409 ProblemDetails`.
- `POST /api/ach/responses/{id}/reprocess` → `202 AchResponseReprocessModel`, `400/404/409 ProblemDetails`.
- `GET /api/ach/responses/{id}/reprocess-attempts` → `200 IReadOnlyList<AchResponseReprocessModel>`.
- `GET /api/ach/responses/{id}/reprocess-attempts/{attemptId}` → `200 AchResponseReprocessModel`, `404 ProblemDetails`.

No se introdujeron schemas artificiales para bodies anónimos; SC-304 no contiene nuevos bodies anónimos que requieran modelado.

#### Quality gates

- **QG-1:** 14 operaciones SC-304; ya presentes en baseline = `0`; nuevas = `14`; delta = `+14`; total `104 -> 118` operaciones únicas.
- **QG-2:** 14 operaciones SC-304; ya presentes en baseline = `0`; nuevas = `14`; delta = `+14`; total `104 -> 118` operaciones únicas. El gate valida Bearer, tag, parámetros, request bodies, schemas/XML docs, responses y `X-Correlation-ID` solo en las ocho acciones que lo consumen; protege contra `Authorization`/`Idempotency-Key` falsos y compara exactamente los status publicados por cada operación SC-304.
- **QG-3:** permanece `DESACTIVADO`.
- **Examples:** `0` nuevos.

#### Validación técnica y control de cambios

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `OpenApiDocumentGenerationTests`: no ejecutado ni simulado porque `dotnet` no está disponible.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por la misma limitación.
- `python scripts/validate-solution-projects.py`: exit `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y referencias resueltas.
- Revisión estática: delimitadores C# balanceados; exactamente 14 acciones públicas en el controller; rutas, verbos y policies preservados; implementaciones de las 14 acciones comparadas contra r304 y sin cambios.
- Recuento estático: QG-1 = `118` entradas / `118` pares únicos; QG-2 = `118` operaciones únicas; las 14 de SC-304 eran `0` en ambos gates del baseline y quedaron `14` en ambos.
- QG-3 sigue sin gate activo; no se agregaron metadata/providers de examples.
- Cambios contra `ACHInterbank-v124-scalar-r304-SC304.zip`: 5 archivos modificados, 0 agregados, 0 eliminados.

**Archivos modificados:**

- `src/Cfa.ACHInterbank.Api/Controllers/AchResponseOperationsController.cs`
- `src/Cfa.ACHInterbank.Api/Contracts/AchResponses/AchResponseOperationsContracts.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Responses/Operations/AchResponseOperationModels.cs`
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`

**Exclusiones confirmadas:** `AchOutboundReturnTransportController` no cambió; `RegulatoryCatalogsController` no cambió; la configuración NACHA reservada no cambió. SC-305 y SC-309 no fueron ejecutados. SC-305 conserva el estado `TODO` y el objetivo `CENIT Operations`.

**Limitación real:** la implementación está completa, pero falta validación ejecutable .NET por ausencia de `dotnet`; conforme a la instrucción de la unidad y DEC-009, SC-304 queda `VERIFY`, no `BLOCKED`.

### Resultado de SC-305 — `CENIT Operations`

**Estado:** `VERIFY` — delimitación humana autorizada incorporada e implementación contractual completa. La única validación pendiente es .NET, no disponible en este entorno.

#### Delimitación autorizada y DEC-002

- **Controller:** `CenitOperationsController`.
- **Controllers:** `1`.
- **Operaciones:** `7`.
- **Resultado DEC-002:** `CUMPLE` — dentro del máximo de 2 controllers o 15 endpoints.
- Operaciones autorizadas y documentadas: `POST /api/cenit/chamber-responses`; `GET /api/cenit/chamber-responses/{id}`; `GET /api/cenit/chamber-responses`; `GET /api/cenit/queues`; `GET /api/cenit/net-positions`; `GET /api/cenit/optimization-decisions`; `GET /api/cenit/traceability`.
- La acción de detalle usa en código `[HttpGet("chamber-responses/{id:guid}")]`; OpenAPI conserva la forma canónica `/api/cenit/chamber-responses/{id}`. No existen aliases adicionales en el controller.
- Exclusiones preservadas: `AchOutboundReturnTransportController`, `RegulatoryCatalogsController`, `AchResponseOperationsController`, superficies de `SC-309`, `NachaConfigProfilesController`, `NachaConfigProfilesReadOnlyController`, configuración NACHA reservada y cualquier otro controller.

#### Contrato documentado

- Tag funcional: `CENIT` en las 7 operaciones.
- Security: Bearer en las 7 operaciones por metadata real de autorización.
- Policies preservadas: `CanManageAch` para `POST /api/cenit/chamber-responses`; `CanReadAch` para las seis operaciones `GET`.
- No se documentan `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.
- DTOs/schemas directamente alcanzados y documentados mediante XML: `CenitChamberResponseImportCommand`, `CenitChamberResponseResult`, `CenitChamberResponsePage`.
- Las respuestas de `queues`, `net-positions`, `optimization-decisions` y `traceability` siguen siendo bodies anónimos del controller; no se inventaron DTOs ni schemas artificiales.
- No se modificaron tipos, nullability, defaults, nombres JSON, serialización, required/optional, enums, DataAnnotations ni validadores.

#### Responses reales

- `POST /api/cenit/chamber-responses`: `201 CenitChamberResponseResult`, `200 CenitChamberResponseResult`, `400 ProblemDetails`, `409 ProblemDetails`, `422 ProblemDetails`, `503 ProblemDetails`.
- `GET /api/cenit/chamber-responses/{id}`: `200 CenitChamberResponseResult`, `404` sin body.
- `GET /api/cenit/chamber-responses`: `200 CenitChamberResponsePage`.
- `GET /api/cenit/queues`: `200` con body anónimo paginado.
- `GET /api/cenit/net-positions`: `200` con body anónimo paginado.
- `GET /api/cenit/optimization-decisions`: `200` con body anónimo paginado.
- `GET /api/cenit/traceability`: `200` con body anónimo paginado.
- No se agregaron `401`, `403`, `404`, `409`, `410`, `422` ni `500` por convención fuera de los status realmente alcanzables de cada acción.

#### Quality gates y validación

- **QG-1:** operaciones SC-305 `7`; ya presentes `0`; nuevas `7`; delta `+7`; total final `125` operaciones únicas.
- **QG-2:** ya presentes `0`; nuevas `7`; delta `+7`; total final `125` operaciones. Se validan Summary/Description, tag `CENIT`, Bearer, policy del action, parámetros path/query exactos, request body/schema cuando aplica, XML docs y conjunto exacto de responses.
- **QG-3:** permanece `DESACTIVADO`.
- **Examples:** `0` nuevos.
- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `OpenApiDocumentGenerationTests` ni `dotnet build`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos).
- Verificación estática: 7/7 acciones con rutas/verbs/policies esperadas, cuerpos productivos byte-text iguales al baseline r305, QG-1 con 125 pares `METHOD + PATH` únicos y QG-2 con 125 operaciones.

#### Archivos de SC-305

- `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs` — metadata OpenAPI/XML docs únicamente; cuerpos productivos preservados.
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitChamberResponseModels.cs` — XML docs de contratos directamente alcanzados.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental de las 7 operaciones.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de alcance, estado y evidencia de validación.

**Limitación:** ausencia de `dotnet` impide ejecutar tests/build; por DEC-009 esto deja SC-305 en `VERIFY`, no `BLOCKED`.

**Siguiente unidad real:** `SC-306 — Certificados — grupo 1`, que queda `READY`. No se ejecutó en esta sesión.

### Resultado de SC-306 — `Certificados — grupo 1`

**Estado:** `VERIFY` — la Parte A autorizada fue implementada completamente. La única validación pendiente es la ejecución .NET, porque el entorno no dispone de `dotnet`.

#### Partición autorizada y DEC-002

Parte A ejecutada:

- `CertificateManagementController`: 6 acciones lógicas autorizadas (`managed/preview`, `managed`, `public`, `private`, listado base y `{id}/versions`). El controller conserva dos prefijos activos: `/nacha-security/certificates/management` y `/api/nacha-security/certificates/management`; por tanto publica `6 × 2 = 12` operaciones OpenAPI.
- `DigitalEnvelopeCertificatesController`: 3 operaciones (`GET /nacha-security/certificates`, `POST /nacha-security/certificates`, `DELETE /nacha-security/certificates/{id}`).
- Total: **2 controllers / 9 acciones lógicas / 15 operaciones OpenAPI**.
- Resultado DEC-002: **CUMPLE exactamente en el límite de 15 operaciones**. Los route constraints `:int` conservan el routing real y se expresan canónicamente como `{id}` en OpenAPI.

Operaciones efectivas documentadas:

1. `POST /nacha-security/certificates/management/managed/preview`
2. `POST /api/nacha-security/certificates/management/managed/preview`
3. `POST /nacha-security/certificates/management/managed`
4. `POST /api/nacha-security/certificates/management/managed`
5. `POST /nacha-security/certificates/management/public`
6. `POST /api/nacha-security/certificates/management/public`
7. `POST /nacha-security/certificates/management/private`
8. `POST /api/nacha-security/certificates/management/private`
9. `GET /nacha-security/certificates/management`
10. `GET /api/nacha-security/certificates/management`
11. `GET /nacha-security/certificates/management/{id}/versions`
12. `GET /api/nacha-security/certificates/management/{id}/versions`
13. `GET /nacha-security/certificates`
14. `POST /nacha-security/certificates`
15. `DELETE /nacha-security/certificates/{id}`

#### Contratos, policies y responses

- `managed/preview` (`P1.CertificatesUploadPublic`): request multipart `ManagedCertificateApiRequest`; `200 CertificatePreviewDto`, `400 ProblemDetails`, `413 ProblemDetails`.
- `managed` (`P1.CertificatesRegisterPrivate`): request multipart `ManagedCertificateApiRequest`; `200 CertificateVersionApiDto`, `400/409/413 ProblemDetails`.
- `public` (`P1.CertificatesUploadPublic`): request multipart `UploadPublicCertificateApiRequest`; `200 CertificateVersionApiDto`, `400/409/413 ProblemDetails`.
- `private` (`P1.CertificatesRegisterPrivate`): request multipart `UploadPrivateCertificateApiRequest`; `200 CertificateVersionApiDto`, `400/409/413 ProblemDetails`.
- listado base (`P1.CertificatesRead`): query opcional `clearingHouseId`, `environment`, `purpose`, `holderType`, `status`; `200 IEnumerable<CertificateVersionApiDto>`.
- `{id}/versions` (`P1.CertificatesRead`): path `id`; `200 IEnumerable<CertificateVersionApiDto>`; el controller no traduce colección vacía a 404.
- Digital Envelope GET (`P1.CertificatesRead`): `200 IEnumerable<DigitalEnvelopeCertificateResponse>`.
- Digital Envelope POST (`P1.CertificatesUploadPublic`): `410 ProblemDetails`; la acción está retirada y no recibe request body.
- Digital Envelope DELETE (`P1.CertificatesRevoke`): path `id`; `204` sin body. El servicio/repositorio actual no produce 404 cuando el id no existe.

Se agregaron XML docs únicamente a contratos directamente alcanzados por Parte A: `ManagedCertificateApiRequest`, `UploadPublicCertificateApiRequest`, `UploadPrivateCertificateApiRequest`, `CertificateVersionApiDto`, `CertificatePreviewDto` y `DigitalEnvelopeCertificateResponse`. No se modificaron `RevokeVersionBody`, `CertificateValidationResultDto`, `DeleteCertificateVersionResultDto` ni `CertificateAuditDto` por pertenecer al remanente.

#### Quality gates y validación

- **QG-1:** acciones lógicas `9`; operaciones efectivas `15`; ya presentes `0`; nuevas `15`; delta `+15`; total final `140` operaciones únicas.
- **QG-2:** ya presentes `0`; nuevas `15`; delta `+15`; total final `140`. Se validan tag `Seguridad y certificados`, Bearer, policy real por acción, parámetros path/query, multipart request bodies, schemas/XML docs, responses exactas y ausencia de headers transversales falsos.
- **QG-3:** `DESACTIVADO`.
- **Examples:** `0` nuevos.
- `dotnet --info`: exit `127` (`dotnet: command not found`). `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: rutas/verbs/policies y materialización de ambos aliases verificadas; cuerpos productivos de las nueve acciones lógicas preservados.

#### Remanente reservado y siguiente unidad

Quedan **fuera de SC-306** las cinco acciones lógicas restantes de `CertificateManagementController`: `activate`, `revoke`, `delete`, `validate`, `audit`. Con los dos prefijos corresponden a **10 operaciones OpenAPI**. El tracker define `SC-307` como **“Sobre digital / operaciones criptográficas”**, por lo que este bloque **no se reasigna a SC-307** y permanece como **“Certificados — remanente de gestión” sin ID**.

`SC-307` queda `READY` como siguiente unidad real del plan y no fue ejecutada. `SC-308` y `SC-309` permanecen `TODO`.

#### Archivos de SC-306

- `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs` — metadata OpenAPI/XML docs únicamente en las seis acciones autorizadas y contratos Parte A; cinco acciones reservadas sin documentación anticipada.
- `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs` — metadata contractual corregida para sus tres operaciones, eliminando descripciones especulativas.
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/CertificateManagement/CertificateManagementDtos.cs` — XML docs de `CertificatePreviewDto`.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 15 operaciones efectivas.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de partición, estado, remanente y evidencia de validación.

### Resultado de SC-307 — `Sobre digital / operaciones criptográficas`

**Estado:** `VERIFY` — alcance humano resuelto e implementación contractual completa sobre `SobreDigitalController`. La única validación pendiente es la ejecución .NET, porque el entorno no dispone de `dotnet`.

#### Alcance autorizado y DEC-002

- Controller: `SobreDigitalController`.
- Prefijo único: `/api/nacha-security/digital-envelope`; no existen aliases de controller ni rutas alternativas para las operaciones públicas incluidas.
- Acciones HTTP lógicas: `4`.
- `POST testRSA`: excluida de OpenAPI por `[ApiExplorerSettings(IgnoreApi = true)]`; la exclusión se conserva sin cambios.
- Operaciones OpenAPI efectivas: `3`.
- Resultado DEC-002: **CUMPLE** (`1 controller`, `3 operaciones <= 15`).

Operaciones documentadas:

1. `GET /api/nacha-security/digital-envelope/certificates`
2. `POST /api/nacha-security/digital-envelope/encrypt`
3. `POST /api/nacha-security/digital-envelope/decrypt`

#### Contratos, policies y responses

- `GET certificates` — policy `P1.CertificatesRead`; sin path/query/request body; `200 IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>`.
- `POST encrypt` — policy `P1.DigitalEnvelopeEncrypt`; request `multipart/form-data` mediante `DigitalEnvelopeFileRequest`; `200 FileContentResult`/`application/octet-stream` con nombre de descarga y header real `X-Cryptographic-Profile`; `400/404/409/413 ProblemDetails`. No se publica 422 porque el flujo de cifrado actual no alcanza los códigos que `ToProblem` clasifica como 422.
- `POST decrypt` — policy `P1.DigitalEnvelopeDecrypt`; request `multipart/form-data` mediante `DigitalEnvelopeFileRequest`; `200 FileContentResult`/`application/octet-stream` con nombre de descarga y header real `X-Cryptographic-Profile`; `400/404/409/413/422 ProblemDetails`.

Los status se obtuvieron de `ValidateRequest`, `ToProblem`, `ManagedDigitalEnvelopeException` y el servicio invocado. No se agregaron 401/403 por la mera presencia de `[Authorize]`.

Se agregaron XML docs únicamente a contratos directamente alcanzados por SC-307: `ManagedDigitalEnvelopeCertificateDto` y `SobreDigitalController.DigitalEnvelopeFileRequest`. `DigitalEnvelopeOperationDto` pertenece a otra superficie y no fue modificado.

#### Quality gates y validación

- **QG-1:** operaciones efectivas `3`; ya presentes `0`; nuevas `3`; delta `+3`; total final `143` pares `METHOD + PATH` únicos. `testRSA` no se agrega.
- **QG-2:** ya presentes `0`; nuevas `3`; delta `+3`; total final `143`. Se validan tag `Seguridad y certificados`, Bearer, policy real, ausencia de parámetros path/query inexistentes, request multipart y documentación XML, responses exactos, `application/octet-stream` en los éxitos binarios y la declaración de `X-Cryptographic-Profile` en las descripciones contractuales.
- **QG-3:** `DESACTIVADO`.
- **Examples:** `0` nuevos.
- `dotnet --info`: exit `127` (`dotnet: command not found`). `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: rutas/verbs/policies, exclusión de `testRSA`, request/response y cuerpos productivos verificados sin cambios funcionales.

#### Exclusiones y siguiente unidad

Permanecen intactos `NachaSecurityOperationsController`, `NachaExportController`, `NachaUploadController`, el remanente de gestión de certificados de SC-306 y la configuración NACHA reservada. SC-308 y SC-309 no fueron ejecutados.

`SC-308` queda `READY` como siguiente unidad real con objetivo **“NACHA Security Operations”**. `SC-309` permanece `TODO` con objetivo **“Downloads/uploads: content types, nombres y errores”**.

#### Archivos de SC-307

- `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs` — metadata OpenAPI, responses y XML docs del request; cuerpos productivos y exclusión de testRSA preservados.
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/ManagedDigitalEnvelope/ManagedDigitalEnvelopeContracts.cs` — XML docs de `ManagedDigitalEnvelopeCertificateDto`.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 3 operaciones y protección explícita de testRSA fuera de OpenAPI.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de alcance, estado, quality gates y evidencia de validación.

### Resultado de SC-308 — `NACHA Security Operations`

**Estado:** `VERIFY` — alcance humano resuelto e implementación contractual completa sobre `NachaSecurityOperationsController`. La única validación pendiente es la ejecución .NET, porque el entorno no dispone de `dotnet`.

#### Alcance autorizado y DEC-002

- Controller: `NachaSecurityOperationsController`.
- Prefijo único: `/nacha-security/operations`; no existen aliases de controller ni rutas alternativas adicionales en las acciones incluidas.
- Acciones HTTP lógicas: `8`.
- Operaciones OpenAPI efectivas: `8`.
- **Resultado DEC-002:** `CUMPLE` — 1 controller y 8 operaciones, dentro del máximo de 2 controllers o 15 endpoints.
- Operaciones: `POST /nacha-security/operations/nacha/generate`; `POST /nacha-security/operations/nacha/generate-encrypted`; `POST /nacha-security/operations/envelope/manual-encrypt`; `POST /nacha-security/operations/envelope/manual-decrypt`; `GET /nacha-security/operations/{operationId}`; `GET /nacha-security/operations/audit`; `POST /nacha-security/operations/{operationId}/authorize-download`; `GET /nacha-security/operations/{operationId}/download`.

#### Contrato documentado

- Tag funcional existente: `NACHA-M` en las 8 operaciones.
- Security: Bearer en las 8 operaciones por metadata real de `[Authorize]`.
- Policies preservadas: `P1.NachaSecurityGenerateEncrypted` para ambas generaciones; `P1.NachaSecurityManualEncrypt`; `P1.NachaSecurityManualDecrypt`; `P1.NachaSecurityRead` para consulta, auditoría y descarga; `P1.NachaSecurityAuthorizeDownload` para autorización de descarga.
- Request JSON de generación: `NachaGenerateApiRequest` con `cycleId`.
- Requests manuales: archivo multipart `file` existente en la firma; no se creó DTO artificial.
- DTOs/schemas directamente alcanzados y documentados mediante XML: `DigitalEnvelopeOperationDto`, `DigitalEnvelopeOperationArtifactDto`, `DigitalEnvelopeOperationErrorDto`, `DigitalEnvelopeCertificateSummaryDto` y `NachaGenerateApiRequest`.
- Los bodies anónimos de errores y de autorización de descarga se conservan sin DTO/schema artificial. No se documenta `ProblemDetails` porque estas acciones no lo devuelven.
- No se modificaron tipos, nullability, defaults, nombres JSON, serialización, required/optional, enums, DataAnnotations ni validadores.

#### Responses reales

- `POST /nacha-security/operations/nacha/generate`: `200 DigitalEnvelopeOperationDto`. Los fallos funcionales de generación se representan dentro del DTO persistido; la acción no los traduce a 4xx.
- `POST /nacha-security/operations/nacha/generate-encrypted`: `200 DigitalEnvelopeOperationDto`, con el mismo patrón de resultado persistido.
- `POST /nacha-security/operations/envelope/manual-encrypt`: `200 DigitalEnvelopeOperationDto`; `400` con body anónimo `code/message` cuando el archivo falta o está vacío; `413` por el límite de 50 MB aplicado mediante `RequestSizeLimit`.
- `POST /nacha-security/operations/envelope/manual-decrypt`: `200 DigitalEnvelopeOperationDto`; `400` con body anónimo `code/message` cuando el archivo falta o está vacío; `413` por el límite de 50 MB aplicado mediante `RequestSizeLimit`.
- `GET /nacha-security/operations/{operationId}`: `200 DigitalEnvelopeOperationDto`; `404` con body anónimo `code/message/operationId` cuando no existe.
- `GET /nacha-security/operations/audit`: `200 IReadOnlyList<DigitalEnvelopeOperationDto>`; `take` se normaliza en el servicio al rango 1..200 y no genera un status de error por quedar fuera de ese rango.
- `POST /nacha-security/operations/{operationId}/authorize-download`: `200` body anónimo `operationId/authorized/expiresAtUtc`; `400` body anónimo; `403` sin body por el `Forbid()` explícito de la acción; `404` body anónimo.
- `GET /nacha-security/operations/{operationId}/download`: `200` stream con Content-Type y nombre entregados por el descriptor del servicio; `400` body anónimo; `403` sin body por el `Forbid()` explícito; `404` body anónimo. No se fuerza un media type único porque el código devuelve el `ContentType` persistido del artefacto.

#### Quality gates y validación

- **QG-1:** operaciones efectivas `8`; ya presentes `0`; nuevas `8`; delta `+8`; total final `151` pares `METHOD + PATH` únicos.
- **QG-2:** ya presentes `0`; nuevas `8`; delta `+8`; total final `151`. Se validan tag `NACHA-M`, Bearer, policy real por acción, parámetros path/query, request JSON/multipart, schemas/XML docs, responses exactos, cuerpos anónimos no convertidos a `ProblemDetails` y ausencia de headers transversales falsos.
- **QG-3:** `DESACTIVADO`.
- **Examples:** `0` nuevos.
- `dotnet --info`: exit `127` (`dotnet: command not found`). `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: rutas/verbs/policies/request/responses verificados; cuerpos productivos de las 8 acciones idénticos al baseline `r307b`.

#### Exclusiones y siguiente unidad

Permanecen intactos `SobreDigitalController`, el remanente de gestión de certificados de SC-306, `DigitalEnvelopeCertificatesController`, `NachaExportController`, `NachaUploadController`, `AchOutboundReturnTransportController`, `RegulatoryCatalogsController`, `NachaConfigProfilesController`, `NachaConfigProfilesReadOnlyController` y la configuración NACHA reservada. SC-309 no fue ejecutado.

`SC-309` queda `READY` como siguiente unidad real con objetivo **“Downloads/uploads: content types, nombres y errores”**. El tracker de entrada no asigna inequívocamente controllers a SC-309; en particular, `NachaExportController` y `NachaUploadController` permanecen sin asignación automática y no fueron modificados.

#### Archivos de SC-308

- `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs` — metadata OpenAPI, responses y XML docs de parámetros/request; cuerpos productivos preservados.
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Operations/NachaSecurityOperationsContracts.cs` — XML docs de los schemas directamente expuestos por `DigitalEnvelopeOperationDto`.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 8 operaciones efectivas.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de alcance, estado, quality gates y evidencia de validación.

### SC-309 — Downloads/uploads: content types, nombres y errores

Estado final: **VERIFY** — implementación contractual completa; validación ejecutable .NET pendiente por ausencia de `dotnet`.

#### Alcance autorizado y DEC-002

- Controllers: `NachaExportController` y `NachaUploadController`.
- Ambos conservan un único `[Route("[controller]")]`; no existen aliases de controller ni rutas alternativas en las acciones incluidas.
- Acciones lógicas: `4`.
- Operaciones OpenAPI efectivas: `4`.
- **Resultado DEC-002:** `CUMPLE` — 2 controllers y 4 operaciones, dentro del máximo de 2 controllers o 15 endpoints.
- Operaciones: `GET /NachaExport/{cycleId}`; `GET /NachaExport/{cycleId}/sobre-digital`; `POST /NachaUpload/upload`; `GET /NachaUpload/records`.

#### Contrato documentado

- Tag funcional existente: `NACHA-M` en las 4 operaciones.
- Security: Bearer en las 4 operaciones por metadata real de autorización.
- Policies preservadas: `CanReadAch` en ambas exportaciones; `P1.NachaUpload` en carga; `P1.NachaRead` en consulta de cargas.
- `GET /NachaExport/{cycleId}` no recibe body. Un único artefacto se devuelve como `text/plain` con `Content-Disposition` basado en el `ExternalFileName` resuelto; múltiples artefactos se devuelven como `application/zip` con nombre `<primer ExternalFileName>.zip`.
- `GET /NachaExport/{cycleId}/sobre-digital` no recibe body y admite `forceEncryption` opcional. Un archivo sin cifrar se devuelve como `text/plain`; la implementación vigente del servicio de sobre digital devuelve un único archivo cifrado como `application/octet-stream` y agrega `.ENV` al nombre de entrada; múltiples artefactos se agrupan como `application/zip`. Para un único archivo cifrado el controller emite `X-Cryptographic-Profile` y `X-Plaintext-SHA256`.
- `POST /NachaUpload/upload` consume `multipart/form-data` mediante `NachaUploadRequest` (`File`, `ClearingHouseId`, `ForceReprocess`, `ParentIngestionId`). El archivo debe ser no vacío y de hasta 10 MB; se aceptan los nombres/extensiones implementados por el controller. El MIME del archivo, cuando se informa, debe ser `text/plain` o `application/octet-stream`; un MIME vacío se entrega al servicio como `application/octet-stream`.
- `GET /NachaUpload/records` no recibe body y publica filtros query opcionales `immediateOrigin`, `immediateDestination`, `referenceCode`, `achCycleId` y `fileCreationDate`; devuelve JSON con `NachaUploadRecordResponse`.
- DTOs/schemas directamente alcanzados y documentados mediante XML: `NachaUploadRequest`, `NachaUploadResponseDto` y `NachaUploadRecordResponse`. `IncomingNachaAdmissionIssue` ya tenía XML docs y se reutiliza como propiedad del response.
- No se crearon DTOs artificiales para archivos ni se modificaron tipos, nullability, defaults, serialización, enums, DataAnnotations o validadores.

#### Responses y errores reales

- `GET /NachaExport/{cycleId}`: `200 FileContentResult` (`text/plain` o `application/zip` según cantidad de artefactos), `404` sin body cuando no existe ciclo/cámara, `422 ProblemDetails` para ausencia de contenido exportable o fallas controladas de generación/precondición.
- `GET /NachaExport/{cycleId}/sobre-digital`: `200 FileContentResult` (`text/plain`, `application/octet-stream` o `application/zip` según el camino real), `400 ProblemDetails` para error controlado genérico del sobre, `404` sin body para ciclo/cámara inexistente, `413 ProblemDetails` para `FILE_TOO_LARGE`, `422 ProblemDetails` para falta de contenido/precondición o errores controlados no procesables de certificado/sobre.
- `POST /NachaUpload/upload`: `200 NachaUploadResponseDto`; `400 NachaUploadResponseDto` para validaciones explícitas/`ArgumentException`; `408 NachaUploadResponseDto` por cancelación; `413` sin schema contractual por los límites `RequestSizeLimit`/`RequestFormLimits`; `422 NachaUploadResponseDto` cuando la ingesta queda `Bloqueado` o `Fallido`; `500 NachaUploadResponseDto` por la captura explícita de excepción inesperada.
- `GET /NachaUpload/records`: `200 IReadOnlyList<NachaUploadRecordResponse>` como JSON. No se documentaron status codes especulativos.

#### Quality gates y validación

- **QG-1:** operaciones efectivas `4`; ya presentes `0`; nuevas `4`; delta `+4`; total final `155` pares `METHOD + PATH` únicos.
- **QG-2:** ya presentes `0`; nuevas `4`; delta `+4`; total final `155`. Se validan tag `NACHA-M`, Bearer, policy real, parámetros, multipart/schema/XML docs, status exactos, request/response media types, semántica de filename/`Content-Disposition` disponible y ausencia de headers transversales falsos.
- **QG-3:** `DESACTIVADO`.
- **Examples:** `0` nuevos.
- `dotnet --info`: exit `127` (`dotnet: command not found`). `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: 4 rutas/verbs, ausencia de aliases, policies, `Consumes`, multipart/IFormFile, `FileContentResult`, content types, filenames, responses y cuerpos productivos verificados contra `r308`.

#### Exclusiones y siguiente unidad

Permanecen intactos `NachaSecurityOperationsController`, `SobreDigitalController`, el remanente de gestión de certificados de SC-306, `DigitalEnvelopeCertificatesController`, `CenitOperationsController`, `AchResponseOperationsController`, `NachaConfigProfilesController`, `NachaConfigProfilesReadOnlyController` y la configuración NACHA reservada. Ninguna unidad posterior fue ejecutada.

`SC-390` queda `READY` como siguiente unidad real del plan (**Checkpoint Fase D**); no se ejecuta en esta sesión.

#### Archivos de SC-309

- `src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs` — Summary/Description, content types y responses contractuales de las dos exportaciones; cuerpos productivos preservados.
- `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs` — metadata OpenAPI de upload/consulta y XML docs de `NachaUploadRequest`, `NachaUploadResponseDto` y `NachaUploadRecordResponse`; cuerpos productivos preservados.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 4 operaciones efectivas.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de alcance, estado, quality gates y evidencia de validación.


### Resultado de SC-390 — checkpoint de Fase D

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Checkpoint Fase D`.  
**Tipo de unidad:** checkpoint acumulado; no documenta endpoints nuevos ni modifica contratos OpenAPI.  
**Implementación de Fase D:** `COMPLETADA`.  
**Validación ejecutable:** `PENDIENTE` exclusivamente por ausencia de `dotnet`.  
**Bloqueo para Fase E:** `NO`.

#### Alcance y métricas del checkpoint

1. El tracker define Fase D exactamente como `SC-301` a `SC-309`; todas permanecen en `VERIFY` por validación .NET pendiente y se revisaron sin reabrir ni reinterpretar sus contratos.
2. El alcance acumulado de Fase D comprende **11 controllers** y **78 operaciones contractuales** cubiertas por QG-1/QG-2: Incoming NACHA `17`, NACHA legacy `10`, respuestas `14`, CENIT `7`, certificados Parte A `15`, sobre digital `3`, NACHA Security `8` y downloads/uploads `4`.
3. QG-1 conserva **155 operaciones únicas**, sin duplicados ni eliminaciones; SC-390 agrega `0` operaciones.
4. QG-2 conserva **155 operaciones únicas**, sin duplicados. Su conjunto `METHOD + PATH` coincide exactamente con QG-1; las **78 operaciones de Fase D** están presentes en ambos gates y SC-390 no amplía allowlists.
5. Los 11 controllers de Fase D conservan **72 acciones documentadas mediante `EndpointSummary`/`EndpointDescription`**. La diferencia frente a las 78 operaciones contractuales corresponde a materialización de aliases ya aceptada en SC-306; `SobreDigitalController.testRSA` conserva `ApiExplorerSettings(IgnoreApi = true)` y no entra a los gates. SC-390 no reinterpreta aliases legacy cerrados en SC-303.
6. QG-3 permanece `DESACTIVADO`: no existe símbolo/gate QG-3 activo en `OpenApiDocumentGenerationTests`. SC-390 no agrega examples, status codes, schemas, headers, metadata ni documentación funcional nueva.
7. No fue necesaria ninguna corrección de código ni de tests. El único archivo modificado por el checkpoint es este tracker.

#### Validación de SC-390

- `dotnet --info`: exit code `127`; `dotnet` no está instalado.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado por ausencia de `dotnet`.
- Suite backend completa prevista para checkpoints: no ejecutada por ausencia de `dotnet`; no se simuló ningún resultado.
- `python scripts/validate-solution-projects.py`: exit code `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` resueltos.
- Revisión estática de `OpenApiDocumentGenerationTests`: QG-1 = `155/155` únicas; QG-2 = `155/155` únicas; conjuntos idénticos; `0` duplicados; las 78 operaciones de Fase D están en ambos gates; QG-3 permanece sin activar.
- Revisión estructural de **34 archivos C#** de Fase D/configuración OpenAPI: delimitadores balanceados y **0 merge markers**. Se verificó además que los ocho transformers/document transformers esperados continúan registrados, junto con `MapOpenApi` y `MapScalarApiReference`.
- Comparación contra `r309`: fuera de este tracker no existe ningún cambio; controllers, DTOs, tests y configuración OpenAPI permanecen byte a byte idénticos al baseline.

La ausencia de `dotnet` es una limitación ambiental, no un bloqueo del checkpoint. SC-390 queda en `VERIFY`. La siguiente unidad real del plan es `SC-401 — ACH Colombia File Exchange`, que queda `READY` sin ejecutarse.

### Resultado de SC-401 — ACH Colombia File Exchange

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `ACH Colombia File Exchange`.  
**Baseline:** `ACHInterbank-v124-scalar-r401-SC401.zip`.  
**Alcance autorizado:** `AchColombiaFileExchangeController`, exclusivamente 9 acciones de transferencia/ejecución; 1 controller, 9 operaciones OpenAPI efectivas, 0 aliases. Cumple DEC-002.

#### Operaciones ejecutadas

1. `GET /api/ach-colombia/file-exchange/transfers` — `Query` — policy `CanReadAch` — `200 IReadOnlyList<AchManagedFileTransferSummary>`.
2. `GET /api/ach-colombia/file-exchange/transfers/{id}` — `Get` (`{id:guid}` en routing) — `CanReadAch` — `200 AchManagedFileTransferDetail`, `404` sin body.
3. `GET /api/ach-colombia/file-exchange/transfers/{id}/download` — `Download` (`{id:guid}`) — `CanReadAch` — `200 FileContentResult`, `404` sin body cuando el servicio no dispone de contenido retenido.
4. `POST /api/ach-colombia/file-exchange/outbound/execute` — `ExecuteOutbound` — `CanManageAch` — request `ExecuteOutboundRequest`, header opcional `Idempotency-Key`, `200 AchManagedFileExecutionResult`.
5. `POST /api/ach-colombia/file-exchange/inbound/execute` — `ExecuteInbound` — `CanManageAch` — sin body, header opcional `Idempotency-Key`, `200 AchManagedFileExecutionResult`.
6. `POST /api/ach-colombia/file-exchange/transfers/{id}/retry` — `Retry` — `CanManageAch` — header opcional `Idempotency-Key`, `200 AchManagedFileTransferDetail`.
7. `POST /api/ach-colombia/file-exchange/transfers/{id}/reprocess` — `Reprocess` — `CanManageAch` — `200 AchManagedFileTransferDetail`.
8. `POST /api/ach-colombia/file-exchange/transfers/{id}/archive` — `Archive` — `CanManageAch` — `200 AchManagedFileTransferDetail`.
9. `POST /api/ach-colombia/file-exchange/transfers/{id}/retire` — `Retire` — `CanManageAch` — request `RetireRequest`, `200 AchManagedFileTransferDetail`.

El tag publicado sigue siendo `ACH Colombia` por la taxonomía vigente y el requisito Bearer se deriva de `[Authorize]`. No se añadieron `401`/`403` por convención ni otros status especulativos. Las excepciones de servicio que estas acciones no traducen explícitamente no se publicaron como nuevos responses.

#### Download: media type y filename

`DownloadAsync` devuelve `AchManagedFileDownload(transfer.PhysicalFileName, "application/octet-stream", transfer.RetainedContent)` cuando existe contenido retenido. El controller conserva `File(result.Content, result.ContentType, result.FileName)`: el response exitoso publica `application/octet-stream`, el filename procede de `AchManagedFileDownload.FileName` y actualmente corresponde al nombre físico persistido; `File(...)` usa ese valor como nombre de descarga/`Content-Disposition`. No se documentó una extensión o patrón adicional.

#### DTOs/schemas directos

Se agregaron XML docs sin cambiar contratos a `AchManagedFileTransferSummary`, `AchManagedFileTransferEventDto`, `AchManagedFileTransferDetail`, `AchManagedFileExecutionResult`, `AchManagedFileDownload`, `ExecuteOutboundRequest` y `RetireRequest`. No se modificaron `AchManagedFileTransferConfigurationDto`, `AchManagedMftAdministrationDto`, `UpdateAchManagedMftAdministrationRequest` ni `SetAchManagedMftCredentialRequest`.

#### Reserva explícita fuera de SC-401

Permanece sin ID el bloque **“ACH Colombia File Exchange — configuration/administration pendiente”**:

- `GET /api/ach-colombia/file-exchange/configuration`
- `PUT /api/ach-colombia/file-exchange/configuration`
- `GET /api/ach-colombia/file-exchange/administration`
- `PUT /api/ach-colombia/file-exchange/administration`
- `PUT /api/ach-colombia/file-exchange/administration/credential`

Estas cinco acciones conservaron byte a byte su metadata y cuerpos respecto de `r401` y no entraron a QG-1/QG-2. No se asignan automáticamente a SC-402 ni a ninguna otra unidad.

#### Quality gates y validación

- QG-1: baseline `155`; operaciones SC-401 `9`; ya presentes `0`; nuevas `9`; delta `+9`; total final `164/164` pares `METHOD + PATH` únicos.
- QG-2: baseline `155`; ya presentes `0`; nuevas `9`; delta `+9`; total final `164`. Se validan Summary/Description, tag `ACH Colombia`, Bearer, policy real, parámetros, bodies JSON, schemas/XML docs, responses exactos, `Idempotency-Key` solo en ExecuteOutbound/ExecuteInbound/Retry, media type y semántica de filename de Download, y ausencia de `Authorization`/`X-Correlation-ID` falsos.
- QG-3: `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`); `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: rutas/verbs/policies y 9 cuerpos productivos preservados; bloque reservado configuration/administration intacto; QG-1/QG-2 contados estáticamente en 164; QG-3 sin activar.

#### Archivos de SC-401

- `src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs` — metadata OpenAPI/XML únicamente para las 9 acciones autorizadas y requests anidados; cuerpos productivos preservados.
- `src/Cfa.ACHInterbank.Application/ACH/Models/AchManagedFileTransferModels.cs` — XML docs de contratos directamente alcanzados por SC-401.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 9 operaciones efectivas.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión de alcance, reserva pendiente, estado y evidencia.

La única limitación es la ausencia de `dotnet`; por DEC-009 SC-401 queda `VERIFY`. `SC-402 — Integration Mapping Sets` queda `READY` y no se ejecuta en esta sesión.

### Resultado de SC-402 — Integration Mapping Sets

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Integration Mapping Sets`.  
**Baseline:** `ACHInterbank-v124-scalar-r402-SC402.zip`.  
**Alcance autorizado:** exclusivamente `IntegrationMappingSetsController`, 1 controller, 12 acciones/operaciones OpenAPI efectivas, 0 aliases. Cumple DEC-002.

#### Operaciones ejecutadas

1. `GET /api/integrations/mappingsets` — `Get` — query opcional `methodId` — policy `CanReadAch` — `200 IReadOnlyCollection<IntegrationMappingSetDto>`.
2. `GET /api/integrations/mappingsets/published` — `GetPublished` — query requerido `methodId` — `CanReadAch` — `200 IntegrationMappingSetDto`, `404` sin body cuando no existe publicado.
3. `GET /api/integrations/mappingsets/{id}` — `GetById` (`{id:guid}` en routing) — `CanReadAch` — `200 IntegrationMappingSetDto`, `404` sin body.
4. `POST /api/integrations/mappingsets` — `Create` — request `CreateIntegrationMappingSetRequest` — `CanManageAch` — `201 IntegrationMappingSetDto` mediante `CreatedAtAction`.
5. `PUT /api/integrations/mappingsets/{id}` — `Update` — request `UpdateIntegrationMappingSetRequest` — `CanManageAch` — `200 IntegrationMappingSetDto`.
6. `PUT /api/integrations/mappingsets/{id}/rules` — `UpsertRules` — request `UpsertIntegrationMappingRulesRequest` — `CanManageAch` — `200 IntegrationMappingSetDto`.
7. `POST /api/integrations/mappingsets/{id}/validate` — `Validate` — request `ValidateIntegrationMappingSetRequest` — `CanManageAch` — `200 IntegrationMappingValidationResultDto`.
8. `POST /api/integrations/mappingsets/{id}/preview` — `Preview` — request `PreviewIntegrationMappingSetRequest` — `CanManageAch` — `200 IntegrationMappingPreviewResultDto`.
9. `POST /api/integrations/mappingsets/{id}/publish` — `Publish` — request `PublishIntegrationMappingSetRequest` — `CanManageAch` — `200 IntegrationMappingSetDto`.
10. `POST /api/integrations/mappingsets/{id}/clone` — `Clone` — request `CloneIntegrationMappingSetRequest` — `CanManageAch` — `200 IntegrationMappingSetDto`.
11. `GET /api/integrations/mappingsets/{id}/history` — `History` — `CanReadAch` — `200 IReadOnlyCollection<IntegrationMappingSetHistoryDto>`; colección vacía si no hay historia.
12. `POST /api/integrations/mappingsets/compare` — `Compare` — request `CompareIntegrationMappingSetsRequest` — `CanManageAch` — `200 IntegrationMappingSetComparisonResultDto`.

El tag publicado sigue siendo `Integraciones` por la taxonomía vigente y Bearer se deriva de `[Authorize]`. No se añadieron `400/401/403/409/422/500` por convención: las excepciones de servicio no traducidas por el controller no se publican como responses contractuales. Tampoco se agregaron `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.

#### Comportamiento contractual documentado

- Las consultas devuelven los mapping sets persistidos y sus reglas; la consulta general filtra opcionalmente por `methodId` y el servicio ordena por actualización descendente.
- `GetPublished` devuelve el mapping set `Published` de mayor versión del método o `404` sin body.
- La creación genera un mapping set `Draft`, activo y versión 0 y registra historia de creación.
- Update/UpsertRules operan sobre borradores según el servicio; el upsert exige parámetros activos del método, rechaza `Expression`/`ConditionExpression` internamente y normaliza prioridad a mínimo 1.
- Validate devuelve issues/cobertura/estado por parámetro y persiste el resumen calculado.
- Preview usa la muestra solicitada por el request y devuelve valores resueltos y JSON de previsualización.
- Publish valida el borrador, archiva publicaciones vigentes del mismo método y asigna la siguiente versión antes de publicar.
- Clone copia reglas a un nuevo borrador; History devuelve eventos en orden descendente; Compare compara mapping sets del mismo método según la regla de menor prioridad numérica por parámetro.

#### DTOs/schemas directos

Se agregaron XML docs, sin cambiar contratos, a los requests y responses de `IntegrationMappingSetDtos.cs`: create/update/rules/validate/preview/publish/clone/compare; `IntegrationMappingSetDto`, `IntegrationMappingRuleDto`, validación y cobertura, preview, historia y comparación. No se modificaron DTOs de `IntegrationCatalogController`.

#### Reserva explícita fuera de SC-402

Permanece sin ID **“Integration Catalog — pendiente”**, byte a byte intacto:

- `GET /api/integrations/methods`
- `GET /api/integrations/methods/{methodId}/parameters`
- `GET /api/integrations/source-catalog`
- `GET /api/integrations/transformations`

No se reasigna a SC-403. `SoapIntegrationSettingsController` también queda intacto; SC-403 conserva el objetivo `Integraciones SOAP/external expuestas por REST`.

#### Quality gates y validación

- QG-1: baseline `164`; operaciones SC-402 `12`; ya presentes `0`; nuevas `12`; delta `+12`; total final `176/176` pares `METHOD + PATH` únicos.
- QG-2: baseline `164`; ya presentes `0`; nuevas `12`; delta `+12`; total final `176`. Se validan Summary/Description, tag `Integraciones`, Bearer, policy real, parámetros path/query, request bodies/schemas/XML docs, responses exactos y ausencia de headers/status especulativos.
- QG-3: `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`); `dotnet test` y `dotnet build` no se ejecutaron ni simularon.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: 12 rutas/verbs/policies y cuerpos productivos preservados; estructura C# balanceada; QG-1/QG-2 contados estáticamente en 176; QG-3 no activado.

#### Archivos de SC-402

- `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs` — XML/metadata OpenAPI para las 12 acciones autorizadas; cuerpos productivos preservados.
- `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs` — XML docs de contratos directamente alcanzados.
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs` — QG-1/QG-2 incremental para las 12 operaciones.
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` — decisión, reserva, estado y evidencia.

La única limitación es la ausencia de `dotnet`; por DEC-009 SC-402 queda `VERIFY`. `SC-403 — Integraciones SOAP/external expuestas por REST` queda `READY` y no se ejecuta en esta sesión.

### Resultado de SC-403 — Integraciones SOAP/external expuestas por REST

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Integraciones SOAP/external expuestas por REST`.  
**Baseline:** `ACHInterbank-v124-scalar-r403-SC403.zip`.  
**Decisión de alcance:** la decisión humana autorizada limita SC-403 exclusivamente a `SoapIntegrationSettingsController` y `UatContrapartidasController`: 2 controllers, 3 acciones lógicas, 0 aliases y 3 operaciones OpenAPI efectivas. Cumple DEC-002.

#### Operaciones ejecutadas

1. **`GET /api/users/soap-integrations`** — `SoapIntegrationSettingsController.GetAsync` — policy `P1.ConfigRead`.
   - `200 SoapIntegrationSettingsDto`.
   - Consulta los mappings SOAP persistidos; cuando no existe fila de configuración devuelve listas vacías y agrega la vista calculada `ProcTransaccionesEffectiveSettings`.
2. **`PUT /api/users/soap-integrations`** — `SoapIntegrationSettingsController.SaveAsync` — policy `P1.ConfigManage` — body `SoapIntegrationSettingsDto`.
   - `200 SoapIntegrationSettingsDto`.
   - El servicio normaliza y persiste `WscfaachMappings` y `WsAxonRespuestaTransaccionesMappings`, descarta mappings sin `MethodName`, normaliza textos/modo operativo y recalcula la vista efectiva de `Proc_Transacciones` para la respuesta.
3. **`POST /api/uat/contrapartidas/dispatch-cycle`** — `UatContrapartidasController.DispatchCycle` — policy de controller `CanManageAch` — body `UatContrapartidasDispatchCycleRequest`.
   - `200 ContrapartidaCycleDispatchResult`.
   - `400` con body anónimo `errorCode/message` cuando `TransactionId <= 0`.
   - `404` sin body cuando la operación UAT está deshabilitada, o con body anónimo `errorCode` cuando no existe el objetivo en la cola.
   - `409` con body anónimo `errorCode/message` cuando la transacción ya tiene resultado funcional exitoso.
   - La acción solo se habilita fuera de Production, en Development/Testing/UAT y con `ACH_SOAP_LIVE_TESTS=true`. Invoca `IContrapartidaDispatchJobService.ProcessTransactionAsync`; la implementación del servicio resuelve ciclo/cámara desde la cola y ejecuta el flujo `Proc_Contrapartidas` mediante `IWscfaachSoapClient`.

#### Contratos documentados

- `SoapIntegrationSettingsDto`
- `SoapEndpointMethodMappingDto`
- `SoapInputParameterMappingDto`
- `ProcTransaccionesEffectiveSettingsDto`
- `UatContrapartidasDispatchCycleRequest`
- `ContrapartidaCycleDispatchResult`

Los comentarios XML aclaran únicamente semántica demostrable por código; no se agregaron propiedades, DataAnnotations, cambios de nullability, serialización, defaults ni reglas funcionales.

#### Quality gates

- QG-1: baseline `176`; operaciones SC-403 `3`; ya presentes `0`; nuevas `3`; delta `+3`; total final `179/179` pares `METHOD + PATH` únicos, sin duplicados.
- QG-2: baseline `176`; ya presentes `0`; nuevas `3`; delta `+3`; total final `179`. Para SC-403 se validan tag `Integraciones`, Bearer, policies reales, request schemas/XML docs, responses exactos y ausencia de `Authorization`, `Idempotency-Key` y `X-Correlation-ID` como parámetros falsos.
- QG-3: `DESACTIVADO`.
- Examples nuevos: `0`.

#### Exclusiones preservadas

Permanece fuera como **“NACHA SOAP-UAT/readiness — pendiente”**, sin ID y sin ejecución:

- `NachaSoapUatConsoleController` — 4 operaciones.
- `NachaOperationalReadinessController` — 7 operaciones, incluida `soap-readiness`.

También permanecen intactos `IntegrationCatalogController` (“Integration Catalog — pendiente”), las 5 operaciones configuration/administration reservadas de SC-401, el remanente de certificados SC-306 y la configuración NACHA reservada.

#### Archivos modificados

- `src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/UatContrapartidasController.cs`
- `src/Cfa.ACHInterbank.Application/Security/Dtos/SoapIntegrationSettingsDto.cs`
- `src/Cfa.ACHInterbank.Application/Security/Dtos/SoapEndpointMethodMappingDto.cs`
- `src/Cfa.ACHInterbank.Application/Security/Dtos/SoapInputParameterMappingDto.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/ContrapartidaDispatchJobModels.cs`
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`

#### Validación

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test ...OpenApiDocumentGenerationTests` y `dotnet build ACHInterbank.sln -c Release`: no ejecutados ni simulados por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y todas las referencias cargadas).
- Revisión estática: las 3 rutas/verbs/policies coinciden con el baseline; 0 aliases; cuerpos productivos sin cambios; QG-1/QG-2 contados en 179; QG-3 permanece desactivado.

La única limitación prevista es la validación ejecutable .NET pendiente; conforme DEC-009, SC-403 queda `VERIFY` y `SC-404 — Scheduler — grupo 1` queda `READY` sin ejecución.


### Resultado de SC-404 — Scheduler — grupo 1

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Scheduler — grupo 1`.  
**Baseline:** `ACHInterbank-v124-scalar-r404-SC404.zip`.  
**Decisión de alcance autorizada:** exclusivamente `SchedulerController`; `TaskDefinitionsController` queda fuera de SC-404.

#### Alcance ejecutado y DEC-002

- `SchedulerController`: **C = 1 controller, L = 13 acciones lógicas, A = 0 aliases, X = 13 operaciones OpenAPI efectivas**.
- DEC-002 exige como máximo 2 controllers o 15 endpoints por sesión: **CUMPLE** (`1 <= 2` y `13 <= 15`).
- Operaciones documentadas:
  - `GET /api/scheduler/overview` — policy `P1.SchedulerView`.
  - `GET /api/scheduler/tasks` — policy `P1.SchedulerView`.
  - `GET /api/scheduler/tasks/{taskCode}` — policy `P1.SchedulerView`.
  - `GET /api/scheduler/tasks/{taskCode}/technical` — policy `P1.SchedulerViewTechnical`.
  - `GET /api/scheduler/history` — policy `P1.SchedulerHistoryView`.
  - `GET /api/scheduler/tasks/{taskCode}/history` — policy `P1.SchedulerHistoryView`.
  - `GET /api/scheduler/instances` — policy `P1.SchedulerViewInstances`.
  - `GET /api/scheduler/executions/{executionId}` — policy `P1.SchedulerHistoryView`; el route template conserva `{executionId:guid}` y ApiExplorer publica `{executionId}`.
  - `POST /api/scheduler/tasks/{taskCode}/execute` — policy `P1.SchedulerExecute`.
  - `POST /api/scheduler/tasks/{taskCode}/pause` — policy `P1.SchedulerPauseResume`; `X-Correlation-ID` opcional real para auditoría.
  - `POST /api/scheduler/tasks/{taskCode}/resume` — policy `P1.SchedulerPauseResume`; `X-Correlation-ID` opcional real para auditoría.
  - `PUT /api/scheduler/tasks/{taskCode}/schedule` — policy `P1.SchedulerManageSchedule`; `X-Correlation-ID` opcional real para auditoría.
  - `POST /api/scheduler/schedule/preview` — policy `P1.SchedulerManageSchedule`.
- Todas las operaciones conservan `[Authorize]` a nivel controller y su policy fina real; OpenAPI publica Bearer mediante el transformer de seguridad. No se documentan `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` donde no son parámetros reales.

#### Contratos y reglas documentadas

Se añadieron XML docs únicamente a contratos alcanzados directamente por `SchedulerController`: `SchedulerOverviewDto`, `SchedulerTaskDto`, `SchedulerOperationalContextDto`, `SchedulerTechnicalInfoDto`, `SchedulerHistoryQuery`, `SchedulerPagedResult<T>`, `SchedulerExecutionDto`, `SchedulerInstanceDto`, `ExecuteSchedulerTaskRequest`, `ManualExecutionResult`, `SchedulerScheduleUpdateRequest`, `SchedulerSchedulePreviewDto`, `ManualExecutionOutcome`, `SchedulerMisfirePolicy` y `SchedulerExecutionStatus`. No se cambió ninguna propiedad, tipo, nullability, default, enum, valor, serialización o validator.

- `history`: `Page` se normaliza a mínimo 1 y `PageSize` se limita a 1..100; filtros reales por `Status`, `FromUtc`, `ToUtc`, `TriggerType`, `InstanceId`, `UserName` y `CorrelationId`. En historia por tarea, un `taskCode` ajeno al catálogo produce página vacía, no 404.
- `execute`: `RequestId` debe ser distinto de `Guid.Empty`; `Reason` recortado debe medir 10..500. `RequestId` es usado por el servicio para detectar solicitudes ya registradas.
- `schedule`/`preview`: se documentaron solo validaciones demostradas por `ValidateSchedule`: periodicidad y misfire definidos, `StartAt < EndAt` cuando ambos existen, `TimeZoneId` resoluble por `TimeZoneInfo`, cron validado por Quartz cuando aplica, intervalo `EveryNMinutes` 1..1440, minuto 0..59, hora interpretable por `TimeOnly`, día semanal/mensual requerido según periodicidad y `StartAt` requerido para `Once`/`Yearly`. `UpdateSchedule` también rechaza programación directa de tareas gobernadas por ciclo y aplica el intervalo mínimo del catálogo cuando corresponde.
- No se inventaron retries, backoff, locks, prioridades, timeouts, ventanas, sintaxis cron adicional, idempotency headers ni estados ajenos al código.

#### Responses documentados

- `GET /api/scheduler/overview`: `200 SchedulerOverviewDto`.
- `GET /api/scheduler/tasks`: `200 IReadOnlyList<SchedulerTaskDto>`.
- `GET /api/scheduler/tasks/{taskCode}`: `200 SchedulerTaskDto`, `404` sin body.
- `GET /api/scheduler/tasks/{taskCode}/technical`: `200 SchedulerTechnicalInfoDto`, `404` sin body.
- `GET /api/scheduler/history`: `200 SchedulerPagedResult<SchedulerExecutionDto>`.
- `GET /api/scheduler/tasks/{taskCode}/history`: `200 SchedulerPagedResult<SchedulerExecutionDto>`.
- `GET /api/scheduler/instances`: `200 IReadOnlyList<SchedulerInstanceDto>`.
- `GET /api/scheduler/executions/{executionId}`: `200 SchedulerExecutionDto`, `404` sin body.
- `POST /api/scheduler/tasks/{taskCode}/execute`: `202 ManualExecutionResult` para `Accepted`; `200 ManualExecutionResult` para `Duplicate`; `400 ValidationProblemDetails` por validación explícita de `RequestId`/`Reason`; `404 ManualExecutionResult` para `NotFound`; `409 ManualExecutionResult` para `Conflict` o `Rejected`.
- `POST /api/scheduler/tasks/{taskCode}/pause`: `204` sin body o `404` sin body.
- `POST /api/scheduler/tasks/{taskCode}/resume`: `204` sin body o `404` sin body.
- `PUT /api/scheduler/tasks/{taskCode}/schedule`: `200 SchedulerTaskDto`, `400` con body anónimo `{ message }` conservado como response no tipado, `404` sin body.
- `POST /api/scheduler/schedule/preview`: `200 SchedulerSchedulePreviewDto`, `400` con body anónimo `{ message }` conservado como response no tipado.
- No se agregaron 401/403 por el mero uso de `[Authorize]` ni 500/503 u otros status no traducidos por el controller.

#### Quality gates

- **QG-1:** baseline `179`; operaciones SC-404 `13`; ya presentes `0`; nuevas `13`; delta `+13`; total final `192/192` pares `METHOD + PATH` únicos.
- **QG-2:** baseline `179`; ya presentes `0`; nuevas `13`; delta `+13`; total final `192/192`. El conjunto de QG-2 coincide exactamente con QG-1. Se validan summary, description, tag `Operación y programación`, Bearer, policy real, path/query, request schemas, XML docs, responses y `X-Correlation-ID` exclusivamente en pause/resume/update-schedule.
- Las cinco operaciones `/TaskDefinitions` no fueron incorporadas a QG-1 ni QG-2.
- **QG-3:** `DESACTIVADO`.
- **Examples nuevos:** `0`.

#### Task Definitions y siguiente unidad real

`TaskDefinitionsController` permanece intacto y fuera de SC-404:

- `GET /TaskDefinitions`
- `GET /TaskDefinitions/{id}`
- `POST /TaskDefinitions`
- `PUT /TaskDefinitions/{id}`
- `DELETE /TaskDefinitions/{id}`

El objetivo real ya existente para `SC-405` es **`Scheduler — grupo 2 si aplica`**. Como SC-404 fue delimitado a `SchedulerController` y `TaskDefinitionsController` es el único remanente Scheduler directo inventariado, el tracker registra `TaskDefinitionsController` (1 controller, 5 operaciones, 0 aliases) como alcance reservado compatible de `SC-405` y deja `SC-405 = READY`. **SC-405 no fue ejecutado.**

#### Archivos modificados

- `src/Cfa.ACHInterbank.Api/Controllers/SchedulerController.cs`
- `src/Cfa.ACHInterbank.Application/Scheduler/Models/SchedulerAdminModels.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/SchedulerMisfirePolicy.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/SchedulerExecutionStatus.cs`
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`

Agregados: `0`. Eliminados: `0`.

#### Validación y limitaciones

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: 13 atributos `Http*`, 13 summaries, 13 descriptions, 0 aliases; policies y bodies productivos conservados; QG-1/QG-2 = 192 únicas y conjuntos idénticos; TaskDefinitions = 0 operaciones en QG.
- La única limitación es la falta de runtime/SDK .NET para validar generación OpenAPI y build; conforme DEC-009, SC-404 queda `VERIFY`, no `BLOCKED`.
- Permanecen intactos y fuera de SC-404: SOAP-UAT/readiness pendiente, Integration Catalog pendiente, 5 operaciones configuration/administration de SC-401, remanente de certificados SC-306 y configuración NACHA reservada.
- `SC-405` y toda unidad posterior no fueron ejecutados.

### SC-405 — Scheduler — grupo 2

**Estado final:** `VERIFY` — implementación documental completa; falta únicamente validación ejecutable .NET porque `dotnet` no está disponible en el entorno.

#### Objetivo y delimitación

- Objetivo real: `Scheduler — grupo 2`.
- Alcance autorizado y ejecutado exclusivamente: `TaskDefinitionsController`.
- Controllers: `1`.
- Acciones lógicas: `5`.
- Aliases: `0`.
- Operaciones OpenAPI efectivas: `5`.
- DEC-002: **CUMPLE** (`1 <= 2` controllers y `5 <= 15` operaciones).
- `SchedulerController` de SC-404 no se reabrió ni modificó.

#### Operaciones documentadas

- `GET /TaskDefinitions`
- `GET /TaskDefinitions/{id}`
- `POST /TaskDefinitions`
- `PUT /TaskDefinitions/{id}`
- `DELETE /TaskDefinitions/{id}`

#### Contratos y reglas demostradas

- Contratos alcanzados: `TaskDefinitionDto`, `TaskParameterDto`, `TaskStatusEnum`, `CalendarPolicyEnum`, `ConcurrencyPolicyEnum`, `PeriodicityTypeEnum` y `SchedulerMisfirePolicy`.
- `GET` colección incluye parámetros y el servicio ordena las definiciones por `Id` ascendente.
- `GET {id}` incluye parámetros y devuelve `404` sin body cuando la definición no existe.
- `POST` crea una definición nueva; `Code` y `Name` se recortan; `Description`, `TimeZoneId` y `CronExpression` vacíos se persisten como `null`; `TimeOfDay` se intenta convertir con `TimeOnly.TryParse` y queda `null` si no se interpreta; parámetros con `Key` vacío se descartan y `Key`/`Value` se recortan.
- `PUT {id}` exige igualdad entre el id de ruta y `TaskDefinitionDto.Id`; la diferencia produce `400` sin body. Para una entidad existente aplica la misma normalización; los parámetros se actualizan por Id, `Id = 0` agrega uno nuevo y los persistidos no presentes por Id se eliminan.
- `DELETE {id}` devuelve `204` cuando elimina la definición y `404` cuando no existe.
- No se inventaron reglas cron, zonas horarias válidas, retries/backoff válidos, locks, prioridades, activación automática, versionado ni validaciones ausentes del código.

#### Policies, seguridad y headers

- `GET /TaskDefinitions` y `GET /TaskDefinitions/{id}`: policy `CanReadAch`.
- `POST`, `PUT` y `DELETE`: policy `CanManageAch`.
- Las cinco operaciones permanecen protegidas por Bearer debido al `[Authorize]` real del controller/acciones.
- No se documentan `Authorization`, `Idempotency-Key` ni `X-Correlation-ID` como parámetros.

#### Responses documentados

- `GET /TaskDefinitions`: `200 IEnumerable<TaskDefinitionDto>`.
- `GET /TaskDefinitions/{id}`: `200 TaskDefinitionDto`, `404` sin body.
- `POST /TaskDefinitions`: `201 TaskDefinitionDto` mediante `CreatedAtAction`.
- `PUT /TaskDefinitions/{id}`: `200 TaskDefinitionDto`, `400` sin body por discrepancia entre ids, `404` sin body.
- `DELETE /TaskDefinitions/{id}`: `204` sin body, `404` sin body.
- No se agregaron 401/403 por el mero uso de `[Authorize]`, ni 409/410/422/500 u otros status no traducidos por el controller.

#### Quality gates

- **QG-1:** baseline `192`; operaciones SC-405 `5`; ya presentes `0`; nuevas `5`; delta `+5`; total final `197/197` pares `METHOD + PATH` únicos.
- **QG-2:** baseline `192`; ya presentes `0`; nuevas `5`; delta `+5`; total final `197`. Se validan summary, description, tag `Operación y programación`, Bearer, policy real, path params, request bodies, schemas/XML docs y responses exactos para las cinco operaciones.
- **QG-3:** `DESACTIVADO`.
- **Examples nuevos:** `0`.

#### Archivos modificados

- `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/Dtos/TaskDefinitionDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/Dtos/TaskParameterDto.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/CalendarPolicyEnum.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/ConcurrencyPolicyEnum.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/PeriodicityTypeEnum.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/enums/TaskStatusEnum.cs`
- `tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs`
- `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`

Agregados: `0`. Eliminados: `0`.

#### Validación y limitaciones

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: exactamente 5 atributos `Http*`, 5 summaries, 5 descriptions, 0 aliases; policies y bodies verificados; cuerpos productivos de las cinco acciones sin cambios frente a r404b; QG-1 = 197 únicas; QG-2 suma las cinco operaciones SC-405 al baseline 192; QG-3 desactivado.
- `SchedulerController` permanece byte a byte idéntico a r404b.
- Permanecen fuera e intactos: SOAP-UAT/readiness pendiente, Integration Catalog pendiente, 5 operaciones configuration/administration de SC-401, remanente de certificados SC-306 y configuración NACHA reservada.
- La única limitación es la falta de runtime/SDK .NET; conforme DEC-009, SC-405 queda `VERIFY`, no `BLOCKED`.
- Siguiente unidad real: `SC-406 — Reports — grupo 1`, dejada `READY`; no ejecutada. Ninguna unidad posterior fue ejecutada.


### Resultado de SC-406 — Reports — grupo 1

**Estado:** `VERIFY` — implementación contractual completa; falta únicamente validación ejecutable .NET porque `dotnet` no está disponible.

**Decisión de alcance autorizada:** exclusivamente 12 operaciones de `ReportsController` para transacciones, devoluciones/rechazos, archivos NACHA y ciclos, con sus variantes PDF. Las 8 operaciones restantes quedan reservadas a `SC-407 — Reports — grupo 2` y no se ejecutan en esta unidad.

#### DEC-002 y routing efectivo

- Controller: `ReportsController`.
- Controllers `C = 1`.
- Acciones lógicas `L = 12`.
- Aliases `A = 0`.
- Operaciones OpenAPI efectivas `X = 12`.
- Resultado DEC-002: **CUMPLE** (`C <= 2` y `X <= 15`).
- Route base: `/api/reports`.
- Cada acción autorizada conserva un único `[HttpGet]`; no se modificó routing.
- Autorización real: `[Authorize]` a nivel controller y `[Authorize(Policy = "CanReadAch")]` en cada una de las 12 acciones; Bearer permanece publicado por la metadata vigente.

#### Operaciones documentadas

| # | METHOD + PATH | Acción | Query params reales | Response real |
|---:|---|---|---|---|
| 1 | `GET /api/reports/transactions/sent` | `GetSentTransactions` | `date`, `clearingHouseId`, `achCycleId`, `state`, `reference`, `bankId`, `transactionType`, `page`, `pageSize` | `200 AchTransactionReportResponseDto`; `400` body anónimo `{ message }` por `clearingHouseId` inválido/inexistente |
| 2 | `GET /api/reports/transactions/received` | `GetReceivedTransactions` | mismos filtros; `bankId` aplica a institución destino | `200 AchTransactionReportResponseDto`; `400` anónimo por cámara inválida/inexistente |
| 3 | `GET /api/reports/transactions/sent/pdf` | `GetSentTransactionsPdf` | filtros de transacciones sin paginación publicada | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |
| 4 | `GET /api/reports/transactions/received/pdf` | `GetReceivedTransactionsPdf` | filtros de transacciones sin paginación publicada | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |
| 5 | `GET /api/reports/returns` | `GetReturns` | `date`, `causal`, `clearingHouseId`, `state`, `reference`, `page`, `pageSize` | `200 AchReturnRejectionReportResponseDto`; `400` anónimo por cámara inválida/inexistente |
| 6 | `GET /api/reports/rejections` | `GetRejections` | mismos filtros | `200 AchReturnRejectionReportResponseDto`; `400` anónimo por cámara inválida/inexistente |
| 7 | `GET /api/reports/returns/pdf` | `GetReturnsPdf` | filtros de devoluciones sin paginación publicada | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |
| 8 | `GET /api/reports/rejections/pdf` | `GetRejectionsPdf` | filtros de rechazos sin paginación publicada | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |
| 9 | `GET /api/reports/nacha-files` | `GetNachaFiles` | `date`, `clearingHouseId`, `page`, `pageSize` | `200 AchNachaFileReportResponseDto`; `400` anónimo por cámara inválida/inexistente |
| 10 | `GET /api/reports/nacha-files/pdf` | `GetNachaFilesPdf` | `date`, `clearingHouseId` | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |
| 11 | `GET /api/reports/cycles` | `GetCycles` | `date`, `clearingHouseId`, `name`, `page`, `pageSize` | `200 AchCycleReportResponseDto`; `400` anónimo por cámara inválida/inexistente |
| 12 | `GET /api/reports/cycles/pdf` | `GetCyclesPdf` | `date`, `clearingHouseId`, `name` | `200 application/pdf`; `400` anónimo por cámara inválida/inexistente |

No se documentaron 401/403 solamente por `[Authorize]`, ni 404/409/413/422/500/503 especulativos. Los `400` publicados provienen de `ValidateClearingHouseIdAsync`: `clearingHouseId <= 0` o cámara inexistente; se conserva el body anónimo y no se inventa DTO/ProblemDetails.

#### Reglas demostradas por código

- Transacciones enviadas: excluyen `ReturnedByOperator`, `ReturnedByEpr` y registros con `ReturnReasonCode`; `bankId` filtra `SourceInstitutionId`.
- Transacciones recibidas: `bankId` filtra `DestinationInstitutionId`.
- En ambas: `date` compara `EffectiveEntryDate.Date`; `achCycleId` se recorta y compara por igualdad; `reference` busca por contenido en `Reference` o `TransactionExternalId`; `page <= 0` se normaliza a 1; `pageSize <= 0` a 25 y `pageSize > 200` a 200.
- Devoluciones: `ReturnReasonCode` coincide con patrones SQL `R__` o `DEV__`; rechazos: patrón `D__`. `date` compara `StateChangedAtUtc.Date`; `causal` se recorta y pasa a mayúsculas; `reference` busca por contenido.
- Archivos NACHA: `date` compara `GeneratedAtUtc.Date`; los totales se calculan antes de paginar.
- Ciclos: `date` compara `ProcessingDate.Date`; `name` se recorta y busca por contenido. Los totales se calculan sobre todos los ciclos filtrados; cada item deriva `Status` como `Programado`, `En ejecución` o `Cerrado` usando `DateTime.UtcNow`, `StartTime` y `EndTime`, considerando fin al día siguiente cuando `EndTime < StartTime`.
- Las variantes PDF construyen internamente filtros con `Page=1` y `PageSize=5000`; los servicios aplican su límite efectivo de 200 filas y mantienen totales sobre el conjunto filtrado completo.

#### Contratos/DTOs documentados

Se agregaron/mejoraron XML docs sin cambiar propiedades, tipos, nullability, defaults, JSON, DataAnnotations ni enums:

- `AchTransactionReportResponseDto`, `AchTransactionReportRowDto`, `AchTransactionReportTotalsDto`.
- `AchReturnRejectionReportResponseDto`, `AchReturnRejectionReportRowDto`, `AchReturnRejectionReportTotalsDto`.
- `AchNachaFileReportResponseDto`, `AchNachaFileReportRowDto`, `AchNachaFileReportTotalsDto`.
- `AchCycleReportResponseDto`, `AchCycleReportRowDto`, `AchCycleReportTotalsDto`.

Los filtros `AchTransactionReportFilter`, `AchReturnRejectionReportFilter`, `AchNachaFileReportFilter` y `AchCycleReportFilter` son objetos internos construidos por el controller a partir de parámetros query; no se publican como request body. `AchTransferStateEnum` y `TransactionTypeEnum` se reutilizan como parámetros existentes y no fueron modificados.

#### PDF: media type, filename y Content-Disposition

Los seis endpoints PDF devuelven `File(file.Content, file.ContentType, file.FileName)`. `QuestPdfReportGenerator` establece `ContentType = application/pdf`; MVC deriva `Content-Disposition` del `FileDownloadName` recibido.

| Operación | Content-Type | Filename generado |
|---|---|---|
| `transactions/sent/pdf` | `application/pdf` | `ACH_Sent_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `transactions/received/pdf` | `application/pdf` | `ACH_Received_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `returns/pdf` | `application/pdf` | `ACH_Returns_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `rejections/pdf` | `application/pdf` | `ACH_Rejections_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `nacha-files/pdf` | `application/pdf` | `ACH_NachaFiles_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `cycles/pdf` | `application/pdf` | `ACH_Cycles_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |

No se documentaron PDF/A, layout, orientación, columnas, encoding ni otros formatos no garantizados.

#### Reserva SC-407 — Reports — grupo 2

Las siguientes 8 operaciones permanecen fuera de QG-1/QG-2 de SC-406 y su metadata/cuerpos productivos permanecen intactos frente a `ACHInterbank-v124-scalar-r406-SC406.zip`:

1. `GET /api/reports/reconciliation`
2. `GET /api/reports/reconciliation/pdf`
3. `GET /api/reports/audit`
4. `GET /api/reports/audit/pdf`
5. `GET /api/reports/history`
6. `GET /api/reports/history/pdf`
7. `GET /api/reports/traceability/pdf`
8. `POST /api/reports/accounting-review/export`

`SC-407` queda `READY`; no se ejecuta en esta sesión.

#### Quality gates y validación

- QG-1 baseline `197`; operaciones SC-406 `12`; ya presentes `0`; nuevas `12`; delta `+12`; total final `209` operaciones únicas. Las 8 rutas SC-407 quedan excluidas del allowlist.
- QG-2 baseline `197`; ya presentes `0`; nuevas `12`; delta `+12`; total final `209`. Valida Summary, Description, tag `Reportes`, Bearer, policy `CanReadAch`, ausencia de headers falsos, query params exactos, ausencia de request body, schemas JSON, status codes `200/400`, `application/pdf` y documentación de `Content-Disposition`/filename en PDF.
- QG-3 permanece `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `dotnet test` ni `dotnet build`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco, referencias válidas).
- Cuerpos productivos de las 12 acciones autorizadas: byte/textualmente idénticos frente al baseline; solo se agregó metadata/documentación. Desde la primera acción reservada de SC-407 (`GetReconciliation`) hasta el final de `ReportsController` el contenido permanece byte a byte idéntico frente al baseline.
- Scheduler SC-404/SC-405, SOAP-UAT/readiness, Integration Catalog, configuration/administration de SC-401, remanente SC-306 y configuración NACHA reservada permanecen fuera de alcance.

#### Archivos modificados en SC-406

```text
src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs
src/Cfa.ACHInterbank.Application/Reports/Models/AchTransactionReportRowDto.cs
src/Cfa.ACHInterbank.Application/Reports/Models/AchReturnRejectionReportModels.cs
src/Cfa.ACHInterbank.Application/Reports/Models/AchNachaAndCycleReportModels.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.


### Resultado de SC-407 — Reports — grupo 2

**Estado:** `VERIFY` — implementación contractual completa; falta únicamente validación ejecutable .NET porque `dotnet` no está disponible.

**Decisión de alcance autorizada:** exclusivamente las 8 operaciones restantes de `ReportsController`: reconciliation, audit, history, traceability PDF y accounting-review export. SC-406 permanece cerrado e intacto; ninguna unidad posterior se ejecuta.

#### DEC-002 y routing efectivo

- Controller: `ReportsController`.
- Controllers `C = 1`.
- Acciones lógicas `L = 8`.
- Aliases `A = 0`.
- Operaciones OpenAPI efectivas `X = 8`.
- Resultado DEC-002: **CUMPLE** (`C <= 2` y `X <= 15`).
- Route base: `/api/reports`.
- Las siete consultas conservan un único `[HttpGet]` y el export un único `[HttpPost]`; no se modificó routing ni se agregaron aliases.
- Autorización real: `[Authorize]` a nivel controller y `[Authorize(Policy = "CanReadAch")]` en cada una de las 8 acciones; Bearer permanece publicado por la metadata vigente. No se publican 401/403 solo por autorización.

#### Operaciones documentadas

| # | METHOD + PATH | Acción | Parámetros/body reales | Response real |
|---:|---|---|---|---|
| 1 | `GET /api/reports/reconciliation` | `GetReconciliation` | query `date`, `clearingHouseId`, `achCycleId` | `200 AchReconciliationReportResponseDto`; `400` body anónimo `{ message }` por cámara inválida/inexistente |
| 2 | `GET /api/reports/reconciliation/pdf` | `GetReconciliationPdf` | mismos query params | `200 application/pdf`; `400` body anónimo por cámara inválida/inexistente |
| 3 | `GET /api/reports/audit` | `GetAudit` | query `user`, `action`, `entity`, `fromUtc`, `toUtc`, `page`, `pageSize` | `200 AchAuditReportResponseDto` |
| 4 | `GET /api/reports/audit/pdf` | `GetAuditPdf` | query `user`, `action`, `entity`, `fromUtc`, `toUtc` | `200 application/pdf` |
| 5 | `GET /api/reports/history` | `GetHistory` | query `fromUtc`, `toUtc`, `transactionId`, `toState`, `source`, `page`, `pageSize` | `200 AchHistoryReportResponseDto` |
| 6 | `GET /api/reports/history/pdf` | `GetHistoryPdf` | query `fromUtc`, `toUtc`, `transactionId`, `toState`, `source` | `200 application/pdf` |
| 7 | `GET /api/reports/traceability/pdf` | `GetTraceabilityPdf` | query `fromUtc`, `toUtc`, `state`, `achCycleId` | `200 application/pdf`; `400`, `408`, `500` con body anónimo `{ message }` |
| 8 | `POST /api/reports/accounting-review/export` | `ExportAccountingReview` | body `AccountingReviewExportApiRequest` | `200` archivo PDF/CSV/XLSX; `400` body anónimo `{ message }` |

#### Reglas demostradas por código

- Conciliación: `date` compara `EffectiveEntryDate.Date`; `clearingHouseId` filtra `AchCycle.ClearingHouseId`; `achCycleId` se recorta y compara por igualdad. Enviados excluyen `TransactionTypeEnum.Return`; recibidos incluyen estados `Certified` o `AppliedTacitly`; devueltos incluyen `ReturnedByOperator` o `ReturnedByEpr`. Las inconsistencias actuales son devolución sin causal (`INC-RET-NO-CAUSAL`), causal en estado no devuelto (`INC-CAUSAL-STATE`) y monto negativo (`INC-NEG-AMOUNT`).
- Auditoría: `user`, `action` y `entity` se recortan y buscan por contenido; `fromUtc`/`toUtc` se aplican inclusivamente sobre `ChangedAt`; `page <= 0` se normaliza a 1; `pageSize <= 0` a 25 y `pageSize > 200` a 200; orden descendente por `ChangedAt`.
- History: filtros inclusivos sobre `CreatedAt`, con filtros opcionales por `transactionId`, `ToState` y `Source`; misma normalización de paginación y orden descendente por `CreatedAt`.
- Los PDF de auditoría/history solicitan internamente `Page=1` y `PageSize=5000`, pero el servicio limita `pageSize` a 200.
- Trazabilidad PDF: sin fechas usa `DateTime.UtcNow` como fin y siete días antes como inicio; si falta un solo extremo completa 31 días. `fromUtc > toUtc` o un rango superior a 31 días devuelve `400`; timeout interno de 30 segundos sin cancelación del cliente devuelve `408`; una excepción capturada por la acción devuelve `500`.
- Accounting review: `Format` se recorta y compara sin distinguir mayúsculas. `pdf`, `csv`, `excel` y `xlsx` son los formatos aceptados. Body nulo, formato inválido (`ArgumentException`) o contenido generado vacío devuelve `400`. La acción no contabiliza ni crea asientos.

#### Contratos/DTOs documentados

Se agregaron/mejoraron XML docs sin cambiar propiedades, tipos, nullability, defaults, JSON, DataAnnotations, validators ni enums:

- `AchReconciliationReportResponseDto`, `AchReconciliationTotalsDto`, `AchReconciliationDifferencesDto`, `AchReconciliationInconsistencyDto`.
- `AchAuditReportResponseDto`, `AchAuditReportRowDto`.
- `AchHistoryReportResponseDto`, `AchHistoryReportRowDto`.
- `AccountingReviewExportApiRequest` y sus propiedades públicas.
- `TraceabilityReportFilter` recibió documentación XML como contrato interno del generador; no se publica como request body REST.

`GeneratedReportFile` y `AccountingReviewExportResult` permanecen contratos internos y no se publican como schemas REST. Enums compartidos ya existentes se reutilizan sin modificación.

#### Archivos: media types, filename y Content-Disposition

Las respuestas de archivo usan `File(content, contentType, fileName)`; MVC deriva `Content-Disposition` del `FileDownloadName` proporcionado.

| Operación | Content-Type real | Filename generado |
|---|---|---|
| `reconciliation/pdf` | `application/pdf` | `ACH_Reconciliation_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `audit/pdf` | `application/pdf` | `ACH_Audit_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `history/pdf` | `application/pdf` | `ACH_History_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `traceability/pdf` | `application/pdf` | `ACH_Traceability_{yyyyMMdd_HHmmss}.pdf` con timestamp UTC |
| `accounting-review/export`, `Format=pdf` | `application/pdf` | `accounting-review-reconciliation-{yyyyMMddHHmmss}.pdf` con timestamp UTC |
| `accounting-review/export`, `Format=csv` | `text/csv` | `accounting-review-reconciliation-{yyyyMMddHHmmss}.csv` con timestamp UTC |
| `accounting-review/export`, `Format=excel` o `xlsx` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | `accounting-review-reconciliation-{yyyyMMddHHmmss}.xlsx` con timestamp UTC |

No se documentan encoding/delimitador de salida, columnas, sheets, PDF/A, layout ni otros detalles de archivo no garantizados por el contrato HTTP inspeccionado.

#### Quality gates y validación

- QG-1 baseline `209`; operaciones SC-407 `8`; ya presentes `0`; nuevas `8`; delta `+8`; total final `217` operaciones únicas.
- QG-2 baseline `209`; ya presentes `0`; nuevas `8`; delta `+8`; total final `217`. Valida Summary, Description, tag `Reportes`, Bearer, policy `CanReadAch`, ausencia de headers falsos, query params exactos, request body del export, schemas/XML docs, status codes reales, media types y documentación de `Content-Disposition`/filename.
- QG-3 permanece `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `dotnet test` ni `dotnet build`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco, referencias válidas).
- Los cuerpos productivos de las 20 acciones de `ReportsController` permanecen iguales frente a `ACHInterbank-v124-scalar-r406b-SC406.zip`; SC-406 conserva su metadata y contratos sin reapertura.
- SOAP-UAT/readiness, Integration Catalog, configuration/administration de SC-401, remanente SC-306, configuración NACHA reservada y Scheduler SC-404/SC-405 permanecen fuera de alcance e intactos.
- Única limitación: ausencia de runtime/SDK .NET; conforme DEC-009, SC-407 queda `VERIFY`, no `BLOCKED`.
- Siguiente unidad real del plan: `SC-408 — Administración/catálogos — lote 1`, dejada `READY`; no ejecutada.

#### Archivos modificados en SC-407

```text
src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs
src/Cfa.ACHInterbank.Application/Reports/Export/Interfaces/IAccountingReviewExportAppService.cs
src/Cfa.ACHInterbank.Application/Reports/Models/AchAuditHistoryReportModels.cs
src/Cfa.ACHInterbank.Application/Reports/Models/AchReconciliationReportModels.cs
src/Cfa.ACHInterbank.Application/Reports/Models/TraceabilityReportFilter.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

### Resultado de SC-408 — Administración/catálogos — lote 1

**Estado:** `VERIFY` — implementación contractual completa; falta únicamente validación ejecutable .NET porque `dotnet` no está disponible.

**Decisión de alcance autorizada:** exclusivamente `IntegrationCatalogController` (4 operaciones) y las 5 operaciones `configuration/administration` pendientes de `AchColombiaFileExchangeController`. No se reabren SC-401/SC-402 ni se absorben SOAP-UAT/readiness, remanente de certificados o configuración NACHA.

#### DEC-002 y routing efectivo

- Controllers `C = 2`: `IntegrationCatalogController` y `AchColombiaFileExchangeController`.
- Acciones lógicas `L = 9`.
- Aliases `A = 0`: ambos controllers conservan un único `[Route]` y cada acción autorizada un solo atributo `Http*`.
- Operaciones OpenAPI efectivas `X = 9`.
- Resultado DEC-002: **CUMPLE** (`C <= 2` y `X <= 15`).
- `IntegrationCatalogController`: policy `CanReadAch` en sus 4 operaciones; tag `Integraciones`; Bearer por `[Authorize]`.
- ACH Colombia configuration/administration: `CanReadAch` en los dos GET y `CanManageAch` en los tres PUT; tag `ACH Colombia`; Bearer por `[Authorize]`.
- No se documentaron 401/403 por convención ni headers falsos `Authorization`, `Idempotency-Key` o `X-Correlation-ID`.

#### Operaciones documentadas

| # | METHOD + PATH | Acción | Contrato real | Responses documentados |
|---:|---|---|---|---|
| 1 | `GET /api/integrations/methods` | `GetMethods` | sin parámetros/body | `200 IReadOnlyCollection<IntegrationMethodDto>` |
| 2 | `GET /api/integrations/methods/{methodId}/parameters` | `GetParametersByMethod` | path `methodId:int` | `200 IReadOnlyCollection<IntegrationMethodParameterDto>`; colección vacía si no hay parámetros activos |
| 3 | `GET /api/integrations/source-catalog` | `GetSourceCatalog` | query opcional `methodId` | `200 IReadOnlyCollection<IntegrationSourceCatalogFieldDto>` |
| 4 | `GET /api/integrations/transformations` | `GetTransformations` | sin parámetros/body | `200 IReadOnlyCollection<IntegrationTransformationCatalogDto>` |
| 5 | `GET /api/ach-colombia/file-exchange/configuration` | `GetConfiguration` | sin parámetros/body | `200 AchManagedFileTransferConfigurationDto` |
| 6 | `PUT /api/ach-colombia/file-exchange/configuration` | `UpdateConfiguration` | body `AchManagedFileTransferConfigurationDto` | `200 AchManagedFileTransferConfigurationDto`; `409 ProblemDetails` por conflicto de `ConcurrencyToken` traducido por el controller |
| 7 | `GET /api/ach-colombia/file-exchange/administration` | `GetAdministration` | sin parámetros/body | `200 AchManagedMftAdministrationDto` |
| 8 | `PUT /api/ach-colombia/file-exchange/administration` | `UpdateAdministration` | body `UpdateAchManagedMftAdministrationRequest` | `200 AchManagedMftAdministrationDto`; `400 ProblemDetails` por `ArgumentException`; `409 ProblemDetails` por concurrencia |
| 9 | `PUT /api/ach-colombia/file-exchange/administration/credential` | `SetCredential` | body `SetAchManagedMftCredentialRequest` | `200 AchManagedMftAdministrationDto`; `400 ProblemDetails` por credencial inválida |

`UpdateConfiguration` no publica un 400 especulativo: aunque el servicio valida valores, esa acción solo traduce explícitamente `DbUpdateConcurrencyException`; SC-408 documenta el contrato HTTP que el controller materializa.

#### Reglas demostradas y DTOs/schemas

- `GetMethodsAsync` ejecuta el bootstrap del catálogo, consulta métodos sin tracking, los ordena por `Id` y agrega la clasificación funcional calculada por `ClassifyMethod`.
- Los parámetros de método se limitan a `IsActive`, se ordenan por `SortOrder`/`ParameterPath` y un methodId sin coincidencias devuelve colección vacía, no 404.
- `source-catalog` sin `methodId` devuelve campos activos; con filtro incluye campos globales (`MethodId == null`) y los del método solicitado, ordenados por `SourceKind`, `SortOrder` y `FieldPath`.
- Transformations devuelve el catálogo en memoria definido por el servicio, incluido `SupportsFormatMask` y `SupportsMultipleSources`; SC-408 no inventa catálogos adicionales.
- Se agregaron XML docs a `IntegrationMethodDto`, `IntegrationMethodParameterDto`, `IntegrationSourceCatalogFieldDto` e `IntegrationTransformationCatalogDto` sin cambiar sus propiedades, tipos, nullability, defaults, JSON ni enums.
- Se agregaron XML docs a `AchManagedFileTransferConfigurationDto`, `AchManagedMftAdministrationDto`, `UpdateAchManagedMftAdministrationRequest` y `SetAchManagedMftCredentialRequest`. Las restricciones descritas provienen del servicio: retries `0..20`; retention `1..3650`; retry delay administrativo `0..86400`; port opcional `1..65535`; textos requeridos/normalizados según `LimitRequired`/`Optional`.
- `SetCredentialAsync` exige `CredentialType` y `Secret` no vacíos. El secret se recibe solo en el request, se protege antes de persistirse y **no aparece** en `AchManagedMftAdministrationDto`; el response expone únicamente `CredentialConfigured`, `CredentialType` y `CredentialUpdatedAtUtc` además del resto del perfil. No se agregaron examples de credenciales.

#### Quality gates y validación

- QG-1 baseline `217`; operaciones SC-408 `9`; ya presentes `0`; nuevas `9`; delta `+9`; total final `226` operaciones únicas.
- QG-2 baseline `217`; ya presentes `0`; nuevas `9`; delta `+9`; total final `226`. Valida Summary/Description, tags, Bearer, policies, route constraints, parámetros path/query, request bodies, schemas/XML docs, responses exactos, ausencia de aliases y ausencia de headers/status especulativos. Para credential verifica además ausencia de `example/examples` y que el schema de response no contiene `secret`.
- QG-3 permanece `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `dotnet test` ni `dotnet build`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Revisión estática: QG-1 `226/226` pares únicos; bloque QG-2 SC-408 `9/9` único; 2 controllers, 9 acciones, 0 aliases; QG-3 sin símbolo activo.
- Las 9 acciones SC-401 del mismo `AchColombiaFileExchangeController` son byte a byte idénticas al baseline hasta el inicio del bloque SC-408. `IntegrationMappingSetsController`, Scheduler y Reports también permanecen intactos.
- SOAP-UAT/readiness, remanente de certificados SC-306 y configuración NACHA reservada permanecen fuera e intactos.
- La única limitación es la ausencia del SDK/runtime .NET; conforme DEC-009, SC-408 queda `VERIFY`, no `BLOCKED`.
- Siguiente unidad real: `SC-409 — Administración/catálogos — lote 2`, dejada `READY`; no ejecutada ni preasignada a reservas pendientes.

#### Archivos modificados en SC-408

```text
src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs
src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs
src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs
src/Cfa.ACHInterbank.Application/ACH/Models/AchManagedFileTransferModels.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

### Resultado de SC-409 — Administración/catálogos — lote 2

**Estado:** `VERIFY` — implementación contractual completa; falta únicamente validación ejecutable .NET porque `dotnet` no está disponible.

**Decisión de alcance autorizada:** exclusivamente el bloque histórico **“Configuración NACHA — lectura”**: 5 acciones GET de `NachaConfigProfilesController` y las 6 acciones GET de `NachaConfigProfilesReadOnlyController`. Las 12 operaciones no-GET de `NachaConfigProfilesController` permanecen reservadas como **“Configuración NACHA — gestión/mutación pendiente”**, sin ID nuevo ni reasignación automática.

#### DEC-002 y routing efectivo

- Controllers `C = 2`.
- Acciones lógicas GET `L = 11`.
- Aliases `A = 0`: ambos controllers conservan un único `[Route]` y cada acción autorizada un único `[HttpGet]`.
- Operaciones OpenAPI efectivas `X = 11`.
- Resultado DEC-002: **CUMPLE** (`C <= 2` y `X <= 15`).
- `NachaConfigProfilesController`: policy `P1.ConfigRead` en los 5 GET.
- `NachaConfigProfilesReadOnlyController`: policy `P1.NachaRead` en los 6 GET.
- Ambos controllers conservan `[Authorize]`; Bearer se mantiene por la configuración OpenAPI vigente. No se documentaron 401/403 por convención ni headers falsos `Authorization`, `Idempotency-Key` o `X-Correlation-ID`.

#### Operaciones documentadas

| # | METHOD + PATH | Acción | Response HTTP real |
|---:|---|---|---|
| 1 | `GET /nacha-config/perfiles` | `GetProfiles` | `200 IReadOnlyList<NachaConfigProfileListItemDto>` |
| 2 | `GET /nacha-config/catalogos-filtro` | `GetFilterCatalogs` | `200 NachaConfigFilterCatalogsDto` |
| 3 | `GET /nacha-config/perfiles/{id}` | `GetProfile` | `200 NachaConfigProfileDetailDto`; `404` sin body |
| 4 | `GET /nacha-config/perfiles/{id}/historial` | `History` | `200 IReadOnlyList<NachaConfigHistoryItemDto>` |
| 5 | `GET /nacha-config/perfiles/{id}/snapshots` | `Snapshots` | `200 IReadOnlyList<NachaConfigSnapshotItemDto>` |
| 6 | `GET /api/ach/nacha/config-profiles/dashboard` | `GetDashboard` | `200 NachaConfigProfilesDashboardReadModel` |
| 7 | `GET /api/ach/nacha/config-profiles` | `GetProfiles` | `200 IReadOnlyList<NachaConfigProfileReadModel>` |
| 8 | `GET /api/ach/nacha/config-profiles/{id}` | `GetProfile` | `200 NachaConfigProfileDetailReadModel`; `404` sin body |
| 9 | `GET /api/ach/nacha/config-profiles/by-code/{profileCode}` | `GetProfileByCode` | `200 NachaConfigProfileDetailReadModel`; `404` sin body |
| 10 | `GET /api/ach/nacha/config-profiles/{id}/variants` | `GetVariants` | `200 IReadOnlyList<NachaConfigProfileVariantReadModel>` |
| 11 | `GET /api/ach/nacha/config-profiles/{id}/fields` | `GetFields` | `200 IReadOnlyList<NachaConfigProfileFieldReadModel>` |

#### Contratos y reglas demostradas

- La lista administrativa ordena por `UpdatedAt` descendente; el detalle ordena registros por secuencia, variantes por código de registro/prioridad, campos por posición inicial y reglas por id.
- `catalogos-filtro` devuelve estados sin filtro de actividad y cámaras/flujos/direcciones/servicios activos, ordenados por código.
- Historial y snapshots filtran por `ProfileId` y ordenan sus fechas UTC en orden descendente; ausencia de filas se representa como colección vacía.
- El read model lista perfiles por cámara y código. `IsPublished` se deriva de `Status == PUBLICADO`; `IsCurrent` se calcula contra `DateTime.UtcNow` y la vigencia persistida.
- La consulta `by-code` recorta espacios exteriores antes de comparar `ProfileCode` por igualdad.
- Variantes se ordenan por tipo de registro/prioridad; `IsActive` exige variante predeterminada y estado `PUBLICADO`; `BlockingFactor` usa el valor entero configurado en `BLOCKINGFACTOR` o `10` cuando no puede resolverse.
- Campos se ordenan por tipo de registro/posición inicial y el read model deriva `EndPosition`, dirección de padding, `IsComputed` e `IsControlTotalField` según la implementación existente.
- XML docs se agregaron únicamente a contratos alcanzados por lectura: familia `NachaConfigProfile*`, `NachaConfigFilterCatalog*`, historial/snapshots y read models de dashboard/perfil/variante/campo. No se cambiaron propiedades, tipos, nullability, defaults, JSON, DataAnnotations ni enums.

#### Quality gates y validación

- QG-1 baseline `226`; operaciones SC-409 `11`; ya presentes `0`; nuevas `11`; delta `+11`; total final `237` operaciones únicas.
- QG-2 baseline `226`; ya presentes `0`; nuevas `11`; delta `+11`; total final `237`. Valida Summary/Description, tag `NACHA-M`, Bearer, policies, route constraints, parámetros path exactos, ausencia de body, schemas/XML docs y status codes reales.
- Las 12 mutaciones NACHA **no** se agregaron a QG-1/QG-2 y su metadata/lógica permanecen intactas.
- QG-3 permanece `DESACTIVADO`.
- Examples nuevos: `0`.
- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `dotnet test` ni `dotnet build`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Única limitación: ausencia del SDK/runtime .NET; conforme DEC-009, SC-409 queda `VERIFY`, no `BLOCKED`.
- Siguiente unidad real del tracker: `SC-410 — Auditoría, logs y observabilidad expuesta`, dejada `READY`; no ejecutada.
- SOAP-UAT/readiness y remanente de certificados SC-306 continúan pendientes; no se reabrieron Scheduler, Reports ni SC-408.

#### Archivos modificados en SC-409

```text
src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs
src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesReadOnlyController.cs
src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs
src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigProfileReadModels.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

### Resultado de SC-410 — Auditoría, logs y observabilidad expuesta

**Estado:** `VERIFY` — implementación documental completa sobre la partición humana autorizada. La validación ejecutable .NET queda pendiente únicamente porque `dotnet` no está disponible en el entorno.

#### Alcance autorizado, inventario y DEC-002

- Controllers MVC: `AuditLogsController`, `NavigationLogsController` (`2`).
- Acciones controller: `GET /api/audit-logs`, `GET /api/navigation-logs`, `POST /api/navigation-logs` (`3`).
- Superficies HTTP no-controller: `GET /health/live`, `GET /health/ready` (`2`).
- Aliases reales: `0`.
- Operaciones HTTP/OpenAPI efectivas: `5`.
- DEC-002: **CUMPLE** (`2 <= 2` controllers y `5 <= 15` endpoints; las dos rutas health cuentan como endpoints sin incrementar controllers).

`GET /api/auth-logs` queda explícitamente reservado como **“Auditoría/logs — Auth Logs pendiente”**, sin ID asignado y sin cambios. `OutgoingTransactionMonitoringController` conserva sus dos GET bajo **“Trazabilidad y conciliación”** y no se reasigna ni entra en QG SC-410.

#### Contratos documentados

| METHOD + PATH | Auth/policy real | Query/body real | Responses reales documentadas |
|---|---|---|---|
| `GET /api/audit-logs` | controller `[Authorize]`, sin policy nominal adicional | query `startDate`, `endDate`, `changedBy`, `action`, `page=1`, `pageSize=50` | `200 BaseResponseModel`; en éxito `data` contiene `PagedResponse<AuditLogDto>` |
| `GET /api/navigation-logs` | controller `[Authorize]` + `CanReadAch` | query `startDate`, `endDate`, `userId`, `route`, `page=1`, `pageSize=50` | `200 BaseResponseModel`; en éxito `data` contiene `PagedResponse<NavigationLogDto>` |
| `POST /api/navigation-logs` | controller `[Authorize]`, sin policy nominal adicional | JSON `NavigationLogCreate` (`Route`, `VisitedAt?`, `SessionId?`, `DurationMs?`) | `200 BaseResponseModel`; `400 BaseResponseModel` únicamente cuando la ruta normalizada queda vacía (`NAV_ROUTE_REQUIRED`) |
| `GET /health/live` | `.AllowAnonymous()` | sin query/body | `200`; body JSON anónimo con `status`, `check`, `service`, `timestampUtc` |
| `GET /health/ready` | `.AllowAnonymous()` | sin query/body | `200` o `503`; body JSON anónimo con `status`, `check`, `database`, `dataProtectionKeyRing`, `scheduler`, `persistentStore`, `clusterInstance`, `timestampUtc` |

No se agregaron `401/403` por inferencia de autorización ni otros status especulativos. No se inventó DTO de health.

DTOs/tipos realmente alcanzados y documentados: `AuditLogQuery`, `AuditLogDto`, `NavigationLogQuery`, `NavigationLogDto`, `NavigationLogCreate`, `PagedResponse<T>` y `BaseResponseModel`. En HTTP, los query DTOs se materializan como parámetros escalares del controller; el schema nombrado de request es `NavigationLogCreate` y el response controller directo sigue siendo `BaseResponseModel`. Los tipos paginados contenidos en `data` dinámico se describen sin alterar el contrato JSON.

#### Health / observabilidad pública

Las dos rutas siguen implementadas con `MapGet` y sus handlers productivos originales. Se extrajo exclusivamente el mapeo a `MapPublicHealthEndpoints` para reutilizarlo en el test de generación OpenAPI y se agregó metadata estándar (`WithTags`, `WithSummary`, `WithDescription`, `Produces`) sin cambiar lógica, routing, autenticación, timeout, comprobaciones, status codes ni body.

- `/health/live`: anónimo; `application/json`; `200`; body anónimo `status=Healthy`, `check=live`, `service=ACHInterbank`, `timestampUtc`; no comprueba dependencias.
- `/health/ready`: anónimo; `application/json`; `200` o `503`; conserva conectividad de DB, claves Data Protection y estado de scheduler cuando los servicios están registrados, timeout enlazado de 5 segundos y catches existentes; el body permanece anónimo/dinámico con los campos reales del handler.

#### Quality gates y ejemplos

- QG-1 baseline: `237`; operaciones SC-410: `5`; ya presentes: `0`; nuevas: `5`; delta: `+5`; total final: `242` METHOD + PATH únicos.
- QG-2 baseline: `237`; operaciones SC-410: `5`; ya presentes: `0`; nuevas: `5`; delta: `+5`; total final: `242`. El test exige que el conjunto SC-410 de QG-2 sea exactamente el mismo que QG-1.
- QG-2 valida Bearer/security de controllers, anonymous health, tag, policy nominal `CanReadAch` solo donde existe, query, request body de navigation, XML/schema docs, responses, descripciones, health status/media types y ausencia de headers/status inventados.
- QG-3: `DESACTIVADO`.
- Examples nuevos: `0`.

#### Validación y reservas

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test ...OpenApiDocumentGenerationTests` y `dotnet build ACHInterbank.sln -c Release`: no ejecutados ni simulados por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0` (`SOLUTION GRAPH: OK`; 7 proyectos en solución, 7 en disco y referencias válidas).
- Validación estática: QG-1 `242/242` únicos; las cinco operaciones SC-410 tenían conteo `0` en r410 y `1` cada una en r410b; `GET /api/auth-logs` y los dos Outgoing Monitoring tienen conteo `0` en QG-1 SC-410.
- Reservas intactas: Auth Logs pendiente; 12 mutaciones NACHA; SOAP-UAT/readiness (`NachaSoapUatConsoleController`, `NachaOperationalReadinessController`); remanente de certificados SC-306; Outgoing Monitoring; Scheduler; Reports. SC-408/SC-409 no se reabrieron.
- Siguiente unidad real: `SC-490 — Checkpoint Fase E`, dejada `READY`; no ejecutada. AuthLogsController no se asigna automáticamente a SC-490.

#### Archivos modificados en SC-410

```text
src/Cfa.ACHInterbank.Api/Controllers/AuditLogsController.cs
src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs
src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs
src/Cfa.ACHInterbank.Application/Audit/Dtos/AuditLogDtos.cs
src/Cfa.ACHInterbank.Application/Common/PagedResponse.cs
src/Cfa.ACHInterbank.Application/NavigationLogs/Dtos/NavigationLogDtos.cs
src/Cfa.ACHInterbank.Domain/Models/Configurations/BaseResponseModel.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.


### Resultado de SC-490 — Checkpoint Fase E

**Estado:** `VERIFY`  
**Objetivo exacto del plan:** `Checkpoint Fase E`.  
**Tipo de unidad:** checkpoint acumulado; no documenta endpoints nuevos, no modifica contratos OpenAPI y no resuelve reservas pendientes.  
**Rango real de Fase E según tracker:** `SC-401..SC-410`.  
**Baseline previo a Fase E:** `155` operaciones.  
**Total Fase E:** `87` operaciones.  
**Total global posterior a Fase E:** `242` operaciones.

#### Inventario consolidado de Fase E

| Unidad | Controllers de la unidad | Minimal APIs | Operaciones efectivas | Aliases | Estado | QG-1 incorporado | QG-2 incorporado | DEC-002 histórico |
|---|---:|---:|---:|---:|---|---:|---:|---|
| `SC-401` | 1 | 0 | 9 | 0 | `VERIFY` | 9 | 9 | CUMPLE |
| `SC-402` | 1 | 0 | 12 | 0 | `VERIFY` | 12 | 12 | CUMPLE |
| `SC-403` | 2 | 0 | 3 | 0 | `VERIFY` | 3 | 3 | CUMPLE |
| `SC-404` | 1 | 0 | 13 | 0 | `VERIFY` | 13 | 13 | CUMPLE |
| `SC-405` | 1 | 0 | 5 | 0 | `VERIFY` | 5 | 5 | CUMPLE |
| `SC-406` | 1 | 0 | 12 | 0 | `VERIFY` | 12 | 12 | CUMPLE |
| `SC-407` | 1 | 0 | 8 | 0 | `VERIFY` | 8 | 8 | CUMPLE |
| `SC-408` | 2 | 0 | 9 | 0 | `VERIFY` | 9 | 9 | CUMPLE |
| `SC-409` | 2 | 0 | 11 | 0 | `VERIFY` | 11 | 11 | CUMPLE |
| `SC-410` | 2 | 2 | 5 | 0 | `VERIFY` | 5 | 5 | CUMPLE |

La suma reconstruida es `9 + 12 + 3 + 13 + 5 + 12 + 8 + 9 + 11 + 5 = 87`. Las 87 claves `METHOD + PATH` son únicas y aparecen tanto en QG-1 como en QG-2. SC-490 incorpora `0` controllers nuevos, `0` operaciones nuevas y `0` aliases; DEC-002 **NO APLICA** como partición funcional nueva del checkpoint.

#### QG-1 / QG-2

- QG-1 reconstruido estáticamente desde `OpenApiDocumentGenerationTests.cs`: `242/242` claves `METHOD + PATH` únicas, `0` duplicados.
- QG-2 reconstruido desde los arrays de operaciones validados por el test: `242/242` claves únicas, `0` duplicados.
- Diferencia de conjuntos: `QG-1 - QG-2 = 0` y `QG-2 - QG-1 = 0`.
- Fase E esperada: `87`; Fase E real en QG-1: `87`; Fase E real en QG-2: `87`; diferencia: `0`.
- SC-490 no cambia allowlists: delta QG-1 = `0`, delta QG-2 = `0`, total final = `242`.
- Los helpers QG-2 existentes de SC-401..SC-410 conservan validaciones contractuales de Summary/Description, tag, Bearer/security o anonimato, policy nominal cuando aplica, parámetros path/query, request bodies, schemas/XML docs, responses, media types, filenames/headers contractuales y negativos de headers inventados según cada operación.
- QG-3 permanece `DESACTIVADO`.
- Examples nuevos en SC-490: `0`.

#### Reservas verificadas fuera de QG

Se comprobó que las reservas siguientes tienen `0` coincidencias en QG-1 y `0` en QG-2:

- **Auditoría/logs — Auth Logs pendiente:** `GET /api/auth-logs` (`1` operación), sin ID.
- **Configuración NACHA — gestión/mutación pendiente:** `12` operaciones no-GET de `NachaConfigProfilesController`, sin ID.
- **NACHA SOAP-UAT/readiness — pendiente:** `NachaSoapUatConsoleController` (`4`) + `NachaOperationalReadinessController` (`7`) = `11`, sin ID.
- **Certificados — remanente de gestión SC-306:** `activate`, `revoke`, `delete`, `validate`, `audit` bajo dos prefijos = `10` operaciones, sin ID.
- **OutgoingTransactionMonitoringController:** `2` operaciones; conserva clasificación `Trazabilidad y conciliación` y no se reasigna.

#### Validación estructural y técnica

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y todos los `ProjectReference` válidos.
- Revisión estructural de `1.790` archivos `.cs` bajo `src/` y `tests/`: delimitadores `{}`, `()` y `[]` balanceados; `0` archivos con error estructural detectado por la revisión estática.
- Búsqueda de merge markers en el árbol: `0`.
- Configuración OpenAPI vigente intacta: `AddOpenApi("v1")`, 6 operation transformers (`OpenApiOperationIdTransformer`, `OpenApiIntegrationTagsTransformer`, `OpenApiTransversalHeadersTransformer`, `OpenApiExamplesTransformer`, `OpenApiResponseDescriptionsTransformer`, `OpenApiSecurityMetadataTransformer`) y 2 document transformers (`OpenApiDocumentMetadataTransformer`, `OpenApiBearerSecuritySchemeTransformer`).
- `MapOpenApi("/openapi/{documentName}.json")`, `MapScalarApiReference(...)` y `MapPublicHealthEndpoints()` permanecen registrados.
- `OpenApiDocumentGenerationTests.cs`, controllers, DTOs y configuración productiva no se modificaron en SC-490.

La ausencia de `dotnet` es la única limitación real de validación. No se detectó una inconsistencia que invalide el checkpoint; por DEC-009, SC-490 queda `VERIFY`, no `BLOCKED`.

#### Control de cambios de SC-490

Modificado:

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

La siguiente unidad real del tracker es `SC-501 — Inventario de aliases/rutas duplicadas y ruta canónica`, dejada `READY`; no se ejecuta en SC-490 y no se asignan reservas pendientes.

### SC-501 — Inventario de aliases/rutas duplicadas y ruta canónica

**Estado:** `VERIFY` — inventario transversal completado sin cambios de routing, contratos, controllers, DTOs ni quality gates. La ejecución .NET continúa pendiente porque `dotnet --info` devuelve exit `127`.

#### Alcance y reconstrucción de routing

- Se revisaron los `71` archivos de controllers de `Cfa.ACHInterbank.Api`, con `370` atributos `Http*` físicos.
- La combinación de prefijos de controller materializa `422` selectores `METHOD + PATH`: `414` visibles para ApiExplorer y `8` rutas públicas explícitamente ignoradas por ApiExplorer (`JwksController` 4, `ServersController` 1, `SobreDigitalController.testRSA` 1 y `TestsController` 2).
- Se revisaron también los Minimal APIs explícitos de `DependencyInjectionService`: `/`, `/index.html`, `/health/live` y `/health/ready`, además de la infraestructura `MapOpenApi`/Scalar. Los dos health siguen siendo las únicas Minimal APIs incluidas en QG; root/index son redirects y no se incorporan al gate.
- QG-1 y QG-2 siguen definiendo el conjunto contractual gobernado actual: `242` operaciones únicas y exactamente el mismo conjunto. Las `242/242` operaciones se pudieron relacionar estáticamente con un selector de controller o con `/health/live`/`/health/ready`.
- SC-501 incorpora `0` controllers y `0` operaciones nuevas. DEC-002 no aplica como nueva partición funcional; el inventario es transversal.

#### Resultado consolidado

- **Aliases reales:** `52` grupos, `104` claves `METHOD + PATH` involucradas, distribuidos en 8 controllers: `AchCyclesController` 7, `CertificateManagementController` 11, `ClearingHousesController` 10, `MenuItemsController` 4, `NachaRecordDefinitionsController` 5, `NachaRecordLayoutsController` 5, `NavigationController` 1 y `TransactionsController` 9.
- De esas 104 claves de alias, `48` ya pertenecen a QG-1/QG-2 y `56` permanecen fuera. En `6` grupos de certificados ambas variantes forman parte de QG; en `36` grupos solo una variante representativa está en QG; `10` grupos están completamente fuera del gate.
- **Duplicados exactos `METHOD + PATH`: `0`**, incluso al incluir las rutas ignoradas por ApiExplorer.
- **Colisiones de template reales: `0`**. La normalización case-insensitive, slash final y nombres de parámetros no produjo dos acciones distintas con el mismo template. Se analizaron constraints `:int`, `:guid`, `:long` y parámetros sin constraint. Se detectaron `34` pares de solapamiento literal/parámetro potencial, todos resueltos por precedencia de literal y/o constraints; no se califican como colisión.
- **Canónicas definidas:** `10` grupos — las cinco acciones de `NachaRecordDefinitionsController` y las cinco de `NachaRecordLayoutsController`, conforme a la evidencia explícita de SC-303 que denomina canónicas las variantes sin `/api`.
- **Canónicas pendientes:** `42` grupos. No se usa presencia en QG, orden de atributos ni preferencia estética como sustituto de una decisión contractual.
- **Equivalencias no confirmadas:** `0`. Las dos superficies `[Obsolete]` de definiciones/layouts NACHA se registran como legacy con reemplazo funcional hacia `nacha-config profiles`, pero no se declara una equivalencia uno-a-uno con los endpoints oficiales. Similitudes como navigation/menu frente a menu-items o las familias de certificados no se consolidan porque no comparten la misma acción/contrato.

#### Inventario ALIAS-* de SC-501

| ID | Controller/action | Method | Rutas efectivas reales | Canónica | Alias/legacy | Evidencia | QG actual | Tag OpenAPI | Ownership |
|---|---|---|---|---|---|---|---|---|---|
| `ALIAS-001` | `AchCyclesController.Get` | `GET` | `/ach-cycles`<br>`/api/ach-cycles` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles`: QG-1/QG-2<br>`/api/ach-cycles`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-002` | `AchCyclesController.GetExportable` | `GET` | `/ach-cycles/exportable`<br>`/api/ach-cycles/exportable` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles/exportable`: QG-1/QG-2<br>`/api/ach-cycles/exportable`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-003` | `AchCyclesController.GetById` | `GET` | `/ach-cycles/{id}`<br>`/api/ach-cycles/{id}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles/{id}`: QG-1/QG-2<br>`/api/ach-cycles/{id}`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-004` | `AchCyclesController.Create` | `POST` | `/ach-cycles`<br>`/api/ach-cycles` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles`: QG-1/QG-2<br>`/api/ach-cycles`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-005` | `AchCyclesController.Update` | `PUT` | `/ach-cycles/{id}`<br>`/api/ach-cycles/{id}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles/{id}`: QG-1/QG-2<br>`/api/ach-cycles/{id}`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-006` | `AchCyclesController.RepairConfigurationLinks` | `POST` | `/ach-cycles/repair-configuration-links`<br>`/api/ach-cycles/repair-configuration-links` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles/repair-configuration-links`: QG-1/QG-2<br>`/api/ach-cycles/repair-configuration-links`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-007` | `AchCyclesController.Delete` | `DELETE` | `/ach-cycles/{id}`<br>`/api/ach-cycles/{id}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/ach-cycles/{id}`: QG-1/QG-2<br>`/api/ach-cycles/{id}`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-008` | `CertificateManagementController.PreviewManagedAsync` | `POST` | `/nacha-security/certificates/management/managed/preview`<br>`/api/nacha-security/certificates/management/managed/preview` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/managed/preview`: QG-1/QG-2<br>`/api/nacha-security/certificates/management/managed/preview`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-009` | `CertificateManagementController.SaveManagedAsync` | `POST` | `/nacha-security/certificates/management/managed`<br>`/api/nacha-security/certificates/management/managed` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/managed`: QG-1/QG-2<br>`/api/nacha-security/certificates/management/managed`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-010` | `CertificateManagementController.UploadPublicAsync` | `POST` | `/nacha-security/certificates/management/public`<br>`/api/nacha-security/certificates/management/public` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/public`: QG-1/QG-2<br>`/api/nacha-security/certificates/management/public`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-011` | `CertificateManagementController.UploadPrivateAsync` | `POST` | `/nacha-security/certificates/management/private`<br>`/api/nacha-security/certificates/management/private` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/private`: QG-1/QG-2<br>`/api/nacha-security/certificates/management/private`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-012` | `CertificateManagementController.ListAsync` | `GET` | `/nacha-security/certificates/management`<br>`/api/nacha-security/certificates/management` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management`: QG-1/QG-2<br>`/api/nacha-security/certificates/management`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-013` | `CertificateManagementController.ListVersionsAsync` | `GET` | `/nacha-security/certificates/management/{id:int}/versions`<br>`/api/nacha-security/certificates/management/{id:int}/versions` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/{id:int}/versions`: QG-1/QG-2<br>`/api/nacha-security/certificates/management/{id:int}/versions`: QG-1/QG-2 | `Seguridad y certificados` | `SC-306` |
| `ALIAS-014` | `CertificateManagementController.ActivateAsync` | `POST` | `/nacha-security/certificates/management/versions/{id:int}/activate`<br>`/api/nacha-security/certificates/management/versions/{id:int}/activate` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/versions/{id:int}/activate`: fuera de QG<br>`/api/nacha-security/certificates/management/versions/{id:int}/activate`: fuera de QG | `Seguridad y certificados` | `Reserva certificados sin ID` |
| `ALIAS-015` | `CertificateManagementController.RevokeAsync` | `POST` | `/nacha-security/certificates/management/versions/{id:int}/revoke`<br>`/api/nacha-security/certificates/management/versions/{id:int}/revoke` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/versions/{id:int}/revoke`: fuera de QG<br>`/api/nacha-security/certificates/management/versions/{id:int}/revoke`: fuera de QG | `Seguridad y certificados` | `Reserva certificados sin ID` |
| `ALIAS-016` | `CertificateManagementController.DeleteAsync` | `DELETE` | `/nacha-security/certificates/management/versions/{id:int}`<br>`/api/nacha-security/certificates/management/versions/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/versions/{id:int}`: fuera de QG<br>`/api/nacha-security/certificates/management/versions/{id:int}`: fuera de QG | `Seguridad y certificados` | `Reserva certificados sin ID` |
| `ALIAS-017` | `CertificateManagementController.ValidateAsync` | `POST` | `/nacha-security/certificates/management/versions/{id:int}/validate`<br>`/api/nacha-security/certificates/management/versions/{id:int}/validate` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/versions/{id:int}/validate`: fuera de QG<br>`/api/nacha-security/certificates/management/versions/{id:int}/validate`: fuera de QG | `Seguridad y certificados` | `Reserva certificados sin ID` |
| `ALIAS-018` | `CertificateManagementController.AuditAsync` | `GET` | `/nacha-security/certificates/management/audit`<br>`/api/nacha-security/certificates/management/audit` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/nacha-security/certificates/management/audit`: fuera de QG<br>`/api/nacha-security/certificates/management/audit`: fuera de QG | `Seguridad y certificados` | `Reserva certificados sin ID` |
| `ALIAS-019` | `ClearingHousesController.Get` | `GET` | `/clearing-houses`<br>`/api/clearing-houses` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses`: QG-1/QG-2<br>`/api/clearing-houses`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-020` | `ClearingHousesController.GetOperational` | `GET` | `/clearing-houses/operational`<br>`/api/clearing-houses/operational` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/operational`: QG-1/QG-2<br>`/api/clearing-houses/operational`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-021` | `ClearingHousesController.GetPaymentRailOptions` | `GET` | `/clearing-houses/payment-rail-options`<br>`/api/clearing-houses/payment-rail-options` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/payment-rail-options`: QG-1/QG-2<br>`/api/clearing-houses/payment-rail-options`: fuera de QG | `Cámaras y ciclos` | `SC-106` |
| `ALIAS-022` | `ClearingHousesController.GetNachaProfiles` | `GET` | `/clearing-houses/nacha-profiles`<br>`/api/clearing-houses/nacha-profiles` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/nacha-profiles`: QG-1/QG-2<br>`/api/clearing-houses/nacha-profiles`: fuera de QG | `Cámaras y ciclos` | `SC-106` |
| `ALIAS-023` | `ClearingHousesController.GetById` | `GET` | `/clearing-houses/{id:int}`<br>`/api/clearing-houses/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/{id:int}`: QG-1/QG-2<br>`/api/clearing-houses/{id:int}`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-024` | `ClearingHousesController.GetReadiness` | `GET` | `/clearing-houses/{id:int}/readiness`<br>`/api/clearing-houses/{id:int}/readiness` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/{id:int}/readiness`: QG-1/QG-2<br>`/api/clearing-houses/{id:int}/readiness`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-025` | `ClearingHousesController.Create` | `POST` | `/clearing-houses`<br>`/api/clearing-houses` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses`: QG-1/QG-2<br>`/api/clearing-houses`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-026` | `ClearingHousesController.Update` | `PUT` | `/clearing-houses/{id:int}`<br>`/api/clearing-houses/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/{id:int}`: QG-1/QG-2<br>`/api/clearing-houses/{id:int}`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-027` | `ClearingHousesController.ChangeStatus` | `PATCH` | `/clearing-houses/{id:int}/status`<br>`/api/clearing-houses/{id:int}/status` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/{id:int}/status`: QG-1/QG-2<br>`/api/clearing-houses/{id:int}/status`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-028` | `ClearingHousesController.GetCyclesForClearingHouse` | `GET` | `/clearing-houses/{id:int}/cycles`<br>`/api/clearing-houses/{id:int}/cycles` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/clearing-houses/{id:int}/cycles`: QG-1/QG-2<br>`/api/clearing-houses/{id:int}/cycles`: fuera de QG | `Cámaras y ciclos` | `SC-105` |
| `ALIAS-029` | `MenuItemsController.GetMenuItemsAsync` | `GET` | `/navigation/menu-items`<br>`/api/navigation/menu-items` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/navigation/menu-items`: fuera de QG<br>`/api/navigation/menu-items`: fuera de QG | `Administración y catálogos` | `Sin ownership SC demostrado` |
| `ALIAS-030` | `MenuItemsController.CreateMenuItemAsync` | `POST` | `/navigation/menu-items`<br>`/api/navigation/menu-items` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/navigation/menu-items`: fuera de QG<br>`/api/navigation/menu-items`: fuera de QG | `Administración y catálogos` | `Sin ownership SC demostrado` |
| `ALIAS-031` | `MenuItemsController.UpdateMenuItemAsync` | `PUT` | `/navigation/menu-items/{id:int}`<br>`/api/navigation/menu-items/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/navigation/menu-items/{id:int}`: fuera de QG<br>`/api/navigation/menu-items/{id:int}`: fuera de QG | `Administración y catálogos` | `Sin ownership SC demostrado` |
| `ALIAS-032` | `MenuItemsController.DeleteMenuItemAsync` | `DELETE` | `/navigation/menu-items/{id:int}`<br>`/api/navigation/menu-items/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/navigation/menu-items/{id:int}`: fuera de QG<br>`/api/navigation/menu-items/{id:int}`: fuera de QG | `Administración y catálogos` | `Sin ownership SC demostrado` |
| `ALIAS-033` | `NachaRecordDefinitionsController.GetAll` | `GET` | `/nacha-record-definitions`<br>`/api/nacha-record-definitions` | `/nacha-record-definitions` | `/api/nacha-record-definitions`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-record-definitions`: QG-1/QG-2<br>`/api/nacha-record-definitions`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-034` | `NachaRecordDefinitionsController.GetById` | `GET` | `/nacha-record-definitions/{id:int}`<br>`/api/nacha-record-definitions/{id:int}` | `/nacha-record-definitions/{id:int}` | `/api/nacha-record-definitions/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-record-definitions/{id:int}`: QG-1/QG-2<br>`/api/nacha-record-definitions/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-035` | `NachaRecordDefinitionsController.Create` | `POST` | `/nacha-record-definitions`<br>`/api/nacha-record-definitions` | `/nacha-record-definitions` | `/api/nacha-record-definitions`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-record-definitions`: QG-1/QG-2<br>`/api/nacha-record-definitions`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-036` | `NachaRecordDefinitionsController.Update` | `PUT` | `/nacha-record-definitions/{id:int}`<br>`/api/nacha-record-definitions/{id:int}` | `/nacha-record-definitions/{id:int}` | `/api/nacha-record-definitions/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-record-definitions/{id:int}`: QG-1/QG-2<br>`/api/nacha-record-definitions/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-037` | `NachaRecordDefinitionsController.Delete` | `DELETE` | `/nacha-record-definitions/{id:int}`<br>`/api/nacha-record-definitions/{id:int}` | `/nacha-record-definitions/{id:int}` | `/api/nacha-record-definitions/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-record-definitions/{id:int}`: QG-1/QG-2<br>`/api/nacha-record-definitions/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-038` | `NachaRecordLayoutsController.GetAll` | `GET` | `/nacha-layouts`<br>`/api/nacha-layouts` | `/nacha-layouts` | `/api/nacha-layouts`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-layouts`: QG-1/QG-2<br>`/api/nacha-layouts`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-039` | `NachaRecordLayoutsController.GetById` | `GET` | `/nacha-layouts/{id:int}`<br>`/api/nacha-layouts/{id:int}` | `/nacha-layouts/{id:int}` | `/api/nacha-layouts/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-layouts/{id:int}`: QG-1/QG-2<br>`/api/nacha-layouts/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-040` | `NachaRecordLayoutsController.Create` | `POST` | `/nacha-layouts`<br>`/api/nacha-layouts` | `/nacha-layouts` | `/api/nacha-layouts`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-layouts`: QG-1/QG-2<br>`/api/nacha-layouts`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-041` | `NachaRecordLayoutsController.Update` | `PUT` | `/nacha-layouts/{id:int}`<br>`/api/nacha-layouts/{id:int}` | `/nacha-layouts/{id:int}` | `/api/nacha-layouts/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-layouts/{id:int}`: QG-1/QG-2<br>`/api/nacha-layouts/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-042` | `NachaRecordLayoutsController.Delete` | `DELETE` | `/nacha-layouts/{id:int}`<br>`/api/nacha-layouts/{id:int}` | `/nacha-layouts/{id:int}` | `/api/nacha-layouts/{id:int}`; controller legacy `[Obsolete]` | SC-303 califica como canónicas las rutas sin `/api`; el alias `/api` se preserva. | `/nacha-layouts/{id:int}`: QG-1/QG-2<br>`/api/nacha-layouts/{id:int}`: fuera de QG | `NACHA-M` | `SC-303` |
| `ALIAS-043` | `NavigationController.GetMenuAsync` | `GET` | `/navigation/menu`<br>`/api/navigation/menu` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/navigation/menu`: fuera de QG<br>`/api/navigation/menu`: fuera de QG | `Administración y catálogos` | `Sin ownership SC demostrado` |
| `ALIAS-044` | `TransactionsController.GetAll` | `GET` | `/Transactions`<br>`/api/transactions` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions`: QG-1/QG-2<br>`/api/transactions`: fuera de QG | `Transacciones interbancarias` | `SC-201` |
| `ALIAS-045` | `TransactionsController.GetCompanyEntryDescriptions` | `GET` | `/Transactions/company-entry-descriptions`<br>`/api/transactions/company-entry-descriptions` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/company-entry-descriptions`: QG-1/QG-2<br>`/api/transactions/company-entry-descriptions`: fuera de QG | `Transacciones interbancarias` | `SC-201` |
| `ALIAS-046` | `TransactionsController.PreviewPolicy` | `GET` | `/Transactions/policies/preview`<br>`/api/transactions/policies/preview` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/policies/preview`: QG-1/QG-2<br>`/api/transactions/policies/preview`: fuera de QG | `Transacciones interbancarias` | `SC-201` |
| `ALIAS-047` | `TransactionsController.CreateTransaction` | `POST` | `/Transactions`<br>`/api/transactions` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions`: QG-1/QG-2<br>`/api/transactions`: fuera de QG | `Transacciones interbancarias` | `SC-201` |
| `ALIAS-048` | `TransactionsController.SubmitBulkIngestion` | `POST` | `/Transactions/bulk/submit`<br>`/api/transactions/bulk/submit` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/bulk/submit`: QG-1/QG-2<br>`/api/transactions/bulk/submit`: fuera de QG | `Transacciones interbancarias` | `SC-202` |
| `ALIAS-049` | `TransactionsController.CreateTransactionsBulk` | `POST` | `/Transactions/bulk`<br>`/api/transactions/bulk` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/bulk`: QG-1/QG-2<br>`/api/transactions/bulk`: fuera de QG | `Transacciones interbancarias` | `SC-202` |
| `ALIAS-050` | `TransactionsController.GetById` | `GET` | `/Transactions/{id:int}`<br>`/api/transactions/{id:int}` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/{id:int}`: QG-1/QG-2<br>`/api/transactions/{id:int}`: fuera de QG | `Transacciones interbancarias` | `SC-201` |
| `ALIAS-051` | `TransactionsController.GetIntegrationResult` | `GET` | `/Transactions/{id:int}/integration-result`<br>`/api/transactions/{id:int}/integration-result` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/{id:int}/integration-result`: QG-1/QG-2<br>`/api/transactions/{id:int}/integration-result`: fuera de QG | `Transacciones interbancarias` | `SC-202` |
| `ALIAS-052` | `TransactionsController.GetIntegrationReadiness` | `GET` | `/Transactions/{id:int}/integration-readiness`<br>`/api/transactions/{id:int}/integration-readiness` | `CANÓNICA PENDIENTE` | Par de rutas activas de la misma acción; sin subordinación decidida. | Sin evidencia suficiente para preferir un prefijo; requiere decisión humana, sin retirar rutas. | `/Transactions/{id:int}/integration-readiness`: QG-1/QG-2<br>`/api/transactions/{id:int}/integration-readiness`: fuera de QG | `Transacciones interbancarias` | `SC-202` |

#### Decisiones humanas aún requeridas para canónica

Las `42` canónicas pendientes se reducen a seis familias de prefijo, sin necesidad de decidir acción por acción: `AchCyclesController` (`/ach-cycles` vs `/api/ach-cycles`), `CertificateManagementController` (sin `/api` vs `/api`), `ClearingHousesController` (`/clearing-houses` vs `/api/clearing-houses`), `MenuItemsController` (`/navigation/menu-items` vs `/api/navigation/menu-items`), `NavigationController` (`/navigation/menu` vs `/api/navigation/menu`) y `TransactionsController` (`/Transactions` vs `/api/transactions`). Una decisión futura debe indicar cuál familia se prefiere para documentación/gobierno; SC-501 no elimina, redirige ni depreca ninguna ruta.

#### QG y reservas

- **QG-1:** baseline `242`; nuevas SC-501 `0`; delta `0`; total `242`; duplicados `0`.
- **QG-2:** baseline `242`; nuevas SC-501 `0`; delta `0`; total `242`; conjunto exactamente igual a QG-1, con `0` faltantes y `0` extras.
- **QG-3:** `DESACTIVADO`.
- **Examples nuevos:** `0`.
- Reservas verificadas fuera de ambos gates: Auth Logs `1`; mutaciones NACHA `12`; SOAP-UAT/readiness `11`; certificados remanentes `10`; Outgoing Monitoring `2`. En total, `36` selectores reservados y `0` coincidencias QG. El remanente de certificados sí fue inventariado por aliases (`ALIAS-014..018`) pero no fue documentado/ejecutado.

#### Validación técnica y estructural

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`; `SOLUTION GRAPH: OK`, 7 proyectos en solución, 7 en disco y referencias válidas.
- QG reconstruido estáticamente desde el test: QG-1 `242/242` únicos; QG-2 `242/242` únicos; diferencias entre conjuntos `0/0`.
- Inventario de routing estático: 52 grupos alias, 0 duplicados exactos, 0 colisiones de template. Los constraints observados son `:int`, `:guid`, `:long` y no restringidos.
- Merge markers: `0` en `src/`, `tests/`, `docs/` y scripts textuales revisados.
- Estructura C#: `1.790` archivos bajo `src/`/`tests`; conteo de delimitadores directo satisfactorio en todos salvo 11 archivos con literales que alteran el conteo bruto, y esos 11 fueron verificados con análisis léxico dirigido sin desbalance real.
- Configuración OpenAPI/Scalar preservada: `AddOpenApi("v1")`, 6 operation transformers, 2 document transformers, `MapOpenApi`, `MapScalarApiReference` y health mappings.

#### Control de cambios de SC-501

Único archivo modificado:

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`. `OpenApiDocumentGenerationTests.cs`, controllers, DTOs, routing, Program/DI y lógica productiva permanecen byte a byte intactos respecto de r490.

La siguiente unidad real es `SC-502 — Marcar/documentar endpoints deprecated sin eliminar rutas`, dejada `READY`; no se ejecuta en SC-501 y ninguna reserva pendiente recibe ID nuevo.


### SC-502 — Marcar/documentar endpoints deprecated sin eliminar rutas

**Estado:** `VERIFY` — la decisión humana de producto prevalece sobre el título histórico de la unidad: la aplicación no está en producción, no existe compatibilidad legacy por defecto y no se mantiene una ruta duplicada sin necesidad vigente. No se añadió `deprecated: true`.

#### Decisión aplicada sobre ALIAS-033..042

La búsqueda en backend, tests, frontend, SDKs/clientes, configuración, scripts, Postman, documentación y tracker mostró que las variantes `/api` solo estaban declaradas por los dos `[Route]` de controller y protegidas por `SpaApiRouteIsolationTests`. No existe consumidor vigente, contrato independiente ni transición real que exija coexistencia. El test era una protección de la duplicación, no un consumidor. En contraste, las variantes canónicas sin `/api` permanecen en QG-1/QG-2 y en artefactos contractuales/colecciones vigentes.

| ID | Alternativa analizada | Canónica SC-303 | Clasificación | Evidencia | Acción |
|---|---|---|---|---|---|
| `ALIAS-033` | `GET /api/nacha-record-definitions` | `GET /nacha-record-definitions` | `UNUSED_ALIAS` | Sin consumidor; solo Route + test de aislamiento. Canónica presente en QG/Postman. | Alias eliminado. |
| `ALIAS-034` | `GET /api/nacha-record-definitions/{id:int}` | `GET /nacha-record-definitions/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-035` | `POST /api/nacha-record-definitions` | `POST /nacha-record-definitions` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-036` | `PUT /api/nacha-record-definitions/{id:int}` | `PUT /nacha-record-definitions/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-037` | `DELETE /api/nacha-record-definitions/{id:int}` | `DELETE /nacha-record-definitions/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-038` | `GET /api/nacha-layouts` | `GET /nacha-layouts` | `UNUSED_ALIAS` | Sin consumidor; solo Route + test de aislamiento. Canónica presente en QG/Postman. | Alias eliminado. |
| `ALIAS-039` | `GET /api/nacha-layouts/{id:int}` | `GET /nacha-layouts/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-040` | `POST /api/nacha-layouts` | `POST /nacha-layouts` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-041` | `PUT /api/nacha-layouts/{id:int}` | `PUT /nacha-layouts/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |
| `ALIAS-042` | `DELETE /api/nacha-layouts/{id:int}` | `DELETE /nacha-layouts/{id:int}` | `UNUSED_ALIAS` | Mismo prefijo alternativo sin uso independiente. | Alias eliminado. |

Consolidado: `ACTIVE_ALIAS = 0`, `DEPRECATED_TRANSITION = 0`, `UNUSED_ALIAS = 10`. Se eliminaron dos prefijos de controller, equivalentes a 10 `METHOD + PATH`; no se agregaron rutas ni redirects.

#### Metadata deprecated/Obsolete

Los dos controllers tenían `[Obsolete("Legacy diagnostic endpoint...")]` a nivel de clase. No existe una transición activa que justifique propagar esa metadata a todas las operaciones retenidas; se eliminó `[Obsolete]` de ambos controllers y se ajustó `LegacyNachaEndpointsTests` para exigir ausencia de esa metadata. No se agregó `deprecated: true`. Los comportamientos HTTP existentes (incluidos 410 de mutaciones retiradas), summaries/descriptions, requests, responses, policies y DTOs no cambiaron.

#### QG y routing final

- QG-1: baseline `242`; nuevas `0`; eliminadas del gate `0`; delta `0`; total final `242`; duplicados `0`.
- QG-2: total final `242`; conjunto exactamente igual a QG-1; faltantes `0`, extras `0`.
- Los 10 aliases eliminados ya estaban fuera de QG-1/QG-2 en r501; por eso su eliminación física no reduce el total contractual gobernado.
- QG-3: `DESACTIVADO`; examples nuevos: `0`.
- Canónicas SC-303 sin `/api`: las 10 permanecen activas.
- Los otros 42 grupos alias de SC-501 permanecen sin cambios y con `CANÓNICA PENDIENTE`.
- DEC-002: no aplica como nueva partición funcional; nuevas `0`, rutas eliminadas `10`.

#### Validación SC-502

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test ...OpenApiDocumentGenerationTests`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- QG reconstruido estáticamente: `242/242` únicas en QG-1 y `242/242` únicas en QG-2; diferencias `0/0`.
- Merge markers: `0`.
- Estructura de los cuatro C# modificados: delimitadores balanceados. El resto del árbol C# permanece byte a byte respecto de r501.
- `OpenApiDocumentGenerationTests.cs` permanece byte a byte intacto porque las rutas `/api` eliminadas no pertenecían a QG.

#### Reservas y control de alcance

Auth Logs, 12 mutaciones NACHA, SOAP-UAT/readiness, remanente de certificados SC-306 y Outgoing Monitoring permanecen intactos. Los 42 aliases no autorizados para limpieza física no se tocaron. Ninguna unidad posterior se ejecutó.

La siguiente unidad real es `SC-503 — Auditoría de parámetros sin descripción`, dejada `READY`; no se ejecuta en SC-502.


### SC-503 — Auditoría de parámetros sin descripción

**Estado:** `VERIFY` — auditoría contractual completada estáticamente sobre las `242` operaciones actuales de QG-1/QG-2. No se añadieron endpoints, parámetros ni metadata de headers ficticios; routing y contrato funcional permanecen intactos. La única validación pendiente es la ejecución .NET por ausencia de `dotnet`.

#### Inventario inicial

Se reconstruyeron los parámetros OpenAPI reales de tipo `path`, `query` y `header` de las 242 operaciones contractuales. Los properties de request bodies/schemas quedaron fuera de esta unidad.

- Parámetros OpenAPI auditados: `431`.
- `path`: `119`.
- `query`: `298`.
- `header`: `14`.
- `DESCRIBED_VALID`: `423`.
- `MISSING_DESCRIPTION`: `8`.
- `WEAK_DESCRIPTION`: `0`.
- `INVALID_PARAMETER_METADATA`: `0`.
- `INSUFFICIENT_EVIDENCE`: `0`.

Los 14 headers auditados corresponden exclusivamente a consumo real ya demostrado por código: `Idempotency-Key` en 3 operaciones QG de ACH Colombia File Exchange y `X-Correlation-ID` en 11 operaciones QG de AchResponse/Scheduler. `Authorization` continúa representado mediante OpenAPI Security y no como parámetro.

#### Parámetros corregidos

Se añadieron `8` descripciones de parámetros, todas derivadas de código real y sin modificar firmas, binding, tipos, required, defaults ni validadores:

- `NachaExportController`: `cycleId` en `GET /NachaExport/{cycleId}`; `cycleId` y `forceEncryption` en `GET /NachaExport/{cycleId}/sobre-digital` (`3` ocurrencias).
- `NachaUploadController`: `immediateOrigin`, `immediateDestination`, `referenceCode`, `achCycleId` y `fileCreationDate` en `GET /NachaUpload/records` (`5` ocurrencias).

Evidencia usada: resolución del ciclo mediante `GetByIdAsync`; combinación explícita `forceEncryption || policy.ShouldEncrypt(...)`; filtros `Contains` para origen/destino/reference, igualdad para `achCycleId` y comparación de `fileCreationDate` transformada a `yyyyMMdd`. No se inventaron unidades, defaults, rangos, zonas horarias, enums ni semántica regulatoria.

Consolidado de cambios documentales: descripciones añadidas `8`; descripciones mejoradas `0`; metadata inválida eliminada `0`; parámetros agregados `0`.

#### QG-2 de parámetros

`OpenApiDocumentGenerationTests` incorpora un gate transversal limitado a las 242 claves de QG-1. Para cada parámetro OpenAPI real `path/query/header` valida:

- nombre y ubicación HTTP válidos;
- ausencia de duplicación ubicación + nombre;
- `description` no nula/vacía y no obviamente tautológica mediante una heurística conservadora;
- schema presente;
- `required = true` para parámetros `path`;
- `Authorization` ausente como parámetro;
- `Idempotency-Key` y `X-Correlation-ID` únicamente en las operaciones QG donde el código los consume explícitamente;
- inventario contractual total estable de `119 path + 298 query + 14 header = 431`.

QG-1 permanece en `242`, delta `0`. QG-2 permanece en `242` y su conjunto `METHOD + PATH` es exactamente igual a QG-1. QG-3 continúa `DESACTIVADO`. Examples nuevos: `0`.

#### No regresión y alcance

- Rutas agregadas: `0`; rutas eliminadas: `0`.
- Las 10 variantes `/api` eliminadas por SC-502 continúan ausentes y no se reintrodujo `[Obsolete]`/deprecated metadata retirada allí.
- Las 10 canónicas sin `/api` de SC-303 continúan activas.
- Los otros 42 grupos alias con `CANÓNICA PENDIENTE` no cambiaron.
- Auth Logs, 12 mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes SC-306 y Outgoing Monitoring permanecen fuera e intactos.
- Controllers nuevos: `0`; operaciones nuevas: `0`; DEC-002 no aplica como partición funcional nueva.

#### Validación SC-503

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- QG reconstruido estáticamente: `242/242` únicas en QG-1 y `242/242` únicas en QG-2; diferencias `0/0`.
- Inventario de parámetros estático: `431` (`119 path`, `298 query`, `14 header`).
- Merge markers: `0`; estructura C# revisada sin desbalance real.

Archivos modificados en SC-503:

```text
src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs
src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

La siguiente unidad real es `SC-504 — Auditoría de responses/status codes faltantes`, dejada `READY`; no se ejecuta en SC-503.


### SC-504 — Auditoría de responses/status codes faltantes

**Estado:** `VERIFY` — auditoría contractual completada estáticamente sobre las `242` operaciones vigentes de QG-1/QG-2. No se añadieron endpoints ni se cambió comportamiento productivo. La única validación pendiente es la ejecución .NET por ausencia de `dotnet`.

#### Inventario y clasificación

Se revisaron `476` pares `METHOD + PATH / status code`: `473` responses de controllers y `3` responses de las dos rutas health (`/health/live` = 200; `/health/ready` = 200/503).

- `DOCUMENTED_VALID`: `476`.
- `MISSING_RESPONSE`: `0`.
- `WRONG_SCHEMA`: `0`.
- `WRONG_MEDIA_TYPE`: `0`.
- `SPECULATIVE_RESPONSE_METADATA`: `0`.
- `INSUFFICIENT_EVIDENCE`: `0`.

La comprobación estática cruzó las 242 claves QG con routing/controller real. En las `240` operaciones MVC, el conjunto de status declarado por `ProducesResponseType` coincide exactamente con el inventario QG-2. Para health se verificó el mapping real: liveness publica únicamente `200 application/json` y readiness publica `200/503 application/json` sin DTO artificial. Las formas de body y media types ya protegidas por los asserts QG-2 por operación permanecen congruentes con los tipos declarados y respuestas de archivo existentes. No se detectó metadata que requiriera corrección o eliminación.

#### Quality gate de responses

`OpenApiDocumentGenerationTests` incorpora un gate transversal adicional que:

- reconstruye las 242 operaciones desde los inventarios QG-2 existentes;
- exige igualdad exacta entre los status codes publicados y los status demostrados para cada operación;
- rechaza status faltantes y metadata especulativa, incluido cualquier response extra no inventariado;
- exige descripción no vacía para los `476` pares operación/status;
- conserva las comprobaciones existentes de schemas, bodies anónimos, responses sin body y media types de archivo;
- no exige 400/401/403/404/409/5xx genéricos por convención.

No se añadieron, corrigieron ni eliminaron responses de controllers: responses agregados `0`, corregidos `0`, eliminados `0`; schemas corregidos `0`; media types corregidos `0`.

QG-1 permanece en `242`, delta `0`. QG-2 permanece en `242` y su conjunto `METHOD + PATH` sigue siendo exactamente igual a QG-1. El gate transversal de `431` parámetros incorporado por SC-503 continúa activo. QG-3 permanece `DESACTIVADO`. Examples nuevos: `0`.

#### No regresión y alcance

- Rutas agregadas: `0`; rutas eliminadas: `0`.
- SC-502 preservado: las 10 variantes `/api/nacha-record-definitions...` y `/api/nacha-layouts...` siguen ausentes; las canónicas sin `/api` permanecen activas.
- SC-503 preservado: 431 parámetros contractuales y su gate transversal permanecen sin cambios.
- Los otros 42 grupos alias con `CANÓNICA PENDIENTE` no cambiaron.
- Auth Logs, 12 mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes SC-306 y Outgoing Monitoring permanecen fuera e intactos.
- Controllers nuevos: `0`; operaciones nuevas: `0`; operaciones eliminadas: `0`; DEC-002 no aplica como partición funcional nueva.

#### Validación SC-504

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- QG reconstruido estáticamente: `242/242` únicas en QG-1 y `242/242` únicas en QG-2; diferencias `0/0`.
- Inventario de response metadata: `476` pares operación/status; clasificación `476/0/0/0/0/0`.
- Merge markers: `0`; estructura C# modificada revisada sin desbalance real.

Archivos modificados en SC-504:

```text
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

La siguiente unidad real es `SC-505 — Auditoría de ejemplos faltantes en flujos críticos`, dejada `READY`; no se ejecuta en SC-504.


### Resultado de SC-505 — auditoría de ejemplos faltantes en flujos críticos

**Estado:** `VERIFY`  
**Decisión humana aplicada:** “flujos críticos” queda delimitado exclusivamente a 10 `METHOD + PATH`; no se amplía el alcance y no se decide canonicidad de los aliases de `TransactionsController`.

#### Operaciones autorizadas

```text
POST /Transactions
POST /api/transactions
POST /Transactions/bulk/submit
POST /api/transactions/bulk/submit
POST /Transactions/bulk
POST /api/transactions/bulk
POST /api/ach-colombia/file-exchange/outbound/execute
POST /api/ach-colombia/file-exchange/inbound/execute
POST /nacha-security/operations/nacha/generate
POST /nacha-security/operations/nacha/generate-encrypted
```

Las seis rutas de Transactions corresponden a tres acciones lógicas expuestas por dos prefijos. Los examples se asocian a la acción y el mismo contrato se reutiliza para ambos aliases; SC-505 no declara canónica ni modifica routing.

#### Auditoría de examples

La clasificación se realizó sobre `13` superficies contractuales lógicas de example, de-duplicando aliases: `6` request bodies estructurados y `7` success responses. La ejecución entrante de ACH Colombia no recibe request body.

Clasificación inicial:

- `EXAMPLE_VALID = 0`.
- `MISSING_USEFUL_EXAMPLE = 10`: los 6 request bodies estructurados y los success responses de bulk submit, bulk directo, outbound e inbound de ACH Colombia.
- `INVALID_OR_STALE_EXAMPLE = 0`.
- `SENSITIVE_EXAMPLE = 0`.
- `NOT_USEFUL = 3`: success response de creación individual (`AchTransaction`, agregado persistente amplio con identifiers/estado/ciclo/lote dinámicos) y los dos success responses `DigitalEnvelopeOperationDto` de generación NACHA Security (artefacto, filename, hashes, certificados y timestamps dependen de ejecución). Publicar un example allí exigiría inventar metadata operativa que no mejora el contrato.
- `INSUFFICIENT_EVIDENCE = 0`.

Resultado final:

- `EXAMPLE_VALID = 10`.
- `MISSING_USEFUL_EXAMPLE = 0`.
- `NOT_USEFUL = 3` se mantiene deliberadamente sin example.
- Examples añadidos a nivel de metadata de acción: `10` (`6` requests + `4` responses). Por los tres aliases dobles de Transactions, se materializan `15` placements de example en el documento OpenAPI.
- Examples corregidos: `0`; eliminados: `0`.

Examples incorporados:

- `POST /Transactions` y `POST /api/transactions`: request `AchTransactionRequest` ficticio con monto, identificador externo/referencia, enums reales, cuentas numéricas sintéticas, originador ficticio e IDs ilustrativos; no se publica success-response example por `NOT_USEFUL`.
- `POST /Transactions/bulk/submit` y alias `/api`: request `BulkIngestionRequest` síncrono `InlineTransactions` con un ítem ficticio; response 200 `BulkIngestionResponse` con `Status = COMPLETED` e `ImmediateResult` coherente con el servicio.
- `POST /Transactions/bulk` y alias `/api`: request `BulkAchTransactionRequest` con un ítem ficticio; response 200 `BulkAchTransactionResponse` con totales coherentes y un resultado exitoso ficticio.
- `POST /api/ach-colombia/file-exchange/outbound/execute`: request `ExecuteOutboundRequest` con `CycleId` ficticio y sin `CorrectedFromTransferId`; response 200 `AchManagedFileExecutionResult` con conteos coherentes y GUID sintético.
- `POST /api/ach-colombia/file-exchange/inbound/execute`: sin request body; response 200 reutiliza el mismo contrato `AchManagedFileExecutionResult`.
- `POST /nacha-security/operations/nacha/generate` y `.../generate-encrypted`: request `NachaGenerateApiRequest` con `CycleId` ficticio compartido; success-response examples omitidos deliberadamente por `NOT_USEFUL`.

Los valores proceden de DTOs, enums, validaciones y tests existentes. Los números de cuenta, identificadores, referencias, CycleId y GUID usados son sintéticos/demostrativos; no se incluyeron secretos, tokens, certificados, datos personales ni valores provenientes de ambientes reales. Auth SC-104 permanece intacto y se usó únicamente como referencia del patrón provider + transformer.

#### Quality gates y no regresión

- QG-1: `242/242` operaciones únicas; delta SC-505 `0`.
- QG-2: `242/242`; mismo conjunto `METHOD + PATH` que QG-1; gates SC-503 (`431` parámetros) y SC-504 (`476` pares operación/status) preservados. Se agregó validación de examples solo para la metadata SC-505 realmente publicada, incluidos ambos aliases de las tres acciones de Transactions.
- QG-3: `DESACTIVADO`; no se activa por SC-505.
- Routing agregado/eliminado: `0/0`.
- SC-502 preservado: las 10 rutas `/api/nacha-record-definitions...` y `/api/nacha-layouts...` siguen ausentes.
- Los otros 42 grupos alias `CANÓNICA PENDIENTE` permanecen intactos.
- Auth Logs, mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes SC-306 y Outgoing Monitoring permanecen fuera e intactos.

#### Validación SC-505b

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- Reconstrucción estática: QG-1 `242`, QG-2 `242`, conjuntos idénticos; 0 rutas añadidas/eliminadas; 0 merge markers; estructura C# modificada balanceada.

Archivos modificados en SC-505b:

```text
src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs
src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs
src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregado:

```text
src/Cfa.ACHInterbank.Api/OpenApi/CriticalFlowOpenApiExamples.cs
```

Eliminados: `0`.

La siguiente unidad real del tracker es `SC-506 — Auditoría de DTO/schemas ambiguos`, dejada `READY`; no se ejecuta en SC-505.


### Resultado de SC-506 — auditoría de DTO/schemas ambiguos

**Estado:** `VERIFY`  
**Alcance:** auditoría transversal de schemas realmente alcanzados por las `242` operaciones contractuales de QG-1/QG-2; no incorpora operaciones ni superficies reservadas.

#### Inventario y clasificación

La reconstrucción estática parte de los request/response schemas referenciados por QG-2, sigue objetos anidados, herencia serializada y wrappers genéricos realmente usados, y respeta las exclusiones del `OpenApiSchemaJsonTypeInfoResolver`. Se auditaron `290` contratos schema alcanzables (`288` contratos del repositorio + `ProblemDetails` y `ValidationProblemDetails`) y `2.179` propiedades serializadas.

Clasificación inicial:

- `SCHEMA_CLEAR = 287`.
- `MISSING_DESCRIPTION = 3` familias / `31` propiedades expuestas sin descripción útil.
- `AMBIGUOUS_DESCRIPTION = 0`.
- `WRONG_SCHEMA_METADATA = 0`.
- `REDUNDANT_OR_UNUSED_SCHEMA_METADATA = 0`.
- `INSUFFICIENT_EVIDENCE = 0`.

Hallazgos corregidos:

- `AchTransaction`: `19` propiedades expuestas sin descripción (`CreatedAt`/`UpdatedAt` heredadas y 17 propiedades propias de clasificación, trazabilidad, devolución e integración). Las navegaciones retiradas por `OpenApiSchemaJsonTypeInfoResolver` no se trataron como propiedades contractuales.
- `NachaValidationFailure`: `5` propiedades posicionales sin descripción; su significado se deriva del parser que genera fallas para registros NACHA-M y de sus campos reales.
- `PaginatedResult<T>` usado por `GET /clearing-houses`: `7` propiedades sin descripción; `TotalPages`, `HasNext` y `HasPrevious` se documentan exactamente según sus expresiones de cálculo existentes.

Resultado final: `SCHEMA_CLEAR = 290`; las demás categorías quedan en `0`. También se añadieron resúmenes XML de las tres familias. No se inventaron formatos, unidades, defaults, rangos, nullability, required, enums ni reglas de negocio.

#### Quality gates y no regresión

- QG-1: `242/242` `METHOD + PATH` únicos; delta SC-506 `0`.
- QG-2: `242/242`, conjunto exactamente igual a QG-1. Se preservan los gates SC-503 (`431` parámetros), SC-504 (`476` pares operación/status) y SC-505 (examples críticos).
- Gate SC-506: `AssertSc506SchemaDocumentationAudit` protege las `19` propiedades corregidas de `AchTransaction` y las `5` de `NachaValidationFailure`; el helper paginado existente se endurece para exigir descripción no vacía en las `7` propiedades de `PaginatedResult<T>`, sin duplicar el DTO completo en una nueva allowlist.
- QG-3: `DESACTIVADO`.
- Examples nuevos: `0`.
- Operaciones nuevas/eliminadas: `0/0`; routing agregado/eliminado: `0/0`.
- SC-502 preservado: las diez variantes `/api/nacha-record-definitions...` y `/api/nacha-layouts...` siguen ausentes.
- Los `42` grupos alias `CANÓNICA PENDIENTE` permanecen intactos. Auth Logs, mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes SC-306 y Outgoing Monitoring continúan fuera e intactos.

#### Validación SC-506

- `dotnet --info`: exit `127` (`dotnet: command not found`).
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~OpenApiDocumentGenerationTests"`: no ejecutado ni simulado por ausencia de `dotnet`.
- `dotnet build ACHInterbank.sln -c Release`: no ejecutado ni simulado por ausencia de `dotnet`.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- Auditoría estática posterior: `0` familias con descripciones faltantes, `0` merge markers; estructuras de los C# modificados balanceadas; QG-1/QG-2 permanecen en `242/242`.

Archivos modificados en SC-506:

```text
src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransaction.cs
src/Cfa.ACHInterbank.Domain/Entities/SchedulerTask/Base/AuditableEntity.cs
src/Cfa.ACHInterbank.Application/ACH/Models/NachaValidationFailure.cs
src/Cfa.ACHInterbank.Domain/Models/Configurations/PaginatedResult.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados: `0`.

La siguiente unidad real es `SC-507 — Regenerar o retirar DOC_API.md, docs_api.json, diagram.scalar.json según decisión técnica`, dejada `READY`; no se ejecuta en SC-506.


### Resultado de SC-507 — regenerar o retirar artefactos derivados

**Estado:** `VERIFY`  
**Alcance:** decisión técnica exclusiva sobre `DOC_API.md`, `docs_api.json` y `diagram.scalar.json`; no modifica OpenAPI productivo, routing, controllers, DTOs, tests ni operaciones QG.

#### Decisión por artefacto

| Artefacto | Clasificación | Evidencia de uso/generación | Sincronización | Decisión |
|---|---|---|---|---|
| `DOC_API.md` | `REMOVE_UNUSED` | Se autodefine como documentación generada desde controllers, pero no existe script/comando/generador reproducible en el repositorio. No lo consume código, CI, Scalar, tests, tooling ni documentación activa. La única referencia externa al tracker es un snapshot histórico de `find`, que se conserva como evidencia. | Declara `36` controllers / `97` acciones y publica responses genéricos `400/401/500` que no representan el contrato QG vigente de `242` operaciones. | Eliminado; no se regenera manualmente. |
| `docs_api.json` | `REMOVE_UNUSED` | `x-generated` declara `36` controllers / `97` endpoints, sin generador reproducible ni consumidor vigente. Scalar no lo carga: el runtime publica `/openapi/{documentName}.json` mediante `MapOpenApi`. | Contiene `63` paths / `91` `METHOD + PATH` únicos, incluye la reserva `GET /api/auth-logs` y omite `/health/live` y `/health/ready`; no coincide con QG-1/QG-2 `242`. | Eliminado; no se reconstruye a mano. |
| `diagram.scalar.json` | `REMOVE_UNUSED` | Snapshot `scalar-diagram-v1` sin referencia desde Scalar, código, CI, tests o tooling y sin generador reproducible. | Contiene `36` nodos controller + `97` nodos endpoint / `97` edges (`91` `METHOD + PATH` únicos), incluye `GET /api/auth-logs` y omite health; no representa el contrato vigente. | Eliminado; no se reconstruye a mano. |

La fuente de verdad contractual vigente es la infraestructura ASP.NET Core (`AddOpenApi("v1")`, transformers documentales, `MapOpenApi("/openapi/{documentName}.json")`) presentada por `MapScalarApiReference`, con QG-1/QG-2 como allowlist/verificación del conjunto contractual. Ninguno de los tres archivos auditados participa en esa ruta de publicación.

No se eliminó la mención de los tres nombres en `docs/audits/evidence/find-root-maxdepth3.txt`: ese archivo es evidencia histórica de un listado anterior del árbol y no un consumidor ni una referencia operativa. Las referencias del tracker se conservan/actualizan porque documentan la decisión SC-507.

#### Quality gates y no regresión

- QG-1: `242/242` `METHOD + PATH` únicos; delta SC-507 `0`.
- QG-2: `242/242`, conjunto contractual protegido por el mismo test y con aserción explícita de `242`; `OpenApiDocumentGenerationTests.cs` permanece byte a byte intacto.
- QG-3: `DESACTIVADO`.
- Routing agregado/eliminado: `0/0`; contratos funcionales modificados: `0`.
- SC-502/503/504/505/506 permanecen preservados.
- Los `42` grupos alias `CANÓNICA PENDIENTE` y las reservas (Auth Logs, mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes y Outgoing Monitoring) permanecen intactos.

#### Validación SC-507

- `dotnet --info`: exit `127` (`dotnet: command not found`). No se simuló generación .NET.
- `python scripts/validate-solution-projects.py`: exit `0`, `SOLUTION GRAPH: OK`.
- Búsqueda transversal de referencias por nombre: sin consumidores vigentes; solo tracker y snapshot histórico de `find`.
- Reconstrucción estática QG: QG-1 conserva `242` entradas únicas; QG-2 conserva su aserción de `242` y equivalencia exacta con QG-1.
- `0` merge markers; ningún C# fue modificado.

Archivos modificados en SC-507:

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

Agregados: `0`. Eliminados:

```text
DOC_API.md
docs_api.json
diagram.scalar.json
```

La siguiente unidad real es `SC-508 — Quality gate final para todas las operaciones públicas`, dejada `READY`; no se ejecuta en SC-507.


### Resultado de SC-508 — quality gate final para todas las operaciones públicas

**Estado:** `VERIFY`  
**Alcance SC-508a:** se procesaron exclusivamente las `92` operaciones registradas por r508 como `UNEXPECTED_UNCOVERED_OPERATION`; no se repitió el inventario transversal ni se ejecutó SC-900.

- `PUBLIC_CONTRACT = 80`. Se incorporan a QG-1/QG-2 reutilizando metadata vigente; un transformer dirigido completa únicamente Summary/Description ausentes o placeholder para esas acciones. El gate adicional contrasta parámetros, request body, responses/status, schemas y seguridad contra ApiExplorer/metadata actual, sin inventar contratos.
- `INTENTIONAL_EXCLUSION = 12`:
  - `MaintenanceController = 4`: superficie técnica de mantenimiento/seed/setup/post-processing protegida por policies de mantenimiento.
  - `NachaInboundSimulatorController = 8`: ruta `api/uat/nacha-inbound-simulator`, documentación explícita UAT/local y guard de disponibilidad fuera de entornos autorizados.
- QG baseline: `242`; delta contractual: `+80`; **QG-1 = 322** y **QG-2 = 322**, con exactamente el mismo conjunto `METHOD + PATH`.
- `UNEXPECTED_UNCOVERED_OPERATION = 0`.
- Gate final sobre las `416` operaciones ya inventariadas por r508: `322 CONTRACTUAL_QG + 36 PENDING_RESERVED_SCOPE + 50 INTENTIONAL_EXCLUSION + 8 APIEXPLORER_IGNORED`. Dentro de `INTENTIONAL_EXCLUSION`, `48` permanecen visibles por ApiExplorer/OpenAPI (`36` aliases pendientes + `12` exclusiones SC-508a) y `2` son redirects raíz fuera de ApiExplorer.
- Las `36` reservas conocidas permanecen sin absorber: Auth Logs, `12` mutaciones NACHA, SOAP-UAT/readiness, certificados remanentes y Outgoing Monitoring. Los `42` grupos alias `CANÓNICA PENDIENTE` permanecen sin resolver.
- SC-503 conserva su gate de `431` parámetros sobre el baseline contractual de `242`; SC-504 conserva `476` pares operación/status sobre el mismo baseline. SC-505, SC-506, la eliminación de snapshots de SC-507, health y la limpieza de aliases de SC-502 permanecen vigentes. QG-3 continúa `DESACTIVADO`.

Validación:

- `dotnet --info`: exit `127` (`dotnet: command not found`). No se ejecutaron ni simularon `OpenApiDocumentGenerationTests` ni build Release.
- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` (`7/7` proyectos; referencias válidas).
- Validación estática: conjuntos QG `322/322`, `80` PUBLIC_CONTRACT únicos, `12` exclusiones únicas, superficie visible clasificada `406/406` y total clasificado `416/416`, sin solapamientos entre categorías contractuales/reservadas/excluidas.

Archivos de SC-508a:

```text
src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs
src/Cfa.ACHInterbank.Api/OpenApi/OpenApiSc508PublicContractDocumentationTransformer.cs
tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```

La siguiente unidad real es `SC-900 — Auditoría final y cierre`, dejada `READY` y **no ejecutada**.


### Resultado de SC-900 — auditoría final y cierre

**Estado:** `VERIFY` — cierre estático coherente; no se declara `DONE` porque `dotnet` no está disponible para ejecutar la validación final. No se implementó funcionalidad ni se modificaron tests, controllers, DTOs o routing.

- Cobertura pública final: `416/416 = 322 CONTRACTUAL_QG + 36 PENDING_RESERVED_SCOPE + 50 INTENTIONAL_EXCLUSION + 8 APIEXPLORER_IGNORED`; categorías sin solapamientos y `UNEXPECTED_UNCOVERED_OPERATION = 0`.
- QG-1 y QG-2: `322/322`, sin duplicados y con exactamente el mismo conjunto `METHOD + PATH`. QG-3 permanece `DESACTIVADO`.
- Gates preservados en `OpenApiDocumentGenerationTests`: SC-503 (`431` parámetros), SC-504 (`476` pares operación/status), SC-505 (examples críticos), SC-506 (schemas) y SC-508 (cobertura pública final).
- SC-502 preservado: continúan ausentes las 10 variantes `/api` de ALIAS-033..042; las rutas canónicas sin `/api` siguen en su estado previo.
- Los `42` grupos `CANÓNICA PENDIENTE` siguen sin resolver. Las `36` reservas siguen fuera de QG. Los `50` INTENTIONAL_EXCLUSION permanecen justificados por el gate vigente: `36` rutas alias pendientes fuera de QG + `12` exclusiones SC-508 (Maintenance/UAT simulator) + `2` redirects documentales raíz.
- SC-507 preservado: `DOC_API.md`, `docs_api.json` y `diagram.scalar.json` siguen ausentes.
- Estructura: `python scripts/validate-solution-projects.py` = `SOLUTION GRAPH: OK`; merge markers = `0`.
- Validación ejecutable: `dotnet --info` = exit `127` (`dotnet: command not found`); por tanto no se ejecutaron ni simularon `OpenApiDocumentGenerationTests` ni `dotnet build ACHInterbank.sln -c Release`.

**Estado final de la iniciativa Scalar:** `VERIFY`. La cobertura y los gates quedan cerrados estáticamente, sin abrir trabajo posterior; el único pendiente es ejecutar la validación .NET final en un entorno que disponga de `dotnet`.

Archivos modificados en SC-900:

```text
docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md
```


## Fase D — NACHA-M, CENIT, archivos y seguridad

| ID | Estado | Alcance |
|---|---|---|
| `SC-301` | `VERIFY` | Incoming NACHA Command Center — grupo 1 |
| `SC-302` | `VERIFY` | Incoming NACHA Command Center — grupo 2 |
| `SC-303` | `VERIFY` | Definiciones/layouts NACHA legacy: `NachaRecordDefinitionsController` + `NachaRecordLayoutsController` (10 operaciones) implementadas contractualmente; QG-1/QG-2 en 104. Validación .NET pendiente. Configuración oficial NACHA excluida y reservada sin IDs. |
| `SC-304` | `VERIFY` | Operaciones de rechazo/respuesta de archivos — delimitado e implementado exclusivamente en `AchResponseOperationsController` (14 operaciones); QG-1/QG-2 en 118; validación .NET pendiente |
| `SC-305` | `VERIFY` | CENIT Operations — delimitado e implementado exclusivamente en `CenitOperationsController` (7 operaciones); QG-1/QG-2 en 125; validación .NET pendiente |
| `SC-306` | `VERIFY` | Certificados — grupo 1 — Parte A ejecutada: `CertificateManagementController` (6 acciones / 12 operaciones por aliases) + `DigitalEnvelopeCertificatesController` (3), total 15; QG-1/QG-2 en 140; validación .NET pendiente. Remanente de gestión: 10 operaciones sin ID. |
| `SC-307` | `VERIFY` | Sobre digital / operaciones criptográficas — delimitado e implementado exclusivamente en `SobreDigitalController`: 4 acciones lógicas, `testRSA` excluida, 3 operaciones OpenAPI; QG-1/QG-2 en 143; validación .NET pendiente |
| `SC-308` | `VERIFY` | NACHA Security Operations — delimitado e implementado exclusivamente en `NachaSecurityOperationsController` (8 operaciones); QG-1/QG-2 en 151; validación .NET pendiente |
| `SC-309` | `VERIFY` | Downloads/uploads: content types, nombres y errores — delimitado e implementado en `NachaExportController` + `NachaUploadController` (4 operaciones); QG-1/QG-2 en 155; validación .NET pendiente |
| `SC-390` | `VERIFY` | Checkpoint Fase D — validación estática acumulada completada; QG-1/QG-2 en 155, QG-3 desactivado; validación .NET pendiente |

## Fase E — Integraciones, administración y operación

| ID | Estado | Alcance |
|---|---|---|
| `SC-401` | `VERIFY` | ACH Colombia File Exchange — delimitado e implementado exclusivamente en 9 operaciones de transferencia/ejecución de `AchColombiaFileExchangeController` (1 controller, 9 operaciones, sin aliases); QG-1/QG-2 en 164; validación .NET pendiente. Sus 5 operaciones configuration/administration fueron documentadas posteriormente en SC-408 sin reabrir SC-401. |
| `SC-402` | `VERIFY` | Integration Mapping Sets — delimitado e implementado exclusivamente en `IntegrationMappingSetsController` (12 operaciones, sin aliases); QG-1/QG-2 en 176; validación .NET pendiente. `IntegrationCatalogController` fue documentado posteriormente en SC-408 sin reabrir SC-402. |
| `SC-403` | `VERIFY` | Integraciones SOAP/external expuestas por REST — delimitado e implementado en `SoapIntegrationSettingsController` (2 operaciones) + `UatContrapartidasController` (1), total 3 operaciones sin aliases; QG-1/QG-2 en 179; validación .NET pendiente. SOAP-UAT/readiness queda reservado sin ID. |
| `SC-404` | `VERIFY` | Scheduler — grupo 1 — delimitado e implementado exclusivamente en `SchedulerController` (13 operaciones, 0 aliases); QG-1/QG-2 en 192; validación .NET pendiente. |
| `SC-405` | `VERIFY` | Scheduler — grupo 2 — implementado exclusivamente en `TaskDefinitionsController` (5 operaciones, 0 aliases); QG-1/QG-2 en 197; validación .NET pendiente. |
| `SC-406` | `VERIFY` | Reports — grupo 1 — 12 operaciones autorizadas de `ReportsController` documentadas; QG-1/QG-2 197→209; validación .NET pendiente. |
| `SC-407` | `VERIFY` | Reports — grupo 2 — 8 operaciones restantes de `ReportsController` documentadas; QG-1/QG-2 209→217; validación .NET pendiente. |
| `SC-408` | `VERIFY` | Administración/catálogos — lote 1 — implementado exclusivamente en `IntegrationCatalogController` (4) + configuration/administration de `AchColombiaFileExchangeController` (5), total 9 operaciones; QG-1/QG-2 en 226; validación .NET pendiente. |
| `SC-409` | `VERIFY` | Administración/catálogos — lote 2 — implementado exclusivamente sobre lectura de configuración NACHA: 5 GET de `NachaConfigProfilesController` + 6 GET de `NachaConfigProfilesReadOnlyController`; 2 controllers / 11 operaciones / 0 aliases; QG-1/QG-2 en 237; validación .NET pendiente. Las 12 mutaciones NACHA permanecen reservadas sin ID. |
| `SC-410` | `VERIFY` | Auditoría, logs y observabilidad expuesta — `AuditLogsController` + `NavigationLogsController` + `/health/live` + `/health/ready`; 2 controllers / 5 operaciones / 0 aliases; QG-1/QG-2 237→242; validación .NET pendiente por exit 127. |
| `SC-490` | `VERIFY` | Checkpoint Fase E — validación estática acumulada completada sobre SC-401..SC-410; 87 operaciones de Fase E, QG-1/QG-2 en 242 y conjuntos idénticos; validación .NET pendiente. |

## Fase F — Legacy, cobertura total y cierre

| ID | Estado | Alcance |
|---|---|---|
| `SC-501` | `VERIFY` | Inventario transversal completado: 52 grupos alias / 104 METHOD + PATH; 0 duplicados exactos; 0 colisiones de template; 10 canónicas definidas por SC-303 y 42 pendientes de decisión; QG-1/QG-2 preservados en 242; validación .NET pendiente. |
| `SC-502` | `VERIFY` | Política humana aplicada: aplicación no productiva, sin legacy por defecto. ALIAS-033..042: 10 variantes `/api` clasificadas `UNUSED_ALIAS` y eliminadas; 0 `ACTIVE_ALIAS`, 0 `DEPRECATED_TRANSITION`; `[Obsolete]` retirado de ambos controllers; canónicas sin `/api` preservadas; QG-1/QG-2 siguen en 242. |
| `SC-503` | `VERIFY` | Auditoría transversal de 431 parámetros en las 242 operaciones QG: 8 descripciones faltantes documentadas, 0 metadata inválida, 0 casos sin evidencia; QG-1/QG-2 preservados en 242; validación .NET pendiente. |
| `SC-504` | `VERIFY` | Auditoría transversal de 476 pares operación/status sobre las 242 operaciones QG: 476 DOCUMENTED_VALID, 0 faltantes/especulativos/incorrectos; QG-1/QG-2 preservados en 242; gate exacto de status añadido; validación .NET pendiente. |
| `SC-505` | `VERIFY` | Auditoría de examples completada sobre 10 METHOD + PATH críticos definidos por decisión humana: 6 request examples + 4 success-response examples explícitos, reutilizados en aliases sin decidir canonicidad; 3 success responses `NOT_USEFUL` permanecen sin example; QG-1/QG-2 242/242; QG-3 desactivado; validación .NET pendiente. |
| `SC-506` | `VERIFY` | Auditoría transversal de schemas sobre 242 operaciones: 290 contratos alcanzables auditados; 3 familias/31 propiedades sin descripción corregidas con XML docs; 0 metadata incorrecta/redundante/ambigua o sin evidencia; QG-1/QG-2 242/242; validación .NET pendiente. |
| `SC-507` | `VERIFY` | Los tres artefactos derivados (`DOC_API.md`, `docs_api.json`, `diagram.scalar.json`) se clasificaron `REMOVE_UNUSED` y fueron retirados: sin consumidor vigente, sin generador reproducible en repo y desincronizados del contrato QG de 242 operaciones. |
| `SC-508` | `VERIFY` | 92 operaciones r508 resueltas: 80 PUBLIC_CONTRACT + 12 INTENTIONAL_EXCLUSION; QG-1/QG-2 = 322/322; gate final con UNEXPECTED = 0. Validación ejecutable pendiente solo por ausencia de dotnet. |
| `SC-900` | `VERIFY` | Auditoría final de cierre completada estáticamente: 416/416 clasificadas, QG-1/QG-2 322/322 e idénticos, UNEXPECTED = 0; gates SC-503..508 y decisiones SC-502/507 preservados. Pendiente únicamente ejecución OpenApiDocumentGenerationTests + build Release por ausencia de dotnet. |

---

# 9. Prioridad de documentación por dominio

Usar este orden salvo bloqueo técnico:

1. Autenticación y autorización.
2. Clearing houses / ciclos / catálogos para onboarding.
3. Transacciones ACH.
4. Bulk ingestion.
5. Trazabilidad.
6. Devoluciones.
7. NACHA-M.
8. CENIT.
9. Certificados, firmas y cifrado.
10. Intercambio de archivos e integraciones.
11. Scheduler y procesamiento operacional.
12. Reportes.
13. Administración y catálogos secundarios.
14. Legacy/deprecación.

---

# 10. Quality gate incremental

El quality gate debe crecer por etapas para no bloquear todo el proyecto desde el primer commit.

## Nivel QG-1 — Fase A

Exigir solo para operaciones que ya fueron migradas o para un allowlist controlado:

- summary no vacío;
- description no genérica;
- operationId;
- tag.

## Nivel QG-2 — Fase B/C

Agregar:

- security correcta;
- documentación de parámetros;
- response principal;
- headers especiales.

## Nivel QG-3 — Fase D/E

Agregar:

- status codes relevantes;
- content types;
- schema documentation;
- ejemplos en operaciones marcadas como críticas.

## Nivel QG-FINAL

Aplicar todas las reglas a todas las operaciones públicas.

> No activar una regla global que haga fallar cientos de endpoints antes de haber creado un mecanismo de migración incremental.

---

# 11. Formato de evidencia por unidad

Al cerrar una unidad, actualizar esta sección con una entrada compacta. Mantener únicamente las últimas 15 entradas; las más antiguas pueden condensarse por fase.

| Unidad | Fecha | Resultado | Archivos principales | Validación | Siguiente |
|---|---|---|---|---|---|
| `SC-001` | 2026-10-05 | Auditoría cerrada: transformers limitados a metadata transversal determinística; heurísticas funcionales identificadas para retiro | `DependencyInjectionService.cs`, `ScalarOperationDocumentationTransformer.cs`, `OpenApiSecurityMetadataTransformer.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; test dirigido no ejecutable porque `dotnet` no está instalado | `SC-002` |
| `SC-002` | 2026-10-05 | Transformer heurístico retirado y eliminado; test OpenAPI ajustado para no exigir prosa generada y detectar los marcadores heurísticos retirados | `DependencyInjectionService.cs`, `ScalarOperationDocumentationTransformer.cs` (eliminado), `OpenApiDocumentGenerationTests.cs` | Revisión estática; prueba dirigida pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-003` |
| `SC-003` | 2026-10-05 | Detector determinístico creado en tests para identificar ausencia, vacíos y placeholders sin modificar OpenAPI en runtime | `OpenApiDocumentationCompletenessDetector.cs`, `OpenApiDocumentationCompletenessDetectorTests.cs` | Revisión estática; tests dirigidos pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-004` |
| `SC-004` | 2026-10-05 | Transformer determinístico de `operationId` agregado; conserva IDs explícitos y usa controller, acción, método y ruta normalizada para los faltantes | `OpenApiOperationIdTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; prueba dirigida pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-005` |
| `SC-005` | 2026-10-05 | Taxonomía cerrada de 16 tags implementada con tabla explícita controller → tag; agrupamiento automático por controller sustituido | `OpenApiIntegrationTagsTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática de 69 controllers no excluidos de ApiExplorer; prueba dirigida pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-006` |
| `SC-006` | 2026-10-05 | Headers transversales documentados solo para acciones con lectura explícita; `Authorization` permanece en el modelo de seguridad y no se duplica como parámetro | `OpenApiTransversalHeadersTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida de headers y casos negativos; prueba dirigida pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-007` |
| `SC-007` | 2026-10-05 | Portada OpenAPI global definida para integradores con título, versión, separación ACH Colombia/CENIT, contexto NACHA-M, seguridad Bearer y convenciones transversales | `OpenApiDocumentMetadataTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática del diff; prueba dirigida pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-008` |
| `SC-008` | 2026-10-05 | QG-1 incremental activado mediante allowlist explícito de 4 operaciones; reutiliza detector SC-003 y exige Summary, Description, operationId y tag sin globalizar documentación funcional | `OpenApiDocumentGenerationTests.cs` | Revisión estática; `OpenApiDocumentationCompletenessDetectorTests` y `OpenApiDocumentGenerationTests` pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-090` |
| `SC-090` | 2026-10-05 | Checkpoint Fase A completado estáticamente: infraestructura SC-002 a SC-008 coherente, sin regresiones técnicas detectadas y sin cambios de código; implementación de Fase A completada | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet` no disponible: build, tests dirigidos y suite completa pendientes; estado `VERIFY` no bloqueante | `SC-101` |
| `SC-101` | 2026-10-05 | Generación XML habilitada en Application y Domain para contratos públicos; API conserva su configuración existente y se agregó prueba reutilizable de portabilidad | `Cfa.ACHInterbank.Application.csproj`, `Cfa.ACHInterbank.Domain.csproj`, `ProjectConfigurationPortabilityTests.cs` | Revisión estática; `ProjectConfigurationPortabilityTests` y `OpenApiDocumentGenerationTests` pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-102` |
| `SC-102` | 2026-10-06 | Patrón explícito de ejemplos request/response implementado con providers determinísticos, atributos por operación y transformer que solo completa `Example` sobre contratos existentes | `OpenApiExampleMetadata.cs`, `OpenApiExamplesTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiExamplesTransformerTests.cs` | Revisión estática; `OpenApiExamplesTransformerTests` y `OpenApiDocumentGenerationTests` pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-103` |
| `SC-103` | 2026-10-06 | Patrón de responses definido: contrato mediante metadata estándar y descripciones explícitas solo sobre responses ya publicados; ProblemDetails y BaseResponseModel permanecen diferenciados | `OpenApiResponseMetadata.cs`, `OpenApiResponseDescriptionsTransformer.cs`, `DependencyInjectionService.cs`, `OpenApiResponseDescriptionsTransformerTests.cs` | Revisión estática; `OpenApiResponseDescriptionsTransformerTests` y `OpenApiDocumentGenerationTests` pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-104` |
| `SC-104` | 2026-10-06 | Auth documentado explícitamente: cuatro operaciones, DTO XML, responses BaseResponseModel, ejemplos ficticios, QG-1 ampliado y QG-2 incremental activado para Auth | `AuthController.cs`, `AuthOpenApiExamples.cs`, `LoginRequest.cs`, `ForgotPasswordRequest.cs`, `ResetPasswordRequest.cs`, `AuthResult.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-105` |
| `SC-105` | 2026-10-06 | 15 operaciones de cámaras y ciclos documentadas: DTO/params XML, responses reales y QG-1/QG-2 ampliados sin ejemplos ni normativa específica | `ClearingHousesController.cs`, `AchCyclesController.cs`, `ClearingHouseDtos.cs`, `AchCycleDto.cs`, `PaginationRequest.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-106` |
| `SC-106` | 2026-10-06 | Dos catálogos de onboarding documentados: rieles de pago y perfiles NACHA-M, DTO XML, filtro opcional por cámara y QG-1/QG-2 ampliados sin ejemplos | `ClearingHousesController.cs`, `ClearingHouseDtos.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-107` |
| `SC-107` | 2026-10-06 | Quick Start agregado a la portada OpenAPI: login, `data.token`, Bearer, primera consulta protegida, catálogos de configuración y continuidad hacia ciclos, sin URLs ni códigos inventados | `OpenApiDocumentMetadataTransformer.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-190` |
| `SC-190` | 2026-10-06 | Checkpoint Fase B completado estáticamente: onboarding SC-101 a SC-107 coherente, QG-1/QG-2 conservados, infraestructura de Fase A intacta y sin regresiones técnicas detectadas | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet` no disponible: build, cinco clases de tests dirigidos y suite completa pendientes; estado `VERIFY` no bloqueante | `SC-201` |
| `SC-201` | 2026-10-06 | Cinco operaciones core de Transactions documentadas con status reales saneados, DTO/parámetros XML y QG-1/QG-2 ampliados; cuatro acciones reservadas a SC-202 | `TransactionsController.cs`, `TransactionPolicyModels.cs`, `AchTransactionListDto.cs`, `CompanyEntryDescriptionDto.cs`, `CreateTransactionDto.cs`, `AchTransaction.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-202` |
| `SC-202` | 2026-10-06 | Cuatro acciones restantes de Transactions documentadas: diferencias bulk, resultados parciales, consultas de integración, status reales y QG-1/QG-2 ampliados | `TransactionsController.cs`, `BulkIngestionDtos.cs`, `BulkAchTransactionDtos.cs`, `TransactionIntegrationResultModels.cs`, `TransactionIntegrationReadinessModels.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-203` |
| `SC-203` | 2026-10-06 | Seis operaciones de BulkIngestion documentadas: upload por archivo, tracking, paginación, resumen, retry/cancel, status reales, DTO XML y QG-1/QG-2 ampliados | `BulkIngestionController.cs`, `BulkFileUploadDtos.cs`, `BulkTrackingDtos.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` pendiente porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-204` |
| `SC-204` | 2026-10-06 | Tres operaciones de AchTraceability documentadas: certificación SOL02 y consultas de trazabilidad con status reales saneados, DTO XML y QG-1/QG-2 ampliados | `AchTraceabilityController.cs`, `AchTraceabilityDtos.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` y build pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-205` |
| `SC-205` | 2026-10-06 | Tres operaciones de Devoluciones ACH grupo 1 documentadas: consulta por ciclo, generación de archivo y catálogo de causales; status reales, DTO XML y QG-1/QG-2 ampliados a 46 operaciones | `AchReturnsController.cs`, `ReturnReasonsController.cs`, `AchReturnDtos.cs`, `CenitIncomingReturnPolicyModels.cs`, `ReturnReasonDto.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` y build pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-206` |
| `SC-206` | 2026-10-06 | Once operaciones de devolución de devolución y respuestas ACH documentadas en dos controllers; responses reales, DTO XML y QG-1/QG-2 ampliados a 57 operaciones | `AchReturnOfReturnController.cs`, `AchResponsesController.cs`, `AchResponseApiContracts.cs`, `AchReturnOfReturnEligibilityModels.cs`, `CenitReturnOfReturnModels.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` y build pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-207` |
| `SC-207` | 2026-10-06 | Catorce operaciones de políticas y reglas transaccionales documentadas en dos controllers; status reales, DTO XML y QG-1/QG-2 ampliados a 71 operaciones | `ClearingHouseTransactionPoliciesController.cs`, `ClearingHouseTransactionRulesController.cs`, `ClearingHouseTransactionRuleDtos.cs`, `OpenApiDocumentGenerationTests.cs` | Revisión estática dirigida; `OpenApiDocumentGenerationTests` y build pendientes porque `dotnet` no está instalado; estado `VERIFY` no bloqueante | `SC-208` |
| `SC-208` | 2026-10-06 | Nueve operaciones de políticas y reglas transaccionales documentadas en restricciones operacionales CFA y Capability Registry; status reales, DTO XML y QG-1/QG-2 ampliados a 80 operaciones; el tercer controller remanente se separa por límite de sesión | `CfaOperationalTransactionRestrictionsController.cs`, `PaymentRailCapabilityRegistryController.cs`, `CfaOperationalTransactionRestrictionAdminDtos.cs`, `PaymentRailCapabilityRegistryModels.cs`, `OpenApiDocumentGenerationTests.cs` | `dotnet --info` exit 127; validación .NET pendiente, revisión estática y script de solución ejecutados; estado `VERIFY` no bloqueante | `SC-209` |
| `SC-209` | 2026-10-07 | Una operación de TransactionPrerequisitePolicyController documentada con request/response reales, response 200 tipado y QG-1/QG-2 ampliados a 81 operaciones | `TransactionPrerequisitePolicyController.cs`, `OpenApiDocumentGenerationTests.cs` | `dotnet --info` exit 127; validación .NET pendiente, revisión estática y script de solución ejecutados; estado `VERIFY` no bloqueante | `SC-290` |
| `SC-290` | 2026-10-07 | Checkpoint Fase C completado estáticamente: SC-201 a SC-209 coherentes, QG-1 preservado en 81 operaciones, QG-2 en 77 y QG-3 desactivado; sin cambios funcionales ni de OpenAPI | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; build/tests .NET pendientes; `validate-solution-projects.py` exit 0 y revisión estática acumulada satisfactoria; estado `VERIFY` no bloqueante | `SC-301` |
| `SC-301` | 2026-10-07 | Cuatro consultas Incoming NACHA del grupo 1 cerradas contractualmente: summaries/descriptions saneados, responses reales, XML docs y QG-2 ampliado; QG-1 permanece en 81 y QG-2 sube a 81 | `IncomingNachaCommandCenterController.cs`, `IncomingNachaCommandCenterDtos.cs`, `OpenApiDocumentGenerationTests.cs` | `dotnet --info` exit 127; build/tests .NET no ejecutados; `validate-solution-projects.py` exit 0 y revisión estática dirigida satisfactoria; estado `VERIFY` no bloqueante | `SC-302` |
| `SC-302` | 2026-10-07 | Trece operaciones restantes de Incoming NACHA grupo 2 documentadas: observabilidad, ingestas, huérfanos y cola/acciones manuales; responses reales, XML docs y QG-1/QG-2 ampliados de 81 a 94 | `IncomingNachaCommandCenterController.cs`, `IncomingNachaCommandCenterDtos.cs`, `IncomingNachaStateMachineDtos.cs`, `IncomingNachaOrphanManualResolutionModels.cs`, `IncomingNachaAdmissionModels.cs`, `OpenApiDocumentGenerationTests.cs` | `dotnet --info` exit 127; build/tests .NET no ejecutados; `validate-solution-projects.py` exit 0 y revisión estática dirigida satisfactoria; estado `VERIFY` no bloqueante | `SC-303` |
| `SC-303` | 2026-10-07 | Delimitación resuelta en sesión de planificación: SC-303 queda READY para `NachaRecordDefinitionsController` + `NachaRecordLayoutsController` (10 operaciones); configuración oficial separada en dos reservas sin ID (11 lecturas / 12 mutaciones) para cumplir DEC-002, sin implementar OpenAPI | `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `validate-solution-projects.py` ejecutado; diff limitado al tracker; QG-1/QG-2 preservados en 94, QG-3 desactivado y examples 0 | `SC-303` READY para implementación; `SC-304` permanece `TODO` |
| `SC-303` | 2026-10-07 | Implementación contractual de definiciones/layouts NACHA legacy sobre la delimitación r303a: 10 operaciones con responses reales (200/404/410), XML docs y QG-1/QG-2 ampliados de 94 a 104; QG-3/examples sin cambios | `NachaRecordDefinitionsController.cs`, `NachaRecordLayoutsController.cs`, `NachaRecordDefinitionDto.cs`, `NachaRecordLayoutDto.cs`, `NachaRecordFieldDto.cs`, `NachaRecordSourceType.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; build/tests .NET no ejecutados; `validate-solution-projects.py` exit 0 y revisión estática dirigida satisfactoria; estado `VERIFY` no bloqueante | `SC-304` READY; reservas de configuración oficial NACHA sin IDs |
| `SC-304` | 2026-10-07 | Salida temprana por alcance ambiguo: el tracker solo nombra “Operaciones de rechazo/respuesta de archivos” y no asigna controllers/acciones/DTOs; múltiples superficies plausibles impiden una partición no arbitraria compatible con DEC-002 | `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; no se ejecutaron tests/build; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 104, QG-3 desactivado, examples 0 | `SC-304` BLOCKED; `SC-305` permanece TODO |
| `SC-304` | 2026-10-07 | Delimitación autorizada e implementación contractual exclusiva de `AchResponseOperationsController`: 14 operaciones de mappings, auditoría, huérfanos y reproceso; XML docs y QG-1/QG-2 ampliados de 104 a 118; QG-3/examples sin cambios | `AchResponseOperationsController.cs`, `AchResponseOperationsContracts.cs`, `AchResponseOperationModels.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática y comparación de cuerpos productivos satisfactoria; estado `VERIFY` | `SC-305` permanece `TODO` y no ejecutado |
| `SC-305` | 2026-10-07 | Salida temprana por alcance ambiguo: el tracker solo declara “CENIT Operations” sin controllers/acciones/DTOs asignados; se inventarió `CenitOperationsController` (7 operaciones) sin adoptarlo como alcance; reservas de SC-309 y configuración NACHA preservadas | `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 118, QG-3 desactivado, examples 0 | `SC-305` BLOCKED; pipeline no avanza |
| `SC-305` | 2026-10-07 | Delimitación humana autorizada e implementación contractual exclusiva de `CenitOperationsController`: 7 operaciones CENIT, XML docs de contratos directos y QG-1/QG-2 ampliados de 118 a 125; QG-3/examples sin cambios | `CenitOperationsController.cs`, `CenitChamberResponseModels.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática, policies y cuerpos productivos verificados; estado `VERIFY` | `SC-306` queda `READY` y no ejecutado |
| `SC-306` | 2026-10-07 | Salida temprana por alcance ambiguo: “Certificados — grupo 1” no asigna controllers/acciones. Se inventariaron `CertificateManagementController` (11 acciones lógicas, dos aliases de ruta) y `DigitalEnvelopeCertificatesController` (3 acciones) sin adoptar ninguna partición; SC-307/308/309 y configuración NACHA reservada preservadas | `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 125, QG-3 desactivado, examples 0 | `SC-306` BLOCKED; pipeline no avanza |
| `SC-306` | 2026-10-07 | Parte A autorizada e implementada: 6 acciones de `CertificateManagementController` bajo ambos aliases (12 operaciones) + 3 de `DigitalEnvelopeCertificatesController`; total 15 operaciones, XML docs directos y QG-1/QG-2 de 125 a 140. Remanente activate/revoke/delete/validate/audit reservado sin ID. | `CertificateManagementController.cs`, `DigitalEnvelopeCertificatesController.cs`, `CertificateManagementDtos.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de aliases, policies, responses y cuerpos productivos satisfactoria; estado `VERIFY` | `SC-307` READY; remanente certificados sin ID |
| `SC-307` | 2026-10-07 | Salida temprana por alcance ambiguo: el tracker solo indica “Sobre digital / operaciones criptográficas” y no asigna controllers/acciones. Se inventariaron `SobreDigitalController` (3 operaciones efectivas + testRSA ignorada), `NachaSecurityOperationsController` (8), `NachaExportController` (2) y `NachaUploadController` (2); varias particiones cumplen DEC-002 y existen solapamientos con SC-308/SC-309. | `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados ni simulados; QG-1/QG-2 preservados en 140, QG-3 desactivado, examples 0; `validate-solution-projects.py` exit 0 (`SOLUTION GRAPH: OK`) | `SC-307` BLOCKED; pipeline no avanza |
| `SC-307` | 2026-10-07 | Delimitación humana autorizada e implementación contractual exclusiva de `SobreDigitalController`: 3 operaciones públicas, testRSA preservada fuera de ApiExplorer, XML docs directos y QG-1/QG-2 ampliados de 140 a 143; QG-3/examples sin cambios | `SobreDigitalController.cs`, `ManagedDigitalEnvelopeContracts.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas, policies, responses, exclusión testRSA y cuerpos productivos satisfactoria; estado `VERIFY` | `SC-308` queda `READY`; SC-309 no ejecutado |
| `SC-308` | 2026-10-07 | Delimitación humana autorizada e implementación contractual exclusiva de `NachaSecurityOperationsController`: 8 operaciones, sin aliases, XML docs directos y QG-1/QG-2 ampliados de 143 a 151; QG-3/examples sin cambios | `NachaSecurityOperationsController.cs`, `NachaSecurityOperationsContracts.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas, policies, requests/responses y cuerpos productivos satisfactoria; estado `VERIFY` | `SC-309` queda `READY`; no ejecutado y sin controllers asignados inequívocamente |
| `SC-309` | 2026-10-07 | Delimitación humana autorizada e implementación contractual exclusiva de `NachaExportController` y `NachaUploadController`: 4 operaciones, sin aliases; content types, nombres de descarga/upload, errors y XML docs directos; QG-1/QG-2 ampliados de 151 a 155; QG-3/examples sin cambios | `NachaExportController.cs`, `NachaUploadController.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build .NET no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas, policies, media types, filenames, responses y cuerpos productivos satisfactoria; estado `VERIFY` | `SC-390` queda `READY`; no ejecutado |
| `SC-390` | 2026-10-07 | Checkpoint Fase D completado estáticamente: SC-301 a SC-309 coherentes; 11 controllers/78 operaciones de Fase D presentes en QG-1/QG-2; QG-1/QG-2 preservados en 155 únicas, QG-3 desactivado y sin documentación funcional nueva | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build/suite backend no ejecutados; `validate-solution-projects.py` exit 0; revisión estática acumulada y estructural satisfactoria; estado `VERIFY` | `SC-401` queda `READY`; no ejecutado |
| `SC-401` | 2026-10-07 | ACH Colombia File Exchange inspeccionado sin implementación: tracker no delimita controller/acciones/rutas/DTOs; `AchColombiaFileExchangeController` candidato con 14 operaciones/sin aliases y dentro de DEC-002, pero no se infiere si incluye configuration/administration | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 155; estado `BLOCKED` por planificación | decisión humana exacta requerida; no avanzar a `SC-402` |
| `SC-401` | 2026-10-07 | Delimitación humana autorizada e implementación contractual de 9 operaciones de transferencia/ejecución de `AchColombiaFileExchangeController`; bloque configuration/administration (5 operaciones) preservado sin ID; DTO XML y QG-1/QG-2 ampliados de 155 a 164 | `AchColombiaFileExchangeController.cs`, `AchManagedFileTransferModels.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas/policies/responses/download y cuerpos productivos satisfactoria; estado `VERIFY` | `SC-402` queda `READY`; no ejecutado |
| `SC-402` | 2026-10-07 | Integration Mapping Sets inspeccionado sin implementación: `IntegrationMappingSetsController` (12 operaciones) e `IntegrationCatalogController` (4) son superficies plausibles; el tracker no asigna alcance y ambos completos suman 16 > DEC-002 | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 164; estado `BLOCKED` por planificación | decisión humana exacta requerida; no avanzar a `SC-403` |
| `SC-402` | 2026-10-07 | Delimitación humana autorizada e implementación contractual exclusiva de 12 operaciones de `IntegrationMappingSetsController`; `IntegrationCatalogController` preservado como reserva sin ID | `IntegrationMappingSetsController.cs`, `IntegrationMappingSetDtos.cs`, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas/policies/responses/cuerpos satisfactoria; QG-1/QG-2 164→176; estado `VERIFY` | `SC-403` queda `READY`; no ejecutado |
| `SC-403` | 2026-10-07 | Integraciones SOAP/external inspeccionadas sin implementación: tracker sin asignación concreta; candidatos `SoapIntegrationSettingsController` (2), `UatContrapartidasController` (1), `NachaSoapUatConsoleController` (4) y acción SOAP dentro de `NachaOperationalReadinessController`; varias particiones plausibles | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; QG-1/QG-2 preservados en 176; estado `BLOCKED` por planificación | decisión humana exacta requerida; no avanzar a `SC-404` |
| `SC-403` | 2026-10-07 | Delimitación humana autorizada e implementación contractual de `SoapIntegrationSettingsController` (GET/PUT settings) + `UatContrapartidasController` (POST dispatch-cycle), 2 controllers / 3 operaciones / 0 aliases; SOAP-UAT/readiness preservado sin ID | `SoapIntegrationSettingsController.cs`, `UatContrapartidasController.cs`, DTOs SOAP/contrapartidas, `OpenApiDocumentGenerationTests.cs`, `SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados; `validate-solution-projects.py` exit 0; revisión estática de rutas/policies/responses/cuerpos satisfactoria; QG-1/QG-2 176→179; estado `VERIFY` | `SC-404` queda `READY`; no ejecutado |
| `SC-404` | 2026-10-07 | Scheduler — grupo 1 implementado exclusivamente en `SchedulerController`: 1 controller, 13 acciones/operaciones, 0 aliases; `TaskDefinitionsController` (5) reservado fuera de alcance. | `SchedulerController.cs`; `SchedulerAdminModels.cs`; `SchedulerMisfirePolicy.cs`; `SchedulerExecutionStatus.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; QG-1/QG-2 ampliados 179→192 y conjuntos idénticos; QG-3 desactivado; examples 0; `validate-solution-projects.py` exit 0 (`SOLUTION GRAPH: OK`) | SC-404 `VERIFY` solo por ausencia de .NET; SC-405 real = Scheduler grupo 2 si aplica, asociado a TaskDefinitions y dejado `READY`, no ejecutado |
| `SC-405` | 2026-10-07 | Scheduler — grupo 2 implementado exclusivamente en `TaskDefinitionsController`: 1 controller, 5 acciones/operaciones, 0 aliases; SC-404 cerrado e intacto. | `TaskDefinitionsController.cs`; `TaskDefinitionDto.cs`; `TaskParameterDto.cs`; `CalendarPolicyEnum.cs`; `ConcurrencyPolicyEnum.cs`; `PeriodicityTypeEnum.cs`; `TaskStatusEnum.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; QG-1/QG-2 ampliados 192→197; QG-3 desactivado; examples 0; `validate-solution-projects.py` exit 0 (`SOLUTION GRAPH: OK`) | SC-405 `VERIFY` solo por ausencia de .NET; SC-406 real = Reports — grupo 1, dejado `READY`, no ejecutado |
| `SC-406` | 2026-10-07 | Reports — grupo 1 inspeccionado sin implementación: `ReportsController` es la única superficie con tag funcional `Reportes`, pero contiene 20 operaciones y excede DEC-002; el tracker no asigna acciones a grupo 1/grupo 2 y existen particiones 9+11, 14+6 y 12+8 igualmente válidas. | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` ejecutado; QG-1/QG-2 preservados en 197; QG-3 desactivado; examples 0 | SC-406 `BLOCKED`; decisión humana debe enumerar METHOD + PATH de grupo 1; SC-407 y posteriores no avanzan |
| `SC-406` | 2026-10-07 | Decisión humana 12+8 aplicada: grupo 1 documenta transacciones, devoluciones/rechazos, archivos NACHA y ciclos con variantes PDF; 1 controller / 12 operaciones / 0 aliases. | `ReportsController.cs`; DTOs de Reports; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 197→209; QG-3 desactivado; examples 0 | SC-406 `VERIFY`; SC-407 grupo 2 reservado con 8 operaciones y dejado `READY`, no ejecutado |
| `SC-407` | 2026-10-07 | Reports — grupo 2 implementado exclusivamente sobre las 8 operaciones restantes de `ReportsController`: reconciliation, audit, history, traceability/pdf y accounting-review/export; 1 controller / 8 operaciones / 0 aliases. | `ReportsController.cs`; contratos directos de reconciliation/audit/history/traceability/accounting export; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 209→217; QG-3 desactivado; examples 0 | SC-407 `VERIFY`; SC-406 intacto; SC-408 dejado `READY`, no ejecutado |
| `SC-408` | 2026-10-07 | Administración/catálogos — lote 1 implementado exclusivamente en `IntegrationCatalogController` (4 operaciones) y configuration/administration de `AchColombiaFileExchangeController` (5); 2 controllers / 9 operaciones / 0 aliases. | `IntegrationCatalogController.cs`; `AchColombiaFileExchangeController.cs`; DTOs directos; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 217→226; QG-3 desactivado; examples 0; credential sin examples/secret en response | SC-408 `VERIFY`; SC-401/SC-402 cerrados; SC-409 dejado `READY`, no ejecutado |
| `SC-409` | 2026-10-07 | Administración/catálogos — lote 2 implementado exclusivamente sobre lectura de configuración NACHA: `NachaConfigProfilesController` (5 GET) + `NachaConfigProfilesReadOnlyController` (6 GET); 2 controllers / 11 operaciones / 0 aliases. | `NachaConfigProfilesController.cs`; `NachaConfigProfilesReadOnlyController.cs`; `NachaConfigAdminDtos.cs`; `NachaConfigProfileReadModels.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 226→237; QG-3 desactivado; examples 0; 12 mutaciones NACHA fuera e intactas | SC-409 `VERIFY`; SC-410 dejado `READY`, no ejecutado; mutaciones NACHA siguen reservadas sin ID |
| `SC-410` | 2026-10-08 | Auditoría, logs y observabilidad expuesta implementada exclusivamente sobre `AuditLogsController`, `NavigationLogsController`, `/health/live` y `/health/ready`; 2 controllers / 3 acciones controller / 2 Minimal APIs / 0 aliases / 5 operaciones. | `AuditLogsController.cs`; `NavigationLogsController.cs`; `DependencyInjectionService.cs`; DTOs directos/paginación/envelope; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 237→242; QG-3 desactivado; examples 0; Auth Logs y Outgoing Monitoring fuera e intactos | SC-410 `VERIFY`; `SC-490 — Checkpoint Fase E` dejado `READY`, no ejecutado |
| `SC-490` | 2026-10-08 | Checkpoint Fase E completado estáticamente sobre `SC-401..SC-410`: 87/87 operaciones; QG-1/QG-2 preservados en 242 únicas, conjuntos idénticos, 0 duplicados; reservas pendientes verificadas fuera. | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; 1.790 C# balanceados, 0 merge markers; OpenAPI/Scalar vigente; estado `VERIFY` | `SC-501 — Inventario de aliases/rutas duplicadas y ruta canónica` queda `READY`, no ejecutado |
| `SC-501` | 2026-10-08 | Inventario transversal de routing completado: 52 grupos alias / 104 METHOD + PATH, 0 duplicados exactos, 0 colisiones de template; 10 canónicas definidas por evidencia SC-303 y 42 CANÓNICA PENDIENTE. QG-1/QG-2 preservados en 242/242 e idénticos; delta = 0; reservas solo inventariadas cuando aplica. | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; 0 merge markers; 1.790 C# sin desbalance real; routing y OpenAPI/Scalar intactos; estado `VERIFY` | `SC-502 — Marcar/documentar endpoints deprecated sin eliminar rutas` queda `READY`, no ejecutado |
| `SC-502` | 2026-10-08 | Decisión humana de producto aplicada sobre ALIAS-033..042: 10 variantes `/api` sin uso clasificadas `UNUSED_ALIAS` y eliminadas; 0 aliases activos, 0 transiciones deprecated; canónicas sin `/api` preservadas. `[Obsolete]` retirado de ambos controllers; otros 42 grupos intactos. | `NachaRecordDefinitionsController.cs`; `NachaRecordLayoutsController.cs`; `LegacyNachaEndpointsTests.cs`; `SpaApiRouteIsolationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242 e idénticos; 0 merge markers; estado `VERIFY` | `SC-503 — Auditoría de parámetros sin descripción` queda `READY`, no ejecutado |
| `SC-503` | 2026-10-08 | Auditoría transversal de parámetros OpenAPI sobre 242 operaciones contractuales: 431 parámetros auditados (119 path, 298 query, 14 header); 423 válidos iniciales y 8 sin descripción corregidos con evidencia de código; 0 débiles, inválidos o sin evidencia. | `NachaExportController.cs`; `NachaUploadController.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242 e idénticos; nuevo gate de parámetros; routing sin cambios; estado `VERIFY` | `SC-504 — Auditoría de responses/status codes faltantes` queda `READY`, no ejecutado |
| `SC-504` | 2026-10-08 | Auditoría transversal de 476 pares operación/status sobre 242 operaciones; todos documentados válidamente, 0 faltantes/especulativos/incorrectos; gate exacto de status añadido sin cambiar metadata productiva. | `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242 e idénticos; estado `VERIFY` | `SC-505 — Auditoría de ejemplos faltantes en flujos críticos` queda `READY`, no ejecutado |
| `SC-505` | 2026-10-08 | Decisión humana aplicada: 10 METHOD + PATH críticos / 7 acciones lógicas. Auditoría de 13 superficies de example de-duplicadas por alias: inicial 10 MISSING_USEFUL_EXAMPLE + 3 NOT_USEFUL; final 10 EXAMPLE_VALID + 3 NOT_USEFUL. Añadidos 6 request examples y 4 response examples mediante providers explícitos; 15 placements OpenAPI por aliases; 0 corregidos/eliminados. | `TransactionsController.cs`; `AchColombiaFileExchangeController.cs`; `NachaSecurityOperationsController.cs`; `CriticalFlowOpenApiExamples.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242 e idénticos; QG-3 desactivado; routing, SC-502/503/504, 42 aliases pendientes y reservas preservados; estado `VERIFY` | `SC-506 — Auditoría de DTO/schemas ambiguos` queda `READY`, no ejecutado |
| `SC-506` | 2026-10-08 | Auditoría transversal de DTO/schemas sobre 242 operaciones: 290 contratos schema alcanzables / 2.179 propiedades serializadas; inicial 287 SCHEMA_CLEAR + 3 MISSING_DESCRIPTION (31 propiedades), final 290 SCHEMA_CLEAR; 0 ambiguos, metadata incorrecta/redundante o casos sin evidencia. | `AchTransaction.cs`; `AuditableEntity.cs`; `NachaValidationFailure.cs`; `PaginatedResult.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242; gates SC-503/504/505, routing, aliases pendientes y reservas preservados; estado `VERIFY` | `SC-507 — Regenerar o retirar DOC_API.md, docs_api.json, diagram.scalar.json según decisión técnica` queda `READY`, no ejecutado |
| `SC-507` | 2026-10-08 | Decisión técnica sobre artefactos derivados: `DOC_API.md`, `docs_api.json` y `diagram.scalar.json` clasificados `REMOVE_UNUSED` y eliminados; 0 KEEP_REGENERATE/KEEP_CURRENT/BLOCKED_GENERATION. Sin consumidores vigentes ni generador reproducible; snapshots desincronizados (97 acciones / 91 claves únicas frente a QG 242) e incluso con `GET /api/auth-logs` fuera de QG y health ausente. | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md`; eliminados `DOC_API.md`, `docs_api.json`, `diagram.scalar.json` | `dotnet --info` exit 127; no se simuló generación; `validate-solution-projects.py` exit 0; QG-1/QG-2 242/242; tests, routing, controllers, DTOs, aliases pendientes y reservas intactos; estado `VERIFY` | `SC-508 — Quality gate final para todas las operaciones públicas` queda `READY`, no ejecutado |
| `SC-508` | 2026-10-08 | Cierre r508a de las 92 operaciones previamente UNEXPECTED: 80 PUBLIC_CONTRACT incorporadas a QG y 12 INTENTIONAL_EXCLUSION justificadas (4 Maintenance, 8 simulador NACHA UAT/local). Gate final clasifica 416/416 y deja UNEXPECTED = 0; QG-1/QG-2 = 322/322; QG-3 desactivado. | `DependencyInjectionService.cs`; nuevo `OpenApiSc508PublicContractDocumentationTransformer.cs`; `OpenApiDocumentGenerationTests.cs`; tracker | `dotnet --info` exit 127; tests/build no ejecutados ni simulados; `validate-solution-projects.py` SOLUTION GRAPH: OK; SC-503=431 y SC-504=476 preservados; reservas y aliases intactos | `SC-900 — Auditoría final y cierre` queda `READY`, no ejecutada |
| `SC-900` | 2026-10-08 | Auditoría final y cierre estático: 416/416 operaciones clasificadas; QG-1/QG-2 = 322/322 e idénticos; UNEXPECTED = 0; SC-503..508, 10 aliases retirados, 42 grupos pendientes, 36 reservas y retiro de artefactos verificados sin inconsistencia. | `docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md` | `dotnet --info` exit 127; OpenApiDocumentGenerationTests/build Release no ejecutados ni simulados; `validate-solution-projects.py` = SOLUTION GRAPH: OK; merge markers = 0; estado `VERIFY` | Sin siguiente unidad; iniciativa Scalar queda `VERIFY` pendiente solo de validación .NET final |

---

# 12. Registro de decisiones

Registrar aquí decisiones que deban sobrevivir entre chats.

| ID | Decisión | Estado |
|---|---|---|
| `DEC-001` | La documentación funcional no será generada por heurísticas de nombre/ruta. | Aprobada |
| `DEC-002` | Se trabajará como máximo 2 controllers o 15 endpoints por sesión. | Aprobada |
| `DEC-003` | El quality gate se endurecerá incrementalmente. | Aprobada |
| `DEC-004` | Cambios de contrato funcional quedan fuera del alcance documental. | Aprobada |
| `DEC-005` | Este archivo será el tracker persistente entre sesiones de ChatGPT. | Aprobada |
| `DEC-006` | Los transformers OpenAPI solo pueden reflejar metadata transversal explícita, verificable y determinística; no redactarán `Summary` ni `Description` funcionales. | Aprobada |
| `DEC-007` | El esquema Bearer y el requisito de seguridad por operación derivado de `[AllowAnonymous]` / `[Authorize]` permanecen como responsabilidades de transformers separados. | Aprobada |
| `DEC-008` | La detección de documentación vacía, genérica o placeholder se moverá al quality gate de `SC-003`; no se rellenará automáticamente. | Aprobada |
| `DEC-009` | Una validación pendiente exclusivamente por limitaciones del entorno no bloquea el avance incremental; una unidad puede quedar en `VERIFY` mientras la siguiente queda `READY`. | Aprobada |
| `DEC-010` | La documentación funcional parte del comportamiento real del código; la normativa complementa, pero no prueba por sí sola lo implementado. | Aprobada |
| `DEC-011` | ACH Colombia y CENIT son cámaras independientes; sus reglas, ciclos, horarios, causales, estructuras y validaciones no se mezclan ni se extrapolan entre sí. | Aprobada |
| `DEC-012` | NACHA-M debe documentarse con contexto de cámara cuando existan diferencias o riesgo de ambigüedad; una regla solo se declara común tras comprobarla para ambas cámaras. | Aprobada |
| `DEC-013` | Las discrepancias entre código y documentación normativa se registran con cámara, fuente y componente de código, sin resolverlas mediante suposiciones. | Aprobada |
| `DEC-014` | Se permiten fragmentos normativos breves, fieles y relevantes; Scalar no se convertirá en una reproducción extensa de los manuales. | Aprobada |
| `DEC-015` | Cada unidad trabaja exclusivamente sobre el ZIP acumulado de la unidad inmediatamente anterior. `v124` permanece como versión base del aplicativo; desde SC-003 los ZIP usan revisión `rNNN` equivalente a la unidad, sin renombrar los ZIP históricos SC-001 y SC-002. | Aprobada |
| `DEC-016` | La convención estable de `operationId` conserva cualquier ID explícito no vacío; para acciones MVC sin ID usa `Controller_Action_HTTP_RouteNormalizada` y, sin `ControllerActionDescriptor`, `HTTP_RouteNormalizada`. La ruta forma parte obligatoria del ID, los parámetros se representan por nombre y no se permiten sufijos basados en orden, contadores, GUID o tiempo. | Aprobada |
| `DEC-017` | La taxonomía funcional OpenAPI es cerrada y explícita. La asignación controller → tag se resuelve por tabla exacta basada en `ControllerActionDescriptor.ControllerName`, con un tag primario por operación; no se deriva mediante heurísticas de nombres, rutas o verbos. ACH Colombia, CENIT y NACHA-M se mantienen diferenciados. Cualquier redefinición futura exige registrar una nueva decisión. | Aprobada |
| `DEC-018` | Los headers transversales solo se documentan cuando existe evidencia explícita de que la operación los consume. La asignación se mantiene cerrada por controller + acción, sin inferencias por rutas, tags o verbos. `Authorization` se representa mediante el modelo de seguridad OpenAPI y no se duplica como parámetro header. | Aprobada |
| `DEC-019` | La metadata global OpenAPI describe el contrato ACHInterbank para integradores, mantiene separados ACH Colombia y CENIT, presenta NACHA-M siempre con contexto de cámara cuando corresponda y documenta la seguridad mediante Bearer JWT sin duplicar `Authorization` como parámetro. La portada no inventa metadata corporativa, servidores ni reglas funcionales por operación. | Aprobada |
| `DEC-020` | QG-1 se aplica incrementalmente mediante un allowlist explícito de operaciones identificado por `METHOD + PATH`. Una operación entra al quality gate únicamente por una decisión deliberada de una unidad de documentación; la pertenencia no se infiere por controller, ruta parcial, tag, `operationId` ni por la mera existencia actual de `Summary`/`Description`. | Aprobada |
| `DEC-021` | Cada unidad funcional posterior debe incorporar al allowlist QG-1 las operaciones que deje documentalmente terminadas según el nivel aplicable. `Summary` y `Description` no se vuelven requisitos globales hasta la fase prevista; `operationId` y tags conservan sus validaciones globales. | Aprobada |
| `DEC-022` | Los comentarios de schemas deben residir junto al DTO o contrato en su proyecto de origen. Los proyectos que exponen tipos públicos al contrato OpenAPI deben generar documentación XML; no se duplicará documentación de propiedades mediante transformers globales ni se cargarán XML manualmente en runtime. | Aprobada |
| `DEC-023` | Los ejemplos OpenAPI son metadata explícita y determinística. Se definen mediante providers reutilizables asociados deliberadamente a una operación. Los transformers no inventan ejemplos, status codes, media types ni contratos, no sobrescriben ejemplos existentes y usan la configuración JSON real de la API. Todo ejemplo debe ser ficticio, técnicamente válido y libre de datos sensibles. | Aprobada |
| `DEC-024` | La documentación de responses refleja el contrato HTTP existente. Los status codes, schemas y content types no se inventan desde transformers. `ProblemDetails` solo se documenta donde el código realmente lo produce; contratos existentes como `BaseResponseModel` se conservan. Las descripciones son metadata explícita asociada a responses previamente publicados y no reemplazan descripciones específicas preexistentes. | Aprobada |
| `DEC-025` | Las unidades funcionales de Fase B incorporan deliberadamente sus operaciones terminadas a QG-1 y, cuando corresponda, a QG-2 mediante `METHOD + PATH`. Los quality gates no descubren operaciones automáticamente por controller, ruta parcial, tag, `operationId` ni presencia de documentación. | Aprobada |
| `DEC-026` | Para desbloquear SC-303, el alcance ejecutable se limita a los dos controllers legacy de definiciones/layouts NACHA (10 operaciones). La configuración oficial `nacha-config` no se mezcla con ese lote: se reserva en dos grupos de planificación sin ID —lectura (11 operaciones/2 controllers) y gestión/mutación (12 operaciones/1 controller)— hasta que el plan les asigne unidades reales. SC-304 conserva su objetivo original y no se reutiliza. | Aprobada |
| `DEC-027` | Para desbloquear SC-304, la decisión de planificación autorizada limita “Operaciones de rechazo/respuesta de archivos” exclusivamente a `AchResponseOperationsController` y sus 14 operaciones reales (1 controller, 14 endpoints), cumpliendo DEC-002. `AchOutboundReturnTransportController`, `RegulatoryCatalogsController`, otros controllers, SC-305, SC-309, Reportes, catálogos no incluidos y la configuración NACHA reservada quedan fuera y no reciben IDs ni reasignación automática. | Aprobada |
| `DEC-028` | Para desbloquear SC-305, la decisión humana de planificación autorizada limita `CENIT Operations` exclusivamente a `CenitOperationsController` y sus 7 operaciones reales (1 controller, 7 endpoints), cumpliendo DEC-002. `AchOutboundReturnTransportController`, `RegulatoryCatalogsController`, `AchResponseOperationsController`, superficies de SC-309, `NachaConfigProfilesController`, `NachaConfigProfilesReadOnlyController`, configuración NACHA reservada y cualquier otro controller quedan fuera y no reciben reasignación automática. | Aprobada |
| `DEC-029` | Para hacer ejecutable SC-306 sin alterar routing, la decisión humana autorizada define Parte A en `CertificateManagementController` solo para `managed/preview`, `managed`, `public`, `private`, listado base y `{id}/versions` conservando ambos prefijos (12 operaciones), más las 3 operaciones de `DigitalEnvelopeCertificatesController`: 15 operaciones OpenAPI totales, exactamente el límite DEC-002. `activate`, `revoke`, `delete`, `validate` y `audit` quedan como “Certificados — remanente de gestión” (10 operaciones por aliases) sin ID; no se reasignan a SC-307 porque su objetivo real es “Sobre digital / operaciones criptográficas”. | Aprobada |
| `DEC-030` | Para ejecutar SC-308, la decisión humana autorizada limita “NACHA Security Operations” exclusivamente a `NachaSecurityOperationsController`. El controller materializa 8 operaciones OpenAPI bajo un único prefijo, sin aliases, por lo que cumple DEC-002 (1 controller / 8 endpoints). El remanente de certificados de SC-306, `SobreDigitalController`, `NachaExportController`, `NachaUploadController`, configuración NACHA reservada y SC-309 quedan fuera de la unidad. | Aprobada |
| `DEC-031` | Para desbloquear SC-401, la decisión humana autorizada limita “ACH Colombia File Exchange” a 9 operaciones de transferencia/ejecución de `AchColombiaFileExchangeController`: transfers (consulta, detalle, download), outbound/inbound execute, retry, reprocess, archive y retire. Las 5 operaciones configuration/administration del mismo controller quedan como reserva sin ID y no se reasignan automáticamente a SC-402 ni a unidades de administración. El alcance ejecutado es 1 controller / 9 endpoints, sin aliases, y cumple DEC-002. | Aprobada |
| `DEC-032` | Para desbloquear SC-402, la decisión humana autorizada limita “Integration Mapping Sets” exclusivamente a `IntegrationMappingSetsController` y sus 12 operaciones reales, sin aliases (1 controller / 12 endpoints), cumpliendo DEC-002. `IntegrationCatalogController` queda como “Integration Catalog — pendiente” con 4 operaciones, sin ID y sin reasignación automática a SC-403; `SoapIntegrationSettingsController` y reservas previas permanecen fuera. | Aprobada |
| `DEC-033` | Para desbloquear SC-403, la decisión humana autorizada limita “Integraciones SOAP/external expuestas por REST” a `SoapIntegrationSettingsController` (GET/PUT `/api/users/soap-integrations`) y `UatContrapartidasController` (POST `/api/uat/contrapartidas/dispatch-cycle`): 2 controllers, 3 operaciones, 0 aliases, dentro de DEC-002. `NachaSoapUatConsoleController` y `NachaOperationalReadinessController` quedan como “NACHA SOAP-UAT/readiness — pendiente” sin ID; `IntegrationCatalogController` y reservas previas permanecen fuera. | Aprobada |
| `DEC-034` | Para desbloquear SC-404, la decisión humana autorizada limita “Scheduler — grupo 1” exclusivamente a `SchedulerController`: 1 controller, 13 acciones lógicas, 0 aliases y 13 operaciones OpenAPI, dentro de DEC-002. `TaskDefinitionsController` queda fuera de SC-404 con 5 operaciones. El objetivo real preexistente de SC-405 es “Scheduler — grupo 2 si aplica”; al ser el único remanente Scheduler directo inventariado, `TaskDefinitionsController` se registra como alcance reservado compatible de SC-405 y SC-405 queda `READY`, sin ejecución. | Aprobada |
| `DEC-035` | La decisión humana autorizada ejecuta `SC-405 — Scheduler — grupo 2` exclusivamente sobre `TaskDefinitionsController`: 1 controller, 5 acciones lógicas, 0 aliases y 5 operaciones OpenAPI, dentro de DEC-002. SC-404 permanece cerrado; no se absorben reservas ajenas al Scheduler. | Aprobada |
| `DEC-036` | Para desbloquear SC-406, la decisión humana autorizada adopta la partición 12+8 de `ReportsController`: `SC-406 — Reports — grupo 1` incluye exclusivamente transactions sent/received, returns/rejections, nacha-files y cycles, cada familia con su variante PDF (12 operaciones, 0 aliases); `SC-407 — Reports — grupo 2` queda reservado a reconciliation, audit, history, traceability/pdf y accounting-review/export (8 operaciones). SC-406 cumple DEC-002; SC-407 no se ejecuta en la misma sesión. | Aprobada |
| `DEC-037` | La decisión humana autorizada ejecuta `SC-407 — Reports — grupo 2` exclusivamente sobre las 8 operaciones restantes de `ReportsController`: reconciliation, reconciliation/pdf, audit, audit/pdf, history, history/pdf, traceability/pdf y accounting-review/export. El alcance es 1 controller / 8 acciones / 0 aliases / 8 operaciones OpenAPI, cumple DEC-002 y mantiene SC-406 cerrado e intacto. `SC-408` y posteriores no se ejecutan en la misma sesión. | Aprobada |
| `DEC-038` | La decisión humana autorizada ejecuta `SC-408 — Administración/catálogos — lote 1` exclusivamente sobre `IntegrationCatalogController` (4 operaciones) y las 5 operaciones configuration/administration pendientes de `AchColombiaFileExchangeController`: 2 controllers / 9 acciones / 0 aliases / 9 operaciones OpenAPI, dentro de DEC-002. Se cierran únicamente las reservas “Integration Catalog — pendiente” y “ACH Colombia File Exchange — configuration/administration pendiente”; SOAP-UAT/readiness, remanente de certificados SC-306 y configuración NACHA reservada siguen fuera. SC-409 y posteriores no se ejecutan. | Aprobada |
| `DEC-039` | La decisión humana autorizada ejecuta `SC-409 — Administración/catálogos — lote 2` exclusivamente sobre la reserva “Configuración NACHA — lectura”: 5 GET de `NachaConfigProfilesController` + 6 GET de `NachaConfigProfilesReadOnlyController`, total 2 controllers / 11 acciones / 0 aliases / 11 operaciones OpenAPI, dentro de DEC-002. Las 12 operaciones no-GET de `NachaConfigProfilesController` permanecen como “Configuración NACHA — gestión/mutación pendiente” sin ID y no se asignan automáticamente a SC-410. SOAP-UAT/readiness y remanente de certificados SC-306 permanecen fuera. | Aprobada |
| `DEC-040` | La decisión humana autorizada ejecuta `SC-410 — Auditoría, logs y observabilidad expuesta` exclusivamente sobre `AuditLogsController` (`GET /api/audit-logs`), `NavigationLogsController` (`GET`/`POST /api/navigation-logs`) y las rutas públicas `GET /health/live` + `GET /health/ready`: 2 controllers / 3 acciones controller / 2 Minimal APIs / 0 aliases / 5 operaciones efectivas, dentro de DEC-002. `GET /api/auth-logs` permanece como “Auditoría/logs — Auth Logs pendiente” sin ID; `OutgoingTransactionMonitoringController` conserva “Trazabilidad y conciliación”; mutaciones NACHA, SOAP-UAT/readiness y certificados remanentes permanecen fuera. | Aprobada |
| `DEC-041` | Política de producto para SC-502: la aplicación no está en producción y no existe compatibilidad legacy por defecto. Una ruta alternativa solo se conserva si existe necesidad contractual/funcional vigente; `deprecated: true` solo se usa para una transición real demostrada. Para ALIAS-033..042, las variantes `/api` no tienen consumidor vigente ni contrato independiente y se eliminan como `UNUSED_ALIAS`; las canónicas sin `/api` de SC-303 se conservan. La metadata `[Obsolete]` de ambos controllers se retira por no representar una transición activa. Los otros 42 grupos alias permanecen fuera de la limpieza física de SC-502. | Aprobada |

---

# 13. Prompt maestro para una nueva sesión de ChatGPT

Copiar este prompt al iniciar una nueva conversación y adjuntar el **último ZIP** producido por la sesión anterior.

```text
Trabaja sobre el ZIP adjunto de ACHInterbank.

Esta tarea forma parte de una migración incremental de la documentación Scalar/OpenAPI.
Antes de modificar código, localiza y lee completamente:

docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md

Ese archivo es la fuente de verdad del progreso. No reconstruyas el plan desde cero y no
vuelvas a ejecutar fases ya marcadas DONE.

OBJETIVO DE ESTA SESIÓN
1. Lee "Estado de reanudación".
2. Ejecuta ÚNICAMENTE la unidad indicada como "Próxima unidad" o la siguiente unidad READY.
3. No avances a una segunda unidad aunque termines rápido.
4. Actualiza el tracker antes de entregar.

LÍMITES ANTI-BLOQUEO
- Máximo 2 controllers.
- Máximo 15 operaciones HTTP.
- Máximo 8 archivos de código modificados, más el tracker.
- No hagas build del SPA.
- No ejecutes pruebas E2E salvo que la unidad lo exija.
- No ejecutes dotnet test completo salvo que sea un checkpoint.
- Prefiere tests dirigidos.
- No hagas inventarios completos repetidos del repositorio.
- Si un comando tarda más de 120 segundos, detenlo y registra la validación como pendiente.
- Si la tarea supera estos límites, NO continúes: divide la unidad, actualiza el tracker y detente.

REGLAS DE DOCUMENTACIÓN
- No inventes semántica a partir del nombre de ruta, controller o método.
- Verifica comportamiento leyendo implementación, contratos y documentación existente.
- No cambies lógica de negocio para hacer que la documentación sea más sencilla.
- No cambies contratos HTTP salvo instrucción explícita.
- No introduzcas secretos ni datos reales en ejemplos.
- Mantén compatibilidad hacia atrás.

ENTREGA
- Código modificado de la unidad.
- Tracker actualizado con estado, archivos modificados, validación y próxima unidad.
- Resumen de máximo 12 puntos.
- Si trabajas sobre ZIP, genera UN solo ZIP actualizado al final.
- No generes documentos de progreso adicionales: usa el tracker existente.

Si encuentras una ambigüedad funcional que el código no resuelve, marca la unidad BLOCKED y
explica qué decisión necesito tomar en vez de inventar una respuesta.
```

---

# 14. Prompt corto para continuar la siguiente unidad

Cuando ya se está trabajando en la misma conversación o proyecto:

```text
Continúa el plan incremental de Scalar.
Lee primero docs/api/SCALAR-DOCUMENTATION-INCREMENTAL.md y ejecuta solo la próxima unidad READY.
Respeta los límites anti-bloqueo del tracker. No rehagas inventarios ni unidades DONE.
Actualiza el tracker y entrega un único ZIP al finalizar.
```

---

# 15. Prompt para documentar un lote de controladores

Usar únicamente cuando la siguiente unidad sea de documentación explícita de endpoints.

```text
Ejecuta solo la unidad SC-XXX del tracker Scalar.

Para cada endpoint incluido:
1. Lee la acción completa y sus servicios/DTO inmediatos necesarios para entender su contrato.
2. Verifica autorización real, no la deduzcas por nombre.
3. Escribe Summary y Description humanos y específicos.
4. Documenta parámetros, headers y responses que correspondan al comportamiento real.
5. Agrega ejemplos solo cuando puedan construirse sin inventar reglas de negocio.
6. Mantén el contrato HTTP existente.
7. No toques otros controllers fuera de la unidad.
8. Ejecuta únicamente validación dirigida del alcance.
9. Actualiza el tracker y detente.

Límite absoluto: 2 controllers o 15 operaciones, lo que ocurra primero.
```

---

# 16. Prompt para infraestructura OpenAPI/Scalar

```text
Ejecuta solo la unidad SC-XXX de infraestructura del tracker.

La meta es mejorar metadata OpenAPI de forma transversal, no documentar controllers masivamente.
No agregues inferencias funcionales basadas en nombres de ruta.
Mantén los transformers limitados a comportamiento determinístico.
Añade o ajusta tests dirigidos para la convención modificada.
No actives todavía reglas globales que hagan fallar endpoints aún no migrados; usa rollout incremental.
Actualiza el tracker y detente al cerrar la unidad.
```

---

# 17. Prompt para checkpoint

```text
Ejecuta únicamente el checkpoint SC-XXX.

No agregues documentación nueva salvo correcciones necesarias para cerrar regresiones detectadas.
Valida la fase completada con las pruebas disponibles.
Si el entorno permite dotnet build/test completo, ejecútalo una sola vez.
Si no está disponible o supera el tiempo razonable, registra exactamente qué quedó pendiente.
Genera métricas de cobertura solo una vez en este checkpoint.
Actualiza el tracker con:
- unidades completadas;
- métricas;
- fallos;
- deuda pendiente;
- próxima unidad READY.
Después detente.
```

---

# 18. Prompt de auditoría final

```text
Ejecuta SC-900, auditoría final Scalar/OpenAPI.

No asumas cobertura por atributos ni por transformers. Genera/inspecciona el OpenAPI real si el
entorno lo permite y valida todas las operaciones públicas contra el Definition of Done.

Reporta al menos:
- total de operaciones públicas;
- operaciones sin summary específico;
- operaciones sin description específica;
- operationId ausentes/duplicados;
- parámetros sin descripción;
- respuestas insuficientes;
- schemas públicos ambiguos;
- headers transversales faltantes;
- rutas deprecated sin marcar;
- placeholders o textos heurísticos restantes.

No corrijas cientos de hallazgos en esta misma sesión. Si hay hallazgos, crea unidades pequeñas
en el tracker y termina la auditoría.
```

---

# 19. Protocolo para no perder cambios entre chats

Cuando se use ChatGPT Chat con archivos ZIP:

1. La entrada de una sesión debe ser el **ZIP resultante de la sesión anterior**, no una versión más vieja.
2. El tracker dentro del ZIP debe coincidir con el tracker entregado en la sesión anterior.
3. Al comenzar, verificar el nombre del artefacto base registrado en `Estado de reanudación`.
4. Al finalizar, actualizar `Último artefacto base` con el nombre del nuevo ZIP.
5. No mezclar cambios provenientes de dos ZIP distintos sin una conciliación explícita.
6. Si hay duda sobre cuál ZIP es el más reciente, detenerse antes de editar.

## Convención permanente de artefactos

- Cada unidad trabaja **exclusivamente** sobre el ZIP acumulado producido por la unidad inmediatamente anterior.
- `v124` identifica la versión base del aplicativo y no se incrementará artificialmente durante este plan documental.
- Los ZIP históricos de `SC-001` y `SC-002` conservan sus nombres y no necesitan renombrarse.
- Desde `SC-003`, todo ZIP acumulativo usa una revisión `rNNN` equivalente al número de unidad. La siguiente unidad debe tomar ese ZIP como única base.

```text
ACHInterbank-v124-scalar-SC001.zip
ACHInterbank-v124-scalar-SC002.zip
ACHInterbank-v124-scalar-r003-SC003.zip
ACHInterbank-v124-scalar-r004-SC004.zip
...
```

Para checkpoints posteriores a SC-003 se mantiene la misma revisión acumulativa de la unidad correspondiente, de modo que el nombre identifique sin ambigüedad la secuencia de trabajo.

Esta convención hace evidente qué cambios contiene cada archivo y reduce el riesgo de perder modificaciones acumuladas del repositorio.

---

# 20. Checklist de cierre de cada sesión ChatGPT

Antes de responder al usuario:

- [ ] Ejecuté solo una unidad.
- [ ] No excedí 2 controllers / 15 endpoints.
- [ ] No modifiqué lógica de negocio fuera de alcance.
- [ ] No inventé reglas funcionales.
- [ ] Revisé el diff de los archivos modificados.
- [ ] Ejecuté una validación dirigida o expliqué por qué quedó pendiente.
- [ ] Actualicé `Estado de reanudación`.
- [ ] Actualicé la fila de evidencia.
- [ ] Definí exactamente una próxima unidad READY.
- [ ] No dejé una unidad marcada DONE sin criterio de salida cumplido.
- [ ] Si generé ZIP, fue una sola compresión final.
- [ ] El ZIP contiene este tracker actualizado.

---

# 21. Criterio de finalización de toda la iniciativa

La iniciativa se cierra únicamente cuando el OpenAPI real cumple, para todas las operaciones públicas aplicables:

- 100 % con summary específico.
- 100 % con description específica.
- 100 % con operationId único y estable.
- 100 % con tag funcional.
- 100 % con seguridad correctamente representada.
- 100 % de parámetros de negocio comprensibles.
- 100 % de headers especiales documentados donde aplican.
- 100 % con respuesta principal documentada.
- Status codes y content types coherentes con runtime.
- Schemas públicos entendibles.
- Ejemplos de los flujos críticos.
- 0 placeholders documentales.
- 0 afirmaciones funcionales generadas por heurística.
- Rutas legacy claramente diferenciadas de las rutas recomendadas.
- Quality gate activo para impedir regresiones.
- Documentación auxiliar reconciliada o retirada.

El objetivo final no es “tener texto en Scalar”; es que un integrador pueda utilizar la API de forma segura y correcta sin tener que leer los controladores para descubrir el contrato.

## ACH Colombia MFT / horarios operativos — estado FASE 9 (2026-10-09)

Estado del frente: **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**. Esta sección actualiza el inventario documental; no implica build/test del paquete actual, homologación externa ni producción.

Endpoints administrativos vigentes usados por la SPA:

- `GET /api/ach-colombia/file-exchange/administration` — consulta configuración MFT sin devolver el secreto.
- `PUT /api/ach-colombia/file-exchange/administration` — actualiza tipo de conexión, servidor/rutas y parámetros administrativos con control de concurrencia.
- `PUT /api/ach-colombia/file-exchange/administration/credential` — establece/reemplaza credencial; el secreto solo existe en el request y no se devuelve.
- `POST /api/ach-colombia/file-exchange/administration/test-connection` — valida configuración guardada, autenticación y acceso a rutas; no envía NACHA-M.
- `GET /api/ach-colombia/operational-schedules/administration` — consulta ciclos + horario operacional, con ventana regulatoria solo lectura.
- `GET /api/ach-colombia/operational-schedules` y `GET /api/ach-colombia/operational-schedules/by-cycle/{id}` — consulta programación persistida.
- `POST /api/ach-colombia/operational-schedules` — crea programación operacional.
- `PUT /api/ach-colombia/operational-schedules/{id}` — actualiza programación operacional y, en la implementación actual, solicita resincronización Quartz después de persistir.
- `PATCH /api/ach-colombia/operational-schedules/{id}/deactivate` — desactiva programación operacional y sincroniza la programación resultante.

Semántica visible: outbound `Transferred` se presenta como **Publicado para envío**. No equivale a pickup por GoAnywhere, recepción/aceptación ACH Colombia ni procesamiento S3/Integra.

Nota de mantenimiento: algunas descripciones OpenAPI embebidas fueron escritas durante FASE 2 y pueden conservar texto histórico del tipo “no reprograma Quartz en esta fase”. FASE 9 no modifica `src/`; la conducta vigente de FASE 3 sí realiza persist-then-sync. Corregir ese texto embebido requiere un cambio documental en código fuente separado del cierre FASE 9.
