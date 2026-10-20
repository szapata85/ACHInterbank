# ACH Colombia — siete ciclos vigentes desde el 14 de octubre de 2026

Estado de implementación del paquete: **código modificado y validación estática; despliegue y ejecución de pruebas .NET/Angular pendientes**.

## Decisión funcional confirmada (20 de octubre de 2026)

- Los siete ciclos de ACH Colombia entraron en vigencia **2026-10-14**, hora local de Colombia (`America/Bogota`).
- El inicio del ciclo 4 es **11:01 a. m.**, como aclaró expresamente el usuario (el PDF contenía el erróneo `11:01pm`).
- Documento fuente: *Nuevos Ciclos de Proceso y Horarios.pdf*, páginas 1–3. La fuente también escribe `cinco (7)`; la tabla describe inequívocamente siete ciclos.
- ACH Colombia y CENIT son cámaras distintas; la programación de cinco ciclos CENIT no cambia.

| Ciclo | Envío de archivos | Cierre | Planilla de compensación | Pago de compensación | Liberación de archivos |
|---|---|---|---|---|---|
| 1 | 19:01 (día anterior) – 07:00 | 07:00 | 07:00–07:30 | 07:30–07:45 | 07:45–08:00 |
| 2 | 07:01–09:00 | 09:00 | 09:00–09:30 | 09:30–09:45 | 09:45–10:00 |
| 3 | 09:01–11:00 | 11:00 | 11:00–11:30 | 11:30–11:45 | 11:45–12:00 |
| 4 | 11:01–12:30 | 12:30 | 12:30–13:00 | 13:00–13:15 | 13:15–13:30 |
| 5 | 12:31–14:30 | 14:30 | 14:30–15:00 | 15:00–15:15 | 15:15–15:30 |
| 6 | 14:31–16:00 | 16:00 | 16:00–16:30 | 16:30–16:45 | 16:45–17:00 |
| 7 | 16:01–18:00 | 18:00 | 18:00–18:30 | 18:30–18:45 | 18:45–19:00 |

En el ciclo 7 no se permiten débitos monetarios; se mantienen créditos y prenotificaciones según la tabla fuente.

## Persistencia y SPA

- `RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedPolicy`: versión `ACHCOL-7C-20261014`, con siete ventanas y vigencia desde `2026-10-14`.
- `ClearingHouseCycleConfigSeeder`: inserta primero la versión histórica `REGULATORY-2025` (cinco ciclos, hasta `2026-10-13`) y luego la nueva versión. Reparación idempotente de `RegulatoryCycleSeedRepair`; sin borrar históricos ni modificar la política CENIT.
- La ruta **Administración de parámetros → Ciclos de operación** (`/ach-cycles`) contiene ahora dos secciones: configuración vigente por cámara (lee `clearing-house-cycle-configs/current`) e instancias operativas por fecha (lee `api/ach-cycles`). La segunda puede estar vacía hasta que el planificador genere ejecuciones.
- El enlace `Ver reglas e historial` abre `/clearing-houses/:id/cycles` y requiere autorización `ClearingHouses.View`.

## Verificación y despliegue requeridos

1. Ejecutar migraciones pendientes y el seed con `Database:ApplySeed=true` en un entorno autorizado. No se ha aplicado sobre la base desplegada del usuario.
2. Verificar para ACHCOL: cinco configuraciones históricas vigentes hasta `2026-10-13`, y siete configuraciones `ACHCOL-7C-20261014` vigentes desde `2026-10-14`. Para CENIT: cinco ciclos propios.
3. Comprobar que `/ach-cycles` muestre las doce configuraciones vigentes (siete ACHCOL y cinco CENIT), aunque `AchCycles` esté vacío. No confundir instancias diarias con configuración normativa.
4. Ejecutar pruebas focales `RegulatoryCycleScheduleCatalogTests`, `ClearingHouseCycleConfigSeederTests`, y test Angular `AchCycleListComponent` en host con .NET 10 y dependencias npm.
5. Revisar planificador, ventanas automáticas externas y homologación antes de promover el cambio a producción. No afirmar que el scheduler ha procesado retroactivamente los días 14–19 de octubre.

**Limitación del paquete:** sin `dotnet` y sin entorno ejecutable de Angular ni conexión a base de datos; sólo se validaron sintaxis TypeScript y enlaces de fuente estáticamente. La validación runtime e idempotencia real están pendientes.
