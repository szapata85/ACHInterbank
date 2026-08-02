# FASE 1A — CIERRE DE CONTRATOS DEL MONITOREO DE TRANSACCIONES DE SALIDA

## 1. Objetivo

Cerrar las brechas de integridad identificadas en la Fase 0 antes de construir el modelo de lectura del monitor. La raíz canónica continúa siendo `AchTransactions`. Esta fase no crea la pantalla, su API, permisos, menú ni DTO final de consulta.

El alcance implementado conserva evidencia verificable para clasificación, enrutamiento monetario, cambios de estado, causales, reasignación CENIT, eventos semánticamente idempotentes, membresía de exportación y correlación de respuestas.

## 2. Decisiones tomadas

| Decisión | Motivo | Implementación |
| --- | --- | --- |
| Persistir clasificación y snapshot de origen | `FinancialInstitution.IsDefaultSource` es mutable y no conserva historia | `AchTransaction.Direction`, `Origin`, `MonetaryIntegrationRoute`, `ClassificationStatus`, `SourceInstitutionWasDefaultAtCreation`, `ClassifiedAtUtc` y `ClassificationVersion` |
| Centralizar clasificación | Evitar reglas divergentes entre creación, colas e integraciones | `IAchTransactionClassificationPolicy` y `AchTransactionClassificationPolicy` |
| Mantener históricos indeterminados | No existe evidencia suficiente para reclasificar el pasado | Backfill a `Unknown` + `ManualReview`, sin inferir desde el valor actual de la institución |
| Enrutar antes de encolar | Una cola incompatible puede desembocar en un movimiento monetario incorrecto | Clasificación durante creación y controles en persistencia/ejecución de `Proc_Contrapartidas` |
| Conservar transiciones especializadas | La bitácora existente es suficiente; una tabla universal duplicaría información | `AchTransactionStateEvents` recibe identidad semántica y causal contextual |
| Versionar membresía de archivos | El último archivo de un ciclo no prueba qué transacción fue incluida | `AchFileExportTransaction` por versión de `AchFileExport` |
| No simular transporte | El repositorio no contiene transporte o acuse real hacia la cámara | Estados máximos automáticos `Generated`/`Protected`, contrato de extensión sin adaptador simulado |
| Correlacionar solo por claves fuertes | `RecipIdNumber` no tiene contrato demostrado como identificador externo | Coincidencia exacta y única por traza o identificador externo; ambigüedad a revisión |

## 3. Definición canónica de salida

La clasificación se determina una sola vez durante la creación y luego es inmutable. `AchDbContext.SaveChangesAsync` rechaza modificaciones reales de sus propiedades históricas.

| Contexto al crear | Dirección | Origen | Ruta monetaria | Estado de clasificación |
| --- | --- | --- | --- | --- |
| Origen CFA, débito, no prenotificación | `Outgoing` | `Cfa` | `ProcContrapartidas` | `Determined` |
| Origen CFA, prenotificación | `Outgoing` | `Cfa` | `None` | `Determined` |
| Origen externo, destino CFA, crédito | `Incoming` | `ExternalInstitution` | `ProcTransacciones` | `Determined` |
| Crédito originado por CFA | `Outgoing` | `Cfa` | `ManualReview` | `Invalid` |
| Origen/destino no determinable o varias CFA activas | `Unknown` | `Unknown` | `ManualReview` | `Ambiguous` |

`IsDefaultSource` participa únicamente como evidencia de contexto al crear. Cambiarlo después no recalcula dirección, origen ni ruta. El snapshot mínimo evita duplicar datos completos de la institución.

Los registros anteriores a la migración quedan con versión `0`, dirección/origen desconocidos, ruta de revisión manual y sin fecha de clasificación. No se ejecutan automáticamente contra una integración monetaria.

## 4. Matriz de elegibilidad de integraciones

| Clasificación canónica | Tipo | Prenotificación | Integración | Mueve dinero | Resultado |
| --- | --- | --- | --- | --- | --- |
| CFA / salida / determinada | Débito | No | `Proc_Contrapartidas` | Sí, débito | Elegible |
| Externa / entrada / determinada | Crédito | No | `Proc_Transacciones` | Sí, crédito | Elegible |
| CFA / salida / determinada | Cualquiera permitida | Sí | Ninguna monetaria | No | Elegible sin cola monetaria |
| Respuesta diferencial | No aplica | No aplica | `RegistrarRespuestaTransaccion` | No | Notificación no monetaria |
| Desconocida, ambigua o inválida | Cualquiera | Cualquiera | Revisión manual | No | Rechazo controlado antes de encolar |

`ContrapartidaDispatchPersistenceService` y `ContrapartidaDispatchJobService` comprueban la clasificación canónica. La cola mantiene un único ítem activo por transacción y ciclo; un resultado definitivo no se vuelve a despachar. Los reintentos técnicos existentes permanecen disponibles sin crear un segundo movimiento.

El formulario actual inicia en débito y limita las opciones visibles a la combinación soportada para creación CFA. Los mensajes son operativos, en español y usan el formulario reactivo de Angular Material. No se creó ningún componente del monitor.

## 5. Cambios de estados

`AchStateTransitionService` permite las transiciones `AppliedTacitly -> ReturnedByOperator/ReturnedByEpr` y `Certified -> ReturnedByOperator/ReturnedByEpr`. Cada aplicación crea un `AchTransactionStateEvent`; la aceptación previa permanece en la secuencia.

| Dimensión futura | Fuente persistida | Regla preliminar |
| --- | --- | --- |
| Proceso | transacción, ciclo, despacho y exportación | Derivar del último hecho comprobable, no del nombre de una etapa futura |
| Resultado inicial | eventos de aceptación, certificación o rechazo | Conservar el evento aceptado aunque el estado raíz termine en devolución |
| Situación posterior | eventos de devolución/novedad | Si existe devolución posterior válida, mostrar “Procesada y posteriormente devuelta” |

La causal de la devolución se conserva en el nuevo evento mediante cámara, `AchReturnCodeId`, código, descripción resuelta, traza original, fecha del hecho e identidad idempotente. Un error técnico no produce una devolución funcional.

## 6. Códigos y causales

La resolución de `AchReturnCodes` ordena por coincidencia contextual de cámara, flujo, naturaleza, vigencia y aplicabilidad. Un código no encontrado o ambiguo queda sin resolver y no se transforma en resultado por prefijo.

`R96` dejó de ser un éxito global del catálogo de devoluciones. El registro legado genérico se desactiva como `NotProcessed`; su semántica exitosa permanece únicamente en el catálogo de respuestas de integración y por método configurado. Una cámara puede definir otro `R96` explícito con significado diferente.

`AchFileRejectionCodes` incorpora cámara, vigencia y fuente normativa. Los `Dxx` CENIT se separan de códigos homónimos de ACH Colombia. El seed actualiza datos existentes de forma idempotente.

Fuente local usada: `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md`, Circular DSP-152, Anexo B, fecha indicada en el documento 28-11-2023. De allí se homologaron:

| Código CENIT | Causal homologada |
| --- | --- |
| `D01` | Archivo enviado al operador receptor incorrecto |
| `D02` | Archivo firmado o cifrado para receptor/usuarios no válidos |
| `D03` | Formato incorrecto o archivo no procesable |
| `D04` | Archivo o información duplicada |
| `D05` | Conteo del nombre externo diferente del contenido |
| `D06` | Regla de distribución no correspondiente al receptor |

No se reutilizan estas descripciones para ACH Colombia, cuya documentación local contiene códigos homónimos con otra semántica.

## 7. Idempotencia

`AchIncomingEventIdentityPolicy` construye una identidad SHA-256 determinística a partir de datos estables demostrados: tipo de evento, cámara, transacción original, traza original, código y fecha efectiva/recepción aplicable. No incluye el nombre ni el hash binario del archivo, por lo que el mismo evento funcional en archivos distintos no vuelve a producir efectos.

`AchTransactionStateEvents.IdempotencyKey` tiene índice único filtrado. La política distingue:

- repetición idéntica: resultado idempotente sin nuevo evento ni despacho;
- misma clave con contenido incompatible: colisión explícita y revisión, no aplicación arbitraria;
- correlación sin candidato: huérfano;
- más de un candidato fuerte: revisión manual;
- eventos distintos con igual código: identidades distintas cuando cambia la transacción, traza o fecha funcional.

La protección de base de datos complementa la comprobación previa ante concurrencia. Los dos caminos existentes de devoluciones usan la misma política de transición e identidad, eliminando su divergencia funcional; el retiro físico del adaptador legado no fue necesario para esta fase.

## 8. Ciclo, lote y despacho

La optimización de liquidez CENIT ejecuta dentro de una transacción relacional con aislamiento serializable. Una reasignación actualiza como conjunto:

1. ciclo de la transacción;
2. lote asociado al ciclo destino;
3. contexto vigente del ítem `Proc_Contrapartidas`;
4. cola CENIT;
5. decisión de optimización;
6. evento de estado con evidencia del cambio.

La decisión se comprueba dentro de la misma transacción. Hay unicidad para decisión por ejecución/transacción y para cola activa por transacción/ciclo destino. Una falla intermedia revierte el conjunto; la repetición no crea una segunda decisión. La prueba relacional con SQLite utiliza un trigger para demostrar rollback y las migraciones reales comprobaron restricciones equivalentes en SQL Server y PostgreSQL.

La referencia del archivo fuente para una optimización ya no se toma del último archivo del ciclo: debe provenir de la membresía explícita de la transacción.

## 9. Membresía transacción–archivo

`AchFileExportTransaction` persiste por cada versión:

- archivo exportado;
- transacción;
- ciclo y lote al incluir;
- posición en el archivo;
- traza;
- valor `decimal(18,2)`;
- fecha de inclusión.

`NachaFileBuilder` entrega `NachaFileBuildArtifact` con el contenido y los identificadores exactos seleccionados. El controlador calcula SHA-256 del contenido normalizado y `AchFileExportAuditService` crea atómicamente el archivo y su membresía.

Las restricciones impiden repetir transacción o posición dentro de la misma versión. `AchFileExport` se versiona por ciclo, tipo y condición de protección; el mismo nombre con contenido o membresía diferentes se rechaza para revisión. Dos archivos de un ciclo conservan membresías independientes.

Los archivos históricos se marcan `HistoricalUnknown` y sin versión; no se realiza backfill por coincidencia de ciclo.

## 10. Evidencia real de transmisión

No se encontró en el repositorio un MFT, callback o adaptador que demuestre transmisión y acuse de la cámara. La descarga HTTP tampoco constituye envío.

La generación registra `Generated`; una salida cifrada registra `Protected`. `AchDbContext` impide persistir `Transmitted` sin referencia externa y fecha, e impide `Acknowledged`, `Accepted` o `Rejected` sin evidencia adicional de acuse.

`IAchFileTransmissionEvidenceRecorder` define el punto mínimo de extensión para un transporte real. No tiene implementación simulada ni se invoca desde la generación. Los campos `TransmissionReference`, `TransmittedAtUtc`, `AcknowledgedAtUtc` y `AcknowledgementCode` quedan reservados para evidencia aportada por un adaptador real futuro.

Esta ausencia externa justifica el veredicto “cerrada con limitaciones externas”: el modelo es veraz y no fabrica el hecho de envío.

## 11. Respuestas diferenciales

`AchResponse` puede relacionarse explícitamente con `AchTransaction` y conserva estado/criterio de correlación. `AchResponseTransactionCorrelationService` admite coincidencia exacta y única por traza o `TransactionExternalId`.

Se eliminó la asignación de `RecipIdNumber` como identificador externo no demostrado. Sin candidato se conserva la respuesta sin atribución; con varios candidatos queda ambigua/revisión. La prenotificación CFA conserva su asociación directa cuando el vínculo ya está demostrado.

`DifferentialPrenotificationResponseProcessor` usa la política central de transición y no escribe estados directamente. `RegistrarRespuestaTransaccion` permanece no monetaria. La repetición se identifica antes de notificar y no crea movimientos monetarios.

## 12. Cambios de datos

| Tabla | Cambio |
| --- | --- |
| `AchTransactions` | clasificación canónica, snapshot mínimo y versión; índices de dirección/clasificación, ruta/estado, traza e identificador externo |
| `AchTransactionStateEvents` | fecha efectiva obligatoria, identidad semántica, cámara, FK opcional al catálogo y causal resuelta |
| `AchFileExports` | versión, hash, ciclo de vida y campos opcionales de evidencia externa |
| `AchFileExportTransactions` | nueva membresía exacta y snapshots mínimos de inclusión |
| `AchResponses` | FK opcional a transacción y resultado de correlación |
| `AchFileRejectionCodes` | cámara, vigencia y fuente regulatoria contextual |
| `LiquidityOptimizationDecisions` / `CenitCycleQueues` | restricciones de unicidad del proceso |

No cambió la precisión monetaria ni se introdujo moneda por inferencia.

## 13. Migraciones

| Motor | Migración |
| --- | --- |
| PostgreSQL | `20260802180238_OutgoingTransactionTraceabilityPhase1A` |
| SQL Server | `20260802180336_OutgoingTransactionTraceabilityPhase1A` |

Ambas migraciones incluyen precondiciones que abortan antes de crear unicidad si existen decisiones, colas activas, nombres de exportación o códigos contextuales duplicados. También abortan si una traza histórica excede 20 caracteres; no truncan datos.

Validación realizada en bases desechables reales:

1. creación desde cero hasta la migración inmediatamente anterior;
2. aplicación de Fase 1A;
3. reversión a la migración anterior;
4. reaplicación de Fase 1A;
5. eliminación de las bases desechables.

El procedimiento fue verde en PostgreSQL 16 y SQL Server local. La advertencia de herramienta EF `10.0.7` frente a runtime `10.0.8` no afectó la ejecución.

## 14. Estrategia de backfill

- Clasificación histórica: `Unknown`/`ManualReview`, versión `0`, sin snapshot ni fecha de clasificación. Un proceso futuro solo podrá reclasificar con evidencia externa inequívoca y caso de uso auditado.
- Membresía histórica: no se reconstruye. `HistoricalUnknown` evita atribuir el último archivo del ciclo.
- Eventos previos: `OccurredAtUtc` se completa desde `CreatedAt`; no se inventan códigos ni cámaras faltantes.
- Códigos: se asignan a CENIT únicamente cuando el código de cámara existe; D01–D06 se actualizan idempotentemente desde la fuente normativa local.
- `R96` genérico legado: queda inactivo como resultado NACHA; no se borra.
- Duplicados incompatibles con nuevos índices: la migración falla cerrada y exige depuración explícita, sin seleccionar ganadores silenciosamente.

La reversión elimina las nuevas relaciones/columnas y restaura las descripciones anteriores del seed y el `R96` legado identificado por la marca de migración. En una reversión se pierde, como es normal, la evidencia creada exclusivamente en las estructuras nuevas; por eso debe respaldarse antes de ejecutar `Down` en un ambiente persistente.

## 15. Pruebas

Cobertura focalizada implementada:

- matriz de clasificación, inmutabilidad, histórico ambiguo y cambio posterior de `IsDefaultSource`;
- elegibilidad de `Proc_Contrapartidas`, rechazo previo y ausencia de doble cola;
- aceptación/certificación seguida de devolución con historia y causal;
- `R96` contextual, D01–D06 CENIT y seed idempotente;
- devolución repetida en un archivo y en archivos binariamente distintos;
- correlación única, inexistente y ambigua;
- reasignación CENIT coherente, repetición y rollback relacional;
- versiones y membresías exactas de exportación;
- prohibición de transmisión sin evidencia;
- respuestas diferenciales y prenotificaciones sin movimiento monetario;
- migración, rollback y reaplicación en SQL Server y PostgreSQL;
- formulario Angular de creación.

| Comando o grupo | Resultado |
| --- | --- |
| Focalizadas de clasificación, catálogos, devoluciones, correlación, liquidez y archivos | 104 aprobadas, 0 fallidas |
| Focalizadas finales de clasificación/catálogos | 19 aprobadas, 0 fallidas |
| `ContrapartidaDispatchJobServiceTests` + `IncomingNachaTransactionLinkerTests` | 16 aprobadas, 0 fallidas |
| `OpenApiDocumentGenerationTests` | 2 aprobadas, 0 fallidas |
| `ClearingHouseMultiDbTests` con SQL Server y PostgreSQL reales | 2 aprobadas, 0 fallidas |
| Formulario Angular de creación | 28 aprobadas, 0 fallidas |
| `npm run build` | Aprobado |
| `dotnet build ACHInterbank.sln -c Release` | Aprobado, 0 advertencias, 0 errores |
| Regresión backend integral | 2.092 aprobadas, 0 fallidas, 7 omitidas, 2.099 totales; 22 min 43 s |

Las siete omisiones corresponden a diagnóstico SOAP y pruebas históricas opt-in de migraciones financieras. La migración Fase 1A y el rollback/reaplicación se ejecutaron expresamente en ambos motores, y las pruebas `ClearingHouseMultiDbTests` también quedaron verdes con proveedores reales.

No se ejecutó SOAP Live, transporte externo ni operación monetaria.

## 16. Riesgos residuales

| Riesgo | Estado | Tratamiento |
| --- | --- | --- |
| Transporte y acuse reales no existen en este repositorio | Externo | Integrar posteriormente un adaptador que implemente `IAchFileTransmissionEvidenceRecorder` con evidencia verificable |
| Moneda no está en la raíz | Decisión contractual no demostrada | Mantener `MON-SAL-023`; confirmar moneda única o modelarla antes de exponer importes multimoneda |
| Históricos sin clasificación o membresía inequívoca | Limitación de evidencia | Mostrar “No determinado” y no ejecutar movimientos automáticos |
| Política final de tres dimensiones para el DTO | Trabajo de Fase 1B | Proyectar desde las fuentes ahora auditables, sin volver a escribir estados |
| Retiro físico del camino legado de devoluciones | Deuda no funcional | Mantener mientras comparte políticas y pruebas; retirar en cambio separado con caracterización completa |

## 17. Brechas MON-SAL cerradas

| Brecha | Estado | Evidencia de cierre |
| --- | --- | --- |
| `MON-SAL-001` | Cerrada | Clasificación persistida e inmutable en `AchTransaction` |
| `MON-SAL-002` | Cerrada | Snapshot de rol al crear; múltiples/default ausente quedan ambiguos |
| `MON-SAL-003` | Cerrada | Política de ruta y validación antes de cola `Proc_Contrapartidas` |
| `MON-SAL-004` | Cerrada | Formulario CFA inicia en débito y comunica elegibilidad en español |
| `MON-SAL-005` | Cerrada | `AchFileExportTransaction` y versiones exactas |
| `MON-SAL-006` | Cerrada en veracidad; dependencia externa | Generado/protegido separados; transmisión exige evidencia; no existe transporte real |
| `MON-SAL-007` | Cerrada | Aceptada/certificada puede pasar a devolución conservando eventos |
| `MON-SAL-008` | Cerrada | Resolución por cámara/flujo/vigencia; sin clasificación por prefijo |
| `MON-SAL-009` | Cerrada para catálogos locales demostrados | Seed contextual e idempotente por cámara |
| `MON-SAL-010` | Cerrada | D01–D06 CENIT homologados con Anexo B local |
| `MON-SAL-011` | Cerrada | Reasignación ciclo–lote–cola–despacho atómica y con rollback |
| `MON-SAL-012` | Cerrada | Eliminado `RecipIdNumber` como clave no demostrada |
| `MON-SAL-013` | Cerrada en contrato de correlación | Índices no únicos y resolución explícita de ambigüedad, sin falsa unicidad histórica |
| `MON-SAL-014` | Cerrada | Identidad funcional persistida, única y compartida |
| `MON-SAL-016` | Cerrada en datos; proyección pendiente | Historia permite separar proceso, resultado inicial y situación posterior en Fase 1B |
| `MON-SAL-024` | Cerrada | FK opcional y estado/criterio de correlación en `AchResponse` |
| `MON-SAL-025` | Cerrada funcionalmente | Ambos caminos usan la misma transición e identidad; retiro físico diferido |
| `MON-SAL-026` | Cerrada | Pruebas focalizadas, relacionales y multi-motor incorporadas |

## 18. Brechas pendientes justificadas

| Brecha | Motivo de permanencia | Condición de cierre |
| --- | --- | --- |
| `MON-SAL-006` — transporte efectivo | El repositorio no contiene integración real hacia cámara; inventarla violaría la trazabilidad | Adaptador real, identificadores externos y acuse comprobable |
| `MON-SAL-023` — moneda | Código, normativa revisada y Fase 0 no demuestran moneda única ni catálogo de moneda en la raíz | Decisión de dominio respaldada o nueva propiedad con migración explícita |

No quedan combinaciones ambiguas autorizadas para movimiento automático: los históricos o correlaciones no demostrables quedan en revisión.

## 19. Preparación para Fase 1B

Es seguro iniciar el modelo de lectura desde `AchTransactions` con estas condiciones:

1. proyectar una fila por transacción sin `Include` de colecciones;
2. separar consulta paginada y detalle;
3. calcular proceso, resultado inicial y situación posterior mediante una policy de lectura determinística;
4. mostrar “No determinado” en históricos sin evidencia;
5. usar `AchFileExportTransactions` para archivos, nunca el último archivo del ciclo;
6. no mostrar “Enviada” sin `TransmissionReference` y `TransmittedAtUtc` válidos;
7. cargar payloads técnicos solo bajo permiso y demanda;
8. mantener las correlaciones ambiguas/huérfanas fuera de atribuciones automáticas;
9. diseñar permisos backend antes de exponer el endpoint;
10. añadir pruebas de proyección equivalentes en SQL Server y PostgreSQL.

La Fase 1B puede crear consultas, DTO, endpoints paginados y autorización del monitor. La pantalla continúa fuera de este cambio.
