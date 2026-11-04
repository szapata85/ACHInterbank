# PLAN — DEVOLUCIONES AUTOMÁTICAS CENIT — actualización I02

**Actualizado:** 2026-11-02. **Base:** ZIP I01-CENIT. **NO-GO productivo.** Este documento acompaña al plan histórico del I01, que permanece intacto.

| Incremento CENIT | Entregable y restricción | Estado |
|---|---|---|
| I01 | Parámetro independiente, auditoría, autorización, concurrencia | **Incorporado al código; compilación/UAT pendiente** |
| I02 | Detección pasiva **a demanda** basada en rechazo funcional real del core, sin generar ni enviar | **Incorporado al código; compilación/UAT pendiente** |
| I03 | Preparación controlada: evidencia original, siguiente sesión válida, fecha valor, idempotencia con bloqueo único, ventana normativa CENIT | **No iniciado** |
| I04 | Aprobación, solicitudes R06/R23, prenotificaciones y devolución de devolución; permisos y controversias | **No iniciado** |
| I05 | Generación controlada de archivos/artefactos CENIT solamente con causal y sesiones homologadas, sin transmisión productiva no autorizada | **No iniciado** |
| I06 | Transporte/acuses/conciliación CENIT e integración/homologación comprobadas, sin asumir acreditación por archivo | **No iniciado** |

**Progreso planificado CENIT:** 2 de 6 incorporados; **4 por implementar**. **ACH Colombia:** I01–I08 incorporados en código, I09-B ejecución pendiente e I09-A condicional; no tocar código funcional ACH en la ruta CENIT. **Producción de ambas cámaras: NO-GO.**

## Puertas de control I03

1. No usar `INDICIO_CAUSAL_CENIT` como autorización. Reconsultar la última ejecución, correlación, cámara y código efectivo **bajo transacción consistente** antes de preparar.
2. Resolver el ciclo siguiente real, Fecha Valor y límite de devolución del Anexo 2 CENIT. No mezclar ciclos/ventanas ACH.
3. Establecer idempotencia de BD por original sin duplicación y cancelación ante cambio de evidencia o modo.
4. Mantener R06/R23, R34, prenotificaciones y devolución de devolución para evaluación humana/específica; no asumir causales o fondos por descripción del core.
5. No generar NACHA-M ni disparar transporte en I03. Pruebas xUnit y relacionales SQL Server/PostgreSQL y UAT hasta entonces pendientes.
6. Reconciliar ediciones normativas Anexo A/B (2023), DSP-152 Anexo 2 (2025) y NACHA-M (2026) frente a homologación.

**Punto de parada:** I02-CENIT, no iniciar I03 sin instrucción nueva.
