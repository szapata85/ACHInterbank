# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I01-CENIT

**Fecha de corte:** 2026-11-02. **Iniciativa:** DEV-AUTO-CENIT. **Incremento:** I01-CENIT (parametrización independiente).  
**Base única:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I08_ACHCOLOMBIA_2026-11-02.zip`. **SHA-256 origen:** `008a19c8e9e62e5dfaf274983433bdf50a2c4e24789078eddcb0b5247bc15317`.  
**Salida acumulativa:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I01_CENIT_2026-11-02.zip` (SHA-256 externo, en archivo `.sha256`; no incorporar el hash del ZIP en el propio ZIP).  
**Estado de código:** I01 incorporado; validación estática realizada; compilación y ejecución funcional PENDIENTES. **Producción: NO-GO.**

## Memorias consultadas y preservadas

`/MEMORY.md`, `/MEMORY_STATE.md`, `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md`, `MEMORY_STATE_I01_ACH_HISTORICO.md`, `MEMORY_STATE_I08_ACH.md` y memorias CENIT de auditoría/correcciones. No se sobrescribieron memorias históricas. El estado de ACH Colombia sigue: I01–I08 incorporados, I09-A técnico si procede, I09-B ejecución en ambiente compilado pendiente, `AchCol:EnableAutomaticReturnFileGeneration=false` por defecto, NO-GO.

## Objetivo y decisión mínima

Solo habilitar lectura, edición autorizada, persistencia y auditoría del modo **CENIT** `DESACTIVADO | CON_APROBACION | AUTOMATICO`. La selección no produce efectos financieros. `ProcessingConnected=false` en todos los modos; no hay motor de devoluciones automáticas CENIT ni generación/envío/conciliación autónomos. Se reutiliza `ClearingHouseConfigs.AutomaticReturnMode` por fila, sin tablas ni SPA nuevas.

## Código y migración modificados

**Nuevos:**
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitAutomaticReturnSettings.cs`: contratos de GET/PUT y `ExpectedMode` obligatorio.
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitAutomaticReturnSettingsService.cs`: interfaz.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitAutomaticReturnSettingsService.cs`: fila CENIT exclusiva, modos estrictos, comparación de estado anterior, escritura con token EF y auditoría común.
- `src/Cfa.ACHInterbank.Api/Controllers/CenitAutomaticReturnSettingsController.cs`: GET/PUT `api/cenit/devoluciones-automaticas/configuracion`, políticas existentes y conflicto 409.
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261102233000_CenitAutomaticReturnConfigurationI01.cs`: incorpora columna común que faltaba en PostgreSQL con CHECK y default seguro para ambas cámaras (solo esquema).
- `tests/Cfa.ACHInterbank.Tests/CenitAutomaticReturnSettingsI01Tests.cs`: casos xUnit.
- `scripts/test/automatic_returns_i01_cenit_static_check.py`: controles estáticos reproducibles.

**Modificados:**
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`: registro DI exclusivo CENIT.
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`: refleja columna, concurrencia y CHECK ya definidos por modelo compartido en I01-ACH.

**Sin cambios:** servicio/controlador/runner/modelo funcional ACH Colombia, servicios manuales CENIT, migración y snapshot SQL Server, SPA, `MEMORY.md` y `MEMORY_STATE.md` raíz. El detalle completo, huellas de cada archivo y conteos se conserva en `MANIFIESTO_DEVOLUCIONES_AUTOMATICAS_I01_CENIT.json`.

## Validaciones realmente realizadas

- ZIP padre `unzip -t`: sin errores.
- Validador I01-CENIT: **29/29 estáticas**.
- Regresiones estáticas ACH: I02 18/18, I03 25/25, I04 **32/35 (NO aprobada)**, I05 33/33, I06 28/28, I07 24/24, I08 25/25.
- Comparación por SHA-256: 4.860 archivos padre; 4.858 idénticos, 2 modificados de infraestructura y 0 eliminados; nuevos archivos especificados por manifiesto. Integridad del ZIP final: ver evidencia externa y `.sha256`.
- **No realizadas:** `dotnet build`, `dotnet test`, ejecución xUnit CENIT, HTTP real, SQL Server/PostgreSQL reales (migración, auditoría, CAS), Angular, tests end-to-end, certificación/homologación Banco de la República.

## Riesgos y pendientes

1. PostgreSQL venía sin la columna compartida pese a que I01-ACH la agregó en SQL Server; la nueva migración PostgreSQL afecta **infraestructura común**, pero no reconfigura ACH Colombia ni ejecuta código funcional. Verificar ruta de migración y datos existentes antes de aplicar.
2. EF usa `AutomaticReturnMode` como token de concurrencia; además el PUT exige `ExpectedMode`. La coincidencia A→B→A (ABA) no se detecta con solo el modo anterior: es un riesgo residual si se necesita versionado estricto; evaluar con pruebas relacionales antes de explotación real.
3. Las políticas comunes `P1.ConfigRead`/`P1.ConfigManage` son las mismas existentes para configuración; la atribución de usuario/auditoría solo se verifica en runtime autenticado.
4. En SQL Server, la migración histórica usa CHECK entre corchetes; el modelo compartido preexistente define el CHECK con comillas de PostgreSQL. Validar coherencia EF/proveedor en entorno compilado, sin alterar el historial I01-ACH en este incremento.
5. Si faltan registros vinculados de cámara/configuración, el servicio **falla cerrado** y no crea/compartimenta filas automáticamente. Corregir datos maestros operativos, no crear un fallback.
6. No reutilizar causales, ventanas ni reglas NACHA de ACH Colombia para CENIT. Vigencias/versiones de anexos diferentes requieren cotejo previo a motores futuros.
7. Los procesos manuales CENIT se conservaron **byte a byte**; no hubo ejecución transaccional de regresión.

## Estado y parada

**CENIT:** I01 implementado en fuente; inicialmente `DESACTIVADO`, la selección `AUTOMATICO` no ejecuta nada; homologación pendiente; NO-GO. **ACH Colombia:** I01–I08 sin cambios; I09-A/I09-B y NO-GO intactos. **Detenerse aquí.**

**Siguiente incremento autorizado solo mediante nueva instrucción:** I02-CENIT, detección pasiva de condiciones reales de devolución conforme a documentos CENIT (sin generación autónoma ni envío). La continuidad es sobre el ZIP I01-CENIT completo, no sobre bases previas.
