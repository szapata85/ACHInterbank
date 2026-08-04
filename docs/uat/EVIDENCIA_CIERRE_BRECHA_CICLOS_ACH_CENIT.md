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

- GitHub Actions posterior al cierre regulatorio terminó en 8 min 28 s con 2135 aprobadas, 6 fallidas, 7 omitidas y 2148 totales. Las seis fallas estaban en `TransactionPolicyServiceTests` y `AchContrapartidasByCycleHandlerTests`: los fixtures usaban `DateTime.Today` y el reloj real mientras producción evaluaba correctamente el instante en `America/Bogota`.
- Corrección CI focalizada: ambos fixtures usan `FixedTimeProvider` con `2026-08-04T01:15:00Z`, `ProcessingDate = 2026-08-03`, ventana local explícita y `TimeZoneId = America/Bogota`. No se modificó código de producción ni se debilitó comportamiento regulatorio.
- Fallas originales de `TransactionPolicyServiceTests`: 3/3 aprobadas; clase completa: 5/5 aprobadas.
- Fallas originales de `AchContrapartidasByCycleHandlerTests`: 3/3 aprobadas; clase completa: 4/4 aprobadas.
- Regresión UTC/Bogotá: aprobada; con la fecha UTC ya en `2026-08-04`, el ciclo de Bogotá `2026-08-03` permanece activo y alcanza la detección de duplicado.
- Ventanas/zona: 14 aprobadas.
- Política Ciclo 5: 10 aprobadas.
- Seeds/calendario/configuración: 25 aprobadas.
- Asignación/ruteo/preview/scheduler: 13 aprobadas; 3 pruebas adicionales de apertura UTC y fin de semana aprobadas.
- Validación NACHA-M/controlador de exportación: 20 aprobadas.
- Servicio de configuración de ciclos: 7 aprobadas.
- Build Persistence: 0 warnings, 0 errores.
- Build solución Release: 0 warnings, 0 errores.
- Suite CI equivalente posterior a la corrección: la única ejecución local alcanzó 20 min 04 s sin emitir fallos ni resumen antes de ser terminada por el límite del ejecutor. Conteos finales aprobadas/fallidas/omitidas/totales: no disponibles; no se declara aprobada ni se repitió el comando.
- Multi-DB real: 7 pruebas no dependientes de infraestructura aprobaron; las dos variantes reales SQL Server/PostgreSQL exigieron `CLEARING_HOUSES_REQUIRE_DATABASES=true`. No se inició un segundo stack PostgreSQL al no existir indicio de defecto específico de proveedor.

## Evidencia Docker SQL Server 2025

- `docker-compose.yml` principal: build de API aprobado, 0 warnings y 0 errores; volumen SQL preservado; SPA no reconstruida.
- `/health/live=200`; `/health/ready=200`.
- Consulta por código sobre SQL Server: ACHCOL 5 ciclos efectivos/5 nombres; CENIT 5/5; horarios iguales a la tabla anterior; ambas zonas `America/Bogota`.
- Segundo arranque/seed: mismos diez horarios y conteos 5/5, sin duplicados. La prueba automatizada compara además IDs/valores y confirma ausencia de cambio funcional en la segunda ejecución.
- Prueba de independencia del host: API recreada con `ApiHostTimeZone=UTC`, health 200/200; la base siguió mostrando ACHCOL/CENIT `America/Bogota` y 5/5 ciclos. El test controlado con relojes host UTC y UTC+09 produjo la misma ventana Bogotá.

## Riesgos reales restantes

- La suite .NET CI equivalente posterior a la corrección no entregó resumen en este host; aunque las seis fallas y las dos clases afectadas aprobaron, falta una corrida global concluida con cero fallos para marcar el cierre como completo.
- Las pruebas multi-DB reales requieren habilitación explícita y configuración segura de sus dos proveedores; PostgreSQL no fue levantado en este trabajo.
- La certificación externa oficial con ACH Colombia/CENIT permanece fuera de este cierre técnico local.

## Resultado final de regresión

La ejecución de GitHub Actions posterior a la corrección determinista concluyó
satisfactoriamente:

- Build Release: aprobado, 0 advertencias y 0 errores.
- Suite .NET: 2141 aprobadas, 0 fallidas, 7 omitidas y 2148 totales.
- Duración de la suite: 7 minutos 52 segundos.
- dotnet-ci: aprobado.
- angular-ci: aprobado.
- clearing-houses-multidb: aprobado.
- financial-integrity-multidb: aprobado.

Los seis fallos previamente detectados quedaron resueltos mediante relojes
deterministas, fechas operativas explícitas y configuración America/Bogota en
los fixtures. No se modificó código productivo ni se debilitó ninguna regla
regulatoria.

El cierre técnico de la brecha de ciclos ACH Colombia y CENIT se considera
COMPLETADO.