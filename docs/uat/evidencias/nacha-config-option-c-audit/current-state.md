# Fase 6A - Estado actual

| Pregunta | Respuesta |
|---|---|
| La parametrizacion legacy se usa hoy | Si |
| `nacha-config profiles` se usan hoy | Parcial |
| `NachaFileBuilder` puede operar table-driven | Parcial |
| Modo default actual | `LEGACY` |
| Hay fallback legacy silencioso | Si, en varios caminos |
| ACH Colombia tiene perfil separado | Parcial, por backfill legacy si no hay perfiles |
| CENIT tiene perfil separado | No evidenciado |
| Los perfiles cubren registros 1/5/6/7/8/9 | Parcial y derivado de legacy |
| Existe trazabilidad FieldDefinition -> valor generado | Parcial en memoria/auditoria JSON, no normalizada |
| Existen errores controlados Opcion C | Parcial/insuficiente |
| Pantallas legacy | `/ach-cycles/nacha/layouts`, `/ach-cycles/nacha/definitions` |
| Pantalla candidata oficial | `/nacha-config-admin/perfiles` |

El sistema tiene dos mundos activos: legacy/simple y `nacha-config`. El primero sigue siendo fuente funcional del builder en modo default. El segundo es un modelo candidato reutilizable, pero todavia no gobierna oficialmente toda la generacion.

Bloqueo principal: no se puede declarar Opcion C lista mientras el builder pueda generar por legacy/fallback o mientras CENIT no tenga perfil publicado completo y demostrable.

