# Análisis SPA Angular para Command Center de Respuestas ACH

## 1) Stack detectado

SPA localizada en `web/ach-interbank-ui` con stack actual:

- Angular 21.2.7
- TypeScript 5.9.2
- RxJS 7.8.2
- AG Grid 32.3.9
- Karma/Jasmine para pruebas unitarias
- Build Angular CLI (`@angular-devkit/build-angular`)
- Obfuscación opcional para build de producción (`build:obfuscate`)
- Servido productivo vía Nginx (según contexto de despliegue del proyecto)

## 2) Estructura actual

Patrón híbrido identificado:

- `AppModule` tradicional como raíz.
- Features en módulos lazy-loaded vía `AppRoutingModule`.
- Componentes standalone en múltiples features.
- `CoreModule` para interceptores globales HTTP.
- `SharedModule` para componentes y utilidades de UI reutilizables.
- Layout principal con `MainLayoutComponent` y layout de autenticación con `LoginLayoutComponent`.

## 3) Inventario de archivos relevantes

Arquitectura/transversal:

- `web/ach-interbank-ui/package.json`
- `web/ach-interbank-ui/angular.json`
- `web/ach-interbank-ui/src/main.ts`
- `web/ach-interbank-ui/src/app/app.module.ts`
- `web/ach-interbank-ui/src/app/app-routing.module.ts`
- `web/ach-interbank-ui/src/app/core/core.module.ts`
- `web/ach-interbank-ui/src/app/core/services/api.service.ts`
- `web/ach-interbank-ui/src/app/core/services/auth.service.ts`
- `web/ach-interbank-ui/src/app/core/services/navigation.service.ts`
- `web/ach-interbank-ui/src/app/core/interceptors/auth.interceptor.ts`
- `web/ach-interbank-ui/src/app/core/interceptors/error.interceptor.ts`
- `web/ach-interbank-ui/src/app/core/interceptors/loading.interceptor.ts`
- `web/ach-interbank-ui/src/app/core/guards/auth.guard.ts`
- `web/ach-interbank-ui/src/app/core/guards/role.guard.ts`
- `web/ach-interbank-ui/src/app/core/guards/permission.guard.ts`
- `web/ach-interbank-ui/src/app/shared/shared.module.ts`
- `web/ach-interbank-ui/src/app/shared/components/ui/ui-grilla-empresarial.component.ts`
- `web/ach-interbank-ui/src/app/shared/components/ui/ui-etiqueta-estado.component.ts`
- `web/ach-interbank-ui/src/environments/environment.ts`
- `web/ach-interbank-ui/src/environments/environment.prod.ts`

Features de referencia para patrón:

- `web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/*`
- `web/ach-interbank-ui/src/app/features/transactions/*`

## 4) Funcionalidades ACH existentes

Se identifican funcionalidades operativas ACH maduras sobre `transactions` y `incoming-nacha-command-center`:

- Flujos de transacciones individuales y masivas.
- Carga/seguimiento de lotes.
- Carga NACHA-M.
- Devoluciones ACH.
- Command Center inbound NACHA (ingestas, cola, observabilidad).

Esto habilita reutilizar patrones de:

- Enrutamiento lazy con permisos.
- Grillas operativas (AG Grid).
- Vistas de observabilidad y detalle.
- Servicios API usando `ApiService` central.

## 5) Flujos existentes protegidos

| Flujo | Ruta | Estado | Riesgo | Regla |
|---|---|---|---|---|
| Crear transacción individual | /transactions/create | Crítico | Alto | No tocar |
| Listado de transacciones | /transactions/list | Crítico | Medio | Solo referencia |
| Carga masiva | /transactions/bulk-ingestion/upload | Crítico | Medio | No tocar |
| Seguimiento de lotes | /transactions/bulk-ingestion/tracking | Operativo | Medio | No tocar |
| Cargar NACHA-M | /transactions/nacha-upload | Operativo | Medio | No tocar |
| Devoluciones ACH | /transactions/returns | Operativo | Medio | No tocar |

## 6) Nota especial: `/transactions/create` pendiente de UAT/local

Flujo protegido explícitamente en esta fase documental:

- Ruta: `/transactions/create`
- Componente: `TransactionCreateComponent`
- Servicio: `TransactionsApiService.createTransaction()`
- Endpoint: `POST transactions`

Reglas que **no se deben romper** en fases siguientes:

- creación de transacción individual;
- creación de prenotificación;
- validación de monto;
- validación de addendas;
- validación de identidad del receptor;
- carga de terceros activos;
- carga de entidades financieras activas;
- carga de conceptos de lote;
- preview de política ACH;
- navegación posterior a crear transacción;
- manejo de errores 400/401/generales.

Se mantiene pendiente validación manual UAT/local del fix reciente.

## 7) Patrón recomendado a reutilizar

Referencia principal: `incoming-nacha-command-center` + `transactions`.

Conclusión de patrón:

1. `FeatureModule` dedicado + `RoutingModule` propio.
2. Pages standalone importadas desde el módulo de feature.
3. Servicio API específico de feature, `providedIn: 'root'`, usando `ApiService`.
4. Modelos TS del dominio en carpeta `models/`.
5. Rutas hijas con `permissionGuard` y metadata (`title`, `breadcrumb`, `permissions`).
6. Grillas operativas + filtros + vista de detalle + vistas auxiliares (queue/observability en referencia).

## 8) Propuesta de Command Center Respuestas ACH

Estructura propuesta (sin implementar todavía):

```text
web/ach-interbank-ui/src/app/features/ach-responses/
 ├── ach-responses.module.ts
 ├── ach-responses-routing.module.ts
 ├── models/
 │   └── ach-responses.models.ts
 ├── services/
 │   └── ach-responses-api.service.ts
 ├── pages/
 │   ├── ach-response-list-page.component.ts/html/scss
 │   ├── ach-response-detail-page.component.ts/html/scss
 │   ├── ach-response-attempts-page.component.ts/html/scss
 │   ├── ach-response-manual-review-page.component.ts/html/scss
 │   └── ach-response-status-mappings-page.component.ts/html/scss
 └── components/
     ├── ach-response-processing-status-badge.component.ts
     ├── ach-response-notification-status-badge.component.ts
     ├── ach-response-filters.component.ts
     └── ach-response-attempts-table.component.ts
```

## 9) Pantallas recomendadas

### A. Bandeja de respuestas ACH

Filtros:

- FechaDesde
- FechaHasta
- TipoRespuesta
- IdTransaccion
- CodigoCamaraCompensacion
- CodigoEntidadOrigen
- CodigoEntidadDestino
- CodigoEstadoExterno
- EstadoProcesamiento
- CorrelationId

Columnas:

- FechaRecepcion
- TipoRespuesta
- IdTransaccion
- Cámara
- Entidad origen
- Entidad destino
- Estado externo
- Estado interno
- Estado procesamiento
- PermiteNotificacion
- CorrelationId
- Acciones

### B. Detalle de respuesta ACH

Mostrar:

- datos principales;
- homologación aplicada;
- HashIdempotencia;
- MotivoNoHomologacion;
- IdTransaccionServicioExterno;
- CorrelationId;
- fechas;
- estado actual;
- intentos públicos.

No mostrar:

- XML SOAP;
- RequestPayload;
- ResponsePayload;
- idTransaccionAxon.

### C. Intentos de notificación

Mostrar:

- NumeroIntento;
- EstadoNotificacion;
- IdCanal;
- NombreCanal;
- IdEstado;
- Causal;
- IdTransaccionServicioExterno;
- ExisteError;
- CodigoError;
- DescripcionError;
- ErrorTecnico;
- FechaCreacion;
- FechaEnvio.

### D. Bandeja de revisión manual

Estados:

- NoHomologada
- RequiereRevisionManual
- ErrorFuncional
- PendienteReintento
- ErrorTecnico

### E. Homologaciones

- Consulta inicial de mappings.
- CRUD futuro solo si el cliente lo requiere.

### F. Dashboard operativo

KPIs:

- respuestas recibidas;
- homologadas;
- no homologadas;
- notificadas;
- errores funcionales;
- errores técnicos;
- duplicadas;
- pendientes de reintento;
- revisión manual;
- top causales;
- top entidades con fallas.

## 10) Modelos TypeScript propuestos (sin implementar)

- `ProcesarRespuestaAchRequest`
- `ProcesarRespuestaAchResponse`
- `NotificarRespuestaAchRequest`
- `NotificarRespuestaAchResponse`
- `AchResponseSearchRequest`
- `PagedResponse<T>`
- `AchResponseListItemResponse`
- `AchResponseDetailResponse`
- `AchResponseNotificationAttemptResponse`
- `AchResponseStatusMappingRequest`
- `AchResponseStatusMappingResponse`

Reglas de diseño:

- `TipoRespuesta`: `'Prenota' | 'Transaccion'`
- `EstadoProcesamiento` como union type
- `EstadoNotificacion` como union type
- No incluir `idTransaccionAxon`
- No incluir XML
- No incluir `RequestPayload`/`ResponsePayload`

## 11) Servicio HTTP propuesto (sin implementar)

`AchResponsesApiService` con `ApiService` (no `HttpClient` directo):

- `process(request)`
- `sendNotification(request)`
- `search(request)`
- `getDetail(id)`
- `getAttempts(id)`
- `getStatusMappings(filters)`

Alineado a backend cerrado:

- `POST /api/ach/responses/process`
- `POST /api/ach/responses/notifications/send`
- `GET /api/ach/responses`
- `GET /api/ach/responses/{id}`
- `GET /api/ach/responses/{id}/notification-attempts`
- `GET /api/ach/response-status-mappings`

## 12) Rutas propuestas (sin implementar)

En `app-routing.module.ts` (fase futura):

- `path: 'ach-responses'`
- `canActivate: [roleGuard, permissionGuard]`
- `data.roles: ['Admin', 'ACH.Operator']`
- `data.permissions: ['CanReadAch']`
- `data.breadcrumb: 'Respuestas ACH'`
- `data.title: 'Command Center Respuestas ACH'`
- lazy load de `AchResponsesModule`

Rutas internas propuestas:

- `''`
- `':id'`
- `':id/notification-attempts'`
- `'manual-review'`
- `'status-mappings'`
- `'dashboard'`

## 13) Menú propuesto (sin implementar)

Para fase futura en `NavigationService.mergeDefaultMenu`:

- Respuestas ACH
  - Bandeja
  - Revisión manual
  - Homologaciones
  - Dashboard operativo

## 14) Permisos recomendados

Inicial:

- `CanReadAch`
- `CanManageAch`

Fino/futuro:

- `ach.responses.read`
- `ach.responses.detail`
- `ach.responses.process`
- `ach.responses.retry`
- `ach.responses.manual-review`
- `ach.notification-attempts.read`
- `ach.notification-attempts.send`
- `ach.status-mappings.read`
- `ach.status-mappings.manage`
- `ach.dashboard.read`

## 15) Estados visuales

Propuesta: adaptar `UiEtiquetaEstadoComponent` o wrappers específicos.

`AchResponseProcessingStatus`:

- Recibida
- Homologada
- Notificada
- ErrorFuncional
- PendienteReintento
- RequiereRevisionManual
- NoHomologada
- Duplicada

`AchResponseNotificationStatus`:

- Pendiente
- Exitosa
- ErrorFuncional
- ErrorTecnico
- PendienteReintento
- RequiereRevisionManual

Semántica visual objetivo:

- éxito;
- advertencia;
- error funcional;
- error técnico;
- revisión manual;
- pendiente.

## 16) Manejo de errores

Alinear con `ErrorInterceptor` actual:

- 400 validación;
- 404 no encontrado;
- 422 no procesado;
- 500 error inesperado;
- errores funcionales de notificación como resultado de negocio;
- errores técnicos auditados como resultado visible, no como error HTTP genérico.

Reglas UX:

- no mostrar stack traces;
- mostrar `CorrelationId` cuando exista.

## 17) Pruebas recomendadas

Cobertura recomendada:

- pruebas de `AchResponsesApiService`;
- filtros;
- badges de estado;
- grillas;
- permisos por acciones;
- acciones de reintento;
- detalle;
- homologaciones.

Recordatorio de guardrail:

- Las pruebas de `/transactions/create` deben mantenerse y no removerse.

## 18) Plan de commits frontend

- SPA 1: Análisis e inventario documental.
- SPA 2: Modelos TypeScript + servicio API.
- SPA 3: Feature module, rutas internas y navegación base.
- SPA 4: Listado de respuestas ACH con filtros.
- SPA 5: Detalle de respuesta y trazabilidad.
- SPA 6: Intentos de notificación.
- SPA 7: Bandeja de revisión manual.
- SPA 8: Homologaciones.
- SPA 9: Dashboard operativo.
- SPA 10: Permisos, navegación y UX final.
- SPA 11: Pruebas unitarias/refinamiento.

## 19) Riesgos

1. **Regresión en flujos críticos existentes** si se toca accidentalmente `transactions/create` o servicios transversales.
2. **Inconsistencia semántica** entre estado externo/interno/procesamiento/notificación si no se definen unions y mapeos de UI desde el inicio.
3. **Sobreexposición técnica** si se muestran campos prohibidos (XML, payloads, nombres Axon).
4. **Permisos incompletos** que habiliten acciones operativas sin segregación.
5. **Manejo de errores ambiguo** si se tratan errores funcionales como fallos técnicos HTTP genéricos.
6. **Desalineación con backend** si los contratos TS divergen de endpoints cerrados.

## 20) Siguiente paso

Ejecutar **SPA 2** (modelos + servicio API) en commit aislado, manteniendo protección estricta de `/transactions/create` y sin mezclar implementación de pantallas en ese mismo commit.

---

## Referencias internas

- `docs/dev/respuestas-ach-backend-cierre.md`
- `docs/dev/migraciones-respuestas-ach.md`
