# NAV4.2F — Inventario de tablas, columnas y accesibilidad tabular

Estado de referencia: **NAV4.2E FULL**. El inventario cubre copy que identifica datos en tablas/grillas y copy accesible directamente tabular. No incluye acciones de fila, filtros de formulario, empty states, validaciones ni mensajes.

## Método de conteo

- Un **hook** es una aparición concreta de copy tabular en template/configuración (`<th>`, `headerName`, display label, nombre accesible o paginación).
- Una **columna única** es una identidad estática de columna dentro de su tabla/grilla. Se colapsan las 12 etiquetas del selector de columnas que duplican columnas AG Grid y no se cuenta como columna adicional el renderer genérico `headerName: column.label` de `app-table`.
- Los dos builders CENIT con `headerName: header` y el renderer genérico de `app-table` se conservan como `DYNAMIC_DATA`; no se sustituyen por un catálogo hardcodeado.
- `DRIFT` describe el estado de entrada NAV4.2E. Todos los hooks DRIFT listados muestran ya el copy final NAV4.2F y conservan el valor previo en `Antes`.

## Resumen exacto

- Hooks tabulares inspeccionados: **630**.
- Columnas únicas estáticamente representadas: **573**.
- Distribución por tipo: `HTML_TH` **44**, `MATERIAL_HEADER` **206**, `GRID_HEADER_NAME` **250**, `COLUMN_DISPLAY_LABEL` **86**, `SORT_ACCESSIBLE_NAME` **0**, `TABLE_ACCESSIBLE_NAME` **16**, `PAGINATION_ACCESSIBLE_COPY` **25**, `OTHER_TABULAR_COPY` **3**.
- Clasificación: `ALIGNED` **243**, `DRIFT` **112**, `TECHNICAL_TERM_LEGITIMATE` **8**, `DOMAIN_TERM_LEGITIMATE` **54**, `CONTEXTUAL_COLUMN` **210**, `DYNAMIC_DATA` **3**, `OUT_OF_SCOPE` **0**.
- Drift residual tabular identificado después del incremento: **0**.

## Export y contratos externos

- El escaneo de la SPA no encontró `exportDataAsCsv`, `exportDataAsExcel` ni otra exportación de AG Grid que reutilice `headerName` como contrato de archivo.
- Los reportes con `TableColumn.label` descargan PDF mediante endpoints dedicados de `ReportsApiService`; `config.columns` sólo alimenta la tabla SPA y **no** se envía al endpoint PDF.
- Ninguno de los **112** hooks modificados forma parte de un encabezado de archivo contractual/máquina. No se cambiaron columnas exportadas, keys, orden, formato, nombre de archivo ni endpoints.

## Transiciones DRIFT old → new

- `Capability` → `Capacidad`
- `Ciclo validación` → `Ciclo de validación`
- `Correlation ID` → `ID de correlación`
- `CorrelationId` → `ID de correlación` ×5
- `Cuenta destino` → `Cuenta de destino` ×3
- `Cuenta origen` → `Cuenta de origen` ×2
- `Código error` → `Código de error` ×4
- `Código Tx` → `Código de transacción`
- `Decision` → `Decisión`
- `Descripción causal` → `Descripción de la causal` ×2
- `Descripción error` → `Descripción del error` ×2
- `DFI destino` → `DFI de destino`
- `DFI origen` → `DFI de origen`
- `Disparado por` → `Iniciado por`
- `Documento receptor` → `Documento del receptor`
- `Dígito` → `Dígito de chequeo`
- `Entidad destino` → `Entidad de destino` ×5
- `Entidad origen` → `Entidad de origen` ×3
- `Estado notificación` → `Estado de notificación` ×2
- `Estado procesamiento` → `Estado de procesamiento`
- `Fecha archivo` → `Fecha del archivo`
- `Fecha carga` → `Fecha de carga`
- `Fecha creación` → `Fecha de creación` ×4
- `Fecha envío` → `Fecha de envío` ×2
- `Fecha recepción` → `Fecha de recepción` ×2
- `Fecha validación` → `Fecha de validación`
- `Fin` → `Vigencia final`
- `Fin vigencia` → `Vigencia final`
- `Fuente cambio` → `Fuente del cambio`
- `Gestión devolución` → `Gestión de devolución` ×2
- `Hora` → `Hora del archivo`
- `Id` → `ID`
- `ID` → `ID de conciliación`
- `ID entidad` → `ID de entidad`
- `Id estado interno` → `ID del estado interno`
- `Id estado servicio externo` → `ID del estado del servicio externo`
- `ID operación` → `ID de operación` ×3
- `ID prenotificación` → `ID de prenotificación`
- `Id transacción` → `ID de transacción`
- `ID transacción` → `ID de transacción` ×2
- `IdCanal` → `ID de canal` ×2
- `Identificador` → `ID de transacción` ×4
- `Identificador de entidad` → `ID de entidad`
- `Identificador de entidad: ' + row.entityIdDisplay` → `ID de entidad: ' + row.entityIdDisplay`
- `Identificador de operación` → `ID de operación` ×4
- `IdEstado` → `ID del estado` ×2
- `IdTransaccionServicioExterno` → `ID de transacción del servicio externo`
- `Inicio` → `Vigencia inicial`
- `Inicio vigencia` → `Vigencia inicial`
- `Institución destino` → `Institución de destino` ×2
- `Monetario` → `Movimiento monetario`
- `Nacha ID` → `ID del archivo NACHA-M`
- `Nombre canal` → `Nombre del canal` ×2
- `Número intento` → `Número de intento` ×2
- `Paginación de restricciones operacionales CFA` → `Paginación de restricciones operativas`
- `Referencia legado` → `Referencia heredada` ×3
- `Restricciones operacionales CFA` → `Restricciones operativas`
- `SOAP candidato` → `Operación SOAP candidata`
- `Ticket cambio` → `Ticket de cambio`
- `Tipo respuesta` → `Tipo de respuesta`
- `Total monto` → `Monto total`
- `Trace` → `Número de rastreo` ×2
- `Transacción` → `ID de transacción` ×3
- `Traza` → `Número de rastreo`
- `Tx` → `ID de transacción ACH`
- `User agent` → `User-Agent`
- `Vigencia desde (UTC)` → `Vigencia inicial (UTC)`
- `Vigencia hasta (UTC)` → `Vigencia final (UTC)`

## Inventario completo

| # | Archivo:línea | Tipo | Antes | Copy final | Columna/key | Dato representado | Sort | Filter | Export | Clasificación |
|---:|---|---|---|---|---|---|---|---|---|---|
| 1 | `core/services/material-spanish-intl.ts:7` | `PAGINATION_ACCESSIBLE_COPY` | — | Registros por página: | `itemsPerPageLabel` | pagination locale | N/A | N/A | No | **ALIGNED** |
| 2 | `core/services/material-spanish-intl.ts:8` | `PAGINATION_ACCESSIBLE_COPY` | — | Página siguiente | `nextPageLabel` | pagination locale | N/A | N/A | No | **ALIGNED** |
| 3 | `core/services/material-spanish-intl.ts:9` | `PAGINATION_ACCESSIBLE_COPY` | — | Página anterior | `previousPageLabel` | pagination locale | N/A | N/A | No | **ALIGNED** |
| 4 | `core/services/material-spanish-intl.ts:10` | `PAGINATION_ACCESSIBLE_COPY` | — | Primera página | `firstPageLabel` | pagination locale | N/A | N/A | No | **ALIGNED** |
| 5 | `core/services/material-spanish-intl.ts:11` | `PAGINATION_ACCESSIBLE_COPY` | — | Última página | `lastPageLabel` | pagination locale | N/A | N/A | No | **ALIGNED** |
| 6 | `features/ach-claims/ach-claims-detail.component.html:60` | `TABLE_ACCESSIBLE_NAME` | — | Transacciones asociadas a la reclamación | `table` | table | N/A | N/A | No | **ALIGNED** |
| 7 | `features/ach-claims/ach-claims-detail.component.html:61` | `MATERIAL_HEADER` | — | N.º de transacción | `id` | id | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 8 | `features/ach-claims/ach-claims-detail.component.html:62` | `MATERIAL_HEADER` | — | Número de seguimiento | `trace` | trace | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 9 | `features/ach-claims/ach-claims-detail.component.html:63` | `MATERIAL_HEADER` | — | Referencia externa | `externalReference` | externalReference | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 10 | `features/ach-claims/ach-claims-detail.component.html:64` | `MATERIAL_HEADER` | — | Valor | `amount` | amount | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 11 | `features/ach-claims/ach-claims-detail.component.html:65` | `MATERIAL_HEADER` | — | Tipo | `type` | type | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 12 | `features/ach-claims/ach-claims-detail.component.html:66` | `MATERIAL_HEADER` | Entidad origen | Entidad de origen | `source` | source | No | Sin cambio / externo | No identificado | **DRIFT** |
| 13 | `features/ach-claims/ach-claims-detail.component.html:67` | `MATERIAL_HEADER` | Entidad destino | Entidad de destino | `destination` | destination | No | Sin cambio / externo | No identificado | **DRIFT** |
| 14 | `features/ach-claims/ach-claims-list.component.html:80` | `TABLE_ACCESSIBLE_NAME` | — | Listado de reclamaciones ACH | `table` | table | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 15 | `features/ach-claims/ach-claims-list.component.html:82` | `MATERIAL_HEADER` | — | N.º de reclamación | `id` | id | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 16 | `features/ach-claims/ach-claims-list.component.html:86` | `MATERIAL_HEADER` | — | Causal | `cause` | cause | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 17 | `features/ach-claims/ach-claims-list.component.html:90` | `MATERIAL_HEADER` | — | Estado | `status` | status | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 18 | `features/ach-claims/ach-claims-list.component.html:94` | `MATERIAL_HEADER` | — | Fecha de radicación | `filedAt` | filedAt | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 19 | `features/ach-claims/ach-claims-list.component.html:98` | `MATERIAL_HEADER` | — | Fecha límite de gestión | `deadlineAt` | deadlineAt | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 20 | `features/ach-claims/ach-claims-list.component.html:102` | `MATERIAL_HEADER` | — | Entidad solicitante | `requestingParticipant` | requestingParticipant | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 21 | `features/ach-claims/ach-claims-list.component.html:106` | `MATERIAL_HEADER` | — | Entidad destinataria | `respondingParticipant` | respondingParticipant | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 22 | `features/ach-claims/ach-claims-list.component.html:110` | `MATERIAL_HEADER` | — | Transacciones | `transactions` | transactions | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 23 | `features/ach-claims/ach-claims-list.component.html:114` | `MATERIAL_HEADER` | — | Referencia externa | `externalReference` | externalReference | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 24 | `features/ach-claims/ach-claims-list.component.html:118` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 25 | `features/ach-claims/ach-claims-list.component.html:128` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de reclamaciones ACH | `mat-paginator` | pagination | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 26 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Archivo | `plain-th-1` | plain-th-1 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 27 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Última actualización | `plain-th-10` | plain-th-10 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 28 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Operación | `plain-th-2` | plain-th-2 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 29 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Cámara | `plain-th-3` | plain-th-3 | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 30 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Fecha operativa | `plain-th-4` | plain-th-4 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 31 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Ciclo | `plain-th-5` | plain-th-5 | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 32 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Estado | `plain-th-6` | plain-th-6 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 33 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Ejecución | `plain-th-7` | plain-th-7 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 34 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Intentos | `plain-th-8` | plain-th-8 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 35 | `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html:24` | `HTML_TH` | — | Archivo | `plain-th-9` | plain-th-9 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 36 | `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html:151` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 37 | `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html:156` | `MATERIAL_HEADER` | — | Ventana ACH Colombia | `window` | window | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 38 | `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html:161` | `MATERIAL_HEADER` | — | Hora automática | `automaticTime` | automaticTime | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 39 | `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html:168` | `MATERIAL_HEADER` | — | Última hora permitida | `latestTime` | latestTime | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 40 | `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html:175` | `MATERIAL_HEADER` | — | Activo | `active` | active | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 41 | `features/ach-cycles/components/ach-cycle-list.component.html:39` | `COLUMN_DISPLAY_LABEL` | — | Cámara | `clearingHouseName` | clearingHouseName | No (shared table) | No (shared table) | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 42 | `features/ach-cycles/components/ach-cycle-list.component.html:40` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `dateText` | dateText | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 43 | `features/ach-cycles/components/ach-cycle-list.component.html:41` | `COLUMN_DISPLAY_LABEL` | — | Inicio | `startText` | startText | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 44 | `features/ach-cycles/components/ach-cycle-list.component.html:42` | `COLUMN_DISPLAY_LABEL` | — | Fin | `endText` | endText | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 45 | `features/ach-cycles/components/ach-cycle-list.component.html:43` | `COLUMN_DISPLAY_LABEL` | — | Estado | `statusText` | statusText | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 46 | `features/ach-cycles/components/nacha-export.component.html:121` | `TABLE_ACCESSIBLE_NAME` | — | Ciclos disponibles para exportación NACHA-M | `table` | table | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 47 | `features/ach-cycles/components/nacha-export.component.html:123` | `MATERIAL_HEADER` | — | Cámara | `clearingHouse` | clearingHouse | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 48 | `features/ach-cycles/components/nacha-export.component.html:127` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 49 | `features/ach-cycles/components/nacha-export.component.html:134` | `MATERIAL_HEADER` | — | Fecha operativa | `processingDate` | processingDate | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 50 | `features/ach-cycles/components/nacha-export.component.html:138` | `MATERIAL_HEADER` | — | Contenido | `transactions` | transactions | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 51 | `features/ach-cycles/components/nacha-export.component.html:145` | `MATERIAL_HEADER` | — | Estado | `status` | status | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 52 | `features/ach-cycles/components/nacha-export.component.html:153` | `MATERIAL_HEADER` | — | Último archivo | `fileName` | fileName | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 53 | `features/ach-cycles/components/nacha-export.component.html:162` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 54 | `features/ach-responses/pages/ach-response-attempts-page.component.html:28` | `MATERIAL_HEADER` | Número intento | Número de intento | `numeroIntento` | numeroIntento | No | Sin cambio / externo | No identificado | **DRIFT** |
| 55 | `features/ach-responses/pages/ach-response-attempts-page.component.html:33` | `MATERIAL_HEADER` | Estado notificación | Estado de notificación | `estadoNotificacion` | estadoNotificacion | No | Sin cambio / externo | No identificado | **DRIFT** |
| 56 | `features/ach-responses/pages/ach-response-attempts-page.component.html:43` | `MATERIAL_HEADER` | IdCanal | ID de canal | `idCanal` | idCanal | No | Sin cambio / externo | No identificado | **DRIFT** |
| 57 | `features/ach-responses/pages/ach-response-attempts-page.component.html:48` | `MATERIAL_HEADER` | Nombre canal | Nombre del canal | `nombreCanal` | nombreCanal | No | Sin cambio / externo | No identificado | **DRIFT** |
| 58 | `features/ach-responses/pages/ach-response-attempts-page.component.html:53` | `MATERIAL_HEADER` | Id transacción | ID de transacción | `idTransaccion` | idTransaccion | No | Sin cambio / externo | No identificado | **DRIFT** |
| 59 | `features/ach-responses/pages/ach-response-attempts-page.component.html:58` | `MATERIAL_HEADER` | IdEstado | ID del estado | `idEstado` | idEstado | No | Sin cambio / externo | No identificado | **DRIFT** |
| 60 | `features/ach-responses/pages/ach-response-attempts-page.component.html:63` | `MATERIAL_HEADER` | — | Causal | `causal` | causal | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 61 | `features/ach-responses/pages/ach-response-attempts-page.component.html:68` | `MATERIAL_HEADER` | IdTransaccionServicioExterno | ID de transacción del servicio externo | `idTransaccionServicioExterno` | idTransaccionServicioExterno | No | Sin cambio / externo | No identificado | **DRIFT** |
| 62 | `features/ach-responses/pages/ach-response-attempts-page.component.html:73` | `MATERIAL_HEADER` | Descripción causal | Descripción de la causal | `descripcionCausal` | descripcionCausal | No | Sin cambio / externo | No identificado | **DRIFT** |
| 63 | `features/ach-responses/pages/ach-response-attempts-page.component.html:78` | `MATERIAL_HEADER` | — | Existe error | `existeError` | existeError | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 64 | `features/ach-responses/pages/ach-response-attempts-page.component.html:83` | `MATERIAL_HEADER` | Código error | Código de error | `codigoError` | codigoError | No | Sin cambio / externo | No identificado | **DRIFT** |
| 65 | `features/ach-responses/pages/ach-response-attempts-page.component.html:88` | `MATERIAL_HEADER` | Descripción error | Descripción del error | `descripcionError` | descripcionError | No | Sin cambio / externo | No identificado | **DRIFT** |
| 66 | `features/ach-responses/pages/ach-response-attempts-page.component.html:93` | `MATERIAL_HEADER` | — | Error técnico | `errorTecnico` | errorTecnico | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 67 | `features/ach-responses/pages/ach-response-attempts-page.component.html:98` | `MATERIAL_HEADER` | Fecha creación | Fecha de creación | `fechaCreacion` | fechaCreacion | No | Sin cambio / externo | No identificado | **DRIFT** |
| 68 | `features/ach-responses/pages/ach-response-attempts-page.component.html:103` | `MATERIAL_HEADER` | Fecha envío | Fecha de envío | `fechaEnvio` | fechaEnvio | No | Sin cambio / externo | No identificado | **DRIFT** |
| 69 | `features/ach-responses/pages/ach-response-detail-page.component.html:59` | `HTML_TH` | — | Intento | `plain-th-1` | plain-th-1 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 70 | `features/ach-responses/pages/ach-response-detail-page.component.html:59` | `HTML_TH` | — | Estado | `plain-th-2` | plain-th-2 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 71 | `features/ach-responses/pages/ach-response-detail-page.component.html:59` | `HTML_TH` | — | Solicitado por | `plain-th-3` | plain-th-3 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 72 | `features/ach-responses/pages/ach-response-detail-page.component.html:59` | `HTML_TH` | — | Solicitud | `plain-th-4` | plain-th-4 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 73 | `features/ach-responses/pages/ach-response-detail-page.component.html:60` | `HTML_TH` | — | Inicio | `plain-th-5` | plain-th-5 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 74 | `features/ach-responses/pages/ach-response-detail-page.component.html:60` | `HTML_TH` | — | Terminación | `plain-th-6` | plain-th-6 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 75 | `features/ach-responses/pages/ach-response-detail-page.component.html:60` | `HTML_TH` | — | Instancia | `plain-th-7` | plain-th-7 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 76 | `features/ach-responses/pages/ach-response-detail-page.component.html:60` | `HTML_TH` | Correlation ID | ID de correlación | `plain-th-8` | plain-th-8 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 77 | `features/ach-responses/pages/ach-response-detail-page.component.html:61` | `HTML_TH` | — | Detalle | `plain-th-10` | plain-th-10 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 78 | `features/ach-responses/pages/ach-response-detail-page.component.html:61` | `HTML_TH` | — | Resultado | `plain-th-9` | plain-th-9 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 79 | `features/ach-responses/pages/ach-response-detail-page.component.html:140` | `HTML_TH` | Número intento | Número de intento | `plain-th-11` | plain-th-11 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 80 | `features/ach-responses/pages/ach-response-detail-page.component.html:141` | `HTML_TH` | Estado notificación | Estado de notificación | `plain-th-12` | plain-th-12 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 81 | `features/ach-responses/pages/ach-response-detail-page.component.html:142` | `HTML_TH` | IdCanal | ID de canal | `plain-th-13` | plain-th-13 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 82 | `features/ach-responses/pages/ach-response-detail-page.component.html:143` | `HTML_TH` | Nombre canal | Nombre del canal | `plain-th-14` | plain-th-14 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 83 | `features/ach-responses/pages/ach-response-detail-page.component.html:144` | `HTML_TH` | IdEstado | ID del estado | `plain-th-15` | plain-th-15 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 84 | `features/ach-responses/pages/ach-response-detail-page.component.html:145` | `HTML_TH` | — | Causal | `plain-th-16` | plain-th-16 | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 85 | `features/ach-responses/pages/ach-response-detail-page.component.html:146` | `HTML_TH` | — | Existe error | `plain-th-17` | plain-th-17 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 86 | `features/ach-responses/pages/ach-response-detail-page.component.html:147` | `HTML_TH` | Código error | Código de error | `plain-th-18` | plain-th-18 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 87 | `features/ach-responses/pages/ach-response-detail-page.component.html:148` | `HTML_TH` | Descripción error | Descripción del error | `plain-th-19` | plain-th-19 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 88 | `features/ach-responses/pages/ach-response-detail-page.component.html:149` | `HTML_TH` | — | Error técnico | `plain-th-20` | plain-th-20 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 89 | `features/ach-responses/pages/ach-response-detail-page.component.html:150` | `HTML_TH` | Fecha creación | Fecha de creación | `plain-th-21` | plain-th-21 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 90 | `features/ach-responses/pages/ach-response-detail-page.component.html:151` | `HTML_TH` | Fecha envío | Fecha de envío | `plain-th-22` | plain-th-22 | No | Sin cambio / externo | No identificado | **DRIFT** |
| 91 | `features/ach-responses/pages/ach-response-list-page.component.ts:78` | `GRID_HEADER_NAME` | Fecha recepción | Fecha de recepción | `fechaRecepcionText` | fechaRecepcionText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 92 | `features/ach-responses/pages/ach-response-list-page.component.ts:79` | `GRID_HEADER_NAME` | Fecha creación | Fecha de creación | `fechaCreacionText` | fechaCreacionText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 93 | `features/ach-responses/pages/ach-response-list-page.component.ts:80` | `GRID_HEADER_NAME` | — | Tipo | `tipoRespuesta` | tipoRespuesta | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 94 | `features/ach-responses/pages/ach-response-list-page.component.ts:81` | `GRID_HEADER_NAME` | Transacción | ID de transacción | `idTransaccion` | idTransaccion | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 95 | `features/ach-responses/pages/ach-response-list-page.component.ts:82` | `GRID_HEADER_NAME` | — | Cámara | `codigoCamaraCompensacion` | codigoCamaraCompensacion | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 96 | `features/ach-responses/pages/ach-response-list-page.component.ts:83` | `GRID_HEADER_NAME` | Entidad origen | Entidad de origen | `codigoEntidadOrigen` | codigoEntidadOrigen | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 97 | `features/ach-responses/pages/ach-response-list-page.component.ts:84` | `GRID_HEADER_NAME` | Entidad destino | Entidad de destino | `codigoEntidadDestino` | codigoEntidadDestino | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 98 | `features/ach-responses/pages/ach-response-list-page.component.ts:85` | `GRID_HEADER_NAME` | — | Estado externo | `codigoEstadoExterno` | codigoEstadoExterno | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 99 | `features/ach-responses/pages/ach-response-list-page.component.ts:86` | `GRID_HEADER_NAME` | — | Causal externa | `codigoCausalExterna` | codigoCausalExterna | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 100 | `features/ach-responses/pages/ach-response-list-page.component.ts:87` | `GRID_HEADER_NAME` | — | Estado interno | `estadoInternoNombre` | estadoInternoNombre | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 101 | `features/ach-responses/pages/ach-response-list-page.component.ts:90` | `GRID_HEADER_NAME` | Estado procesamiento | Estado de procesamiento | `estadoProcesamiento` | estadoProcesamiento | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 102 | `features/ach-responses/pages/ach-response-list-page.component.ts:96` | `GRID_HEADER_NAME` | — | Notificable | `permiteNotificacionText` | permiteNotificacionText | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 103 | `features/ach-responses/pages/ach-response-list-page.component.ts:97` | `GRID_HEADER_NAME` | CorrelationId | ID de correlación | `correlationId` | correlationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 104 | `features/ach-responses/pages/ach-response-list-page.component.ts:99` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 105 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:86` | `GRID_HEADER_NAME` | — | Prioridad | `prioridadText` | prioridadText | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 106 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:90` | `GRID_HEADER_NAME` | Fecha recepción | Fecha de recepción | `fechaRecepcionText` | fechaRecepcionText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 107 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:91` | `GRID_HEADER_NAME` | Fecha creación | Fecha de creación | `fechaCreacionText` | fechaCreacionText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 108 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:92` | `GRID_HEADER_NAME` | — | Tipo | `tipoRespuesta` | tipoRespuesta | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 109 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:93` | `GRID_HEADER_NAME` | Transacción | ID de transacción | `idTransaccion` | idTransaccion | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 110 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:94` | `GRID_HEADER_NAME` | — | Cámara | `codigoCamaraCompensacion` | codigoCamaraCompensacion | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 111 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:95` | `GRID_HEADER_NAME` | Entidad origen | Entidad de origen | `codigoEntidadOrigen` | codigoEntidadOrigen | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 112 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:96` | `GRID_HEADER_NAME` | Entidad destino | Entidad de destino | `codigoEntidadDestino` | codigoEntidadDestino | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 113 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:97` | `GRID_HEADER_NAME` | — | Estado externo | `codigoEstadoExterno` | codigoEstadoExterno | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 114 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:98` | `GRID_HEADER_NAME` | — | Causal externa | `codigoCausalExterna` | codigoCausalExterna | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 115 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:99` | `GRID_HEADER_NAME` | — | Estado interno | `estadoInternoNombre` | estadoInternoNombre | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 116 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:102` | `GRID_HEADER_NAME` | — | Estado crítico | `estadoProcesamiento` | estadoProcesamiento | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 117 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:106` | `GRID_HEADER_NAME` | — | Notificable | `permiteNotificacionText` | permiteNotificacionText | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 118 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:107` | `GRID_HEADER_NAME` | CorrelationId | ID de correlación | `correlationId` | correlationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 119 | `features/ach-responses/pages/ach-response-manual-review-page.component.ts:109` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 120 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:80` | `GRID_HEADER_NAME` | — | Cámara | `codigoCamaraCompensacion` | codigoCamaraCompensacion | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 121 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:81` | `GRID_HEADER_NAME` | Tipo respuesta | Tipo de respuesta | `tipoRespuesta` | tipoRespuesta | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 122 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:82` | `GRID_HEADER_NAME` | — | Estado externo | `codigoEstadoExterno` | codigoEstadoExterno | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 123 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:83` | `GRID_HEADER_NAME` | — | Causal externa | `codigoCausalExterna` | codigoCausalExterna | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 124 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:84` | `GRID_HEADER_NAME` | — | Prioridad | `priority` | priority | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 125 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:85` | `GRID_HEADER_NAME` | Id estado interno | ID del estado interno | `idEstadoInterno` | idEstadoInterno | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 126 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:86` | `GRID_HEADER_NAME` | Id estado servicio externo | ID del estado del servicio externo | `idEstadoServicioExterno` | idEstadoServicioExterno | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 127 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:87` | `GRID_HEADER_NAME` | — | Estado interno | `estadoInternoNombre` | estadoInternoNombre | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 128 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:88` | `GRID_HEADER_NAME` | — | Causal normalizada | `causalNormalizada` | causalNormalizada | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 129 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:89` | `GRID_HEADER_NAME` | Descripción causal | Descripción de la causal | `descripcionCausalNormalizada` | descripcionCausalNormalizada | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 130 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:92` | `GRID_HEADER_NAME` | — | Requiere causal | `requiereCausalText` | requiereCausalText | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 131 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:98` | `GRID_HEADER_NAME` | — | Permite notificación | `permiteNotificacionText` | permiteNotificacionText | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 132 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:104` | `GRID_HEADER_NAME` | — | Activo | `activoText` | activoText | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 133 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:108` | `GRID_HEADER_NAME` | Inicio vigencia | Vigencia inicial | `fechaInicioVigenciaText` | fechaInicioVigenciaText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 134 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:109` | `GRID_HEADER_NAME` | Fin vigencia | Vigencia final | `fechaFinVigenciaText` | fechaFinVigenciaText | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 135 | `features/ach-responses/pages/ach-response-status-mappings-page.component.ts:111` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 136 | `features/admin/components/users-list.component.html:59` | `TABLE_ACCESSIBLE_NAME` | — | Listado de usuarios | `table` | table | N/A | N/A | No | **ALIGNED** |
| 137 | `features/admin/components/users-list.component.html:60` | `MATERIAL_HEADER` | — | Nombre | `fullName` | fullName | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 138 | `features/admin/components/users-list.component.html:61` | `MATERIAL_HEADER` | — | Nombre de usuario | `userName` | userName | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 139 | `features/admin/components/users-list.component.html:62` | `MATERIAL_HEADER` | — | Correo electrónico | `email` | email | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 140 | `features/admin/components/users-list.component.html:63` | `MATERIAL_HEADER` | — | Perfiles de acceso | `roles` | roles | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 141 | `features/admin/components/users-list.component.html:64` | `MATERIAL_HEADER` | — | Estado | `status` | status | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 142 | `features/admin/components/users-list.component.html:65` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 143 | `features/admin/components/users-list.component.html:70` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de usuarios | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 144 | `features/audit/components/audit-log.component.html:168` | `TABLE_ACCESSIBLE_NAME` | — | Resultados de auditoría. La tabla admite desplazamiento horizontal. | `table-region` | table region | N/A | N/A | No | **ALIGNED** |
| 145 | `features/audit/components/audit-log.component.html:173` | `TABLE_ACCESSIBLE_NAME` | — | Registros de auditoría | `table` | table | N/A | N/A | No | **ALIGNED** |
| 146 | `features/audit/components/audit-log.component.html:184` | `MATERIAL_HEADER` | — | Fecha | `changedAt` | changedAt | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 147 | `features/audit/components/audit-log.component.html:189` | `MATERIAL_HEADER` | — | Usuario | `changedBy` | changedBy | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 148 | `features/audit/components/audit-log.component.html:194` | `MATERIAL_HEADER` | — | Acción | `action` | action | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 149 | `features/audit/components/audit-log.component.html:206` | `MATERIAL_HEADER` | — | Entidad | `entityName` | entityName | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 150 | `features/audit/components/audit-log.component.html:211` | `MATERIAL_HEADER` | ID entidad | ID de entidad | `entityId` | entityId | Sí | Sin cambio / externo | No identificado | **DRIFT** |
| 151 | `features/audit/components/audit-log.component.html:217` | `OTHER_TABULAR_COPY` | Identificador de entidad: ' + row.entityIdDisplay | ID de entidad: ' + row.entityIdDisplay | `entityId-cell-a11y` | entityId | N/A | N/A | No | **DRIFT** |
| 152 | `features/audit/components/audit-log.component.html:225` | `MATERIAL_HEADER` | — | Campos modificados | `changedFields` | changedFields | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 153 | `features/audit/components/audit-log.component.html:243` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación del registro de auditoría | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 154 | `features/auth-logs/components/auth-log.component.html:168` | `TABLE_ACCESSIBLE_NAME` | — | Resultados de autenticación. La tabla admite desplazamiento horizontal. | `table-region` | table region | N/A | N/A | No | **ALIGNED** |
| 155 | `features/auth-logs/components/auth-log.component.html:173` | `TABLE_ACCESSIBLE_NAME` | — | Registros de autenticación | `table` | table | N/A | N/A | No | **ALIGNED** |
| 156 | `features/auth-logs/components/auth-log.component.html:184` | `MATERIAL_HEADER` | — | Fecha | `loggedAt` | loggedAt | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 157 | `features/auth-logs/components/auth-log.component.html:189` | `MATERIAL_HEADER` | — | Hora | `loggedTime` | loggedTime | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 158 | `features/auth-logs/components/auth-log.component.html:194` | `MATERIAL_HEADER` | — | Usuario | `username` | username | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 159 | `features/auth-logs/components/auth-log.component.html:199` | `MATERIAL_HEADER` | — | Resultado | `success` | success | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 160 | `features/auth-logs/components/auth-log.component.html:214` | `MATERIAL_HEADER` | — | Detalle | `failureReason` | failureReason | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 161 | `features/auth-logs/components/auth-log.component.html:228` | `MATERIAL_HEADER` | — | Dirección IP | `ipAddress` | ipAddress | Sí | Sin cambio / externo | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 162 | `features/auth-logs/components/auth-log.component.html:233` | `MATERIAL_HEADER` | — | Navegador | `userAgent` | userAgent | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 163 | `features/auth-logs/components/auth-log.component.html:251` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación del registro de autenticaciones | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 164 | `features/catalogs/components/address-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 165 | `features/catalogs/components/address-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 166 | `features/catalogs/components/address-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 167 | `features/catalogs/components/bank-holidays.component.html:76` | `TABLE_ACCESSIBLE_NAME` | — | Festivos bancarios | `table-region` | table region | N/A | N/A | No | **ALIGNED** |
| 168 | `features/catalogs/components/bank-holidays.component.html:79` | `MATERIAL_HEADER` | — | Día efectivo de descanso | `date` | date | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 169 | `features/catalogs/components/bank-holidays.component.html:84` | `MATERIAL_HEADER` | — | Fecha conmemorativa | `commemorativeDate` | commemorativeDate | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 170 | `features/catalogs/components/bank-holidays.component.html:91` | `MATERIAL_HEADER` | — | Descripción | `description` | description | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 171 | `features/catalogs/components/bank-holidays.component.html:96` | `MATERIAL_HEADER` | — | Origen | `rule` | rule | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 172 | `features/catalogs/components/bank-holidays.component.html:104` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 173 | `features/catalogs/components/catalog-types-admin.component.ts:36` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 174 | `features/catalogs/components/catalog-types-admin.component.ts:37` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 175 | `features/catalogs/components/catalog-types-admin.component.ts:38` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 176 | `features/catalogs/components/catalog-types-admin.component.ts:41` | `GRID_HEADER_NAME` | — | Acciones | `acciones` | acciones | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 177 | `features/catalogs/components/catalogs-list.component.ts:21` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 178 | `features/catalogs/components/catalogs-list.component.ts:22` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 179 | `features/catalogs/components/catalogs-list.component.ts:23` | `GRID_HEADER_NAME` | — | Tipo | `type` | type | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 180 | `features/catalogs/components/clearing-house-preferences.component.ts:88` | `GRID_HEADER_NAME` | — | Institución | `financialInstitutionName` | financialInstitutionName | Hereda grid | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 181 | `features/catalogs/components/clearing-house-preferences.component.ts:95` | `GRID_HEADER_NAME` | — | Cámara compensadora | `clearingHouseName` | clearingHouseName | Hereda grid | Sí | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 182 | `features/catalogs/components/clearing-house-preferences.component.ts:102` | `GRID_HEADER_NAME` | — | Prioridad | `priority` | priority | Hereda grid | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 183 | `features/catalogs/components/clearing-house-preferences.component.ts:110` | `GRID_HEADER_NAME` | — | Predeterminada | `isDefault` | isDefault | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 184 | `features/catalogs/components/clearing-house-preferences.component.ts:130` | `GRID_HEADER_NAME` | — | Estado | `isActive` | isActive | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 185 | `features/catalogs/components/clearing-house-preferences.component.ts:144` | `GRID_HEADER_NAME` | — | Acciones | `actions` | actions | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 186 | `features/catalogs/components/clearing-house-special-dates.component.html:94` | `MATERIAL_HEADER` | — | Fecha | `date` | date | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 187 | `features/catalogs/components/clearing-house-special-dates.component.html:98` | `MATERIAL_HEADER` | — | Día de la semana | `weekday` | weekday | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 188 | `features/catalogs/components/clearing-house-special-dates.component.html:102` | `MATERIAL_HEADER` | — | Descripción | `description` | description | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 189 | `features/catalogs/components/clearing-house-special-dates.component.html:106` | `MATERIAL_HEADER` | — | Efecto operativo | `effect` | effect | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 190 | `features/catalogs/components/clearing-house-special-dates.component.html:113` | `MATERIAL_HEADER` | — | Estado | `state` | state | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 191 | `features/catalogs/components/clearing-house-special-dates.component.html:122` | `MATERIAL_HEADER` | — | Última modificación | `updated` | updated | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 192 | `features/catalogs/components/clearing-house-special-dates.component.html:126` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 193 | `features/catalogs/components/clearing-house-special-dates.component.html:168` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de fechas especiales | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 194 | `features/catalogs/components/company-entry-description-admin.component.ts:29` | `GRID_HEADER_NAME` | Id | ID | `id` | id | Sí | Sí | No identificado | **DRIFT** |
| 195 | `features/catalogs/components/company-entry-description-admin.component.ts:30` | `GRID_HEADER_NAME` | — | Término | `term` | term | Sí | Sí | No identificado | **ALIGNED** |
| 196 | `features/catalogs/components/company-entry-description-admin.component.ts:31` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 197 | `features/catalogs/components/company-entry-description-admin.component.ts:32` | `GRID_HEADER_NAME` | — | SEC | `sec` | sec | Sí | Sí | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 198 | `features/catalogs/components/company-entry-description-admin.component.ts:33` | `GRID_HEADER_NAME` | — | Estado | `estado` | estado | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 199 | `features/catalogs/components/company-entry-description-admin.component.ts:36` | `GRID_HEADER_NAME` | — | Acciones | `acciones` | acciones | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 200 | `features/catalogs/components/document-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 201 | `features/catalogs/components/document-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 202 | `features/catalogs/components/document-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 203 | `features/catalogs/components/email-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 204 | `features/catalogs/components/email-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 205 | `features/catalogs/components/email-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 206 | `features/catalogs/components/financial-institutions.component.ts:79` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Hereda grid | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 207 | `features/catalogs/components/financial-institutions.component.ts:80` | `GRID_HEADER_NAME` | — | Routing | `routingNumber` | routingNumber | Hereda grid | Hereda grid | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 208 | `features/catalogs/components/financial-institutions.component.ts:81` | `GRID_HEADER_NAME` | — | Transit | `transitCode` | transitCode | Hereda grid | Hereda grid | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 209 | `features/catalogs/components/financial-institutions.component.ts:82` | `GRID_HEADER_NAME` | Dígito | Dígito de chequeo | `checkDigit` | checkDigit | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 210 | `features/catalogs/components/financial-institutions.component.ts:85` | `GRID_HEADER_NAME` | — | Origen por defecto | `isDefaultSource` | isDefaultSource | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 211 | `features/catalogs/components/financial-institutions.component.ts:103` | `GRID_HEADER_NAME` | — | Estado | `status` | status | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 212 | `features/catalogs/components/financial-institutions.component.ts:122` | `GRID_HEADER_NAME` | — | Acciones | `actions` | actions | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 213 | `features/catalogs/components/gender-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 214 | `features/catalogs/components/gender-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 215 | `features/catalogs/components/gender-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 216 | `features/catalogs/components/person-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 217 | `features/catalogs/components/person-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 218 | `features/catalogs/components/person-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 219 | `features/catalogs/components/phone-types-admin.component.ts:22` | `GRID_HEADER_NAME` | — | Código | `code` | code | Sí | Sí | No identificado | **ALIGNED** |
| 220 | `features/catalogs/components/phone-types-admin.component.ts:23` | `GRID_HEADER_NAME` | — | Nombre | `name` | name | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 221 | `features/catalogs/components/phone-types-admin.component.ts:24` | `GRID_HEADER_NAME` | — | Descripción | `description` | description | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 222 | `features/cenit/components/cenit-operation-page.component.ts:204` | `GRID_HEADER_NAME` | — | header | `dynamic-header` | dynamic-header | Sí | Sí | No identificado | **DYNAMIC_DATA** |
| 223 | `features/cenit/components/cenit-regulatory-page.component.ts:182` | `GRID_HEADER_NAME` | — | header | `dynamic-header` | dynamic-header | Sí | Sí | No identificado | **DYNAMIC_DATA** |
| 224 | `features/clearing-houses/clearing-houses.component.html:91` | `MATERIAL_HEADER` | — | Código | `code` | code | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 225 | `features/clearing-houses/clearing-houses.component.html:95` | `MATERIAL_HEADER` | — | Nombre | `name` | name | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 226 | `features/clearing-houses/clearing-houses.component.html:99` | `MATERIAL_HEADER` | — | Estado | `state` | state | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 227 | `features/clearing-houses/clearing-houses.component.html:105` | `MATERIAL_HEADER` | — | Disponibilidad | `availability` | availability | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 228 | `features/clearing-houses/clearing-houses.component.html:114` | `MATERIAL_HEADER` | — | Última modificación | `updated` | updated | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 229 | `features/clearing-houses/clearing-houses.component.html:118` | `MATERIAL_HEADER` | — | Configuraciones | `configuration` | configuration | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 230 | `features/clearing-houses/clearing-houses.component.html:125` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 231 | `features/clearing-houses/clearing-houses.component.html:191` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de cámaras compensadoras | `mat-paginator` | pagination | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 232 | `features/clearing-houses/transaction-policies.component.html:121` | `MATERIAL_HEADER` | — | Tipo | `transactionType` | transactionType | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 233 | `features/clearing-houses/transaction-policies.component.html:124` | `MATERIAL_HEADER` | — | Regla | `mode` | mode | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 234 | `features/clearing-houses/transaction-policies.component.html:127` | `MATERIAL_HEADER` | — | Plazo | `lead` | lead | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 235 | `features/clearing-houses/transaction-policies.component.html:130` | `MATERIAL_HEADER` | Inicio | Vigencia inicial | `from` | from | No | Sin cambio / externo | No identificado | **DRIFT** |
| 236 | `features/clearing-houses/transaction-policies.component.html:133` | `MATERIAL_HEADER` | Fin | Vigencia final | `to` | to | No | Sin cambio / externo | No identificado | **DRIFT** |
| 237 | `features/clearing-houses/transaction-policies.component.html:136` | `MATERIAL_HEADER` | — | Estado | `state` | state | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 238 | `features/clearing-houses/transaction-policies.component.html:139` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 239 | `features/customer-third-parties/components/customer-third-parties.component.ts:54` | `COLUMN_DISPLAY_LABEL` | — | Cliente | `customerName` | customerName | No (shared table) | No (shared table) | No identificado | **ALIGNED** |
| 240 | `features/customer-third-parties/components/customer-third-parties.component.ts:55` | `COLUMN_DISPLAY_LABEL` | Entidad destino | Entidad de destino | `destinationInstitutionName` | destinationInstitutionName | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 241 | `features/customer-third-parties/components/customer-third-parties.component.ts:56` | `COLUMN_DISPLAY_LABEL` | Cuenta destino | Cuenta de destino | `destinationAccountNumber` | destinationAccountNumber | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 242 | `features/customer-third-parties/components/customer-third-parties.component.ts:57` | `COLUMN_DISPLAY_LABEL` | Documento receptor | Documento del receptor | `recipientIdNumber` | recipientIdNumber | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 243 | `features/customer-third-parties/components/customer-third-parties.component.ts:58` | `COLUMN_DISPLAY_LABEL` | — | Estado | `statusLabel` | statusLabel | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 244 | `features/customer-third-parties/components/customer-third-parties.component.ts:59` | `COLUMN_DISPLAY_LABEL` | ID prenotificación | ID de prenotificación | `prenotificationTransactionId` | prenotificationTransactionId | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 245 | `features/customer-third-parties/components/customer-third-parties.component.ts:60` | `COLUMN_DISPLAY_LABEL` | Ciclo validación | Ciclo de validación | `validationCycleId` | validationCycleId | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 246 | `features/customer-third-parties/components/customer-third-parties.component.ts:61` | `COLUMN_DISPLAY_LABEL` | Fecha validación | Fecha de validación | `validationReceivedAtDisplay` | validationReceivedAtDisplay | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 247 | `features/customer-third-parties/components/customer-third-parties.component.ts:62` | `COLUMN_DISPLAY_LABEL` | — | Resultado automático | `validationMessage` | validationMessage | No (shared table) | No (shared table) | No identificado | **ALIGNED** |
| 248 | `features/customers/components/customers-list.component.ts:31` | `GRID_HEADER_NAME` | — | Documento | `documento` | documento | Sí | Sí | No identificado | **ALIGNED** |
| 249 | `features/customers/components/customers-list.component.ts:32` | `GRID_HEADER_NAME` | — | Nombre | `nombre` | nombre | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 250 | `features/customers/components/customers-list.component.ts:33` | `GRID_HEADER_NAME` | — | Cuenta | `cuenta` | cuenta | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 251 | `features/customers/components/customers-list.component.ts:34` | `GRID_HEADER_NAME` | — | Tipo persona | `tipoPersona` | tipoPersona | Sí | Sí | No identificado | **ALIGNED** |
| 252 | `features/customers/components/customers-list.component.ts:35` | `GRID_HEADER_NAME` | — | Razón social | `razonSocial` | razonSocial | Sí | Sí | No identificado | **ALIGNED** |
| 253 | `features/customers/components/customers-list.component.ts:38` | `GRID_HEADER_NAME` | — | Acciones | `acciones` | acciones | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 254 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:128` | `HTML_TH` | — | Nombre | `plain-th-1` | plain-th-1 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 255 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:129` | `HTML_TH` | — | Identidad | `plain-th-2` | plain-th-2 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 256 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:130` | `HTML_TH` | — | Estado | `plain-th-3` | plain-th-3 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 257 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:131` | `HTML_TH` | — | Cuentas | `plain-th-4` | plain-th-4 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 258 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:132` | `HTML_TH` | — | Actualizado | `plain-th-5` | plain-th-5 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 259 | `features/customers/third-party-administration/customer-third-parties-admin.component.html:133` | `HTML_TH` | — |  | `plain-th-6` | plain-th-6 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 260 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — | Institución | `plain-th-1` | plain-th-1 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 261 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — | Cuenta | `plain-th-2` | plain-th-2 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 262 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — | Tipo | `plain-th-3` | plain-th-3 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 263 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — | Estado | `plain-th-4` | plain-th-4 | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 264 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — | Validaciones | `plain-th-5` | plain-th-5 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 265 | `features/customers/third-party-administration/third-party-admin-detail.component.html:117` | `HTML_TH` | — |  | `plain-th-6` | plain-th-6 | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 266 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:41` | `GRID_HEADER_NAME` | — | Estado | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 267 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:42` | `GRID_HEADER_NAME` | — | Cantidad | `count` | count | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 268 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:46` | `GRID_HEADER_NAME` | Código error | Código de error | `errorCode` | errorCode | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 269 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:47` | `GRID_HEADER_NAME` | — | Frecuencia | `count` | count | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 270 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:48` | `GRID_HEADER_NAME` | — | Último visto | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 271 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:52` | `GRID_HEADER_NAME` | — | Cámara | `clearingHouseId` | clearingHouseId | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 272 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:53` | `GRID_HEADER_NAME` | — | Ciclo | `achCycleId` | achCycleId | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 273 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:54` | `GRID_HEADER_NAME` | — | Total | `totalItems` | totalItems | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 274 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:55` | `GRID_HEADER_NAME` | — | Bloqueados | `blockedItems` | blockedItems | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 275 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:56` | `GRID_HEADER_NAME` | — | Pendientes de reintento | `retryPendingItems` | retryPendingItems | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 276 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:57` | `GRID_HEADER_NAME` | — | En espera de ventana | `waitingWindowItems` | waitingWindowItems | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 277 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:58` | `GRID_HEADER_NAME` | — | Errores definitivos | `failedFinalItems` | failedFinalItems | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 278 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:59` | `GRID_HEADER_NAME` | — | Procesados | `confirmedItems` | confirmedItems | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 279 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:63` | `GRID_HEADER_NAME` | — | Hora UTC | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 280 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:64` | `GRID_HEADER_NAME` | — | Eventos | `totalEvents` | totalEvents | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 281 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:65` | `GRID_HEADER_NAME` | — | Acciones aplicadas | `manualApplied` | manualApplied | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 282 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:66` | `GRID_HEADER_NAME` | — | Acciones no aplicadas | `manualRejected` | manualRejected | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 283 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:67` | `GRID_HEADER_NAME` | — | Pendientes de reintento | `retryPendingTransitions` | retryPendingTransitions | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 284 | `features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.ts:68` | `GRID_HEADER_NAME` | — | Errores definitivos | `failedFinalTransitions` | failedFinalTransitions | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 285 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:38` | `MATERIAL_HEADER` | — | Archivo | `archivo` | archivo | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 286 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:39` | `MATERIAL_HEADER` | — | Fecha | `fecha` | fecha | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 287 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:40` | `MATERIAL_HEADER` | — | Valor | `valor` | valor | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 288 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:41` | `MATERIAL_HEADER` | — | Causal | `causal` | causal | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 289 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:42` | `MATERIAL_HEADER` | — | Rastreo original | `rastreo` | rastreo | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 290 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:43` | `MATERIAL_HEADER` | — | Estado | `estado` | estado | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 291 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:44` | `MATERIAL_HEADER` | — | Acción | `accion` | accion | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 292 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:83` | `MATERIAL_HEADER` | — | Seleccionar | `seleccion` | seleccion | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 293 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:84` | `MATERIAL_HEADER` | — | Transacción | `transaccion` | transaccion | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 294 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:85` | `MATERIAL_HEADER` | — | Rastreo | `rastreo` | rastreo | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 295 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:86` | `MATERIAL_HEADER` | — | Fecha | `fecha` | fecha | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 296 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:87` | `MATERIAL_HEADER` | — | Valor | `valor` | valor | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 297 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:88` | `MATERIAL_HEADER` | — | Estado | `estado` | estado | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 298 | `features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html:89` | `MATERIAL_HEADER` | — | Validación | `compatibilidad` | compatibilidad | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 299 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:52` | `GRID_HEADER_NAME` | — | Evento | `eventType` | eventType | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 300 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:53` | `GRID_HEADER_NAME` | — | Estado | `eventStatus` | eventStatus | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 301 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:54` | `GRID_HEADER_NAME` | — | Actor | `raisedBy` | raisedBy | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 302 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:55` | `GRID_HEADER_NAME` | — | Fecha | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 303 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:56` | `GRID_HEADER_NAME` | — | Mensaje | `message` | message | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 304 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:60` | `GRID_HEADER_NAME` | — | Método | `methodName` | methodName | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 305 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:61` | `GRID_HEADER_NAME` | — | Código | `responseCode` | responseCode | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 306 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:62` | `GRID_HEADER_NAME` | — | Respuesta | `responseMessage` | responseMessage | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 307 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:63` | `GRID_HEADER_NAME` | — | Éxito | `isSuccess` | isSuccess | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 308 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:64` | `GRID_HEADER_NAME` | — | Permite reintento | `isRetryable` | isRetryable | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 309 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:65` | `GRID_HEADER_NAME` | — | Inicio | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 310 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts:66` | `GRID_HEADER_NAME` | — | Fin | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 311 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:48` | `GRID_HEADER_NAME` | Tx | ID de transacción ACH | `achTransactionId` | achTransactionId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 312 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:49` | `GRID_HEADER_NAME` | — | Estado del procesamiento | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 313 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:50` | `GRID_HEADER_NAME` | — | Prioridad | `priority` | priority | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 314 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:51` | `GRID_HEADER_NAME` | — | Intentos | `attemptCount` | attemptCount | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 315 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:52` | `GRID_HEADER_NAME` | — | Próximo intento | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 316 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:53` | `GRID_HEADER_NAME` | — | Último error | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 317 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:54` | `GRID_HEADER_NAME` | — | Acciones autorizadas | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 318 | `features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts:56` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 319 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:161` | `MATERIAL_HEADER` | — | Archivo | `fileName` | fileName | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 320 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:172` | `MATERIAL_HEADER` | — | Cámara | `clearingHouse` | clearingHouse | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 321 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:177` | `MATERIAL_HEADER` | — | Fecha operativa | `operationalDate` | operationalDate | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 322 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:182` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | Sí | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 323 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:187` | `MATERIAL_HEADER` | — | Fecha de carga | `uploadedAt` | uploadedAt | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 324 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:192` | `MATERIAL_HEADER` | — | Validación | `validation` | validation | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 325 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:197` | `MATERIAL_HEADER` | — | Procesamiento | `processing` | processing | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 326 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:202` | `MATERIAL_HEADER` | — | Contenido | `counts` | counts | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 327 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:207` | `MATERIAL_HEADER` | — | Totales | `amounts` | amounts | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 328 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:215` | `MATERIAL_HEADER` | — | Resultado general | `result` | result | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 329 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:220` | `MATERIAL_HEADER` | — | Programación | `scheduledAt` | scheduledAt | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 330 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:225` | `MATERIAL_HEADER` | — | Acción | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 331 | `features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html:256` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de archivos NACHA-M | `mat-paginator` | pagination | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 332 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:131` | `MATERIAL_HEADER` | — | Número de lote | `batchNumber` | batchNumber | Sí | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 333 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:132` | `MATERIAL_HEADER` | — | Clase de servicio | `serviceClass` | serviceClass | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 334 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:133` | `MATERIAL_HEADER` | — | Empresa | `company` | company | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 335 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:134` | `MATERIAL_HEADER` | — | Descripción de la operación | `description` | description | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 336 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:135` | `MATERIAL_HEADER` | — | Fecha efectiva | `effectiveDate` | effectiveDate | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 337 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:136` | `MATERIAL_HEADER` | — | Transacciones | `transactions` | transactions | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 338 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:137` | `MATERIAL_HEADER` | — | Total débito | `debit` | debit | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 339 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:138` | `MATERIAL_HEADER` | — | Total crédito | `credit` | credit | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 340 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:139` | `MATERIAL_HEADER` | — | Acción | `action` | action | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 341 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:146` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de lotes | `mat-paginator` | pagination | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 342 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:168` | `MATERIAL_HEADER` | — | Número de rastreo | `trace` | trace | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 343 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:169` | `MATERIAL_HEADER` | — | Código | `code` | code | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 344 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:170` | `MATERIAL_HEADER` | — | Cuenta | `account` | account | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 345 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:171` | `MATERIAL_HEADER` | — | Valor | `amount` | amount | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 346 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:172` | `MATERIAL_HEADER` | — | Clasificación | `classification` | classification | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 347 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:173` | `MATERIAL_HEADER` | — | Estado técnico | `technical` | technical | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 348 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:174` | `MATERIAL_HEADER` | — | Resultado funcional | `outcome` | outcome | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 349 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:175` | `MATERIAL_HEADER` | — | Código y causal | `result` | result | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 350 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:176` | `MATERIAL_HEADER` | — | Servicio utilizado | `service` | service | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 351 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:177` | `MATERIAL_HEADER` | — | Intentos | `attempts` | attempts | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 352 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:178` | `MATERIAL_HEADER` | — | Procesamiento | `processedAt` | processedAt | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 353 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:179` | `MATERIAL_HEADER` | — | Acción | `action` | action | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 354 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:186` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de transacciones | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 355 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:211` | `MATERIAL_HEADER` | — | Transacción | `transaction` | transaction | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 356 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:212` | `MATERIAL_HEADER` | — | Estado | `status` | status | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 357 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:213` | `MATERIAL_HEADER` | — | Fecha programada | `scheduled` | scheduled | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 358 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:214` | `MATERIAL_HEADER` | — | Intentos | `attempts` | attempts | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 359 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:215` | `MATERIAL_HEADER` | — | Próximo intento | `nextAttempt` | nextAttempt | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 360 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:216` | `MATERIAL_HEADER` | — | Servicio | `service` | service | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 361 | `features/incoming-nacha-command-center/pages/nacha-operational-file-detail.component.html:217` | `MATERIAL_HEADER` | — | Novedad técnica | `error` | error | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 362 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:81` | `GRID_HEADER_NAME` | — | Código | `profileCode` | profileCode | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 363 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:82` | `GRID_HEADER_NAME` | — | Nombre | `profileName` | profileName | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 364 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:83` | `GRID_HEADER_NAME` | — | Cámara | `clearingHouseCode` | clearingHouseCode | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 365 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:84` | `GRID_HEADER_NAME` | — | Estado | `status` | status | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 366 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:85` | `GRID_HEADER_NAME` | — | Versión | `version` | version | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 367 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:86` | `GRID_HEADER_NAME` | — | Variantes | `layoutVariantCount` | layoutVariantCount | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 368 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:87` | `GRID_HEADER_NAME` | — | Campos | `fieldCount` | fieldCount | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 369 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:88` | `GRID_HEADER_NAME` | — | Registros | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 370 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:90` | `GRID_HEADER_NAME` | — | Vigencia | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 371 | `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts:95` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 372 | `features/nacha-config-admin/workspace/nacha-config-field-list.component.html:10` | `MATERIAL_HEADER` | — | Campo | `field` | field | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 373 | `features/nacha-config-admin/workspace/nacha-config-field-list.component.html:15` | `MATERIAL_HEADER` | — | Variante | `variant` | variant | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 374 | `features/nacha-config-admin/workspace/nacha-config-field-list.component.html:20` | `MATERIAL_HEADER` | — | Posición | `position` | position | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 375 | `features/nacha-config-admin/workspace/nacha-config-field-list.component.html:25` | `MATERIAL_HEADER` | — | Longitud | `length` | length | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 376 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:36` | `GRID_HEADER_NAME` | ID | ID de conciliación | `reconciliationId` | reconciliationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 377 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:37` | `GRID_HEADER_NAME` | CorrelationId | ID de correlación | `correlationId` | correlationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 378 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:38` | `GRID_HEADER_NAME` | — | Archivo | `fileName` | fileName | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 379 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:39` | `GRID_HEADER_NAME` | — | Cámara | `clearingHouseCode` | clearingHouseCode | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 380 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:40` | `GRID_HEADER_NAME` | — | Flujo | `flowType` | flowType | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 381 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:41` | `GRID_HEADER_NAME` | — | Respuesta | `responseType` | responseType | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 382 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:42` | `GRID_HEADER_NAME` | — | Causal | `reasonCode` | reasonCode | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 383 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:43` | `GRID_HEADER_NAME` | Trace | Número de rastreo | `traceNumberMasked` | traceNumberMasked | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 384 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:44` | `GRID_HEADER_NAME` | — | Estado interno | `internalStatus` | internalStatus | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 385 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:45` | `GRID_HEADER_NAME` | — | Conciliación | `reconciliationStatus` | reconciliationStatus | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 386 | `features/nacha-operational/pages/ach-reconciliation-console.component.ts:46` | `GRID_HEADER_NAME` | SOAP candidato | Operación SOAP candidata | `soapOperationCandidate` | soapOperationCandidate | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 387 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:28` | `GRID_HEADER_NAME` | CorrelationId | ID de correlación | `correlationId` | correlationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 388 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:29` | `GRID_HEADER_NAME` | — | Archivo | `fileName` | fileName | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 389 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:30` | `GRID_HEADER_NAME` | Trace | Número de rastreo | `entryTraceNumber` | entryTraceNumber | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 390 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:31` | `GRID_HEADER_NAME` | Decision | Decisión | `decisionType` | decisionType | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 391 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:32` | `GRID_HEADER_NAME` | — | Operación | `operationCandidate` | operationCandidate | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 392 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:33` | `GRID_HEADER_NAME` | Monetario | Movimiento monetario | `requiresMonetaryMovement` | requiresMonetaryMovement | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 393 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:34` | `GRID_HEADER_NAME` | — | Preparación | `readinessStatus` | readinessStatus | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 394 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:35` | `GRID_HEADER_NAME` | — | Simulación | `simulationStatus` | simulationStatus | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 395 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:36` | `GRID_HEADER_NAME` | — | Resiliencia | `resilienceStatus` | resilienceStatus | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 396 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:37` | `GRID_HEADER_NAME` | — | Idempotencia | `idempotencyStatus` | idempotencyStatus | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 397 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:38` | `GRID_HEADER_NAME` | — | Intentos | `attemptCount` | attemptCount | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 398 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:42` | `GRID_HEADER_NAME` | CorrelationId | ID de correlación | `correlationId` | correlationId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 399 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:43` | `GRID_HEADER_NAME` | — | Fase | `phase` | phase | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 400 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:44` | `GRID_HEADER_NAME` | — | Evento | `eventType` | eventType | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 401 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:45` | `GRID_HEADER_NAME` | — | Severidad | `severity` | severity | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 402 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:46` | `GRID_HEADER_NAME` | — | Mensaje | `message` | message | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 403 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:47` | `GRID_HEADER_NAME` | — | Bloqueado | `isBlocked` | isBlocked | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 404 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:48` | `GRID_HEADER_NAME` | — | Marca temporal | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 405 | `features/nacha-operational/pages/nacha-soap-uat-console.component.ts:49` | `GRID_HEADER_NAME` | — | Detalles sanitizados | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 406 | `features/navigation-logs/components/navigation-log.component.ts:38` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `visitedDateDisplay` | visitedDateDisplay | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 407 | `features/navigation-logs/components/navigation-log.component.ts:39` | `COLUMN_DISPLAY_LABEL` | — | Hora | `visitedTimeDisplay` | visitedTimeDisplay | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 408 | `features/navigation-logs/components/navigation-log.component.ts:40` | `COLUMN_DISPLAY_LABEL` | — | Usuario | `userDisplay` | userDisplay | No (shared table) | No (shared table) | No identificado | **CONTEXTUAL_COLUMN** |
| 409 | `features/navigation-logs/components/navigation-log.component.ts:41` | `COLUMN_DISPLAY_LABEL` | — | Ruta | `route` | route | No (shared table) | No (shared table) | No identificado | **ALIGNED** |
| 410 | `features/navigation-logs/components/navigation-log.component.ts:42` | `COLUMN_DISPLAY_LABEL` | — | Duración | `durationDisplay` | durationDisplay | No (shared table) | No (shared table) | No identificado | **ALIGNED** |
| 411 | `features/navigation-logs/components/navigation-log.component.ts:43` | `COLUMN_DISPLAY_LABEL` | — | Sesión | `sessionId` | sessionId | No (shared table) | No (shared table) | No identificado | **ALIGNED** |
| 412 | `features/navigation-logs/components/navigation-log.component.ts:44` | `COLUMN_DISPLAY_LABEL` | — | IP | `ipAddress` | ipAddress | No (shared table) | No (shared table) | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 413 | `features/navigation-logs/components/navigation-log.component.ts:45` | `COLUMN_DISPLAY_LABEL` | User agent | User-Agent | `userAgent` | userAgent | No (shared table) | No (shared table) | No identificado | **DRIFT** |
| 414 | `features/operational-restrictions/operational-restrictions.component.html:129` | `TABLE_ACCESSIBLE_NAME` | Restricciones operacionales CFA | Restricciones operativas | `table` | table | N/A | N/A | No | **DRIFT** |
| 415 | `features/operational-restrictions/operational-restrictions.component.html:131` | `MATERIAL_HEADER` | — | Ámbito | `scope` | scope | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 416 | `features/operational-restrictions/operational-restrictions.component.html:139` | `MATERIAL_HEADER` | — | Aplicación | `transaction` | transaction | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 417 | `features/operational-restrictions/operational-restrictions.component.html:147` | `MATERIAL_HEADER` | — | Límites | `limits` | limits | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 418 | `features/operational-restrictions/operational-restrictions.component.html:156` | `MATERIAL_HEADER` | — | Tipos de cuenta | `accounts` | accounts | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 419 | `features/operational-restrictions/operational-restrictions.component.html:163` | `MATERIAL_HEADER` | — | Vigencia | `validity` | validity | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 420 | `features/operational-restrictions/operational-restrictions.component.html:171` | `MATERIAL_HEADER` | — | Estado | `state` | state | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 421 | `features/operational-restrictions/operational-restrictions.component.html:176` | `MATERIAL_HEADER` | — | Actualizada | `updated` | updated | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 422 | `features/operational-restrictions/operational-restrictions.component.html:181` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 423 | `features/operational-restrictions/operational-restrictions.component.html:230` | `PAGINATION_ACCESSIBLE_COPY` | Paginación de restricciones operacionales CFA | Paginación de restricciones operativas | `mat-paginator` | pagination | N/A | N/A | No | **DRIFT** |
| 424 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:41` | `GRID_HEADER_NAME` | — | Riel | `railCode` | railCode | Sí | Sí | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 425 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:42` | `GRID_HEADER_NAME` | Capability | Capacidad | `capabilityCode` | capabilityCode | Sí | Sí | No identificado | **DRIFT** |
| 426 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:43` | `GRID_HEADER_NAME` | — | Estado efectivo | `state` | state | Sí | Sí | No identificado | **ALIGNED** |
| 427 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:44` | `GRID_HEADER_NAME` | — | Origen | `source` | source | Sí | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 428 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:45` | `GRID_HEADER_NAME` | Vigencia desde (UTC) | Vigencia inicial (UTC) | `effectiveFromUtc` | effectiveFromUtc | Sí | Hereda grid | No identificado | **DRIFT** |
| 429 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:46` | `GRID_HEADER_NAME` | Vigencia hasta (UTC) | Vigencia final (UTC) | `effectiveToUtc` | effectiveToUtc | Sí | Hereda grid | No identificado | **DRIFT** |
| 430 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:47` | `GRID_HEADER_NAME` | — | Versión | `version` | version | Sí | Hereda grid | No identificado | **ALIGNED** |
| 431 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:48` | `GRID_HEADER_NAME` | Fuente cambio | Fuente del cambio | `changeSource` | changeSource | Sí | Hereda grid | No identificado | **DRIFT** |
| 432 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:49` | `GRID_HEADER_NAME` | Ticket cambio | Ticket de cambio | `changeTicket` | changeTicket | Sí | Hereda grid | No identificado | **DRIFT** |
| 433 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:50` | `GRID_HEADER_NAME` | — | Cambiado por | `changedBy` | changedBy | Sí | Hereda grid | No identificado | **ALIGNED** |
| 434 | `features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts:51` | `GRID_HEADER_NAME` | — | Cambio en (UTC) | `changedAtUtc` | changedAtUtc | Sí | Hereda grid | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 435 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Monto | `amount` | amount | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 436 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Banco de destino | `destinationBankName` | destinationBankName | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 437 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `effectiveEntryDate` | effectiveEntryDate | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 438 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Referencia anterior | `reference` | reference | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 439 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Banco de origen | `sourceBankName` | sourceBankName | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 440 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | — | Estado | `state` | state | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 441 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | Identificador de operación | ID de operación | `transactionExternalId` | transactionExternalId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 442 | `features/reports/components/report-list-page.component.ts:196` | `COLUMN_DISPLAY_LABEL` | Identificador | ID de transacción | `transactionId` | transactionId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 443 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Monto | `amount` | amount | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 444 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Banco de destino | `destinationBankName` | destinationBankName | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 445 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `effectiveEntryDate` | effectiveEntryDate | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 446 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Referencia anterior | `reference` | reference | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 447 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Banco de origen | `sourceBankName` | sourceBankName | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 448 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | — | Estado | `state` | state | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 449 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | Identificador de operación | ID de operación | `transactionExternalId` | transactionExternalId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 450 | `features/reports/components/report-list-page.component.ts:199` | `COLUMN_DISPLAY_LABEL` | Identificador | ID de transacción | `transactionId` | transactionId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 451 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | — | Monto | `amount` | amount | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 452 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | — | Causal | `causalCode` | causalCode | No (shared table) | No (shared table) | Backend PDF independiente | **DOMAIN_TERM_LEGITIMATE** |
| 453 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `effectiveEntryDate` | effectiveEntryDate | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 454 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | — | Referencia anterior | `reference` | reference | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 455 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | — | Estado | `state` | state | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 456 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | Identificador de operación | ID de operación | `transactionExternalId` | transactionExternalId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 457 | `features/reports/components/report-list-page.component.ts:202` | `COLUMN_DISPLAY_LABEL` | Identificador | ID de transacción | `transactionId` | transactionId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 458 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | — | Monto | `amount` | amount | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 459 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | — | Causal | `causalCode` | causalCode | No (shared table) | No (shared table) | Backend PDF independiente | **DOMAIN_TERM_LEGITIMATE** |
| 460 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `effectiveEntryDate` | effectiveEntryDate | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 461 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | — | Referencia anterior | `reference` | reference | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 462 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | — | Estado | `state` | state | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 463 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | Identificador de operación | ID de operación | `transactionExternalId` | transactionExternalId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 464 | `features/reports/components/report-list-page.component.ts:205` | `COLUMN_DISPLAY_LABEL` | Identificador | ID de transacción | `transactionId` | transactionId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 465 | `features/reports/components/report-list-page.component.ts:208` | `COLUMN_DISPLAY_LABEL` | — | Cámara | `clearingHouseName` | clearingHouseName | No (shared table) | No (shared table) | Backend PDF independiente | **DOMAIN_TERM_LEGITIMATE** |
| 466 | `features/reports/components/report-list-page.component.ts:208` | `COLUMN_DISPLAY_LABEL` | — | Archivo | `fileName` | fileName | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 467 | `features/reports/components/report-list-page.component.ts:208` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `generatedAtUtc` | generatedAtUtc | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 468 | `features/reports/components/report-list-page.component.ts:208` | `COLUMN_DISPLAY_LABEL` | — | Registros | `totalRecords` | totalRecords | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 469 | `features/reports/components/report-list-page.component.ts:208` | `COLUMN_DISPLAY_LABEL` | — | Transacciones | `totalTransactions` | totalTransactions | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 470 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Nombre | `cycleName` | cycleName | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 471 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Fecha | `processingDate` | processingDate | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 472 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Horario | `schedule` | schedule | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 473 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Estado | `status` | status | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 474 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Monto | `totalAmount` | totalAmount | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 475 | `features/reports/components/report-list-page.component.ts:211` | `COLUMN_DISPLAY_LABEL` | — | Transacciones | `totalTransactions` | totalTransactions | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 476 | `features/reports/components/report-list-page.component.ts:214` | `COLUMN_DISPLAY_LABEL` | — | Acción | `action` | action | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 477 | `features/reports/components/report-list-page.component.ts:214` | `COLUMN_DISPLAY_LABEL` | — | Fecha y hora | `dateUtc` | dateUtc | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 478 | `features/reports/components/report-list-page.component.ts:214` | `COLUMN_DISPLAY_LABEL` | — | Entidad | `entity` | entity | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 479 | `features/reports/components/report-list-page.component.ts:214` | `COLUMN_DISPLAY_LABEL` | Identificador de entidad | ID de entidad | `entityId` | entityId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 480 | `features/reports/components/report-list-page.component.ts:214` | `COLUMN_DISPLAY_LABEL` | — | Usuario | `user` | user | No (shared table) | No (shared table) | Backend PDF independiente | **CONTEXTUAL_COLUMN** |
| 481 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | — | Fecha y hora | `dateUtc` | dateUtc | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 482 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | — | Estado anterior | `fromState` | fromState | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 483 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | — | Causal | `reasonCode` | reasonCode | No (shared table) | No (shared table) | Backend PDF independiente | **DOMAIN_TERM_LEGITIMATE** |
| 484 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | — | Fuente | `source` | source | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 485 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | — | Estado nuevo | `toState` | toState | No (shared table) | No (shared table) | Backend PDF independiente | **ALIGNED** |
| 486 | `features/reports/components/report-list-page.component.ts:217` | `COLUMN_DISPLAY_LABEL` | Transacción | ID de transacción | `transactionId` | transactionId | No (shared table) | No (shared table) | Backend PDF independiente | **DRIFT** |
| 487 | `features/scheduler/components/task-definitions.component.html:34` | `TABLE_ACCESSIBLE_NAME` | — | Tareas programadas | `table` | table | N/A | N/A | No | **ALIGNED** |
| 488 | `features/scheduler/components/task-definitions.component.html:35` | `MATERIAL_HEADER` | — | Tarea | `task` | task | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 489 | `features/scheduler/components/task-definitions.component.html:36` | `MATERIAL_HEADER` | — | Categoría | `category` | category | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 490 | `features/scheduler/components/task-definitions.component.html:37` | `MATERIAL_HEADER` | — | Estado | `status` | status | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 491 | `features/scheduler/components/task-definitions.component.html:38` | `MATERIAL_HEADER` | — | Programación | `schedule` | schedule | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 492 | `features/scheduler/components/task-definitions.component.html:39` | `MATERIAL_HEADER` | — | Última ejecución | `last` | last | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 493 | `features/scheduler/components/task-definitions.component.html:40` | `MATERIAL_HEADER` | — | Próxima ejecución | `next` | next | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 494 | `features/scheduler/components/task-definitions.component.html:41` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 495 | `features/scheduler/components/task-definitions.component.html:58` | `TABLE_ACCESSIBLE_NAME` | — | Historial de ejecuciones | `table` | table | N/A | N/A | No | **ALIGNED** |
| 496 | `features/scheduler/components/task-definitions.component.html:59` | `MATERIAL_HEADER` | — | Inicio | `started` | started | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 497 | `features/scheduler/components/task-definitions.component.html:60` | `MATERIAL_HEADER` | — | Tipo | `type` | type | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 498 | `features/scheduler/components/task-definitions.component.html:61` | `MATERIAL_HEADER` | — | Tarea | `task` | task | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 499 | `features/scheduler/components/task-definitions.component.html:62` | `MATERIAL_HEADER` | — | Resultado | `result` | result | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 500 | `features/scheduler/components/task-definitions.component.html:63` | `MATERIAL_HEADER` | — | Solicitada por | `requestedBy` | requestedBy | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 501 | `features/scheduler/components/task-definitions.component.html:64` | `MATERIAL_HEADER` | — | Duración | `duration` | duration | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 502 | `features/scheduler/components/task-definitions.component.html:65` | `MATERIAL_HEADER` | — | Detalle | `detail` | detail | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 503 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:321` | `GRID_HEADER_NAME` | Traza | Número de rastreo | `traceNumber` | traceNumber | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 504 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:322` | `GRID_HEADER_NAME` | — | Referencia | `reference` | reference | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 505 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:325` | `GRID_HEADER_NAME` | — | Fecha | `effectiveEntryDate` | effectiveEntryDate | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 506 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:331` | `GRID_HEADER_NAME` | DFI origen | DFI de origen | `originatingDfi` | originatingDfi | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 507 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:336` | `GRID_HEADER_NAME` | DFI destino | DFI de destino | `receivingDfi` | receivingDfi | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 508 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:341` | `GRID_HEADER_NAME` | — | Monto | `amount` | amount | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 509 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:347` | `GRID_HEADER_NAME` | Código Tx | Código de transacción | `transactionCode` | transactionCode | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 510 | `features/transactions/components/ach-returns-management/ach-returns-management.component.ts:350` | `GRID_HEADER_NAME` | — | Estado | `isEligible` | isEligible | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 511 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:47` | `GRID_HEADER_NAME` | — | # | `attemptNumber` | attemptNumber | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 512 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:48` | `GRID_HEADER_NAME` | — | Tipo | `triggerType` | triggerType | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 513 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:49` | `GRID_HEADER_NAME` | — | Alcance | `scope` | scope | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 514 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:50` | `GRID_HEADER_NAME` | — | Estado | `status` | status | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 515 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:51` | `GRID_HEADER_NAME` | Disparado por | Iniciado por | `triggeredBy` | triggeredBy | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 516 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:52` | `GRID_HEADER_NAME` | — | Fecha | `triggeredAtUtc` | triggeredAtUtc | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 517 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:53` | `GRID_HEADER_NAME` | — | Resultado | `resultMessage` | resultMessage | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 518 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:56` | `GRID_HEADER_NAME` | — | # | `itemIndex` | itemIndex | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 519 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:57` | `GRID_HEADER_NAME` | — | Referencia | `reference` | reference | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 520 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:58` | `GRID_HEADER_NAME` | — | Estado | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 521 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:59` | `GRID_HEADER_NAME` | ID transacción | ID de transacción | `transactionId` | transactionId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 522 | `features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts:60` | `GRID_HEADER_NAME` | — | Mensaje | `message` | message | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 523 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:44` | `GRID_HEADER_NAME` | — | Referencia | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 524 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:48` | `GRID_HEADER_NAME` | — | Estado | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 525 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:49` | `GRID_HEADER_NAME` | Fecha carga | Fecha de carga | `uploadedAtUtc` | uploadedAtUtc | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 526 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:50` | `GRID_HEADER_NAME` | — | Total | `totalRecords` | totalRecords | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 527 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:51` | `GRID_HEADER_NAME` | — | Progreso | `progressPercent` | progressPercent | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 528 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:52` | `GRID_HEADER_NAME` | — | Éxitos | `totalSucceeded` | totalSucceeded | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 529 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:53` | `GRID_HEADER_NAME` | — | Fallos | `totalFailed` | totalFailed | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 530 | `features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts:55` | `GRID_HEADER_NAME` | — | Acciones | `dynamic-header` | dynamic-header | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 531 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:107` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | Sí | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 532 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:111` | `MATERIAL_HEADER` | — | Ventana operativa | `window` | window | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 533 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:115` | `MATERIAL_HEADER` | — | Corte de entrada | `cutoff` | cutoff | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 534 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:119` | `MATERIAL_HEADER` | — | Etapas | `stages` | stages | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 535 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:123` | `MATERIAL_HEADER` | — | Clases habilitadas | `eligibility` | eligibility | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 536 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:131` | `MATERIAL_HEADER` | — | Vigencia | `validity` | validity | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 537 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:135` | `MATERIAL_HEADER` | — | Estado | `state` | state | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 538 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:139` | `MATERIAL_HEADER` | — | Última modificación | `updated` | updated | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 539 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:143` | `MATERIAL_HEADER` | — | Acciones | `actions` | actions | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 540 | `features/transactions/components/cycle-config-management/cycle-config-management.component.html:188` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de configuraciones de ciclos | `mat-paginator` | pagination | N/A | N/A | No | **DOMAIN_TERM_LEGITIMATE** |
| 541 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:45` | `GRID_HEADER_NAME` | Nacha ID | ID del archivo NACHA-M | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 542 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:46` | `GRID_HEADER_NAME` | — | Originador | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 543 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:47` | `GRID_HEADER_NAME` | — | Receptor | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 544 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:48` | `GRID_HEADER_NAME` | — | Cámara | `clearingHouseName` | clearingHouseName | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 545 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:49` | `GRID_HEADER_NAME` | — | Ciclo | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 546 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:50` | `GRID_HEADER_NAME` | Fecha archivo | Fecha del archivo | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 547 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:51` | `GRID_HEADER_NAME` | Hora | Hora del archivo | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 548 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:52` | `GRID_HEADER_NAME` | — | Lotes | `totalBatches` | totalBatches | Hereda grid | Hereda grid | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 549 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:53` | `GRID_HEADER_NAME` | — | Entradas | `totalEntries` | totalEntries | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 550 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:54` | `GRID_HEADER_NAME` | — | Adendas | `totalAddendas` | totalAddendas | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 551 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:55` | `GRID_HEADER_NAME` | Total monto | Monto total | `totalAmount` | totalAmount | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 552 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:56` | `GRID_HEADER_NAME` | — | Total débito | `totalDebitAmount` | totalDebitAmount | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 553 | `features/transactions/components/nacha-upload/nacha-upload.component.ts:57` | `GRID_HEADER_NAME` | — | Total crédito | `totalCreditAmount` | totalCreditAmount | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 554 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:42` | `GRID_HEADER_NAME` | — | # | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 555 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:43` | `GRID_HEADER_NAME` | ID operación | ID de operación | `transactionExternalId` | transactionExternalId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 556 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:44` | `GRID_HEADER_NAME` | Referencia legado | Referencia heredada | `reference` | reference | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 557 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:45` | `GRID_HEADER_NAME` | — | Estado | `statusLabel` | statusLabel | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 558 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:46` | `GRID_HEADER_NAME` | ID transacción | ID de transacción | `transactionId` | transactionId | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 559 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:47` | `GRID_HEADER_NAME` | Código error | Código de error | `errorCode` | errorCode | Hereda grid | Hereda grid | No identificado | **DRIFT** |
| 560 | `features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.ts:48` | `GRID_HEADER_NAME` | — | Detalle | `dynamic-header` | dynamic-header | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 561 | `features/transactions/components/transaction-list/transaction-list.component.html:142` | `OTHER_TABULAR_COPY` | — | Selector de columnas para la grilla | `column-manager` | column chooser | N/A | N/A | No | **ALIGNED** |
| 562 | `features/transactions/components/transaction-list/transaction-list.component.html:144` | `OTHER_TABULAR_COPY` | — | Columnas visibles (útil en móvil) | `column-manager-summary` | column chooser | N/A | N/A | No | **ALIGNED** |
| 563 | `features/transactions/components/transaction-list/transaction-list.component.ts:117` | `GRID_HEADER_NAME` | — | ID | `id` | id | Sí | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 564 | `features/transactions/components/transaction-list/transaction-list.component.ts:118` | `GRID_HEADER_NAME` | ID operación | ID de operación | `transactionExternalId` | transactionExternalId | Sí | Sí | No identificado | **DRIFT** |
| 565 | `features/transactions/components/transaction-list/transaction-list.component.ts:119` | `GRID_HEADER_NAME` | Referencia legado | Referencia heredada | `reference` | reference | Sí | Sí | No identificado | **DRIFT** |
| 566 | `features/transactions/components/transaction-list/transaction-list.component.ts:120` | `GRID_HEADER_NAME` | — | Tipo | `typeLabel` | typeLabel | Hereda grid | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 567 | `features/transactions/components/transaction-list/transaction-list.component.ts:121` | `GRID_HEADER_NAME` | — | Naturaleza | `transactionNatureLabel` | transactionNatureLabel | Hereda grid | Sí | No identificado | **ALIGNED** |
| 568 | `features/transactions/components/transaction-list/transaction-list.component.ts:122` | `GRID_HEADER_NAME` | Gestión devolución | Gestión de devolución | `returnManagementLabel` | returnManagementLabel | Hereda grid | Sí | No identificado | **DRIFT** |
| 569 | `features/transactions/components/transaction-list/transaction-list.component.ts:123` | `GRID_HEADER_NAME` | — | Ciclo | `achCycleName` | achCycleName | Hereda grid | Sí | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 570 | `features/transactions/components/transaction-list/transaction-list.component.ts:124` | `GRID_HEADER_NAME` | — | Cámara | `clearingHouseName` | clearingHouseName | Hereda grid | Sí | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 571 | `features/transactions/components/transaction-list/transaction-list.component.ts:125` | `GRID_HEADER_NAME` | — | Monto | `amountText` | amountText | Hereda grid | Hereda grid | No identificado | **CONTEXTUAL_COLUMN** |
| 572 | `features/transactions/components/transaction-list/transaction-list.component.ts:126` | `GRID_HEADER_NAME` | Cuenta origen | Cuenta de origen | `sourceAccountNumber` | sourceAccountNumber | Hereda grid | Sí | No identificado | **DRIFT** |
| 573 | `features/transactions/components/transaction-list/transaction-list.component.ts:127` | `GRID_HEADER_NAME` | Cuenta destino | Cuenta de destino | `destinationAccountNumber` | destinationAccountNumber | Hereda grid | Sí | No identificado | **DRIFT** |
| 574 | `features/transactions/components/transaction-list/transaction-list.component.ts:128` | `GRID_HEADER_NAME` | Institución destino | Institución de destino | `destinationInstitutionName` | destinationInstitutionName | Hereda grid | Sí | No identificado | **DRIFT** |
| 575 | `features/transactions/components/transaction-list/transaction-list.component.ts:129` | `GRID_HEADER_NAME` | — | Fecha efectiva | `effectiveEntryDateText` | effectiveEntryDateText | Hereda grid | Hereda grid | No identificado | **ALIGNED** |
| 576 | `features/transactions/components/transaction-list/transaction-list.component.ts:133` | `COLUMN_DISPLAY_LABEL` | ID operación | ID de operación | `transactionExternalId` | transactionExternalId | N/A selector | N/A selector | No | **DRIFT** |
| 577 | `features/transactions/components/transaction-list/transaction-list.component.ts:134` | `COLUMN_DISPLAY_LABEL` | Referencia legado | Referencia heredada | `reference` | reference | N/A selector | N/A selector | No | **DRIFT** |
| 578 | `features/transactions/components/transaction-list/transaction-list.component.ts:135` | `COLUMN_DISPLAY_LABEL` | — | Tipo | `typeLabel` | typeLabel | N/A selector | N/A selector | No | **CONTEXTUAL_COLUMN** |
| 579 | `features/transactions/components/transaction-list/transaction-list.component.ts:136` | `COLUMN_DISPLAY_LABEL` | — | Naturaleza | `transactionNatureLabel` | transactionNatureLabel | N/A selector | N/A selector | No | **ALIGNED** |
| 580 | `features/transactions/components/transaction-list/transaction-list.component.ts:137` | `COLUMN_DISPLAY_LABEL` | Gestión devolución | Gestión de devolución | `returnManagementLabel` | returnManagementLabel | N/A selector | N/A selector | No | **DRIFT** |
| 581 | `features/transactions/components/transaction-list/transaction-list.component.ts:138` | `COLUMN_DISPLAY_LABEL` | — | Ciclo | `achCycleName` | achCycleName | N/A selector | N/A selector | No | **DOMAIN_TERM_LEGITIMATE** |
| 582 | `features/transactions/components/transaction-list/transaction-list.component.ts:139` | `COLUMN_DISPLAY_LABEL` | — | Cámara | `clearingHouseName` | clearingHouseName | N/A selector | N/A selector | No | **DOMAIN_TERM_LEGITIMATE** |
| 583 | `features/transactions/components/transaction-list/transaction-list.component.ts:140` | `COLUMN_DISPLAY_LABEL` | — | Monto | `amountText` | amountText | N/A selector | N/A selector | No | **CONTEXTUAL_COLUMN** |
| 584 | `features/transactions/components/transaction-list/transaction-list.component.ts:141` | `COLUMN_DISPLAY_LABEL` | Cuenta origen | Cuenta de origen | `sourceAccountNumber` | sourceAccountNumber | N/A selector | N/A selector | No | **DRIFT** |
| 585 | `features/transactions/components/transaction-list/transaction-list.component.ts:142` | `COLUMN_DISPLAY_LABEL` | Cuenta destino | Cuenta de destino | `destinationAccountNumber` | destinationAccountNumber | N/A selector | N/A selector | No | **DRIFT** |
| 586 | `features/transactions/components/transaction-list/transaction-list.component.ts:143` | `COLUMN_DISPLAY_LABEL` | Institución destino | Institución de destino | `destinationInstitutionName` | destinationInstitutionName | N/A selector | N/A selector | No | **DRIFT** |
| 587 | `features/transactions/components/transaction-list/transaction-list.component.ts:144` | `COLUMN_DISPLAY_LABEL` | — | Fecha efectiva | `effectiveEntryDateText` | effectiveEntryDateText | N/A selector | N/A selector | No | **ALIGNED** |
| 588 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:172` | `MATERIAL_HEADER` | — | Fecha | `createdAt` | createdAt | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 589 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:173` | `MATERIAL_HEADER` | — | Identificador | `identifier` | identifier | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 590 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:174` | `MATERIAL_HEADER` | — | Cámara | `clearingHouse` | clearingHouse | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 591 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:175` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 592 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:176` | `MATERIAL_HEADER` | Entidad destino | Entidad de destino | `destination` | destination | No | Sin cambio / externo | No identificado | **DRIFT** |
| 593 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:177` | `MATERIAL_HEADER` | — | Valor | `amount` | amount | Sí | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 594 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:178` | `MATERIAL_HEADER` | — | Proceso | `process` | process | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 595 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:179` | `MATERIAL_HEADER` | — | Resultado | `result` | result | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 596 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:180` | `MATERIAL_HEADER` | — | Situación posterior | `subsequent` | subsequent | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 597 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:181` | `MATERIAL_HEADER` | — | Última actualización | `updated` | updated | Sí | Sin cambio / externo | No identificado | **ALIGNED** |
| 598 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:182` | `MATERIAL_HEADER` | — | Acción | `action` | action | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 599 | `features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html:194` | `PAGINATION_ACCESSIBLE_COPY` | — | Paginación de transacciones | `mat-paginator` | pagination | N/A | N/A | No | **ALIGNED** |
| 600 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:428` | `TABLE_ACCESSIBLE_NAME` | — | Operaciones elegibles. La tabla admite desplazamiento horizontal. | `table-region` | table region | N/A | N/A | No | **ALIGNED** |
| 601 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:433` | `TABLE_ACCESSIBLE_NAME` | — | Operaciones CFA elegibles | `table` | table | N/A | N/A | No | **ALIGNED** |
| 602 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:435` | `MATERIAL_HEADER` | — | Seleccionar | `select` | select | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 603 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:446` | `MATERIAL_HEADER` | — | Transacción | `identifier` | identifier | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 604 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:450` | `MATERIAL_HEADER` | — | Rastreo original | `traceNumber` | traceNumber | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 605 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:454` | `MATERIAL_HEADER` | — | Institución | `institution` | institution | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 606 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:458` | `MATERIAL_HEADER` | — | Tipo | `transactionType` | transactionType | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 607 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:462` | `MATERIAL_HEADER` | — | Fecha | `effectiveDate` | effectiveDate | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 608 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:466` | `MATERIAL_HEADER` | — | Ciclo | `cycle` | cycle | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 609 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:470` | `MATERIAL_HEADER` | — | Cuenta | `account` | account | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 610 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:474` | `MATERIAL_HEADER` | — | Valor | `amount` | amount | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 611 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:480` | `MATERIAL_HEADER` | — | Estado | `state` | state | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 612 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:484` | `MATERIAL_HEADER` | — | Elegibilidad | `eligibility` | eligibility | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 613 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:713` | `TABLE_ACCESSIBLE_NAME` | — | Simulaciones recientes. La tabla admite desplazamiento horizontal. | `table-region` | table region | N/A | N/A | No | **ALIGNED** |
| 614 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:718` | `TABLE_ACCESSIBLE_NAME` | — | Simulaciones recientes | `table` | table | N/A | N/A | No | **ALIGNED** |
| 615 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:720` | `MATERIAL_HEADER` | — | ID | `id` | id | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 616 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:724` | `MATERIAL_HEADER` | — | Cámara | `clearingHouse` | clearingHouse | No | Sin cambio / externo | No identificado | **DOMAIN_TERM_LEGITIMATE** |
| 617 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:728` | `MATERIAL_HEADER` | — | Escenario | `scenario` | scenario | No | Sin cambio / externo | No identificado | **ALIGNED** |
| 618 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:732` | `MATERIAL_HEADER` | — | Archivo | `file` | file | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 619 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:736` | `MATERIAL_HEADER` | — | SHA-256 | `sha256` | sha256 | No | Sin cambio / externo | No identificado | **TECHNICAL_TERM_LEGITIMATE** |
| 620 | `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html:742` | `MATERIAL_HEADER` | — | Acción | `action` | action | No | Sin cambio / externo | No identificado | **CONTEXTUAL_COLUMN** |
| 621 | `shared/components/table.component.ts:59` | `GRID_HEADER_NAME` | — | column.label | `column.label` | column.label | Sí | Sí | No identificado | **DYNAMIC_DATA** |
| 622 | `shared/components/table.component.ts:70` | `GRID_HEADER_NAME` | — | Acciones | `acciones` | acciones | No | Sí | No identificado | **CONTEXTUAL_COLUMN** |
| 623 | `shared/components/ui/ui-grilla-empresarial.component.ts:116` | `PAGINATION_ACCESSIBLE_COPY` | — | Página | `page` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 624 | `shared/components/ui/ui-grilla-empresarial.component.ts:117` | `PAGINATION_ACCESSIBLE_COPY` | — | Filas por página | `pageSizeSelectorLabel` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 625 | `shared/components/ui/ui-grilla-empresarial.component.ts:118` | `PAGINATION_ACCESSIBLE_COPY` | — | a | `to` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 626 | `shared/components/ui/ui-grilla-empresarial.component.ts:119` | `PAGINATION_ACCESSIBLE_COPY` | — | de | `of` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 627 | `shared/components/ui/ui-grilla-empresarial.component.ts:120` | `PAGINATION_ACCESSIBLE_COPY` | — | Siguiente | `next` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 628 | `shared/components/ui/ui-grilla-empresarial.component.ts:121` | `PAGINATION_ACCESSIBLE_COPY` | — | Última | `last` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 629 | `shared/components/ui/ui-grilla-empresarial.component.ts:122` | `PAGINATION_ACCESSIBLE_COPY` | — | Primera | `first` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |
| 630 | `shared/components/ui/ui-grilla-empresarial.component.ts:123` | `PAGINATION_ACCESSIBLE_COPY` | — | Anterior | `previous` | AG Grid pagination locale | N/A | N/A | No | **ALIGNED** |

## Decisiones focales

- Se conserva `Acciones`/`Acción` como encabezado cuando sólo identifica la columna; **no** se modificó ningún control dentro de esas celdas.
- `ID` se expande a `ID de …` sólo cuando el binding demuestra qué identificador representa; IDs técnicos relevantes no se ocultan.
- Se preservan `NACHA-M`, `ACH`, `SOAP`, `DFI`, `Routing`, `Transit`, `User-Agent`, `IP`, `SHA-256`, `UTC` y otros términos técnicos donde describen el dato real.
- Valores de estado, chips, badges, datos de backend y contenido de celdas no se humanizaron.
- Paginación local ya estaba mayoritariamente alineada; sólo `Paginación de restricciones operacionales CFA` se corrigió junto con el nombre accesible de su tabla.
- El copy global de paginación Material y AG Grid se inventarió y quedó sin cambios porque ya está alineado.
