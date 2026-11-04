# APN-I11 — Contrato propuesto de evidencia de recepción completa

**Proyecto:** ACH Interbank · **Cámaras:** ACH Colombia y CENIT · **Fecha:** 2026-11-03

**Estado del contrato:** `DISEÑADO / SIN PRODUCTOR EXTERNO VERIFICADO / NO INTEGRADO`.

## 1. Problema que NO debe ocultarse

Un `AchCycle.OperationalStatus == Closed`, el vencimiento de la hora de liberación, un `ACK`, un mensaje `NoActivity`, una lista vacía de ingestiones o una transferencia MFT `Processed` **no constituyen, por sí solos, un comprobante positivo de que el operador entregó todos los archivos esperados**. Las correcciones APN-I08–I11 aplican barreras sobre evidencia conocida, pero no pueden inferir los archivos que aún no llegaron.

## 2. Contrato mínimo para la futura integración

Propuesta de un comprobante **por cámara, fecha operacional, sesión de recepción y participante** emitido o corroborado por una fuente habilitada por el operador/canal, con estos campos:

| Campo | Regla |
|---|---|
| `ClearingHouseCode` | `ACHCOL` o `CENIT` explícito; nunca implícito |
| `OperationalDate` | Fecha de operación **America/Bogota** |
| `ReceivingParticipantId` | Participante receptor al que corresponde la entrega |
| `SessionOrCycleId` | Identificador oficial de ciclo o sesión de salida |
| `EvidenceId` | Identificador único/idempotente del comprobante |
| `ProducedAtUtc` | Instante autenticado de emisión del comprobante |
| `ExpectedObjects` | Manifiesto inmutable de archivos/mensajes esperados, incluyendo cero explícito si hay evidencia válida de no actividad |
| `ObjectDigestSha256` | SHA-256 del contenido y correspondencia con cada archivo recibido (donde el canal provea archivo) |
| `SourceAuthority` | Identificación del operador o proveedor del canal autorizado |
| `ProofReference` | Referencia verificable a firma, comprobante, manifiesto remoto u otro mecanismo contractual |
| `VerificationOutcome` | Verificación de origen, integridad, ámbito y correlación de todos los objetos; rechazado/pendiente ante ambigüedad |

Ninguno de esos campos constituye, por sí mismo, un comprobante real. El contrato **no será habilitado** hasta que la cámara o el operador del canal confirme oficialmente el mecanismo, su semántica, su autenticidad y su cobertura de archivos y sesiones.

## 3. Semántica de la barrera futura

Se podrá evaluar un cierre de recepción solo si se cumple todo:

1. Se verificó la fuente/autenticidad del manifiesto para **esa cámara y ese participante**.
2. Están presentes todas las sesiones relevantes para el plazo de la prenotificación: cuatro posteriores para ACH Colombia (7 ciclos vigentes) y hasta la última sesión del día operacional para CENIT (5 ciclos).
3. Cada objeto del manifiesto existe en los registros locales y está íntegramente procesado y correlacionado, o dispone de un justificativo autorizado de ausencia de actividad.
4. No existen MFT, NACHA ni respuestas CENIT asociadas pendientes, ambiguas, bloqueadas o fallidas sin resolver.
5. El vencimiento normativo ha transcurrido, considerando **hora final de liberación**, no solamente hora de corte.
6. La transmisión original está demostrada y el intento canónico de validación sigue vigente.
7. Las transiciones se realizan de forma transaccional e idempotente.

La falta de comprobante externo no puede reemplazarse por `true`. Una vez implementado un productor, la política de despliegue (fail-closed, degradación supervisada y reconcilación manual) deberá aprobarse operativamente; no se introduce ahora una restricción que bloquearía sin señal real todas las aceptaciones tácitas.

## 4. Comportamiento de las dos cámaras

**ACH Colombia:** siete ciclos. Para una prenotificación cursada en el primero, el cuarto ciclo posterior corresponde al ciclo 5; los archivos asociados se liberan hasta las 15:30 hora de Colombia conforme al calendario suministrado. Una prenotificación en los últimos ciclos puede necesitar ciclos del siguiente día hábil. Los ciclos cancelados **no** deben contarse como oportunidades de devolución.

**CENIT:** cinco sesiones. La última finaliza a las 18:45, con archivo de salida hasta las 19:15 hora de Colombia. Las respuestas recibidas después de las 00:00 UTC pueden pertenecer todavía a la fecha operacional anterior de Colombia.

## 5. Cómo se aceptará el contrato posteriormente

- Prueba negativa: comprobante falso, incompleto o de otra cámara/participante/ciclo → aceptación tácita bloqueada.
- Prueba de integridad: archivo faltante, hash distinto o recepción MFT sin ingestión → bloqueada.
- Prueba de regresión: CENIT NACK/OperatorRejected y devoluciones NACHA dentro de la última sesión → rechazadas o revisión; nunca aceptación positiva.
- Prueba de concurrencia: fin de recepción y devolución versus Quartz sobre SQL Server y PostgreSQL → estados coherentes e idempotentes.
- Prueba de ausencia: cámara acredita positivamente cero archivos esperados para esa sesión, sin extrapolar a otras sesiones.
- Evidencia de un usuario/operador del canal de prueba, con muestras autenticables y verificación de autenticidad.

## 6. Pendencias externas

1. Identificar dueño técnico del MFT ACH Colombia y del gateway CENIT.
2. Confirmar que exista un manifiesto/comprobante o especificación oficial de fin de entrega, sus alcances y forma de validación.
3. Recibir muestras UAT de **todos** los tipos de sesión (con y sin devoluciones), y protocolo de reenvíos tardíos.
4. Definir retención, alertas y resolución manual para casos ambiguos.
5. Implementar productor/verificador únicamente después de confirmar la semántica externa, con pruebas integradas y migración controlada si aplica.

**Conclusión:** APN-I11 entrega el contrato de integración verificable **como diseño**, no como una señal positiva ya implementada. `APN-R07` sigue abierto.
