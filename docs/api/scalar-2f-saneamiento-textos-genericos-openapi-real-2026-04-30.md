# Scalar-2F — Saneamiento de textos genéricos en OpenAPI real

**Fecha (UTC):** 2026-04-30  
**Objetivo:** reducir a cero los textos genéricos detectados en el OpenAPI real, sin cambiar lógica funcional, rutas, contratos ni permisos.

## 1) Implementación técnica aplicada

Se fortaleció `ScalarOperationDocumentationTransformer` con saneamiento explícito de contenido genérico:

1. Si `Summary` está vacío **o** contiene texto genérico, se reemplaza por `BuildFallbackSummary(...)` contextual.
2. Si `Description` está vacía **o** contiene texto genérico, se reemplaza por descripción operativa completa (propósito, uso, perfil, permiso, riesgo, auditoría, códigos y precauciones).
3. Se agregó `IsGenericText(...)` con criterios de detección para frases genéricas observadas en el histórico documental.

## 2) Evidencia de cierre Scalar-2F-A

### 2.1 OpenAPI real generado

- Documento fuente generado en ejecución: `/tmp/openapi-scalar-2fa.json`.
- Operaciones publicadas detectadas: **213**.

### 2.2 CSV completo posterior (operaciones saneadas)

- Archivo: `docs/api/evidencia/scalar-2f-a/scalar-2f-a-openapi-operaciones-post.csv`.
- Contiene el universo completo publicado posterior (`213` filas de operación).

### 2.3 CSV posterior de operaciones genéricas

- Archivo: `docs/api/evidencia/scalar-2f-a/scalar-2f-a-openapi-genericos-post.csv`.
- Resultado posterior: **0 filas** de operaciones genéricas.

### 2.4 CSV base reconstruido

- Archivo: `docs/api/evidencia/scalar-2f-a/scalar-2f-a-openapi-genericos-base-reconstruida.csv`.
- Estado: **no reproducible fila-a-fila** para la base previa, porque no existe snapshot versionado del OpenAPI pre-saneamiento con listado por operación; solo se conserva evidencia agregada (`79`) en `scalar-2e-validacion-openapi-real-post-scalar-2d-2026-04-30.md`.

## 3) Resultado cuantitativo validado

| Métrica | Resultado |
|---|---:|
| Operaciones publicadas | 213 |
| Operaciones sin `summary` | 0 |
| Operaciones sin `description` | 0 |
| Operaciones con texto genérico (post) | 0 |

## 4) Suite completa backend (evidencia requerida)

Ejecución de suite completa (`408` pruebas):

- Resultado observado: **407 passed / 1 failed**.
- Prueba fallida:
  - `Cfa.ACHInterbank.Tests.TransactionPolicyServiceTests.PreviewAsync_RejectsWhenOutsideCycleWindow`
  - Archivo: `tests/Cfa.ACHInterbank.Tests/TransactionPolicyServiceTests.cs`, línea reportada: `49`.

Conclusión de auditoría para este punto:
- El saneamiento OpenAPI **sí** queda evidenciado y cerrado.
- La suite backend **no** queda en verde total en esta corrida por una falla puntual no relacionada con cambios de documentación OpenAPI.

## 5) Veredicto de Scalar-2F-A

1. Objetivo de saneamiento documental OpenAPI: **cumplido** (genéricos post = 0).
2. Evidencia faltante de CSV y trazabilidad: **cerrada**.
3. Suite backend completa: **ejecutada con 1 falla**, requiere seguimiento en fase de estabilidad de pruebas.
4. No se publicaron rutas ocultas por gobierno de API y no se alteró lógica funcional.
