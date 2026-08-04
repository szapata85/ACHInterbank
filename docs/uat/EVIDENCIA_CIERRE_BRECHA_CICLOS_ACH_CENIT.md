# Evidencia de cierre de brecha de ciclos ACH Colombia / CENIT

Fecha: 2026-08-03.

## Fuentes regulatorias

- ACH Colombia: Manual de Servicio ACH Transferencias Interbancarias para Entidad Participante, versión 32, enero de 2025, secciones 2.4.1 y 2.4.7.
- CENIT: Circular Externa Operativa y de Servicios DSP-152, Anexo 2, 27 de febrero de 2025.

## Causa raíz y decisión

Había dos seeds divergentes: uno copiaba a CENIT la huella completa de ACH Colombia y otro contenía horarios históricos incorrectos. Además, un retorno temprano por “existe cualquier ciclo”, cálculos de ventana duplicados, hora local del host y detección de Ciclo 5 por texto impedían una reparación y decisión regulatoria consistentes.

Se creó un catálogo regulatorio único, un reparador conservador por código/período, un resolvedor único de número de ciclo, un resolvedor único de ventana/instantes por `TimeZoneId` y una política reutilizable por PaymentRail. No se creó migración.

## Horarios finales efectivos

| Cámara | Ciclo | Inicio | Fin | Cutoff |
|---|---:|---:|---:|---:|
| ACHCOL | 1 | 19:01 | 08:30 | 08:30 |
| ACHCOL | 2 | 08:31 | 11:00 | 11:00 |
| ACHCOL | 3 | 11:01 | 14:00 | 14:00 |
| ACHCOL | 4 | 14:01 | 16:00 | 16:00 |
| ACHCOL | 5 | 16:01 | 18:00 | 18:00 |
| CENIT | 1 | 07:30 | 10:30 | 10:30 |
| CENIT | 2 | 11:00 | 13:00 | 13:00 |
| CENIT | 3 | 13:30 | 15:00 | 15:00 |
| CENIT | 4 | 15:30 | 17:15 | 17:15 |
| CENIT | 5 | 17:45 | 18:45 | 18:45 |

Para `ProcessingDate=2026-08-04`, ACHCOL Ciclo 1 abre `2026-08-03 19:01 America/Bogota` y cierra `2026-08-04 08:30 America/Bogota`. La apertura usa la fecha calendario anterior, no el día hábil anterior. Inicio y fin son inclusivos.

## Zona horaria y política de Ciclo 5

ACHCOL y CENIT quedan configuradas con `America/Bogota`. Las decisiones parten de `TimeProvider.GetUtcNow()` y convierten el instante con la zona de la cámara; no dependen de `DateTime.Now`, `TimeProvider.GetLocalNow()` ni de un offset fijo.

La razón estable `ACHCOL_CYCLE5_ORDINARY_DEBIT_NOT_ALLOWED` aplica solo a PaymentRail ACH Colombia, Ciclo 5 y débito monetario ordinario originado. Prenotificaciones, créditos y retornos no son bloqueados por esta regla; la clasificación funcional de retorno prevalece sobre el campo débito. CENIT y cámaras futuras no heredan la regla.

La misma política se aplica en preview/asignación y en `NachaTransactionValidationService`, antes de que `NachaFileBuilder` produzca contenido o la API registre una exportación.

## Pruebas y build

- Ventanas/zona: 14 aprobadas.
- Política Ciclo 5: 10 aprobadas.
- Seeds/calendario/configuración: 25 aprobadas.
- Asignación/ruteo/preview/scheduler: 13 aprobadas; 3 pruebas adicionales de apertura UTC y fin de semana aprobadas.
- Validación NACHA-M/controlador de exportación: 20 aprobadas.
- Servicio de configuración de ciclos: 7 aprobadas.
- Build Persistence: 0 warnings, 0 errores.
- Build solución Release: 0 warnings, 0 errores.
- Suite completa: no concluyó en este host; agotó 10 y 20 minutos sin emitir fallos. Una corrida diagnóstica de 10 minutos con `--blame-hang-timeout 2m` no detectó prueba individual colgada. No se declara aprobada.
- Multi-DB real: 7 pruebas no dependientes de infraestructura aprobaron; las dos variantes reales SQL Server/PostgreSQL exigieron `CLEARING_HOUSES_REQUIRE_DATABASES=true`. No se inició un segundo stack PostgreSQL al no existir indicio de defecto específico de proveedor.

## Evidencia Docker SQL Server 2025

- `docker-compose.yml` principal: build de API aprobado, 0 warnings y 0 errores; volumen SQL preservado; SPA no reconstruida.
- `/health/live=200`; `/health/ready=200`.
- Consulta por código sobre SQL Server: ACHCOL 5 ciclos efectivos/5 nombres; CENIT 5/5; horarios iguales a la tabla anterior; ambas zonas `America/Bogota`.
- Segundo arranque/seed: mismos diez horarios y conteos 5/5, sin duplicados. La prueba automatizada compara además IDs/valores y confirma ausencia de cambio funcional en la segunda ejecución.
- Prueba de independencia del host: API recreada con `ApiHostTimeZone=UTC`, health 200/200; la base siguió mostrando ACHCOL/CENIT `America/Bogota` y 5/5 ciclos. El test controlado con relojes host UTC y UTC+09 produjo la misma ventana Bogotá.

## Riesgos reales restantes

- La suite .NET completa no entregó resumen dentro de 20 minutos en este host; aunque los grupos afectados aprobaron y el detector no halló un test individual colgado, falta una corrida global concluida para afirmar regresión total.
- Las pruebas multi-DB reales requieren habilitación explícita y configuración segura de sus dos proveedores; PostgreSQL no fue levantado en este trabajo.
- La certificación externa oficial con ACH Colombia/CENIT permanece fuera de este cierre técnico local.
