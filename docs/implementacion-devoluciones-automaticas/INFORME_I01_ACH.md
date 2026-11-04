# Informe incremental I01-ACH — configuración separada para ACH Colombia

**Fecha:** 2026-11-02. **Base:** ZIP I00 acumulativo. **Alcance:** solo ACH Colombia, ninguna automatización activada.

## Cambios implementados en el código

- `ClearingHouseConfig.AutomaticReturnMode`: campo persistido, longitud 20, predeterminado `DESACTIVADO`, restricción de tres modos, token de concurrencia. Se aprovecha tabla existente; no se crea una tabla nueva.
- Migración SQL Server `20261102210000_AchColombiaAutomaticReturnConfigurationI01`: añade columna y restricción; nuevas y actuales filas quedan desactivadas. Snapshot actualizado.
- Servicio `AchColombiaAutomaticReturnSettingsService`: busca cámara **ACHCOL** por código, verifica configuración propia de la cámara, rechaza configuraciones compartidas, valida modo, actualiza solo el registro ACH Colombia.
- `GET/PUT api/ach-colombia/devoluciones-automaticas/configuracion`: consulta `P1.ConfigRead`; modificación `P1.ConfigManage`; 400 por modo inválido y 409 por conflicto concurrente.
- Auditoría: reutiliza mecanismo existente de `AchDbContext.SaveChangesAsync` que produce `AuditLogs` con usuario, fecha, antes/después y campos cambiados. No añade un motor de auditoría nuevo.
- `ProcessingConnected=false` siempre en esta fase: parámetro persistido pero no conectado a procesadores. No se confunde configuración con funcionalidad activa.
- Pruebas xUnit nuevas para predeterminado, aislamiento CENIT, cambios, persistencia entre contextos, valores no válidos, configuración propia y atributos de autorización. Se incluyen para ejecutar en el ambiente .NET.

## No modificado

CENIT: sin controlador, servicio, reglas de retorno ni conexión del parámetro. SPA Angular Material y procesos manuales existentes: sin cambios. No se incorporó autoenvío, disparador por error, archivo ni efecto monetario.

## Riesgos conocidos y comprobación pendiente

- Requiere aplicar y verificar migración SQL Server en ambiente adecuado (sin datos reales en este chat).
- Dotnet SDK ausente en este ambiente: compilación, xUnit y comprobación de autorización en runtime **no ejecutadas**.
- Aun cambiando `Mode` a `AUTOMATICO`, no se generará devolución automática. Esa conexión se reserva para I05-ACH, tras detección, aprobación e idempotencia.
- La migración comparte infraestructura de configuración, **no comportamiento operativo**. No se debe reusar el endpoint ACHCOL para CENIT.
- Debe confirmarse la configuración propia de ACH Colombia antes de permitir su administración; se falla de forma cerrada si el registro está mal asociado.

## Punto de parada

**I01-ACH realizado en código/documentos; pendiente de ejecutar y certificar en entorno .NET.** Próximo incremental: **I02-ACH — detección de fallos reales de aplicación**, únicamente ACH Colombia. CENIT: NO INICIADO. Producción: NO-GO.
