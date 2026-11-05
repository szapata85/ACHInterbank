# ACH Interbank — Modernización UX/UI: consulta de transacciones

**Fecha:** 2026-11-05  
**Ruta:** `/transactions/list`  
**Base:** `ACHInterbank_CUSTOMERS_UX_MATERIAL_BRANDING_2026-11-05.zip` (con cambios previos de `/customers`, `/catalogs/*` y `/transactions/nacha-upload`).  
**Ámbito:** interfaz Angular y pruebas de regresión; no se modificaron controladores, servicios HTTP, modelos compartidos, rutas, base de datos ni reglas ACH/CENIT.

## Objetivo y criterio de diseño

Reducir la carga visual de una pantalla operativa muy consultada, conservando accesibilidad, búsqueda por fecha/cámara/ciclo, navegación a creación y cargas, agrupación por lote, acceso a resultado del procesamiento, clasificación de devoluciones y personalización de columnas.

## Cambios implementados

1. **Consulta principal:** mantiene tres filtros Angular Material (fecha de aplicación, cámara compensadora, ciclo); validación de fecha y búsqueda/limpieza.
2. **Lectura jerárquica:** primero fecha de aplicación, cámara y ciclo; dentro de cada combinación, lotes expandibles mediante Angular Material Expansion.
3. **Densidad progresiva:** carga visual inicial de **12 lotes** y opción «Mostrar más lotes»; solo se monta la grilla del lote desplegado. La página se mantiene compacta hasta que el operador necesita el detalle.
4. **Información por lote:** nombre y número, cantidad de transacciones, suma COP e intervalo de **horas de registro** cuando `createdAt` está disponible. Los intervalos se formatean para `America/Bogota`; **no representan horarios oficiales de apertura o cierre de ciclo**. Orden por fecha, cámara, ciclo y último registro.
5. **Búsqueda rápida:** referencia, identificador, número de cuenta, trazabilidad, empresa/lote, institución, ciclo o cámara **dentro de los datos de la consulta actual**. Se aplica localmente (espera breve de 200 ms) y no genera nuevas peticiones HTTP.
6. **Devoluciones:** vistas «Todas», «Recibidas», «Enviadas». Conserva exactamente los códigos anteriores (21/31/51 y 26/36/56) y ahora filtra los resultados existentes sin solicitar repetidamente el endpoint.
7. **Grilla más sencilla:** seis columnas inicialmente (incluido número obligatorio); las restantes siguen disponibles mediante selector Material de columnas. Permanece la búsqueda y paginación dentro de la grilla abierta.
8. **Acciones según permisos:** nueva transacción, carga y seguimiento respetan `Transactions.Create` / `CanManageAch`, `BulkIngestion.Upload` / `CanManageAch` y `BulkIngestion.Read` / `CanReadAch` respectivamente. El backend mantiene la autoridad final.
9. **Retroalimentación:** estados de carga, error reintentable, resultados vacíos y ausencia de coincidencias; evita resultados de integración obsoletos y el indicador de carga permanente ante errores.
10. **Branding dinámico:** los colores, bordes y superficies se obtienen de los tokens existentes `--color-*`, sin introducir una paleta corporativa fija.

## Comportamiento y contratos preservados

- `TransactionsApiService.getAll()` mantiene exactamente los filtros `achCycleId`, `achCycleName`, `effectiveDate`, `clearingHouseId`.
- Se conserva la distinción de filtrado por nombre de ciclo si no hay cámara escogida, e ID exacto de ciclo cuando hay cámara.
- Se mantienen la navegación a `/transactions/create`, `/transactions/bulk-ingestion/upload`, `/transactions/bulk-ingestion/tracking` y la consulta `/api/transactions/{id}/integration-result`.
- Valores de cuenta continúan siendo `string` (ceros iniciales preservados); ningún tipo/código transaccional fue modificado.
- Componentes, lógica y branding anteriores de los demás módulos permanecen en el ZIP completo.

## Límites conocidos y verificaciones pendientes

**Importante:** `/api/transactions` no admite paginación en el contrato actual: el controlador entrega una colección completa con filtros opcionales. La intervención limita el trabajo de renderizado y las consultas repetidas, **pero no reduce el número de registros transferidos** en la respuesta original. Para conjuntos realmente masivos la siguiente intervención debe definir paginación/ordenamiento del lado servidor con backend y pruebas de integridad del contrato.

**Se completaron:** 21/21 comprobaciones estáticas del módulo, análisis sintáctico TypeScript de componente/spec/e2e, balanceo de etiquetas HTML y llaves SCSS, y empaquetado ZIP verificado. Se agregaron pruebas Jasmine de agrupación, búsqueda sin HTTP adicional, presentación incremental de lotes, horas de registro, estados de error y concurrencia de ciclos; se amplió la prueba Playwright.

**No se completaron:** `ng build`, ejecución de Jasmine, Playwright en navegador ni validación contra contenedores Docker reales por falta de `node_modules` y servicios de ejecución en el entorno. La superación de las verificaciones estáticas **no equivale** a compilación ni aprobación funcional.

## Validación de usuario recomendada

1. Compilar el frontend y ejecutar las pruebas unitarias.
2. Entrar con un perfil que pueda consultar, pero no crear, y verificar que las acciones de escritura no aparezcan.
3. Buscar por fecha, cámara y ciclo; confirmar parámetros de la llamada a `/api/transactions`.
4. Desplegar un lote y seleccionar una fila para ver el resultado de integración.
5. Usar búsqueda rápida y vista de devoluciones; confirmar que no salen llamadas HTTP adicionales.
6. Probar 1, 12, 13 y más de 24 lotes, estados vacíos/errores y ventanas de 390, 768 y 1366 píxeles.
7. Contrastar el intervalo de hora mostrado con `createdAt` y distinguirlo de horario de compensación oficial.

## Para ver los cambios en Docker

Desde la raíz del proyecto **actualizado**:

```cmd
docker-ach.cmd down
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd up --no-seed --no-auto-migrations
docker-ach.cmd status
```

No utilizar `--no-build` después de cambiar Angular: reutilizaría una imagen anterior de SPA. Finalmente, recargar `/transactions/list` con Ctrl + Shift + R.

## Archivos intervenidos

- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.spec.ts`
- `web/ach-interbank-ui/e2e/transactions-list-ux.spec.ts`
- `docs/ai/SPA-TRANSACTIONS-LIST-UX-MATERIAL-2026-11-05.md`
