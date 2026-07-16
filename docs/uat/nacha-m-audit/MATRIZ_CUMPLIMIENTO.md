# Matriz de cumplimiento preliminar NACHA-M

Estados permitidos aplicados: `Cumple`, `No cumple`, `No demostrado`, `No aplica`, `Pendiente de definición oficial`.

| ID | Dominio | Requisito | ACH Colombia | CENIT | Evidencia | Bloqueante | Observación |
|---|---|---|---|---|---|---|---|
| C-001 | Fuente | Manual técnico vigente aplicable a CFA | No demostrado | No demostrado | MAN-004 V32 local; CENIT sin ficha técnica | Sí | ACH requiere confirmación de vigencia/alcance CFA; CENIT requiere documento técnico |
| C-002 | Físico | 106 por registro | Cumple | No demostrado | Tres archivos 106; sólo ACHCOL tiene fuente | Sí CENIT | Similitud no eleva CENIT |
| C-003 | Físico | Sin fin de línea | Cumple | No demostrado | Bytes | Sí CENIT | Política de parser también falla |
| C-004 | Físico | Factor 10 y padding | Cumple | No demostrado | Bloques y T9 padding | Sí CENIT | Descriptor ACH está desplazado aunque resultado físico cumple |
| C-005 | Físico | Encoding/repertorio | No demostrado | No demostrado | ASCII actual y byte monobyte comparativo | Sí | Falta política explícita por cámara |
| C-006 | T1 | Fecha de 8 y posiciones | No cumple | No demostrado | MAN-004 + bytes + seeder | Sí | Perfil usa 6 |
| C-007 | T1 | Hora/FileId/size/block/format | No cumple | No demostrado | MAN-004 + perfil | Sí | Desplazamiento de dos posiciones |
| C-008 | T1 | Nombres/reference/reservado | No cumple | No demostrado | MAN-004 + bytes | Sí | Literal de ciclo cruza campos ACHCOL |
| C-009 | T5 | Fechas de 8 | No cumple | No demostrado | MAN-004 + perfil | Sí | Usa 6 |
| C-010 | T5 | Estado y entidad originadora | No cumple | No demostrado | MAN-004 + perfil | Sí | Offsets erróneos |
| C-011 | T5 | Lote inicia 1 dentro del archivo | No cumple | No demostrado | MAN-004 + daily generator | Sí | Reinicio diario no equivale a reinicio por archivo |
| C-012 | T5/T8 | Correspondencia de lote | No cumple | No demostrado | 0/4 en offsets oficiales | Sí | Sí coincide en offsets incorrectos |
| C-013 | T6 | Monto 18 | No cumple | No demostrado | MAN-004 + seeder | Sí | Perfil 10 |
| C-014 | T6 | Cuenta/ID/nombre | No cumple | No demostrado | MAN-004 + seeder | Sí | Desplazados |
| C-015 | T6 | Indicador adenda | No cumple | No demostrado | 0/327 oficial | Sí | Perfil posición 79 |
| C-016 | T6 | Trace Number | No cumple | No demostrado | 0/327 oficial | Sí | Perfil 80–94 |
| C-017 | T6/T7 | Asociación y sufijo | No cumple | No demostrado | 4/327 inmediatos y 0 matches | Sí | Builder agrupa records |
| C-018 | T7 | Variantes por flujo | No demostrado | No demostrado | Descriptor genérico | Sí | No se modela ficha completa |
| C-019 | T8 | Conteo de registros | Cumple | No demostrado | Recálculo 4/4 | Sí CENIT | Algoritmo ACH aprovechable |
| C-020 | T8 | Entry hash | Cumple | No demostrado | Recálculo 4/4 | Sí CENIT | Requiere vector oficial para cierre |
| C-021 | T8 | Totales de 18 | No cumple | No demostrado | Perfil 12 vs MAN-004 18 | Sí | Cálculo correcto, render incorrecto |
| C-022 | T9 | Conteo lotes/bloques/entradas | Cumple | No demostrado | Recálculo | Sí CENIT | Offsets comunes |
| C-023 | T9 | Hash general | Cumple | No demostrado | Recálculo | Sí CENIT | Sin vector externo |
| C-024 | T9 | Totales/reservado | No cumple | No demostrado | MAN-004 + bytes | Sí | 12 vs 18 |
| C-025 | Nombre | Patrón y extensión | No demostrado | No demostrado | MAN-004 vs comparativo/Manual STA ausente | Sí | Resolver alcance de interfaces |
| C-026 | Arquitectura | Perfiles separados | Cumple | Cumple | CfgProfile por cámara | No | El contenido CENIT no está demostrado |
| C-027 | Arquitectura | Fail-fast sin fallback | Cumple | Cumple | Official builder/tests | No por sí sola | Cumple sólo en la ruta oficial; `appsettings.json` mantiene HYBRID |
| C-028 | Arquitectura | 100% table-driven | No cumple | No cumple | Switches, aliases, core y parser imperativo | Sí | Conservar sólo cálculos críticos en core |
| C-029 | Reglas | RuleId + fuente + severidad + sensibilidad | No cumple | No cumple | Metamodelo | Sí | Metadata incompleta |
| C-030 | Reglas | Validación previa cerrada | No cumple | No cumple | Config validation/renderer | Sí | No ejecuta todas las rules ni valida posiciones normativas |
| C-031 | Truncamiento | Sin truncamiento silencioso | No cumple | No cumple | Legacy renderer | Sí | Official path falla, HYBRID sigue disponible |
| C-032 | Concurrencia | Consecutivo externo atómico SQL/PG | Cumple | Cumple | Providers/tests estáticos | No por sí sola | Cumplimiento técnico estático; política/fecha normativa aún no demostrada |
| C-033 | Concurrencia | Lote normativo e idempotente | No cumple | No demostrado | Batch store/generator | Sí | Implementación robusta de política errónea para ACHCOL |
| C-034 | Zona horaria | America/Bogota uniforme | No cumple | No cumple | `DateTime.Date`, `Today` | Sí | Existe helper de scheduler no reutilizado |
| C-035 | Duplicados | Gate de nombre/hash antes de envío | No demostrado | No demostrado | Duplicate guard/tests | Sí | No se ejecutó ni homologó por cámara |
| C-036 | Parser | Consume archivo completo por bytes | No cumple | No cumple | `ReadLineAsync` único | Sí | Además persiste; no fue ejecutado |
| C-037 | Privacidad | Archivos reales fuera de Git | No cumple | No cumple | `git ls-files` | Sí | Los tres están rastreados |
| C-038 | Privacidad | Trazas enmascaradas por campo | No cumple | No cumple | Trace reconstruible | Sí | Test sólo filtra secretos técnicos |
| C-039 | Privacidad | Logs/excepciones minimizados | No cumple | No cumple | Parser/builder/global exception | Sí | Pueden contener importes/identificadores/Trace |
| C-040 | Pruebas | Golden normativos sintéticos | No demostrado | No demostrado | Fixtures internos no certificados | Sí | Los golden actuales estabilizan el perfil erróneo |
| C-041 | Pruebas | Negativas/propiedades/límites | No demostrado | No demostrado | Inventario de tests sin ejecución | Sí | Faltan matrices aprobadas como oracle |
| C-042 | Pruebas | SQL Server/PostgreSQL concurrente | No demostrado | No demostrado | Tests identificados, no ejecutados | Sí | Deben ejecutarse en fase autorizada |
| C-043 | Gobierno | Aprobación humana previa a LIVE | No demostrado | No demostrado | Checklists/NO-GO | Sí | Perfil PUBLICADO no es firma regulatoria |
| C-044 | Seguridad | No exposición en test discovery | No demostrado | No demostrado | Nombres de casos parametrizados pueden contener datos completos aunque sintéticos | Sí | Mantener valores fuera del display name |

## Resultado agregado

| Cámara | Decisión | Causa dominante |
|---|---|---|
| ACH Colombia | **NO-GO** | Campos críticos desplazados, fechas/lotes/Trace/adendas/totales no conformes y privacidad insuficiente |
| CENIT | **NO-GO** | Reglas críticas no demostradas por ausencia de ficha técnica/Manual STA, además de brechas comunes de implementación y privacidad |
