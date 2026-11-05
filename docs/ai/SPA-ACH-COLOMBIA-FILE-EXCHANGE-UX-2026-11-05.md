# ACH Interbank — Ajuste UX/UI de `/ach-colombia/file-exchange`

**Fecha:** 2026-11-05  
**Base:** `ACHInterbank_TRANSACTIONS_LIST_UX_MATERIAL_2026-11-05.zip` (contiene las entregas anteriores de `/transactions/list`, `/customers`, `/catalogs/*` y NACHA Upload).  
**Alcance:** únicamente el módulo Angular de intercambio de archivos ACH Colombia y este informe. No se editaron servicios HTTP, modelos, rutas ni backend.

## Objetivo y decisiones de experiencia

1. **Consulta primero.** Cabecera compacta, filtros principales por fecha, operación y estado; filtros técnicos en sección plegable. Se mantienen todos los parámetros de búsqueda existentes (`from`, `to`, `direction`, `status`, `executionOrigin`, `cycleId`, `fileName`, `transferId`, `archived`, `pageNumber`, `pageSize`).
2. **Lectura rápida.** La tabla pasó de diez a seis columnas (archivo, operación, fecha/ciclo, estado, actualización y acción). El número de intentos, trazabilidad e identificadores siguen disponibles en el detalle. Se conserva la paginación del servidor a 50 registros por petición, sin inventar totales.
3. **Detalle progresivo.** Resumen con estado/operación/ciclo/fecha/tamaño/intentos, ayudas contextuales y acciones; historial, transacciones vinculadas y datos de soporte disponibles en paneles desplegables.
4. **Operaciones manuales separadas.** Envío de ciclo y consulta de recibidos dentro de una sección plegable y reservada a `CanManageAch`. Se mantiene el identificador de ciclo exigido por el endpoint.
5. **Acciones protegidas.** `executeOutbound`, `executeInbound`, `retry`, `reprocess` y `archive` utilizan el `OperationalConfirmDialogComponent` de Angular Material ya disponible. `retire` utiliza un diálogo Material propio, exige un motivo no vacío y mantiene la llamada con `reason`. Se eliminan `window.confirm` y `window.prompt`.
6. **Estados explícitos.** Mensajes de error, ausencia de datos, progreso y reintento. Validación de intervalo de fechas, botones bloqueados durante operaciones y separación de estados de carga de lista/detalle/operación/descarga.
7. **Humanización y marca.** Etiquetas operativas en español, términos técnicos relegados al detalle de soporte y estilos basados en `--brand-primary`, `--color-*`, `--radius-*` existentes. Sin paletas corporativas fijas.
8. **Seguridad y semántica.** Se conserva `CanManageAch` y `canRetry/canReprocess/canArchive/canRetire/contentAvailable`. Nunca se muestran `lastError`, `lastErrorCode`, `message` técnico ni nombres de tareas/actores internos sin sus representaciones seguras (`display*`, `actorDisplay`). En particular, **`Transferred` significa publicado para envío, no recibido por ACH Colombia**.

## Archivos

- `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html`
- `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.scss`
- `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/retire-transfer-dialog.component.ts` (nuevo)
- `docs/ai/SPA-ACH-COLOMBIA-FILE-EXCHANGE-UX-2026-11-05.md` (nuevo)

## Evidencia verificada

- 27/27 comprobaciones estáticas focales: Material, filtros, contrato, permisos, acciones, estado externo, vocabulario, branding, HTML equilibrado y sintaxis TypeScript.
- Se actualizaron/añadieron pruebas Jasmine para consultas, permisos, errores, cancelación de confirmaciones, fechas inválidas, reinicio de filtros, envío con ciclo y retiro con motivo.
- El paquete completo se construye copiando todos los archivos del ZIP base y sustituyendo únicamente los anteriores seis recursos; se comprueba igualdad SHA-256 de todos los archivos que no cambian y el contenido efectivo de los modificados.

## Limitaciones de validación

**No se ejecutaron** `npm ci`, `ng build`, Jasmine ni Playwright: el entorno actual no tiene `node_modules` y no dispone de instalación verificable de las dependencias de Angular. El paso sintáctico TypeScript **no** sustituye la compilación Angular con comprobación de plantillas. No se ha probado el comportamiento de navegador ni una operación real de transferencia.

## Validación en estación Windows / Docker

Desde la raíz del proyecto, sin perder datos ni ejecutar sembrado o migraciones automáticas:

```cmd
docker-ach.cmd down
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd up --no-seed --no-auto-migrations
docker-ach.cmd status
```

**No utilizar `--no-build`**, porque puede conservar la imagen anterior `achinterbank-spa:local`. Abrir `http://localhost:743/ach-colombia/file-exchange` y recargar con `Ctrl+Shift+R`. Verificar roles de consulta y gestión, filtros avanzados, páginas, cancelación de diálogos, obligatoriedad del motivo de retiro, enlaces relacionados y estados de publicación. Enviar/recibir/reprocesar/reintentar solamente con autorización y datos controlados.
