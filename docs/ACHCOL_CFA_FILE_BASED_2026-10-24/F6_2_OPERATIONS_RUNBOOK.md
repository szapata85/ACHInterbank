# F6.2 — Runbook operativo ACHCOL sin ciclo

**Ámbito:** ACHINTERBANK, flujo `AchColFileBased` desde archivos `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]` a preparación CFA. Procedimiento **preparatorio**, no autorización de envío monetario. Consultar `F6_2_IMPLEMENTATION.md` para criterios técnicos.

## 1. Estado y responsabilidades

- El archivo `.OUT` ya está en claro; `.OUT.env` requiere validar/descifrar el sobre antes del parser. No renombrar indiscriminadamente `.env` ni tratarlo como texto plano.
- `FileSequence`/`ZZZ`, `ExternalCycleNumber`/`N` y `AchCycleId` son independientes; `H/008/.10` es un caso válido de prueba.
- El flag `AchCol:AllowCycleIndependentInboundDispatch` debe continuar **OFF** hasta aprobación de F6.3. La ruta ACHCOL procesa solo prevalidación: `Blocked`, cero envíos CFA, cero reintentos monetarios.
- Responsable del cambio: equipo de aplicación + DBA PostgreSQL/SQL Server + seguridad y homologación CFA; autorización operativa formal antes de cualquier `Live`.

## 2. Comprobaciones antes de un despliegue controlado

1. Identificar ZIP, rama/SHA/estado real de Git y respaldo; verificar checksums y que `MEMORY_STATE.md` sea el vigente. Preservar otra iniciativa CFAUX.
2. Ejecutar CI y revisar **todos** los errores de compilación, las pruebas xUnit focales y la suite total; no considerar los controles estáticos como sustitutos.
3. En **copias segregadas** de PostgreSQL y SQL Server, verificar migraciones incrementales F2.3 y F3.1–F3.3, `SourceKind`, FKs, checks, índices e idempotencia bajo concurrencia. Guardar plan real de reversión y respaldo probado.
4. Confirmar que `ProcTransacciones:Mode` y configuración equivalente no autorizan ningún `Live` ACHCOL. F5.2/F5.3 son **DryRun parcial**; faltan XSD y homologaciones.
5. Ejecutar los escenarios `.OUT`, `.OUT.env`, ciclos 8/9/10+, prenotificaciones/devoluciones (excluidas) y controles CENIT/RET con datos sintéticos. No usar credenciales ni archivos monetarios productivos.
6. Obtener aprobaciones de QA, DBA, seguridad, CFA y negocio para el plan de activación; **sin todas las aprobaciones no activar**.

## 3. Observabilidad (lectura, sin acciones monetarias)

El endpoint existente `GET /incoming-nacha-command-center/observability/summary?windowHours=24` tiene `[Authorize]` y política `CommandCenterRead`. Requiere usuario autorizado. Consultar también ventanas de 1–168 horas; los contadores solo incluyen filas de cola **creadas dentro de esa ventana**.

Los seis contadores nuevos están en `achColFileBased`: `totalItems`, `pendingPreflight`, `pendingContractHomologation`, `preflightRejected`, `unexpectedNonBlocked`, `withAttempts`. Revisar *por cámara* el resto de los indicadores históricos sin atribuir `AchCycleId = null` a un ciclo inexistente.

**Alarmas de seguridad:** `unexpectedNonBlocked > 0` o `withAttempts > 0` implica suspender cualquier activación y abrir incidente de conciliación. Un aumento de `preflightRejected` requiere diagnóstico de datos/clasificación; muchos `pendingContractHomologation` no significan fallos técnicos mientras CFA no esté homologado.

**Verificación histórica antes de cualquier go/no-go:** ejecutar una lectura sin escrituras en cada base, con permisos de solo lectura. El `QueueStatus` es enum persistido por defecto como entero: `Blocked=7`. La consulta es para detectar evidencia inesperada, NO autoriza modificarla.

PostgreSQL (identificadores de EF Core entre comillas):

```sql
SELECT "QueueStatus", "LastErrorCode", COUNT(*) AS total,
       SUM(CASE WHEN "AttemptCount" > 0 THEN 1 ELSE 0 END) AS con_intentos
FROM "IncomingNachaDispatchQueue"
WHERE "SourceKind" = 'AchColFileBased'
GROUP BY "QueueStatus", "LastErrorCode"
ORDER BY "QueueStatus", "LastErrorCode";
```

SQL Server (identificadores entre corchetes):

```sql
SELECT [QueueStatus], [LastErrorCode], COUNT(*) AS total,
       SUM(CASE WHEN [AttemptCount] > 0 THEN 1 ELSE 0 END) AS con_intentos
FROM [IncomingNachaDispatchQueue]
WHERE [SourceKind] = 'AchColFileBased'
GROUP BY [QueueStatus], [LastErrorCode]
ORDER BY [QueueStatus], [LastErrorCode];
```

**Go/no-go hoy:** mientras persista falta de contrato XSD, mapping CFA no homologado, .NET/DB no probados y ruta Live ACHCOL no implementada/autorizada, **NO-GO**. No transformar un `DryRun` parcial en solicitud real.

## 4. Rollback sin efectos financieros inesperados

1. **Antes de cualquier cambio:** respaldo consistente de BD/configuración, inventario de ingresos y auditoría, responsable de incidente identificado.
2. **Rollback operativo primario:** asegurar `AchCol:AllowCycleIndependentInboundDispatch=false` en el entorno correspondiente y confirmar lectura de configuración tras reinicio/redeploy según su gestor. Con flag OFF, la etapa F4.2 no debe evaluar nuevas entradas; la cola ACHCOL permanece retenida.
3. Controlar MFT/SFTP/carga manual mediante canales operativos autorizados para impedir nuevo ingreso si el incidente lo exige; conservar originales `.OUT` y `.OUT.env` y hashes.
4. **No** hacer replay/reintento/unblock/requeue de entradas `AchColFileBased`; tampoco borrar `IncomingNachaDispatchQueue`, auditoría ni evidencia SOAP. Si existen intentos externos o respuestas inciertas, conciliar primero con CFA.
5. **No revertir migraciones a ciegas**: los métodos `Down` protegidos y la nulabilidad de la cola pueden ser incompatibles con filas file-based. Restauración de backup solo por DBA bajo análisis y evidencia de no pérdida de transacciones.
6. Comprobar regresión CENIT y `CycleBound`, reconsultar los contadores históricos, levantar acta de incidente y decidir continuidad solo con aprobación formal.

## 5. Evidencias de cierre

Registro mínimo: versión de ZIP y commit real, fecha/hora, entorno, resultado de compilación, xUnit/CI, migraciones y respaldos, totales del Command Center, consulta histórica de cola, aprobación de XSD/mapping, responsables, decisión GO/NO-GO y procedimiento de conciliación. Excluir cuentas, nombres, identificaciones y XML monetario sin redacción.
