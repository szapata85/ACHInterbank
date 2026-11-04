# F08 — Puertas GO/NO-GO y responsabilidades

**Decisión actual: NO-GO para producción, integración bancaria real y homologación CENIT.** Se admite traslado del ZIP para revisión, restauración y QA controlado.

| Puerta | Condición verificable | Estado 2026-11-02 | Decisión |
|---|---|---|---|
| G0 | SDK .NET `10.0.403` instalado y proyecto compila | AUSENTE / NO EJECUTADO | BLOQUEA |
| G1 | Suite xUnit completa y ACH Colombia verde, SQL/Quartz/idempotencia PT relevantes | NO EJECUTADO | BLOQUEA |
| G2 | Archivo oficial operador ACK/NACK/ADV/ROR/NACHA/STA, calendario/sesiones | NO EJECUTADO | BLOQUEA |
| G3 | Tesorería reconcilia CUD manualmente y vigencia normativa confirmada | NO EJECUTADO | BLOQUEA |
| G4 | Auditoría confirma 13 HC con trazabilidad y firma | 0/13 cerrados | BLOQUEA |

### Responsabilidades de cierre (propuestas)

**Desarrollo y QA:** TRX, logs, suites C# y casos negativos, evidencias de fallos y reintento. **DB/Operación:** transacciones SQL, locks, concurrencia y bitácoras Quartz. **Equipo CENIT/Operador:** acuses, muestras NACHA y restricciones de sesiones certificadas. **Equipo ACH Colombia:** regresión funcional de la cámara previamente auditada. **Tesorería:** conciliación CUD por procedimiento existente sin nueva API. **Auditoría/Cumplimiento:** aceptación final, validación documental y cierre por HC.

No aprobar al amparo de porcentajes de entrega. El progreso F01–F08 representa entregables completos, no aprobación regulatoria.
