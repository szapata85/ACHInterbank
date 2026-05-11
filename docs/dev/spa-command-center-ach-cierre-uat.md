# Cierre SPA UAT — Command Center Respuestas ACH

## 1) Resumen ejecutivo
Se cierra la fase SPA del Command Center de Respuestas ACH con alcance de lectura/consulta operacional: bandeja, detalle, intentos, revisión manual, homologaciones y dashboard. Este cierre es **documental y de validación**, sin agregar funcionalidades nuevas ni cambios de backend/API.

Estado: **Listo para UAT/local guiado** con checklist formal y riesgos identificados.

Referencias:
- `docs/dev/spa-angular-command-center-ach-analisis.md`
- `docs/dev/respuestas-ach-backend-cierre.md`
- `docs/dev/migraciones-respuestas-ach.md`

## 2) Alcance SPA construido
- Feature Angular lazy: `features/ach-responses`.
- Páginas implementadas:
  - Bandeja (`/ach-responses`)
  - Detalle (`/ach-responses/:id`)
  - Intentos (`/ach-responses/:id/notification-attempts`)
  - Revisión manual (`/ach-responses/manual-review`)
  - Homologaciones (`/ach-responses/status-mappings`)
  - Dashboard operativo (`/ach-responses/dashboard`)
- Servicio API frontend: `AchResponsesApiService`.
- Utilidades visuales/formatters/status/renderers compartidos.

## 3) Rutas implementadas
### 3.1 Ruta global
- `/ach-responses` está registrada en `app-routing.module.ts` como lazy-load de `AchResponsesModule`.
- Protecciones de ruta global verificadas:
  - `canActivate: [roleGuard, permissionGuard]`
  - Roles: `Admin`, `ACH.Operator`
  - Permiso: `CanReadAch`

### 3.2 Rutas internas del feature
Orden de rutas internas verificado como seguro:
1. `''`
2. `'manual-review'`
3. `'status-mappings'`
4. `'dashboard'`
5. `':id/notification-attempts'`
6. `':id'`

## 4) Servicios usados
`AchResponsesApiService` (frontend) utiliza:
- `POST api/ach/responses/process`
- `POST api/ach/responses/notifications/send`
- `GET api/ach/responses`
- `GET api/ach/responses/{id}`
- `GET api/ach/responses/{id}/notification-attempts`
- `GET api/ach/response-status-mappings`

Sin cambios de contrato API en este cierre.

## 5) Pantallas implementadas
- `ach-response-list-page`
- `ach-response-detail-page`
- `ach-response-attempts-page`
- `ach-response-manual-review-page`
- `ach-response-status-mappings-page`
- `ach-response-dashboard-page`

## 6) Permisos y guards
- Guard de autenticación base del layout principal: `authGuard`.
- Para `/ach-responses`: `roleGuard + permissionGuard`.
- Permiso actual operativo documentado: `CanReadAch`.
- Recomendación futura (si el modelo lo habilita): evaluación de permiso fino `CanManageAch` para operaciones de revisión manual/homologaciones.
- **No se inventan permisos nuevos en este cierre.**

## 7) Menú
En `NavigationService` existe el grupo **Respuestas ACH** con accesos:
- Bandeja → `/ach-responses`
- Revisión manual → `/ach-responses/manual-review`
- Homologaciones → `/ach-responses/status-mappings`
- Dashboard operativo → `/ach-responses/dashboard`

Observación de arquitectura:
- El modelo base `MenuItem` no evidencia aquí enforcement de permisos por item en cliente.
- La autorización efectiva depende de guards de ruta y/o menú remoto del backend.

## 8) Checklist UAT/local pendiente — `/transactions/create`
> Estado: **Pendiente de ejecución UAT/local real** (no ejecutado en este cierre documental).

Ruta y elementos a validar:
- Ruta: `/transactions/create`
- Componente: `TransactionCreateComponent`
- Servicio: `TransactionsApiService.createTransaction()`
- Endpoint esperado: `POST transactions` (sin prefijo `/api/transactions`)

Checklist:
1. Abrir `/transactions/create`.
2. Confirmar carga de clientes originadores/terceros.
3. Confirmar carga de entidades financieras activas.
4. Confirmar carga de conceptos/lotes si aplica.
5. Confirmar addenda tipo 05 por defecto.
6. Confirmar `addenda information` obligatoria.
7. Crear transacción normal:
   - tipo `Credit`
   - cuenta origen válida
   - entidad destino válida
   - cuenta destino activa
   - monto > 0
   - addenda diligenciada
8. Confirmar respuesta exitosa.
9. Confirmar navegación posterior a `/transactions`.
10. Confirmar visualización en `/transactions/list`.
11. Crear prenotificación:
   - monto 0 permitido
   - cuenta destino manual si aplica
   - addenda válida
12. Confirmar respuesta exitosa.
13. Probar error 400 con payload inválido y mensaje funcional claro.
14. Probar sesión expirada/401 si aplica.
15. Probar que no se rompió preview de política ACH.
16. Confirmar endpoint `POST transactions` (no `/api/transactions`).
17. Confirmar que reset/limpiar no elimina addenda por defecto.

## 9) Checklist smoke test manual — Command Center ACH
> Estado: **Pendiente de ejecución UAT/local real**.

### A. Navegación
1. Ingresar a menú Respuestas ACH.
2. Abrir Bandeja.
3. Abrir Revisión manual.
4. Abrir Homologaciones.
5. Abrir Dashboard operativo.
6. Verificar ausencia de errores de consola visibles.
7. Verificar bloqueo por guards si faltan permisos.

### B. Bandeja `/ach-responses`
1. Cargar pantalla.
2. Buscar sin filtros.
3. Filtrar por fecha.
4. Filtrar por `tipoRespuesta=Prenota`.
5. Filtrar por `tipoRespuesta=Transaccion`.
6. Filtrar por `idTransaccion`.
7. Filtrar por `estadoProcesamiento`.
8. Validar paginación anterior/siguiente.
9. Abrir detalle desde botón Detalle.

### C. Detalle `/ach-responses/:id`
1. Cargar detalle.
2. Ver datos principales.
3. Ver homologación.
4. Ver hash idempotencia.
5. Ver `idTransaccionServicioExterno`.
6. Ver intentos incluidos si existen.
7. Ir a página de intentos.
8. Volver a bandeja.

### D. Intentos `/ach-responses/:id/notification-attempts`
1. Cargar intentos.
2. Ver tabla.
3. Validar estados visuales.
4. Ver errores funcionales/técnicos si existen.
5. Volver al detalle.
6. Volver a bandeja.

### E. Revisión manual `/ach-responses/manual-review`
1. Cargar filtro default `NoHomologada`.
2. Filtrar `RequiereRevisionManual`.
3. Filtrar `ErrorFuncional`.
4. Filtrar `PendienteReintento`.
5. Ver prioridad Alta/Media.
6. Abrir detalle.

### F. Homologaciones `/ach-responses/status-mappings`
1. Cargar mappings.
2. Filtrar por cámara.
3. Filtrar por tipoRespuesta.
4. Filtrar por activo Sí/No.
5. Confirmar ausencia de botones Crear/Editar/Eliminar.
6. Validar estados visuales Activo/RequiereCausal/PermiteNotificacion.

### G. Dashboard `/ach-responses/dashboard`
1. Cargar KPIs.
2. Filtrar por fecha.
3. Filtrar por tipoRespuesta.
4. Confirmar cards:
   - Total respuestas
   - Recibidas
   - Homologadas
   - Notificadas
   - No homologadas
   - Revisión manual
   - Pendientes reintento
   - Errores funcionales
   - Duplicadas
5. Confirmar resumen operativo.
6. Confirmar navegación desde cards.

### H. Seguridad visual
Confirmar que **no** se muestran en UI:
- Axon
- idTransaccionAxon
- SOAP
- WSDL
- Envelope
- RequestPayload
- ResponsePayload
- XML técnico
- stack traces

## 10) Validaciones de seguridad visual
Las pantallas y specs del módulo ACH Responses están orientadas a no exponer términos técnicos sensibles en UI. La validación final debe hacerse con datos reales en UAT/local usando checklist del punto 9-H.

## 11) Riesgos pendientes
1. `/transactions/create` pendiente de ejecución UAT/local real.
2. `npm test` bloqueado en entorno por dependencia de sistema ChromeHeadless: `libatk-1.0.so.0`.
3. Dashboard actual usa múltiples llamadas `search()` por ausencia de endpoint agregado de KPIs.
4. Menú/permisos finos requieren validación con usuarios y roles reales.
5. Reintentos de negocio, resolución de homologaciones y CRUD de mappings no están implementados por diseño.
6. Validación integral depende de backend/API + base de datos con datos reales de respuestas ACH.
7. Ruta `/ach-responses/:id` debe validarse con IDs reales emitidos por backend.

## 12) Pendientes técnicos
- Habilitar entorno con librerías de sistema para ejecutar Karma/ChromeHeadless (incluyendo `libatk-1.0.so.0`).
- Definir estrategia de KPIs agregados backend para reducir fan-out del dashboard.
- Revisar política de permisos finos por pantalla/acción si negocio exige separación adicional.

## 13) Recomendaciones para siguiente fase
1. Ejecutar UAT/local guiado con los checklists de este documento y evidencias (capturas, trazas, resultados).
2. Resolver prerequisitos del runner de tests para automatización continua de unit tests.
3. Priorizar endpoint agregado de KPIs si se confirma impacto de performance.
4. Definir matriz de permisos por rol para rutas internas sensibles (manual-review/status-mappings).

## 14) Evidencia de build/test
- `npm ci`: ejecutado OK.
- `npm run build`: ejecutado OK.
- `npm test -- --watch=false`: ejecutado con bloqueo de entorno por librería faltante:
  - `libatk-1.0.so.0: cannot open shared object file: No such file or directory`.

