# Remediación UX y funcional de la SPA

Fecha de corte: 2026-07-18  
Rama local: `ACH-Interbank-Postgresql`  
Decisión asociada: **NO-GO**

Este documento resume los cambios aplicados al *working tree*. El inventario y la decisión individual de cada ruta están en `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`; aquí no se repite esa matriz.

## Resultado

La intervención corrigió navegación, fronteras de autorización, separación por cámara, estados de carga y varios contratos frontend-backend. También separó semánticamente los dos modos del simulador y dejó el modo diferencial en estado seguro (*fail-closed*). La SPA todavía no satisface un flujo productivo completo: revisión manual, homologaciones, conciliación operativa, scheduler multiinstancia y el flujo diferencial formal siguen bloqueados.

## Patrones corregidos

### Navegación y menú

- `NavigationService` dejó de reconstruir o inyectar opciones hardcodeadas. El menú efectivo proviene de `GET api/navigation/menu` y de la configuración persistida.
- El backend filtra el menú por rol, permiso, rutas NACHA-M obsoletas y disponibilidad de herramientas UAT.
- Los guards de rol y permiso envían una denegación autenticada a `/unauthorized`, sin simular un cierre de sesión.
- Los formularios de creación y edición de alias y clientes exigen permisos de gestión propios.
- Las rutas legacy del motor NACHA (`/nacha-layouts`, `/nacha-record-definitions` y sus variantes bajo `/ach-cycles`) terminan en `/not-found`.
- El menú canónico conserva una sola entrada para ciclos y una sola entrada para certificados.
- Los prefijos SPA que nginx confundía con API se aislaron: Angular consume `/api/ach-cycles`, `/api/transactions` y `/api/navigation/*`; la navegación directa a `/ach-cycles`, `/transactions`, `/navigation/menu-items` y aliases legacy entrega el shell de la SPA.

### Cabeceras, pestañas y jerarquía

- `/ach-cycles` usa el título funcional **Configuración de ciclos** y presenta acceso coherente a `Ciclos` y `Reglas de procesamiento`.
- `/transactions/cycle-configs` conserva el modelo de reglas sin confundirlo con el catálogo operacional de ciclos.
- `/cenit/operacion/ciclos` redirige a `/ach-cycles?clearingHouseCode=CENIT`; el selector queda preconfigurado sin crear una implementación paralela.
- `/nacha-security/dashboard` redirige a la pantalla canónica de certificados porque el dashboard anterior no acreditaba valor operativo independiente.

### Tablas, loaders y errores

- Las seis vistas intervenidas de respuestas ACH liberan el estado de carga mediante `finalize`, incluso ante error HTTP.
- El dashboard de respuestas cambió nueve consultas del navegador por un único agregado backend: `GET api/ach/responses/dashboard`.
- Los mensajes corregidos se presentan en español y se limpiaron textos con codificación defectuosa en los archivos intervenidos.
- El simulador muestra selección paginada en servidor para operaciones CFA elegibles, deshabilita filas no elegibles y explica el motivo.
- Las notificaciones evitan doble cierre y conservan una jerarquía visual estable.

### Responsive y accesibilidad

- El simulador incorporó tarjetas de modo mutuamente excluyentes, `aria-pressed`, labels asociados, resumen, advertencias y tablas contenidas.
- El cambio de modo pide confirmación cuando existe estado incompatible y limpia selección, código, causal y campos exclusivos.
- La pantalla de certificados incorporó filtros de cámara y ambiente, inventario y alertas sin exponer material secreto.
- Los estilos añadidos son locales a los componentes; no se introdujeron selectores globales para forzar anchos o scroll.

No existe evidencia suficiente para declarar cerrada la validación responsive solicitada de `/nacha-config-admin/perfiles`, `/records` y `/variants-fields` en 1920×1080, 1366×768, 1280×720 y 1024×768. Esas rutas permanecen `BLOCKED`.

## Cambios full-stack

| Área | Frontend | Backend/persistencia | Resultado |
|---|---|---|---|
| Respuestas ACH | Policies coherentes, un request de dashboard, loaders recuperables | Policies `P1.NachaRead`/`P1.NachaGenerate`, DTO de métricas y agregado EF | Consulta y métricas mejoradas; resolución manual y CRUD de mappings siguen ausentes. |
| Ciclos | Pestañas, cámara por query string y redirect CENIT | Seeder idempotente deshabilita entradas duplicadas | Modelos distintos conservados con una sola entrada funcional de menú. |
| Certificados | Selector de cámara/ambiente, inventario y alertas | Contrato de listado admite filtros; permisos de lectura/gestión preservados | Ruta canónica única y sin exposición de PFX, contraseña o clave privada. |
| Catálogos regulatorios | Cliente CENIT transporta cámara | Consulta de causales filtra por `clearingHouseId`/código en base de datos | Se elimina mezcla de causales ACHCOL/CENIT en el flujo intervenido. |
| Trazabilidad CENIT | Vista existente conservada | Query exige relación real con ciclo/cámara CENIT | La consulta ya no toma datos ACHCOL por coincidencia incidental. |
| Simulador NACHA-M | Modos explícitos, selección paginada, resumen y confirmaciones | Permisos finos, filtros de elegibilidad, nomenclatura válida y bloqueo diferencial | Entrantes conservado; diferencial no puede generar hasta homologación. |
| Consola SOAP UAT | Guard de gestión/lectura controlada | Policy fina y 404 fuera de ambiente UAT habilitado | El acceso directo deja de depender solo de ocultar el menú. |
| Logging SOAP | Sin cambio visible | Los errores ya no registran la respuesta completa | Se reduce el riesgo de datos sensibles en logs. |

No se generaron migraciones ni se modificaron archivos golden NACHA-M. Los cambios de manifest del simulador usan la metadata existente y los cambios de menú se implementan con seed idempotente.

## Cámaras y capacidades

La remediación no convirtió artificialmente operaciones CENIT en funciones ACH Colombia.

- Genéricas o parametrizadas por cámara: ciclos, reglas de procesamiento, certificados, respuestas, causales de devolución consultadas y trazabilidad consultada.
- Exclusivas de CENIT conservadas: cola, neteo y optimización de liquidez.
- Capacidades existentes reutilizadas: `Cycle`, `Dispatch`, `Return`, `Netting` y `Liquidity`.
- ACHCOL mantiene `Cycle`, `Dispatch` y `Return`; CENIT mantiene además `Netting` y `Liquidity`.

La aplicación transversal de capacidades a todos los catálogos, rutas y acciones CENIT no quedó completa. En particular, causales de rechazo, políticas y devoluciones todavía requieren una consolidación de dominio por cámara.

## Seguridad aplicada

- Permisos separados para ver el simulador, generar entrantes, generar diferenciales, descargar y ejecutar Live.
- El permiso Live no hereda de permisos genéricos de lectura o gestión.
- Producción configura el simulador como deshabilitado; el backend también bloquea acceso directo fuera de ambiente permitido.
- No se desactivaron autenticación ni autorización.
- No se añadieron credenciales, PFX, contraseñas, tokens ni datos personales a la SPA o a esta evidencia.
- No se registran cuerpos SOAP completos ante error.

## Funciones que permanecen bloqueadas

1. `/ach-responses/manual-review`: no existe resolución autorizada con causal, comentario, auditoría, idempotencia y concurrencia.
2. `/ach-responses/status-mappings`: no existe CRUD administrativo persistente y auditable.
3. `/ach/reconciliation`: no existe resolución/reproceso operativo gobernado ni cierre de diferencias.
4. `/scheduler/tasks`: Quartz sigue en `RAM` y `Clustered=false`; no acredita prevención de duplicados multiinstancia ni historial persistente.
5. `/nacha-config-admin/*`: no se cerró el rediseño funcional, publicación ni la matriz responsive completa.
6. Simulador diferencial: no hay perfil `RETORNO/ENTRADA` publicado y homologado ni generador table-driven homologado.
7. Catálogos y políticas CENIT: la generalización por cámara/capacidad permanece parcial.

## Evidencia relacionada

- Inventario: `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`.
- Playwright: `docs/uat/SPA_PLAYWRIGHT_VALIDATION.md`.
- Simulador diferencial: `docs/uat/NACHA_SIMULATOR_DIFFERENTIAL_RESPONSES_VALIDATION.md`.
- Decisión: `docs/go-live-readiness/SPA_GO_NO_GO_FINAL.md`.
