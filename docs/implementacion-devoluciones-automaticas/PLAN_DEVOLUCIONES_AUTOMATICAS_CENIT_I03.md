# PLAN ACTUALIZADO — DEVOLUCIONES AUTOMÁTICAS CENIT — I03

**Actualización:** 2026-11-03. **Base única del incremento:** ZIP I02-CENIT. **Estado productivo:** NO-GO.

| Incremento | Alcance separado | Estado en código |
|---|---|---|
| **I01-CENIT** | Configuración por cámara, permisos y auditoría | Incorporado; build/BD/UAT pendientes |
| **I02-CENIT** | Detección pasiva basada en fallos funcionales verificados | Incorporado; build/BD/UAT pendientes |
| **I03-CENIT** | Reserva bajo solicitud explícita; evidencia revalidada, ventana ordinaria CENIT, fecha valor y clave única | **Incorporado; build/BD/UAT pendientes** |
| **I04-CENIT** | Revisión/aprobación/descartes; R06/R23, prenotificación, devolución de devolución; ventanas excepcionales justificadas | **No iniciado** |
| **I05-CENIT** | Generación controlada de artefactos NACHA-M CENIT y verificaciones sin transmisión no autorizada | **No iniciado** |
| **I06-CENIT** | Transporte/acuse/conciliación, pruebas de integración y homologación CENIT | **No iniciado** |

**CENIT:** 3 de 6 incrementos incorporados; **3 incrementos por implementar** (I04–I06). **ACH Colombia:** I01–I08 incorporados previamente, sin homologación; I09-B ejecución en entorno compilado pendiente (**1 fase obligatoria**), I09-A preparación técnica **condicional**. Entre ambas cámaras restan 4 fases obligatorias del plan actual (3 CENIT + 1 ACH) y, posiblemente, I09-A. No son una garantía de certificación ni producción.

## Condiciones de entrada I04

1. Ejecutar compilación y xUnit I01–I03, migraciones SQL Server y PostgreSQL, pruebas de transacción serializable e idempotencia concurrente **antes** de aceptar preparaciones como fuente de una aprobación efectiva.
2. Revalidar continuamente modo, estado, unicidad y evidencia (resultado posterior puede invalidar reserva); no asumir que `PREPARADA_SIN_ENVIO` equivale a autorización.
3. Establecer procedimiento propio y trazabilidad para R06/R23 y excepciones de plazo/Fecha Valor; no convertir R34, Dxx, prenotificaciones ni devolución de devolución en devoluciones ordinarias sin reglas confirmadas.
4. Decisiones humanas con segregación de funciones, permisos, auditoría y estados terminales; garantizar que aún no se genere NACHA-M hasta I05.
5. Verificar calendario de festivos, cambios extraordinarios de sesiones, ventana efectiva del receptor y políticas versionadas CENIT. Conservar operaciones manuales.
6. No modificar las reglas ni pendientes ACH Colombia.

**Punto de parada:** I03-CENIT. No iniciar I04 sin solicitud. **NO-GO**.
