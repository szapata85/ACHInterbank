# Autoverificación adversarial — 2026-11-15

**Riesgo analizado:** doble envío monetario, stale-write en CFA/Axon, cuarentena anulada, timeout como rechazo definitivo, pérdida de respuesta tardía, parser con éxito simulado, correlación por referencia duplicada.

**Defectos detectados durante el incremento y corregidos dentro del mismo:**

1. El mapping trace writer utilizaba `SaveChanges` antes de la confirmación final CFA; se agregó una transacción corta con fencing antes del SOAP.
2. La finalización CFA antigua no comprobaba token/lease en persistencia: se sustituyó por CAS + commit corto y evidencia en contexto separado si perdió el claim.
3. Los estados Axon de preflight podían sobrescribir un `InFlight` concurrente: salidas y excepciones pretransporte se finalizaron mediante CAS atómico.
4. Las escrituras SQL de Axon por `ExecuteUpdate` no rotaban `AchResponse.Version`: se añadió nueva versión optimista en fin, preflight y cuarentena.
5. Parser SOAP permitía nodos imprevistos junto a `RTAACH=00`: ahora los rechaza conservadoramente; falta esquema XSD oficial.

**Salvaguardas comprobables:** el lease vencido se pone en `Blocked`/`RequiereRevisionManual` y no se reactiva; la evidencia tardía solo guarda hashes/códigos, el watchdog Axon no invoca SOAP, no se abren transacciones de BD durante llamadas remotas.

**Límites de esta revisión:** 26 controles de código/modelo SQLite pasaron, pero no cubren comportamiento runtime real del código .NET, provider SQL ni circuitos SOAP. No se afirma ausencia certificada de carreras o cierre bancario. Cualquier falla observada en G1–G3 requiere corrección y reauditoría.
