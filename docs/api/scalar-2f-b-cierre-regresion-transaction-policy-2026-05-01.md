# Scalar-2F-B — Cierre de regresión en TransactionPolicyServiceTests

**Fecha (UTC):** 2026-05-01  
**Objetivo:** cerrar la falla de `TransactionPolicyServiceTests.PreviewAsync_RejectsWhenOutsideCycleWindow` y dejar la suite backend completa en verde.

## 1) Diagnóstico técnico

La falla era **intermitente por fecha/hora** en el fixture de prueba.

- La prueba configuraba un ciclo con `ProcessingDate = DateTime.Today` y ventana `00:00-00:30`.
- Si la ejecución ocurría dentro de esa ventana local, `preview.CanSubmit` podía resultar `true`.
- El assert esperaba `false` siempre, por lo que el caso era no determinista.

Clasificación del ajuste:
- **corrección de fixture / corrección de fecha-hora de prueba**.
- No se modificó regla de negocio productiva.

## 2) Cambio aplicado

En `TransactionPolicyServiceTests.PreviewAsync_RejectsWhenOutsideCycleWindow` se cambió:

- de `DateTime.Today`
- a `DateTime.Today.AddDays(1)`

con la misma ventana `00:00-00:30`, garantizando que el caso se ejecute fuera de ventana de forma determinista.

## 3) Evidencia de validación

1. Prueba específica:
   - `PreviewAsync_RejectsWhenOutsideCycleWindow` ✅
2. Grupo relacionado:
   - `TransactionPolicyServiceTests` (4 pruebas) ✅
3. Suite completa backend:
   - `Cfa.ACHInterbank.Tests` (408 pruebas) ✅

## 4) Veredicto de cierre

- Regresión cerrada.
- Suite backend completa en verde (`408/408`).
- Sin cambios en lógica de negocio, rutas, permisos o contratos públicos.
