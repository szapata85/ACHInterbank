# Plan de devoluciones automáticas — implementaciones independientes por cámara

**Iniciativa:** DEV-AUTO-2026-11-02. **Plan revisado:** 2026-11-02, durante I01-ACH. 
**Prevalencia:** este documento reemplaza el orden mixto I01–I08 del `PLAN_INCREMENTAL.md` de I00 **sin borrar la memoria histórica**.

## Regla de separación estricta

1. Un ZIP corresponde a **una sola cámara y un solo objetivo funcional**. Para este incremento: ACH Colombia, configuración persistente únicamente.
2. El código técnico común (modelo existente, generador NACHA-M y auditoría) puede reutilizarse; su reutilización no autoriza a habilitar las dos cámaras simultáneamente.
3. Solo se modifica una cámara en el servicio/endpoint de esta fase. CENIT conserva su estado anterior y su propia puerta de homologación.
4. La configuración de ACH Colombia **nunca** debe habilitar la ejecución de CENIT; CENIT tendrá su endpoint/política propios cuando su fase se autorice.
5. El avance en una cámara no convalida pruebas, causas, plazos, ciclos, formatos ni acuses de la otra.
6. Se entrega ZIP completo, MEMORY_STATE, evidencias, pruebas, riesgos y punto de parada **por microincremento**.

## Ruta A — ACH Colombia (primero)

| Entrega | Alcance | Condición de cierre |
|---|---|---|
| **I01-ACH** | Persistir un modo exclusivo para ACH Colombia (`DESACTIVADO`, `CON_APROBACION`, `AUTOMATICO`) con API administrada, auditoría y concurrencia. No conectar procesamiento. | ZIP, migración, tests de configuración y aislamiento; modo no operativo |
| I02-ACH | Detectar imposibilidad real de aplicar entrada ACH Colombia tras validación; registrar solo incidencia/candidata, sin retorno generado | Casos crédito/débito/prenotificación con causal y prueba de no efecto económico |
| I03-ACH | Preparación de devoluciones elegibles e idempotencia mediante servicio actual; respetar cuatro ciclos y reglas de tardía diferenciadas | Una sola devolución por transacción; sin autoenvío |
| I04-ACH | Aprobación manual en pantalla existente, Angular Material y español natural | Permisos, cancelación, invalidación por vencimiento |
| I05-ACH | Habilitar generación autónoma de casos homologados y reconciliar estados | Test por modo, ciclo, causal, duplicado y recuperación; sin proclamar envío exitoso |
| I06-ACH | Integración autorizada del transporte, acuses, evidencia y pruebas operativas específicas | No envío real sin canal/homologación aprobada |

## Ruta B — CENIT (totalmente independiente; solo después de la ruta A o autorización explícita)

| Entrega | Alcance | Condición de cierre |
|---|---|---|
| I01-CENIT | Configuración propia de CENIT, independiente de ACH Colombia, valor DESACTIVADO | No introducir side effects ni reutilizar habilitaciones de ACH Colombia |
| I02-CENIT | Detección de fallas de aplicación con causas del Anexo A | No confundir rechazo del operador STA con devolución monetaria |
| I03-CENIT | Preparación de devoluciones con control específico de ciclo siguiente y fecha valor | Garantizar formatos/identificación de original; sin autoenvío |
| I04-CENIT | Excepciones R23 y R06, devolución de devolución y aprobación cuando proceda | Plazos y causales respaldados por norma; NO-GO sin homologación |
| I05-CENIT | Generación automática solo sobre rutas de retorno autorizadas | Respeto de bloqueos previos ReturnOut; sin saltar restricciones |
| I06-CENIT | Integración de acuses, conciliación y certificación individual | No equiparar archivo creado con transacción compensada |

**En cualquier fase:** un problema material de cumplimiento, permisos o idempotencia bloquea la activación de esa ruta; no bloquea documentar/testear lo que sea seguro. Se mantiene vigente el estado `NO-GO` productivo sin UAT.

## Condiciones de cierre I01-ACH

- Se confirma configuración por ACHCOL exclusivamente, con validación de modo, permisos de consulta/administración existentes y auditoría genérica `AuditLogs`.
- El nuevo campo en `ClearingHouseConfigs` es infraestructura compartida **con predeterminado DESACTIVADO**, pero solo se expone el endpoint de ACH Colombia; no se modifica ningún proceso CENIT.
- La conexión del parámetro a procesadores queda intencionalmente pendiente. Incluso si se selecciona `AUTOMATICO`, `ProcessingConnected=false`.
- No se modifica SPA en I01-ACH, ni los endpoints de generación manual, ni las transferencias existentes.
- Los tests no ejecutados deben rotularse como tales hasta correrlos en .NET.
