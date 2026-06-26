# Rediseno funcional UX de mappings SOAP

Fecha: 2026-06-26  
Estado: diagnostico y plan UX, sin implementacion  
Productivo: NO-GO

## 1. Resumen ejecutivo

La pantalla `/integraciones/mappings` ya resolvio una brecha importante: las reglas activas que usan fuentes internas validas (`Transaction`, `Cycle`, `ClearingHouse`, `Batch`, `Constant`, `DifferentialResponse`) ya no se muestran como `Sin mapear`.

La nueva brecha es de lenguaje funcional y jerarquia visual. El estado `Mapeado tecnico` evita el falso negativo, pero comunica a usuarios funcionales que el origen es generico o tecnico, aunque en realidad muchos campos vienen de fuentes operativas reales como transaccion, ciclo, camara, lote, entidad financiera o respuesta diferencial.

El rediseño recomendado no cambia mappings, seeds, readiness, WSDL ni logica monetaria. Debe reorganizar la UI para que el usuario pueda responder rapido:

- Que servicio estoy revisando.
- Que version publicada activa estoy viendo.
- Que parametros estan mapeados desde fuentes reales.
- Que parametros usan constantes.
- Que parametros son placeholders o pendientes funcionales.
- Que parametros son opcionales/reservados.
- Que brechas bloquean readiness.
- Que debe revisarse primero.

La ruta de detalle `/integraciones/mappings/{serviceKey}/{mappingSetId}` actualmente funciona como editor avanzado. Mezcla lectura, edicion, validacion, preview, publicacion, clonacion e historial. La recomendacion es separarla en una vista de revision primero, con edicion como modo explicito y auditoria colapsada por defecto.

## 2. Problema observado

Problemas actuales:

1. `Mapeado tecnico` agrupa fuentes muy distintas:
   - fuentes transaccionales reales.
   - ciclo/camara.
   - lote.
   - entidades financieras.
   - constantes.
2. Para negocio, `Transaction.amount` o `Cycle.processingDate` no son "tecnicos"; son origenes funcionales.
3. La pantalla principal no muestra warnings, bloqueantes o placeholders como conceptos de primer nivel.
4. La pantalla de detalle/editor esta sobrecargada:
   - columna izquierda con todos los parametros.
   - panel central de formulario.
   - panel derecho de validacion, preview, publish, clone, compare e historial.
   - mucha informacion tecnica visible antes de explicar estado funcional.
5. `Proc_Contrapartidas` necesita una lectura clara de origen por parametro, especialmente por riesgo monetario y trazabilidad:
   - monto.
   - cuenta/DFI.
   - ciclo/camara.
   - estado/reverso.
   - campos libres.
   - ANS opcionales/reservados.

## 3. Causa funcional de confusion

La causa no es el contrato WSDL ni el seed en esta fase. La causa UX esta en la abstraccion actual:

- `MappingSetsPageComponent` clasifica fuentes NACHA y `DifferentialResponse` como `Mapeado`.
- Fuentes como `Transaction`, `Cycle`, `ClearingHouse`, `Batch`, `FinancialInstitution`, `Prenotification` y `Constant` caen en `Mapeado tecnico`.
- La etiqueta se basa en una distincion interna de implementacion, no en una distincion funcional para usuario final.
- La UI distingue "hay fuente valida" vs "no hay fuente", pero no distingue con claridad:
  - fuente funcional real.
  - constante homologada.
  - constante tecnica.
  - placeholder.
  - pendiente funcional.
  - opcional/reservado.
  - bloqueo readiness.

Resultado: el usuario ve que algo esta "mapeado", pero no sabe si esta listo, si es una fuente real, si es un fallback peligroso o si requiere decision funcional.

## 4. Estado actual de /integraciones/mappings

La pantalla principal actual tiene:

- Encabezado `Matriz de campos SOAP`.
- Selector de servicios.
- Tarjeta de servicio seleccionado.
- Conteos:
  - version de trabajo.
  - mapeado.
  - sin mapear.
  - inactivo.
- Mensaje de origen permitido.
- Tabla:
  - Servicio SOAP.
  - Parametro SOAP.
  - Tabla origen.
  - Campo origen.
  - Regla de conversion.
  - Obligatorio.
  - Estado.
  - Acciones.
- Modales:
  - detalle.
  - edicion.
  - crear borrador.
  - auditoria.

Estado visual actual:

| Estado actual | Uso actual | Problema |
|---|---|---|
| `Mapeado` | NACHA y `DifferentialResponse` | Correcto parcialmente, pero no distingue tipo de fuente. |
| `Mapeado tecnico` | `Transaction`, `Cycle`, `ClearingHouse`, `Batch`, `Constant`, etc. | Nombre confuso para fuentes funcionales reales. |
| `Opcional/reservado` | ANS de `Proc_Contrapartidas` sin regla activa. | Correcto y debe mantenerse. |
| `Sin mapear` | Sin regla activa ni fuente valida. | Correcto si no se usa para fuentes reales. |
| `Inactivo` | MappingSet archivado/inactivo o regla deshabilitada. | Correcto. |

Limitaciones actuales:

- No hay filtros por estado funcional.
- No hay filtro de bloqueantes, warnings o placeholders.
- No hay separacion visual entre parametros funcionales y opcionales/reservados.
- Los conteos no reflejan readiness funcional:
  - no muestran placeholders.
  - no muestran warnings.
  - no muestran bloqueantes.
- La columna `Regla de conversion` usa `Valor por defecto` para defaults que pueden ser funcionalmente peligrosos.
- El boton de auditoria existe y tiene funcion real, pero compite visualmente con acciones principales.

## 5. Estado actual de la pantalla detalle

Ruta analizada:

`/integraciones/mappings/WSCFAACH.Proc_Contrapartidas/d15864ec-62ca-44d0-aeb3-cb5ce0f23ee1`

Componente actual:

- `MappingEditorPageComponent`

Responsabilidades actuales en una sola pantalla:

- Cargar mapping set, parametros, source catalog, transformaciones e historial.
- Mostrar conteos de cobertura.
- Seleccionar parametro destino.
- Editar origen, valor fijo, default, transformacion, mascara, prioridad, required override y enabled.
- Ejecutar validacion.
- Ejecutar preview.
- Publicar.
- Clonar.
- Comparar.
- Mostrar historial.

Problemas UX:

- La pantalla se presenta como editor antes que como revision funcional.
- El usuario debe elegir un parametro para entender la regla, en lugar de ver primero la matriz completa.
- La validacion no aparece como panel de brechas accionable por parametro.
- Preview e historial ocupan espacio principal aunque son tareas secundarias.
- La edicion esta siempre visible, incluso si el usuario solo esta revisando un mapping publicado.
- Hay duplicidad conceptual con la pantalla principal: ambas explican parametros y reglas, pero la de detalle no prioriza lectura por servicio.
- Las fuentes transaccionales no se narran con lenguaje de negocio consistente.
- No separa parametros opcionales/reservados para no contaminar lectura de campos requeridos.

## 6. Nueva clasificacion visual propuesta

Reemplazar `Mapeado tecnico` por categorias funcionales. La UI puede mantener un estado interno normalizado, pero el label visible debe ser comprensible para usuario funcional.

| Categoria visible | Fuentes / condicion | Semantica UX | Bloquea readiness |
|---|---|---|---|
| `Mapeado NACHA` | `NachaHeader`, `BatchHeader`, `EntryDetail`, `AddendaRecord`, `BatchControl`, `FileControl` | El parametro SOAP se resuelve desde el archivo/lote/detalle NACHA. | No, si la fuente y regla son validas. |
| `Mapeado transaccional` | `Transaction`, `Batch`, `Prenotification`, relaciones internas de transaccion | El parametro sale del modelo operativo ACH interno. | No, si no depende de placeholder bloqueante. |
| `Mapeado por ciclo/camara` | `Cycle`, `ClearingHouse`, `FinancialInstitution` | El parametro sale de ciclo, camara o entidad financiera. | No, salvo default generico critico. |
| `Mapeado desde respuesta diferencial` | `DifferentialResponse` | El parametro sale de respuesta/rechazo/notificacion diferencial. | No, para Registrar si conserva 7 WSDL. |
| `Constante homologada` | `Constant` con politica funcional aprobada | Valor fijo aprobado por negocio/proveedor. | No. |
| `Constante tecnica` | `Constant` de auditoria/infraestructura documentada | Valor fijo tecnico no funcional, visible con warning si aplica. | Normalmente no, pero debe advertir. |
| `Placeholder / pendiente funcional` | `SEED`, `TEST`, `1`, `0`, `REF-1`, `ACH`, `000010070`, `900123456`, `constant.value` sin politica | Valor de seed/demo o default no homologado. | Si afecta parametro funcional requerido. |
| `Opcional / reservado` | ANS de `Proc_Contrapartidas` y campos contractuales no bloqueantes aprobados como reservados | Parametro existe por contrato pero no requiere fuente activa. | No. |
| `Sin mapear` | Sin regla activa, sin constante valida y sin clasificacion reservada | Requiere definicion o regla. | Si el parametro es requerido funcionalmente. |
| `Inactivo` | Parametro/regla/mapping set inactivo o archivado | No participa en version visible. | Depende de required funcional. |

Regla importante:

No todo `Constant` es malo y no toda constante es homologada. La UI debe distinguir constante aprobada vs constante tecnica vs placeholder pendiente.

## 7. Labels funcionales propuestos por fuente

### NACHA

| sourceKind | sourceFieldPath | Label tabla | Label campo |
|---|---|---|---|
| `NachaHeader` | `nachaHeaders.immediateDestination` | Archivo NACHA | Banco destino inmediato |
| `NachaHeader` | `nachaHeaders.immediateOrigin` | Archivo NACHA | Banco origen inmediato |
| `NachaHeader` | `nachaHeaders.fileIdModifier` | Archivo NACHA | Modificador de archivo |
| `BatchHeader` | `batchHeaders.companyName` | Lote NACHA | Nombre compania |
| `BatchHeader` | `batchHeaders.companyId` | Lote NACHA | Identificacion compania |
| `BatchHeader` | `batchHeaders.companyEntryDescription` | Lote NACHA | Descripcion de entrada |
| `BatchHeader` | `batchHeaders.effectiveEntryDate` | Lote NACHA | Fecha efectiva |
| `EntryDetail` | `entryDetails.amount` | Detalle NACHA | Monto |
| `EntryDetail` | `entryDetails.accountNumber` | Detalle NACHA | Cuenta receptora |
| `EntryDetail` | `entryDetails.traceNumber` | Detalle NACHA | Trazabilidad |
| `EntryDetail` | `entryDetails.transactionCode` | Detalle NACHA | Codigo de transaccion |
| `AddendaRecord` | `addendaRecords.infofromOriginator` | Addenda NACHA | Informacion de pago |
| `BatchControl` | `batchControls.entryAddendaCount` | Control de lote | Conteo de entradas/addendas |
| `FileControl` | `fileControls.blockCount` | Control de archivo | Conteo de bloques |

### Transaccional

| sourceKind | sourceFieldPath | Label tabla | Label campo |
|---|---|---|---|
| `Transaction` | `transaction.amount` | Transaccion | Monto |
| `Transaction` | `transaction.reference` | Transaccion | Referencia |
| `Transaction` | `transaction.originatingdfi` | Transaccion | Banco originador |
| `Transaction` | `transaction.sourceaccountnumber` | Transaccion | Cuenta origen |
| `Transaction` | `transaction.companyidentification` | Transaccion | NIT/Id empresa origen |
| `Transaction` | `transaction.id` | Transaccion | Id interno de transaccion |
| `Batch` | `batch.id` | Lote operativo | Id lote |
| `Prenotification` | `prenotification.reference` | Prenotificacion | Referencia |
| `Prenotification` | `prenotification.state` | Prenotificacion | Estado |

### Ciclo, camara y entidad financiera

| sourceKind | sourceFieldPath | Label tabla | Label campo |
|---|---|---|---|
| `Cycle` | `cycle.processingdate` | Ciclo | Fecha de proceso |
| `Cycle` | `cycle.id` | Ciclo | Id ciclo |
| `ClearingHouse` | `clearinghouse.id` | Camara | Identificador interno |
| `ClearingHouse` | `clearinghouse.code` | Camara | Codigo |
| `FinancialInstitution` | `financialinstitution.routingnumber` | Entidad financiera | Codigo ruta |
| `FinancialInstitution` | `financialinstitution.transitnumber` | Entidad financiera | Transito |

### Respuesta diferencial

| sourceKind | sourceFieldPath | Label tabla | Label campo |
|---|---|---|---|
| `DifferentialResponse` | `differentialResponse.idCanal` | Respuesta diferencial | Id canal |
| `DifferentialResponse` | `differentialResponse.nombreCanal` | Respuesta diferencial | Nombre canal |
| `DifferentialResponse` | `differentialResponse.idTransaccion` | Respuesta diferencial | Id transaccion |
| `DifferentialResponse` | `differentialResponse.idEstado` | Respuesta diferencial | Estado |
| `DifferentialResponse` | `differentialResponse.codigoCausalExterna` | Respuesta diferencial | Causal externa |
| `DifferentialResponse` | `differentialResponse.idTransaccionServicioExterno` | Respuesta diferencial | Id transaccion Axon |
| `DifferentialResponse` | `differentialResponse.descripcionCausalExterna` | Respuesta diferencial | Descripcion causal |

### Constantes

| Caso | Label tabla | Label campo | Observacion |
|---|---|---|---|
| Constante homologada | Constante homologada | Valor aprobado | Solo si existe evidencia funcional. |
| Constante tecnica | Constante tecnica | Valor tecnico controlado | Mostrar warning si no impacta payload funcional critico. |
| Placeholder | Pendiente funcional | Placeholder no homologado | No llamarlo constante valida. |

## 8. Rediseño propuesto de pantalla principal

Objetivo de la pantalla principal:

Dar una lectura rapida de estado y permitir navegar a lo que requiere atencion, sin convertir la vista en un editor avanzado.

### 8.1 Encabezado

Contenido recomendado:

- Titulo: `Matriz funcional SOAP`.
- Subtitulo: `Relacion entre parametros SOAP y origenes internos controlados`.
- Acciones:
  - `Crear borrador` solo para administradores.
  - `Historial` como accion secundaria.
  - `Comparar versiones` como accion secundaria si aplica.

No mostrar rutas tecnicas ni JSON en el encabezado.

### 8.2 Selector de servicio

Mantener los tres servicios:

- `Proc_Transacciones`
- `Proc_Contrapartidas`
- `RegistrarRespuestaTransaccion`

No mostrar `PLValidarUsuarioBV`.

Cada opcion debe mostrar:

- Nombre de operacion.
- Naturaleza:
  - credito monetario.
  - debito monetario.
  - respuesta diferencial no monetaria.
- Badge `Mueve dinero` o `No monetario`.

### 8.3 Resumen compacto

Reemplazar conteos actuales por un resumen de mas valor:

| Indicador | Significado |
|---|---|
| Version publicada | MappingSet publicado activo visible por defecto. |
| Parametros | Conteo total de parametros activos. |
| Mapeados | Mapeados desde fuente real o constante aprobada. |
| Pendientes | Sin mapear o pendiente funcional. |
| Warnings | Constantes tecnicas/defaults no bloqueantes. |
| Bloqueantes | Placeholders en campos funcionalmente criticos o faltantes requeridos. |
| Reservados | Opcionales/reservados, por ejemplo ANS de `Proc_Contrapartidas`. |
| Borradores | Cantidad de borradores existentes, sin desplazar el publicado. |

Si no existe endpoint de readiness por servicio, la primera fase puede calcular:

- mapeados.
- sin mapear.
- inactivos.
- reservados.
- placeholders detectables localmente por valor.

Y dejar readiness consolidado para una fase posterior.

### 8.4 Filtros

Filtros recomendados:

- Todos.
- Solo bloqueantes.
- Solo warnings.
- Solo pendientes funcionales.
- Solo placeholders.
- Solo reservados/opcionales.
- Solo mapeados NACHA.
- Solo transaccionales.
- Solo ciclo/camara.
- Solo constantes.

Para `Proc_Contrapartidas`, incluir filtro rapido:

- `Campos monetarios`.
- `Campos de trazabilidad`.
- `ANS reservados`.

### 8.5 Columnas recomendadas

Tabla principal:

| Columna | Contenido |
|---|---|
| Parametro SOAP | Nombre, descripcion corta y required. |
| Estado funcional | Categoria visual nueva. |
| Origen funcional | NACHA, Transaccion, Ciclo/Camara, Respuesta diferencial, Constante, Reservado. |
| Tabla / entidad | Label funcional, no necesariamente tabla fisica. |
| Campo / relacion | Campo funcional legible. |
| Regla | Sin conversion, fecha, numerica, constante, default, pendiente. |
| Observacion | Brecha, warning o razon de reservado. |
| Accion | Ver detalle; Editar solo si corresponde. |

Retirar `Servicio SOAP` como columna repetida dentro de la tabla cuando la pantalla ya tiene un servicio seleccionado. Mantenerla solo si se habilita vista multisevicio.

### 8.6 Acciones

Acciones principales:

- `Ver detalle` por fila.
- `Editar` solo para administradores y sobre borrador.

Acciones secundarias:

- `Historial de cambios`.
- `Editor avanzado`.
- `Comparar versiones`.

No poner `Publicar`, `Preview` o `Clonar` en la pantalla principal.

## 9. Rediseño propuesto de pantalla detalle

La ruta `/integraciones/mappings/{serviceKey}/{mappingSetId}` debe funcionar como revision de version antes que editor permanente.

### 9.1 Encabezado

Campos:

- Servicio.
- Operacion.
- MappingSet.
- Version.
- Estado: Published/Draft/Archived.
- Readiness: `OK`, `Ready with warnings`, `Placeholder`, `Not ready`, `No evaluado`.
- Fecha de publicacion.
- Publicado por.

Acciones:

- `Volver a matriz`.
- `Crear borrador desde esta version`.
- `Editar borrador` si es Draft.
- `Historial`.
- `Comparar`.

### 9.2 Panel de brechas

Debe aparecer antes de la matriz:

- Bloqueantes.
- Warnings.
- Pendientes funcionales.
- Parametros reservados.

Cada item:

- Parametro.
- Motivo.
- Evidencia.
- Accion recomendada.

Ejemplo para `Proc_Contrapartidas`:

| Parametro | Tipo | Motivo | Accion |
|---|---|---|---|
| `OFMONDEB` | Bloqueante | Constante `0` en campo monetario critico. | Requiere decision funcional; no decidir en UI. |
| `OFMONCRE` | Warning/Bloqueante segun readiness | Fuente `Transaction.amount` con default `0`. | Revisar definicion OFMONDEB/OFMONCRE. |
| `OFDD` | Bloqueante | Constante `C` sin politica aprobada. | Requiere proveedor/negocio. |
| `ANSIDLOTE` | Reservado | Campo ANS opcional de contrato. | No requiere mapping request. |

### 9.3 Matriz campo a campo

Columnas recomendadas:

| Columna | Contenido |
|---|---|
| Parametro SOAP | Nombre y descripcion. |
| Requerido | WSDL/funcional si esta disponible. |
| Estado funcional | Nueva clasificacion visual. |
| Origen funcional | NACHA, Transaccion, Ciclo/Camara, Respuesta diferencial, Constante, Reservado. |
| Tabla / entidad | Label funcional. |
| Campo / relacion | Label funcional. |
| Regla | Conversion/default/constante. |
| Observacion | Warning, bloqueo o aclaracion. |
| Accion | Ver regla, editar en borrador. |

La matriz debe ser el centro de la pantalla de detalle. El formulario de edicion no debe dominar la vista de lectura.

### 9.4 Parametros opcionales/reservados

Separar visualmente:

- `ANSIDLOTE`
- `ANSST`
- `ANCLC`
- `ANSIDTX`
- `ANSIDREVER`

Solo en `Proc_Contrapartidas`.

Texto recomendado:

`Campos contractuales opcionales/reservados de Proc_Contrapartidas. No bloquean readiness mientras no exista definicion funcional que exija mapearlos.`

### 9.5 Trazabilidad/auditoria

Colapsada por defecto:

- historial de MappingSet.
- snapshot hash.
- version.
- actor.
- accion.
- fecha.

El boton visible puede cambiar de `Ver auditoria` a `Historial de cambios`, pero no es obligatorio para la primera fase.

### 9.6 Edicion

Separar lectura de edicion:

- Si el mapping set es `Published`, mostrar `Crear borrador para editar`.
- Si es `Draft`, mostrar `Editar regla`.
- El formulario de edicion debe abrirse en panel lateral o modo dedicado, no estar siempre visible.
- No permitir que la edicion mezcle constantes homologadas con placeholders sin etiqueta de riesgo.

## 10. Cambios tecnicos requeridos

### Frontend suficiente para UX-1 y UX-2

La SPA ya recibe metadata suficiente para reclasificar visualmente sin backend:

- `sourceKind`
- `sourceFieldPath`
- `fixedValue`
- `defaultValue`
- `transformationCode`
- `enabled`
- `status`
- `isActive`
- `rules`
- `parameters`
- `validation issues` al ejecutar validacion
- `history`
- `preview`

Cambios frontend futuros:

- Expandir `MatrixStatus` o reemplazarlo por `FunctionalMappingStatus`.
- Agregar `sourceGroup` visible:
  - NACHA.
  - Transaccional.
  - Ciclo/Camara.
  - Respuesta diferencial.
  - Constante.
  - Reservado.
- Agregar clasificador de placeholders local para UX:
  - `SEED`, `TEST`, `0`, `1`, `1.00`, `0.0.0.0`, `REF-1`, `ACH`, `000010070`, `900123456`, `constant.value`.
- Agregar filtros.
- Cambiar labels de fuente.
- Ajustar tests Angular.

### Backend no requerido en primera fase

No se requiere backend para:

- Renombrar `Mapeado tecnico`.
- Clasificar fuente por `sourceKind`.
- Mostrar labels funcionales.
- Separar reservados.
- Detectar placeholders visibles en reglas ya cargadas.

### Backend opcional para fase posterior

Se requeriria backend si se quiere que la pantalla muestre readiness consolidado por servicio/mapping set sin depender de una transaccion especifica.

Opcion minima futura:

- Endpoint: `GET /api/integrations/mappingsets/{id}/readiness`
- DTO sugerido:
  - `status`
  - `code`
  - `isReady`
  - `blockingIssues`
  - `warnings`
  - `parameterStatuses`
  - `optionalReservedParameters`

Riesgo:

- Si se duplica la politica de readiness en frontend, puede divergir del backend. Para UX-1 se acepta clasificacion visual local; para readiness oficial conviene backend.

## 11. Cambios que NO deben hacerse

No hacer en este rediseño:

- No cambiar WSDL.
- No cambiar `RegistrarRespuestaTransaccion`.
- No agregar ANS a Registrar.
- No quitar ANS de `Proc_Contrapartidas`.
- No decidir `OFMONDEB` / `OFMONCRE`.
- No decidir `OFDD`.
- No cambiar mappings publicados.
- No cambiar seeds/bootstrap.
- No cambiar readiness backend.
- No cambiar logica monetaria SOAP.
- No ejecutar SOAP real.
- No tocar `/integraciones/soap-settings`.
- No catalogar `PLValidarUsuarioBV`.
- No ocultar placeholders como si fueran valores oficiales.
- No convertir defaults genericos en `Constante homologada`.

## 12. Fases de implementacion

### Fase UX-1 - Renombrar clasificacion visual y labels de fuentes

Objetivo:

Eliminar `Mapeado tecnico` como label visible y reemplazarlo por categorias funcionales.

Archivos probables:

- `mapping-sets-page.component.ts`
- `mapping-sets-page.component.html`
- `mapping-sets-page.component.scss`
- `mapping-sets-page.component.spec.ts`

No tocar:

- backend.
- seeds.
- readiness.
- WSDL.
- Docker.

Resultado esperado:

- `Transaction.amount` se muestra como `Mapeado transaccional`.
- `Cycle.processingDate` se muestra como `Mapeado por ciclo/camara`.
- `ClearingHouse.id` se muestra como `Mapeado por ciclo/camara`.
- `DifferentialResponse.idEstado` se muestra como `Mapeado desde respuesta diferencial`.
- Constantes con valores placeholder se muestran como `Placeholder / pendiente funcional`, no como mapeadas limpias.

### Fase UX-2 - Simplificar pantalla principal

Objetivo:

Agregar resumen y filtros operativos.

Cambios:

- Conteos nuevos.
- Filtros por estado funcional.
- Filtros por bloqueante/warning/placeholder/reservado.
- Columnas simplificadas.
- `Historial de cambios` como accion secundaria.

### Fase UX-3 - Redisenar pantalla detalle

Objetivo:

Convertir la ruta de detalle en una vista de revision funcional por version.

Cambios:

- Encabezado de version.
- Panel de brechas.
- Matriz completa de version.
- Reservados separados.
- Auditoria colapsada.
- Edicion separada por modo.

### Fase UX-4 - Pruebas Angular

Pruebas requeridas:

- `Transaction` muestra `Mapeado transaccional`.
- `Batch` muestra `Mapeado transaccional` o `Lote operativo`.
- `Cycle` muestra `Mapeado por ciclo/camara`.
- `ClearingHouse` muestra `Mapeado por ciclo/camara`.
- `FinancialInstitution` muestra `Mapeado por ciclo/camara`.
- `DifferentialResponse` muestra `Mapeado desde respuesta diferencial`.
- `Constant` con valor homologado simulado muestra `Constante homologada` solo si el test define politica.
- `Constant` con `SEED`, `0`, `1`, `REF-1`, `ACH`, `000010070`, `900123456` muestra `Placeholder / pendiente funcional`.
- ANS de `Proc_Contrapartidas` aparece como `Opcional / reservado`.
- Registrar conserva 7 parametros WSDL y no muestra ANS.
- `PLValidarUsuarioBV` no aparece.
- Boton de auditoria/historial se mantiene.

### Fase UX-5 - Validacion Playwright

Flujos:

1. Login.
2. Abrir `/integraciones/mappings`.
3. Seleccionar `Proc_Contrapartidas`.
4. Validar:
   - version publicada activa.
   - 22 parametros.
   - ANS separados/reservados.
   - fuentes transaccionales no dicen `Mapeado tecnico`.
   - placeholders se ven como pendientes.
5. Abrir `RegistrarRespuestaTransaccion`.
6. Validar 7 parametros WSDL y sin ANS.
7. Abrir detalle de `Proc_Contrapartidas`.
8. Validar panel de brechas, matriz principal, reservados y auditoria colapsada.
9. Confirmar que `/integraciones/soap-settings` no cambia.

## 13. Tests requeridos

### Unit tests Angular

Archivo principal:

- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.spec.ts`

Casos:

- `shouldLabelNachaSourcesAsMapeadoNacha`
- `shouldLabelTransactionSourcesAsMapeadoTransaccional`
- `shouldLabelCycleAndClearingHouseAsMapeadoPorCicloCamara`
- `shouldLabelDifferentialResponseAsMapeadoDesdeRespuestaDiferencial`
- `shouldLabelProcContrapartidasAnsAsOpcionalReservado`
- `shouldLabelSeedValuesAsPlaceholderPendienteFuncional`
- `shouldNotTreatZeroOrOneAsHomologatedConstantsWithoutPolicy`
- `shouldKeepRegistrarWithSevenWsdlParameters`
- `shouldNotShowAnsForRegistrar`
- `shouldKeepHistoryActionAvailable`

Archivo detalle:

- `mapping-editor-page.component.spec.ts`

Casos:

- muestra encabezado de mapping set.
- muestra panel de brechas.
- separa lectura de edicion.
- muestra reservados colapsados o separados.
- auditoria colapsada por defecto.

### Backend tests

No requeridos para UX-1/UX-2 si no se toca backend.

Si se agrega endpoint readiness por mapping set en una fase posterior:

- tests de DTO readiness.
- tests de placeholders.
- tests de Registrar 7 WSDL.
- tests de ANS reservados.

## 14. Validaciones Playwright

Capturas sugeridas:

- `artifacts/playwright/mappings-redesign-proc-contrapartidas-main.png`
- `artifacts/playwright/mappings-redesign-proc-contrapartidas-filters.png`
- `artifacts/playwright/mappings-redesign-registrar.png`
- `artifacts/playwright/mappings-redesign-detail-proc-contrapartidas.png`
- `artifacts/playwright/mappings-redesign-diagnostics.json`

JSON diagnostico:

- servicio seleccionado.
- mapping set visible.
- conteos.
- filas por estado.
- filas placeholder.
- filas reservadas.
- errores consola.
- errores HTTP.
- request failures.

## 15. Riesgos

| Riesgo | Impacto | Mitigacion |
|---|---|---|
| Duplicar readiness en frontend | Estados visuales pueden divergir del backend. | UX-1 solo clasifica visualmente; readiness oficial sigue backend. |
| Llamar `Constante homologada` sin evidencia | Se oficializan valores no aprobados. | Requerir politica explicita; placeholders siempre visibles. |
| Ocultar problemas monetarios bajo labels amigables | Falso confort funcional. | Mostrar `Placeholder / pendiente funcional` y bloqueantes. |
| Sobrecargar pantalla principal con demasiados filtros | Pierde foco operativo. | Filtros compactos y preseleccion `Todos`; chips contadores. |
| Romper flujo de edicion existente | Usuarios administradores pierden capacidad operativa. | Fase UX-3 separa edicion, no la elimina. |
| Mezclar soap-settings con mappings | Confusion de responsabilidad tecnica vs funcional. | Mantener `/integraciones/soap-settings` fuera de alcance. |

## 16. Criterios de aceptacion

- `/integraciones/mappings` no muestra `Mapeado tecnico` como label visible final.
- Fuentes NACHA se muestran como `Mapeado NACHA`.
- Fuentes `Transaction`, `Batch`, `Prenotification` se muestran como `Mapeado transaccional`.
- Fuentes `Cycle`, `ClearingHouse`, `FinancialInstitution` se muestran como `Mapeado por ciclo/camara`.
- `DifferentialResponse` se muestra como `Mapeado desde respuesta diferencial`.
- `Constant` no se muestra como homologada sin politica.
- Placeholders se muestran como `Placeholder / pendiente funcional`.
- ANS de `Proc_Contrapartidas` se muestran como `Opcional / reservado`.
- `RegistrarRespuestaTransaccion` conserva exactamente 7 parametros WSDL y sin ANS.
- `PLValidarUsuarioBV` no aparece.
- La pantalla principal permite filtrar pendientes, bloqueantes, warnings, placeholders y reservados.
- La pantalla detalle prioriza brechas y matriz antes que formulario de edicion.
- Auditoria/historial se mantiene.
- `/integraciones/soap-settings` no se modifica.
- No se cambian mappings, seeds, readiness, WSDL ni logica monetaria SOAP.

## 17. Veredicto

Rediseño recomendado:

- UX-1 debe iniciar por cambio de lenguaje visual y labels, porque resuelve la confusion principal sin backend y sin tocar reglas funcionales.
- UX-2 debe agregar filtros y conteos.
- UX-3 debe redisenar el detalle/editor para separar revision, edicion, brechas y auditoria.

No hay que implementar backend para la primera fase. Backend solo seria necesario si se exige readiness consolidado por mapping set como fuente oficial de bloqueantes/warnings en la pantalla.

## 18. No implementar

Este documento no implementa cambios. No modifica codigo Angular, backend, seeds, readiness, WSDL, Docker, NACHA-M ni logica monetaria SOAP.

