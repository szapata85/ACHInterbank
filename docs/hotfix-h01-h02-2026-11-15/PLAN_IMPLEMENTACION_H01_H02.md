# Plan de implementación — H01/H02 (2026-11-15)

**Abarca exclusivamente** `ContrapartidaDispatchRecovery`, `RetryBatchAsync`/política de ciclo/manual, y pruebas/evidencias. Preserva hotfixes E2E y CENIT/ACH previos. No UAT en esta fase.

| Puerta | Validación y paso | Estado |
|---|---|---|
| H01 | Separar evento watchdog de intento SOAP; 4 intentos + caída de Preparing conservan espacio para quinto; InFlight nunca reenvía | Código y xUnit añadidos |
| H01 legado | Recuperar `RetryPending` con `AttemptCount >=5` como reconciliación terminal, sin borrar registros, sin reset de histórico | Código y xUnit añadidos |
| H02 | Resolver ciclo abierto con política de débito, ventana, calendario y fecha no anterior; CAS para mover cola + claim y revalidar cada ítem | Código y xUnit añadidos |
| G1 | Ejecutar `dotnet restore`, `dotnet build -c Release` y xUnit focal/completo | Pendiente: SDK ausente |
| G2 | SQL Server + PostgreSQL: concurrencia con 2+ instancias; jobs Quartz reinicio y leases; controlar recuento de envíos reales | Pendiente: servidores ausentes |
| G3 | SOAP simulado: caída antes de `InFlight`, después de intención, timeout/transporte, respuesta desconocida; verificar 0 reenvíos inciertos | Pendiente: ambiente ausente |
| G4 | Reconciliar filas heredadas marcadas, revisar logs/metrics, validar retención de XML y auditoría por expertos | Pendiente |
| UAT | Solo al final de todas las fases del proyecto | No ejecutada |

**Criterio de cierre técnico:** ningún `RetryPending` agotado permanece sin transición controlada; una recuperación pre-SOAP no consume el último intento; nunca hay doble claim; un reintento manual no usa ciclo cerrado ni ventana/calendario inválidos; incertidumbre SOAP pasa a revisión, nunca al reenvío. No declarar cierre definitivo sin G1-G3.

**Despliegue y rollback:** tomar respaldo verificado; realizar revisión de registros con contador >=5; desplegar por ventana controlada una vez G1-G4 superados. **No se requiere nueva migración**, pero el escáner cambia filas heredadas a conciliación: el rollback de binarios **no** revierte esas decisiones de estado automáticamente y requerirá proceso operativo de conciliación. Mantener el ZIP original para reversión de código.
