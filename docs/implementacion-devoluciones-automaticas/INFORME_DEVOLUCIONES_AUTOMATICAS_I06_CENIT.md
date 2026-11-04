# INFORME TÉCNICO — I06-CENIT (2026-11-03)

## Resumen y resultado

Se avanzó sobre I06-CENIT (integración y conciliación), implementando un **modelo de lectura para diagnóstico de integración de devoluciones locales I05**. El resultado **NO equivale a completar la integración bancaria**. Se detectó una dependencia fundamental en el código real: I05 genera solo contenido cifrado con nombre `NO_TRANSMITIR` y sin exportación externa ni identidad de archivo homologada; por tanto, no es correcto correlacionar acuses existentes con la generación local ni marcar operaciones devueltas o liquidadas.

## Implementación concreta

| Componente | Cambio verificable | Efectos |
|---|---|---|
| Servicio `CenitReturnIntegrationReadService` | Dos lecturas, una por id y otra acotada con indicador de truncamiento; filtro `CENIT` por navegación relacional | Solo `AsNoTracking`, nunca `SaveChanges` |
| Integridad local | Descifrado interno con mismo propósito I05, comparación SHA-256, longitud 1060 y estructura NACHA-M, perfil, nombre interno, causal, traza original/nueva, monto | No devuelve archivo ni expone datos personales |
| Duplicidad/collision | Cuenta `AchReturnsGenerated` del original y compara traza local con `AchFileExportTransactions` CENIT | Señales conservadoras para intervención humana; no ordena envío |
| Respuestas de cámara | Consulta `CenitChamberResponses` por transacción/trace restringida a CENIT | **Nunca** atribuye un acuse al archivo local I05 |
| API | `GET /api/cenit/devoluciones-automaticas/conciliacion` y `GET /api/cenit/devoluciones-automaticas/conciliacion/{id}` | Autenticación y `P1.ConfigRead`, sin caché |
| DTO | `TransmissionEnabled=false`, `ChamberAcknowledgementAttributed=false`, `FinanciallyReconciled=false` | Impide presentar diagnóstico como conciliación real |
| Infraestructura | Una línea de registro DI; sin nuevas tablas, estados financieros, migraciones, tareas, SPA o reuso indebido ACH Colombia | Mantiene independencia de cámara |

La clasificación admite `NO_GENERADO`, `INTEGRO_LOCAL`, `METADATOS_INCONSISTENTES`, `HASH_NO_COINCIDE`, `ESTRUCTURA_INVALIDA` y `CLAVE_O_CIFRADO_NO_DISPONIBLE`; en el consolidado distingue `PENDIENTE_SIN_TRANSMISION`, `CONFLICTO_REQUIERE_REVISION` o `INCONSISTENTE_O_INCOMPLETA`. Todas son informativas.

## Decisión de no incorporar transporte en I06

La infraestructura común ya maneja exportaciones y respuestas de CENIT, pero falta un contrato aprobado para identificar inequívocamente el artefacto de I05, colocar un nombre externo válido y vincular **ese mismo archivo y su huella** con una exportación, intento, acuse de cámara y liquidación. Reutilizar esos servicios sin un enlace certificado permitiría declarar **enviada o conciliada una devolución que nunca salió de la aplicación**. Se mantuvo el bloqueo, sin inventar un resultado externo.

Como consecuencia, **el tramo de transporte/autorización, acuse y conciliación financiera de I06 continúa abierto** y requerirá ambiente controlado, golden files, trazabilidad, locking con devoluciones manuales y homologación. No se añadieron flags que el operador pudiera activar accidentalmente para enviar I05.

## Normativa respetada y excluida

Las fuentes locales son el *Manual de Especificaciones Formato NACHA-M CENIT* (7 de mayo de 2026, registros de 106 caracteres y adenda 99) y *DSP-152 Anexo 2* (27 de febrero de 2025, ciclos y fecha valor), además del Anexo A de causales de devolución. I06 no reinterpreta ni extiende los casos especiales R06, R23, R34, prenotificaciones, CTX o R60–R69. No usa reglas operativas ACH Colombia para inferir horarios CENIT. La configuración independiente DESACTIVADO se conserva, incluso para artefactos anteriores.

## Pruebas agregadas

`CenitReturnIntegrationI06Tests.cs` cubre: ausencia de artefacto, lectura tras vencimiento sin reautorizar, integridad SHA/cifrado, corrupción de campos NACHA, duplicado manual, ACK de CENIT no atribuible, traza encontrada en otra exportación, respuesta ACHCOL ignorada, pérdida de llaves, ámbito CENIT, límites/paginación, permisos y ausencia de endpoints de escritura y exportaciones financieras. **Casos incorporados, no ejecutados.**

## Validación real disponible

- Grafo solución: `7/7`, referencias comprobadas.
- Estáticos I06: `62/62`; regresiones CENIT I01–I05: `29/29`, `50/50`, `57/57`, `71/71`, `84/84`.
- ACH Colombia conserva I02–I08; I04-ACH mantiene `32/35` abierto.
- Integridad ZIP origen/final y comparación SHA de archivo: ver `EVIDENCIA_...I06_CENIT.md` y manifiesto.
- No hay SDK .NET instalado; ni compilación ni xUnit, ni SQL Server/PostgreSQL, ni integración JWT, ni acuses de cámara, ni homologación ejecutada.

**Estado I06:** avance verificable en código; **integración productiva y conciliación efectiva pendientes**, NO-GO para ambas cámaras.
