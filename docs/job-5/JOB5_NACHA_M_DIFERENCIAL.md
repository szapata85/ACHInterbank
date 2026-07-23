# JOB 5 — NACHA-M diferencial

Fecha de evaluación: 2026-07-23

Línea base funcional JOB 5: `5abd1e91aefc346adbd2dde09632a4e48d7daabb`

Línea base de la corrección focal: `b317e5df7813479ec4d76dc721f5f7f7367ebba7`

Veredicto: **NO-GO NORMATIVO**

## Decisión

No se habilitó un layout, parser, generador ni despacho diferencial para ACHCOL o CENIT. Los documentos locales no permiten publicar responsablemente un perfil diferencial:

- ACHCOL: el PDF se identifica en su portada/control como versión 32, mientras la ficha de devolución 6.6, páginas internas 162–169, se identifica como versión 31 de agosto de 2024. No existe en el repositorio un vector diferencial oficial o una referencia real verificada que resuelva versión, vigencia y aplicabilidad contractual a CFA.
- CENIT: los anexos prueban reglas operativas y catálogos de causales, pero el DSP-152 remite al `Manual de Especificaciones del Formato para el Servicio de Transferencia de Archivos – STA`, que no está disponible. No existe un vector diferencial oficial o referencia real verificada.
- Los archivos `.RET` existentes son fixtures sintéticos de regresión. No se reclasificaron como oficiales ni se modificaron.

La ausencia de un perfil normativamente sustentado bloquea el procesamiento antes del parser y antes de toda correlación, evento funcional o llamada SOAP. Las pruebas E2E de ACHCOL y CENIT usan los `.RET` solo como `SyntheticFixture` y demuestran `ProfileNotFound`, persistencia auditable y cero decisiones o despachos.

## Arquitectura resultante

Se conserva la Opción C: `CfgProfile`, `CfgProfileRecord`, `CfgLayoutVariant` y `CfgLayoutField` son la única ruta de selección oficial. No se creó un segundo motor, endpoint de carga, cliente SOAP, scheduler ni tabla.

El selector `INachaConfigResolver` ahora cierra ante ambigüedad y expone exactamente:

- `ProfileSelected`
- `ProfileNotFound`
- `ProfileAmbiguous`
- `ProfileInactive`
- `ProfileVersionUnsupported`
- `ClearingHouseUndetermined`

La prioridad de selección usa cámara explícita, flujo, dirección, clase de servicio cuando existe, versión solicitada, vigencia, estado publicado, etiqueta de homologación y predicados del layout. Un nombre físico no selecciona un perfil. Dos perfiles o layouts indistinguibles producen `ProfileAmbiguous`; nunca se toma el primero.

Un perfil diferencial ejecutable debe estar publicado, vigente y tener `IsHomologated=true` e `IsPlaceholder!=true`. También debe aportar un único layout aplicable para todos los tipos de registro presentes.

## Flujo de carga

El endpoint `NachaUpload` existente conserva el flujo y ahora admite los nombres externos controlados ya definidos:

- CENIT sin extensión: `^\d{7}\.\d{3}\.\d{8}\.\d+$`
- ACH Colombia: `.OUT`
- devoluciones controladas existentes: `.RET`
- fixtures internos ya soportados: `.ach`, `.nacha`, `.txt`

La detección de candidato diferencial es estructural: registro físico de 106 caracteres, tipo `7`, addenda `99`. El nombre solo es una señal complementaria.

El flujo efectivo queda:

```text
NachaUpload
→ persistencia inicial + SHA-256
→ resolución de ciclo/cámara
→ selección cerrada de perfil
→ bloqueo auditable por NO-GO normativo
→ conciliación
```

Si en el futuro se selecciona un perfil homologado pero el parser diferencial aún no consume su snapshot, el flujo permanece bloqueado con `DIFFERENTIAL_TABLE_DRIVEN_PARSER_NOT_ENABLED`; no cae en el parser estático.

## Generación y validación

La infraestructura table-driven existente sigue disponible para perfiles sustentados, pero no se publicó configuración diferencial nueva. Por tanto:

- generación diferencial: bloqueada;
- parseo diferencial: bloqueado antes del motor estático;
- validación estructural específica: no implementada sin layout verificable;
- validación funcional y elegibilidad SOAP: no se ejecutan si falla la selección;
- `Proc_Transacciones` y `Proc_Contrapartidas`: no son invocados por este flujo.

Esta omisión es deliberada: implementar posiciones, catálogos o reglas a partir de fixtures sintéticos violaría la compuerta normativa.

## Correlación

Se conserva el correlador existente y su prioridad por identificadores inequívocos: traza original, traza, identificador externo y clave compuesta endurecida. Sus resultados persistidos distinguen resolución exacta, no encontrada y ambigua.

El JOB 5 añade una guarda anterior: ningún candidato diferencial alcanza correlación si la selección no es `ProfileSelected`. En consecuencia, `ProfileNotFound`, `ProfileAmbiguous`, `ProfileInactive`, `ProfileVersionUnsupported` y `ClearingHouseUndetermined` no cambian estados funcionales ni generan despacho.

## RegistrarRespuestaTransaccion

Se reutilizan el caso de uso, gateway, cliente físico, catálogo y mapping existentes. El request físico contiene exactamente:

```text
idCanal
nombreCanal
idTransaccion
idEstado
causal
idTransaccionAxon
descripcionCausal
```

Cada intento ahora persiste un request JSON con esos siete nombres y una respuesta funcional sanitizada. Los errores técnicos persisten solamente estado y tipo de excepción; el XML completo no se registra.

El cliente físico resuelve endpoint y SOAPAction desde la configuración persistida, pero aplica antes de red una política independiente por ambiente:

- `ControlledLocal`: solo `http`, `localhost`, `127.0.0.1` o `host.docker.internal`, puerto permitido y ruta `/WSAxonRespuestaTransacciones.svc`;
- `ConfiguredAllowlist`: esquemas, hosts, puertos opcionalmente restringidos y rutas exactas configuradas; `RequireHttps` puede endurecer el ambiente;
- `Unconfigured` o configuración inconsistente: rechazo fail-closed;
- credenciales embebidas, fragmentos, comodines y allowlists incompletas: rechazo antes de crear la solicitud HTTP.

No se hardcodearon destinos UAT o productivos. El endpoint persistido no sustituye la allowlist y la allowlist no selecciona el endpoint funcional. Los siete parámetros permanecen intactos y `RegistrarRespuestaTransaccion` continúa clasificado como no monetario.

## Doble carga e idempotencia

- Mismos bytes, mismo o distinto nombre: se detecta por SHA-256 y tamaño; se conserva una sola ingesta canónica. El segundo intento genera auditoría `DuplicateUploadAttempt` y no repite parser, evento funcional, transición ni despacho.
- Mismo nombre, bytes diferentes: se conserva una ingesta independiente y se registra `FileNameContentConflict`; no hay sobrescritura silenciosa.
- Un reproceso requiere `ForceReprocess` explícito y referencia al padre. Ya no existe replay automático de bytes bloqueados.
- Un SOAP ya exitoso conserva las guardas de idempotencia existentes y no se reenvía.

## Conciliación

La conciliación read-only incorpora los eventos persistidos:

- `NachaProfileSelection`: inconsistente y revisión requerida cuando no fue seleccionado; SOAP candidato `None`.
- `DuplicateUploadAttempt`: conciliado como omisión idempotente; SOAP candidato `None`.
- `FileNameContentConflict`: inconsistente y revisión requerida; SOAP candidato `None`.

Se conservan además las proyecciones existentes de respuestas, devoluciones, ROR y clasificaciones. No se marca conciliado un perfil ausente o ambiguo.

## Recuperación

No se modificó Quartz ni se creó recuperación paralela. El scheduler existente continúa usando el orquestador de postprocesamiento persistido. Como la compuerta se persiste antes del parser, un reinicio conserva la ingesta bloqueada y no crea cola ni repite SOAP. Un segundo upload idéntico se resuelve contra la ingesta canónica.

## Persistencia y compatibilidad

No se agregaron entidades, columnas ni migraciones. Se reutilizan:

- `IncomingNachaFileIngestion`
- `IncomingNachaFileProcessingResult`
- `IncomingNachaProcessingEvent`
- `AchResponseNotificationAttempt`
- cola, ejecuciones y conciliación existentes

No se modificaron snapshots de SQL Server/PostgreSQL ni golden files.

## Limitaciones abiertas

1. Confirmación contractual de la versión y vigencia aplicable de la ficha diferencial ACHCOL.
2. Vector diferencial ACHCOL oficial o referencia real verificada y trazable.
3. Manual STA con layout físico NACHA-M diferencial CENIT.
4. Vector diferencial CENIT oficial o referencia real verificada y trazable.
5. Perfil `nacha-config` diferencial homologado por cámara.
6. Parser/generador diferencial consumiendo exclusivamente el snapshot seleccionado.
7. Evidencia Live pendiente hasta resolver la compuerta normativa; `Development` ya dispone de `ControlledLocal` para el WSAXON local.
