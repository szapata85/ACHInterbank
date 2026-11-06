# Incremento de corrección de seed — orden del menú principal

**Fecha:** 2026-11-05. **Base:** `ACHInterbank_SIDEBAR_CORREGIDO_ALINEACION_MATERIAL_2026-11-05.zip`.

## Problema reproducido

La captura del operador muestra que el menú ya se alinea correctamente, pero conserva **Seguridad NACHA antes de Inicio**. La base SQL Server de la instalación actual conserva el orden anterior (8) y el nombre antiguo. La presentación Angular lee la jerarquía, orden y autorización desde la API; no debe esconder este problema cambiando artificialmente el orden en el cliente.

## Causa en el código fuente

La misma entrada tenía tres definiciones:

- `MenuItemConfiguration.HasData`: `NachaSecurityId=10`, etiqueta **Seguridad NACHA**, orden **8**, ruta histórica `/nacha-security/certificates`.
- `NachaSecurityMenuSeeder` (orden de ejecución 0): volvía a asignar **Seguridad NACHA**, orden **8**, ruta `/nacha-security`.
- `NavigationInformationArchitectureSeeder` (reconciliación final 100): asignaba **Seguridad de archivos**, orden **60**. Solo se ejecuta si el seed se habilita.

Adicionalmente, `OutgoingTransactionMonitoringSeeder` volvía a llamar al dashboard **Panel principal** antes de la reconciliación final.

## Cambios introducidos

1. El baseline `HasData` se mantiene **sin cambios** para no desincronizar las migraciones EF existentes ni romper `docker-ach.cmd migrate` por cambios de modelo pendientes. Los IDs 1 y 10 siguen siendo los mismos; el seed funcional convierte los registros históricos en la arquitectura nueva.
2. `NachaSecurityMenuSeeder`: reutiliza `FileSecurityLabel`, `FileSecurityRoute` y `FileSecurityRootOrder` del reconciliador canónico. Sus dos hijos se nombran/ordenan también como la versión definitiva (10/20). Mantiene los mismos identificadores, controles de permisos y ruta de cada hijo.
3. `NavigationInformationArchitectureSeeder`: constantes explícitas de órdenes del dashboard (10) y seguridad (60) usadas en su reconciliación, para reducir divergencias futuras.
4. `OutgoingTransactionMonitoringSeeder`: no revierte Inicio a Panel principal.
5. Pruebas de regresión: baseline de DB nuevo, reparación de fila antigua, idempotencia del seed funcional, compatibilidad al volver a ejecutar el seed funcional después del canónico y conservación del árbol raíz canónico ya probado.
6. Script **opcional** `scripts/sql/repair-navigation-file-security-root.sql` para instalaciones SQL Server persistidas con `--no-seed`. Es transaccional, verifica ID/rutas y no altera roles/permisos; revisarlo antes de ejecutarlo.

## Puesta en marcha en otro servidor (SQL Server, base nueva)

Desde CMD y la carpeta raíz del proyecto descomprimido, con Docker Desktop y .NET SDK 10 instalados:

```cmd
 docker-ach.cmd config
 docker-ach.cmd infra
 docker-ach.cmd migrate
 docker-ach.cmd up --seed --no-auto-migrations
 docker-ach.cmd status
```

`migrate` aplica el esquema EF; `up --seed --no-auto-migrations` reconstruye imágenes e **invoca todos los sembradores registrados** al iniciar la API, incluido el reconciliador final. En la configuración de desarrollo `Database:ApplySeed=false`, por eso **se requiere `--seed` explícitamente al inicializar**. No uses `--no-build` al incorporar este ZIP.

**Importante:** `--no-seed` deshabilita TODO el seed y no puede reparar registros existentes; `--no-auto-migrations` solo deshabilita migraciones automáticas durante `up`, pero no sustituye `migrate`. No ejecutar `reset --confirm-data-loss` sobre bases con información que deba conservarse.

**Instalación ya poblada:** la opción más controlada es editar `/navigation/menu-items` y corregir `Orden=60` / `Nombre=Seguridad de archivos`, conservando ruta y permisos. El SQL opcional repara solo IDs 1 y 10 cuando se confirma que son las entradas canónicas. Ejecutar `up --seed` sobre una base existente ejecuta todos los demás seeders: evaluar el impacto en los datos del ambiente antes de hacerlo.

El `HasData` de EF y las migraciones EF ya compiladas contienen datos históricos; **no se modifican** en este incremento para conservar la compatibilidad de `migrate`. En una instalación nueva, **ejecutar las migraciones y después el seed canónico** es necesario para obtener toda la arquitectura final. Esta entrega **no contiene una nueva migración EF de datos** y no modifica silenciosamente una base persistida con `--no-seed`.

## Orden esperado de navegación (según permisos)

| Orden | Grupo | Ruta |
| ---: | --- | --- |
| 10 | Inicio | `/dashboard` |
| 20 | Operación | `/transactions` |
| 30 | Clientes | `/customers` |
| 40 | Seguimiento y control | `/seguimiento-control` |
| 50 | Administración de parámetros | `/administracion-parametros` |
| 60 | Seguridad de archivos | `/nacha-security` |
| 70 | Reportes | `/reports` |
| 80 | Administración | `/administracion` |
| 90 | Herramientas de prueba | `/uat` |

## Verificaciones / límites

- En código: los seeders funcional y final comparten la definición canónica; el `HasData` histórico permanece idéntico a las migraciones existentes. La identidad `NachaSecurityId=10`, jerarquía y contratos API se mantienen.
- Pruebas .NET añadidas para el seed funcional sobre un baseline inicial, actualización idempotente de fila legada y no-regresión tras reconciliador; las pruebas existentes validan los nueve grupos y las autorizaciones.
- **Pendiente en este entorno:** `dotnet build` / `dotnet test` (SDK .NET no disponible), ejecución real del nuevo seed contra SQL Server y comprobación visual en Docker. La inspección estática no sustituye estas comprobaciones.
- La corrección del seed no modifica Angular, estilos, NACHA-M, ACH Colombia, CENIT, ni datos monetarios.
