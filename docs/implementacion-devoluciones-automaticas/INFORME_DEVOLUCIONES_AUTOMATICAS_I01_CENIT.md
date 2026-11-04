# Informe técnico — I01-CENIT — configuración independiente

**Corte:** 2026-11-02. **ZIP padre íntegro:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I08_ACHCOLOMBIA_2026-11-02.zip` (SHA-256 `008a19c8e9e62e5dfaf274983433bdf50a2c4e24789078eddcb0b5247bc15317`). **Estado de producción: NO-GO.**

## Trabajo real realizado

Partiendo solo del ZIP padre se verificó el contenido, se revisaron `MEMORY.md`, `MEMORY_STATE.md` y memorias I01/I08-ACH, las configuraciones por cámara, CENIT operativo, el controlador y servicio ACH I01 y auditoría EF. Se comprobó que `ClearingHouseConfigs.AutomaticReturnMode` ya existe en el modelo y en la migración SQL Server I01-ACH; no se añadió tabla ni columna repetida SQL Server. La línea PostgreSQL no tenía todavía esa columna en sus migraciones ni snapshot, por lo que se añadió una **migración de infraestructura compartida** con valor inicial seguro y restricción CHECK de los tres modos.

Se incorporaron la interfaz, el contrato API, el servicio y el controlador independientes CENIT. El acceso se restringe por código canónico `RegulatoryCycleScheduleCatalog.CenitCode` y por relación biunívoca cámara ↔ configuración; configuración inexistente o compartida produce error (sin fallback ACH). `AUTOMATICO` no invoca ejecutores y la API siempre devuelve `ProcessingConnected=false`. El modo inicial y valor de columna para filas existentes es `DESACTIVADO`.

## Contrato HTTP propuesto, instalado en fuente

- `GET /api/cenit/devoluciones-automaticas/configuracion`, con autenticación y `P1.ConfigRead`. Respuesta de ejemplo: `{"mode":"DESACTIVADO","processingConnected":false}`.
- `PUT /api/cenit/devoluciones-automaticas/configuracion`, con autenticación y `P1.ConfigManage`, cuerpo de ejemplo `{"mode":"CON_APROBACION","expectedMode":"DESACTIVADO"}`. Devuelve modo y conexión inactiva.
- Valor fuera del conjunto exacto o falta de `expectedMode`: HTTP 400. `expectedMode` distinto del valor persistido o colisión EF: HTTP 409. Es necesario efectuar GET antes del PUT y consultar nuevamente tras 409.
- Auditoría: `AchDbContext.SaveChangesAsync` registra antes/después, campos modificados, usuario y fecha según la infraestructura existente `AuditLogs`. No se creó un registro de auditoría paralelo.
- El servicio exige `ExpectedMode` aun cuando el modo destino sea igual; si no hay cambio efectivo, no emite nueva actualización.

## Evidencias

El archivo `EVIDENCIA_DEVOLUCIONES_AUTOMATICAS_I01_CENIT.md` especifica resultados ejecutados y pendientes. Se agregaron pruebas xUnit de modos, persistencia entre contextos, auditoría, autorización declarativa, colisiones y aislamiento ACH Colombia/CENIT; **las pruebas C# están escritas pero no ejecutadas** por ausencia de SDK .NET. Se ejecutaron controles de fuente estáticos y comparaciones de SHA-256 de archivos originales versus ZIP nuevo.

## Alcance evitado

No se alteraron servicios manuales CENIT ni motor de devoluciones, estructuras NACHA-M, salidas, colas, conciliación, calendario ni reglas R06/R23. No se modificaron archivos funcionales ACH Colombia, su I09 pendiente, migración SQL Server histórica, configuración de habilitación técnica, SPA Angular ni memorias raíz. Angular no necesita pantalla nueva en I01: el endpoint se administra por cliente autorizado y la interfaz queda para una fase futura si corresponde.

## Riesgos no resueltos

Se requieren ejecución xUnit, migración/aplicación SQL en ambos motores, prueba real de auditoría con identidad, control de concurrencia simultánea y validación de CHECK por proveedor. `ExpectedMode` + token EF protegen frente a modificaciones ordinarias concurrentes, **no garantizan detección de ABA sin versión adicional**. Las discrepancias documentales de edición/fecha normativa CENIT y homologación siguen abiertas. La configuración no es certificación bancaria.

**Parada exacta:** I01-CENIT implementado en código, configuración inicialmente desactivada, sin transmisión, NO-GO. **Siguiente:** I02-CENIT pasivo, solo por nueva instrucción.
