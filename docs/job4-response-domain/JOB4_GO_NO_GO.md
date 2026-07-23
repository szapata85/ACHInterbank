# JOB 4 — GO/NO-GO

| Criterio | Evidencia | Estado | Riesgo residual |
|---|---|---|---|
| Mapping determinístico y separado por cámara | Pruebas de cámara, vigencia, prioridad, `NoMatch` y `Ambiguous` | Cumple | Ninguno conocido |
| Idempotencia concurrente | Índice único, recuperación de conflicto y recepción HTTP simultánea en E2E | Cumple | Ninguno conocido |
| Huérfanas y revisión manual | Persistencia, inicio de revisión, asociación/rechazo y auditoría | Cumple | Candidatos se conservan como referencias sanitizadas, no como entidad normalizada |
| Transiciones y auditoría | Política central y auditoría append-only protegida por contexto | Cumple | Auditoría distribuida no incluida en el alcance |
| Concurrencia | `Guid Version`, conflicto sin overwrite y HTTP 409 | Cumple | Ninguno conocido |
| Reproceso gobernado | Permiso, estado, motivo, correlación, exclusión simultánea e intento consultable | Parcial | No existe aún dispatcher que ejecute el pipeline real y cierre el intento sin repetir efectos |
| Conciliación operacional | Excepciones persistidas y resolución auditada | Cumple | No es conciliación contable por diseño |
| SQL Server y PostgreSQL | Forward, rollback, reaplicación y pruebas reales 2/2 | Cumple | Filas legacy sin cámara inequívoca permanecen sin backfill |
| Backend | Build limpio; resultado lógico 1912 aprobadas y 5 omitidas | Cumple | Cinco omisiones preexistentes documentadas |
| Angular y Playwright | 458/458; build; Chromium real 2/2 | Cumple | Playwright se ejecutó con SQL Server; PostgreSQL quedó cubierto en integración multimotor |
| Seguridad operacional | No se ejecutó SOAP ni se modificaron montos, saldos o asientos | Cumple | `npm ci` reporta vulnerabilidades de dependencias preexistentes |

## Veredicto

**NO-GO.** El dominio, API, persistencia, administración SPA y los flujos resolutivos probados son funcionales, pero el criterio cerrado exige que el reproceso use el pipeline real y deje un resultado terminado. La implementación actual gobierna y persiste la solicitud en estado pendiente, sin dispatcher de ejecución/completado. No se presenta ese pendiente como funcionalidad terminada.

## Acción de desbloqueo

Implementar un dispatcher de `PendingReprocess` sobre la abstracción existente, con exclusión por intento, ejecución idempotente del pipeline, transición terminal y auditoría; agregar su prueba de integración en ambos proveedores y repetir backend y Playwright.
# JOB 4.1 — Dispatcher de reprocesos

Estado: **NO-GO condicionado a evidencia de infraestructura**.

El cierre de código incorpora claim atómico, lease recuperable, pipeline sobre la entidad existente, terminales auditables, consultas de intentos y migraciones por proveedor. La build Release y las pruebas focalizadas están aprobadas.

Pendiente para GO: ejecutar y aprobar las pruebas reales SQL Server y PostgreSQL, Quartz persistent-store con dos instancias, Angular y Playwright. No se declara GO mientras esa evidencia no exista.
# JOB 4.2 — Hardening y certificación final

Estado: **NO-GO**.

El hardening de código y las migraciones PostgreSQL reales están verificados, pero faltan las evidencias ejecutadas de SQL Server, dos nodos Quartz, Angular y Playwright requeridas para el GO definitivo del JOB 4.
