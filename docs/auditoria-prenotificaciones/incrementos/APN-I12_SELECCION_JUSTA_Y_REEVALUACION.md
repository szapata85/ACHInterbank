# APN-I12 — Selección justa y reevaluación de prenotificaciones

**Fecha:** 2026-11-03  
**Base única:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I11_2026-11-03.zip`  
**Estado:** IMPLEMENTADO EN CÓDIGO / VERIFICACIÓN ESTÁTICA APROBADA / .NET, SQL Y UAT NO EJECUTADOS.

## 1. Hallazgo demostrado por el código heredado

`AchTacitAcceptanceJobHandler.ExecuteAsync` seleccionaba como máximo `BatchSize × 4` transacciones canónicas con `ResponseDeadlineAtUtc.HasValue` y `ResponseDeadlineAtUtc <= now`, seguido de candidatas legacy si quedaba espacio. Esto creaba dos riesgos lógicos:

- Si APN-I08/I11 dejaba el plazo almacenado `null` debido a un calendario incompleto, el intento canónico no volvía al conjunto del job cuando el calendario se completaba; no había ruta de reevaluación por el job.
- La selección comenzaba desde el mismo segmento cada ejecución. Registros permanentemente retenidos (respuesta ambigua, falta de transmisión u otra barrera) podían desplazar continuamente IDs posteriores. El conjunto legacy también podía quedar relegado.

Estos son defectos del **selector**, no evidencia de que haya ocurrido una aceptación indebida. El efecto potencial es **demora o falta de resolución automática**, no aprobación anticipada.

## 2. Corrección productiva

### `PrenotificationCandidateScan.cs` (nuevo)

- Incluye intentos canónicos con vencimiento ya alcanzado **o vencimiento almacenado nulo**, siempre que estén pendientes y su política permita aceptación tácita.
- Incluye los pendientes legacy sin intento de validación, manteniendo la revisión individual del job.
- Hace `UNION` por ID para no duplicar candidatos, luego orden estable por `AchTransaction.Id`, con límites `> lastId`, `<= maxId` y páginas `Take(pageSize)`.
- Materializa las páginas antes de consultar la transacción/archivos, evitando un lector activo durante el procesamiento individual.

### `AchTacitAcceptanceJobHandler.cs` (modificado)

- Conserva el límite de **aplicaciones** `BatchSize` y añade el límite de **evaluaciones** `MaxCandidatesPerRun`, configurable en los parámetros existentes de la tarea (por defecto el máximo entre `BatchSize` y 4× `BatchSize`, acotado normalmente a 20.000).
- Lee y escribe el parámetro `ApnI12LastExaminedTransactionId` en la tabla ya existente `TaskParameters`; si no existe, se crea al terminar el job. **No necesita migración.**
- Recorre primero `(cursor, int.MaxValue]` y, si queda presupuesto, `[1, cursor]`. En ejecuciones sucesivas, los candidatos posteriores ya no quedan desplazados permanentemente por el mismo prefijo.
- Conserva `ApplyIfSafeAsync` y todas las barreras de APN-I08–I11: recalcular plazo, transmisión real, estado canónico, respuestas NACHA/MFT/CENIT, transición idempotente transaccional.
- Registra contadores estructurados de examinados, aplicados, omitidos, errores, posición del cursor y aviso al agotar el presupuesto de exploración.
- Una falla al persistir el cursor se registra como advertencia, sin convertirla en aceptación tácita ni revocar transiciones ya confirmadas. El siguiente job podrá reexaminar IDs (idempotencia).

**Limitación deliberada:** incluir un candidato con plazo `null` en la selección **no** significa habilitar la aceptación; si el recálculo sigue siendo indeterminado, `ApplyIfSafeAsync` retorna sin modificar estados.

## 3. Pruebas añadidas

`PrenotificationCandidateScanI12Tests.cs` contiene pruebas xUnit para:

1. Plazo almacenado nulo tras calendario incompleto: candidato reevaluable, sin cambio de estado por la consulta.
2. Paginación `keyset` cuando desaparece un registro tras una aceptación.
3. Vuelta al inicio del rango sin volver a visitar elementos del segmento anterior.
4. Parseo fail-safe del cursor (nulo, inválido, negativo y positivo).

`test_apn_i12_candidate_scan.py` contiene 9 verificaciones estáticas de contratos y 3 ejemplos matemáticos independientes. **No ejecuta C#, EF Core ni el job.**

`run_apn_i12_dotnet.sh` prepara ejecución de las suites actuales y regresiones I08–I11, registra TRX cuando existe SDK. Debe ejecutarse con .NET 10 y dependencias restaurables.

## 4. Regla temporal no modificada

- ACH Colombia: **7 ciclos vigentes**, cuatro ciclos posteriores válidos; excluir cancelados y exigir liberación de salida.
- CENIT: **5 ciclos**, respuesta hasta última sesión, archivos de salida hasta **19:15 America/Bogota**.
- No existe comprobante externo certificado de entrega completa. Este incremento **no lo inventa** ni declara cerrado el riesgo APN-R07.

## 5. Riesgos y homologación pendiente

- La forma `UNION` de ambas consultas, la transacción relacional, `TaskParameters` y la concurrencia Quartz requieren ejecución en **SQL Server y PostgreSQL**. Una prueba EF InMemory no sustituye los proveedores relacionales.
- Con backlog grande, el job puede alcanzar `MaxCandidatesPerRun`; se requiere monitorear avisos, rotación de cursor y cumplimiento del SLA. Un cursor persistido no implica confirmación de respuesta de cámara.
- Fallas de escritura concurrente del parámetro de cursor pueden causar reexamen pero no autorizan una transición; validar configuraciones de ejecución manual en paralelo y reintentos.
- La evaluación de una fila posterior depende de sucesivas ejecuciones si el volumen supera el límite por ejecución. Para determinar latencia real se requieren logs de Quartz con reloj controlado y medición de la cola.

## 6. Ejecución reproducible

```bash
cd ACHInterbank_ajustado
python scripts/audit/prenotificaciones/test_apn_i12_candidate_scan.py
bash scripts/audit/prenotificaciones/run_apn_i12_dotnet.sh
```

**Cierre de incremento:** código y tests escritos; validación Python completada; compilación, xUnit, EF Core SQL Server/PostgreSQL, Quartz y UAT todavía **NO HOMOLOGADOS**.
