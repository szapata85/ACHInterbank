# Conciliación 220 vs 213 en OpenAPI/Scalar — Scalar-2B-A

**Fecha (UTC):** 2026-04-29  
**Objetivo:** cerrar la discrepancia entre el inventario estático (220 operaciones) y la publicación real en OpenAPI (213 operaciones).

## 1) Resultado de conciliación

- Inventario estático por atributos HTTP en controladores: **220 operaciones**.
- Operaciones publicadas en OpenAPI real validado en Scalar-2B: **213 operaciones**.
- Diferencia conciliada: **7 operaciones**.

## 2) Las 7 operaciones faltantes exactas

| # | Método | Ruta estática | Controlador | Evidencia en código |
|---:|---|---|---|---|
| 1 | GET | `/Servers` | `ServersController` | `[HttpGet]` en línea 19. |
| 2 | GET | `/Tests` | `TestsController` | `[HttpGet]` en línea 22. |
| 3 | GET | `/Tests/Prueba` | `TestsController` | `[HttpGet("Prueba")]` en línea 50. |
| 4 | GET | `/oauth2/jwks` | `JwksController` | `[HttpGet("jwks")]` en línea 20. |
| 5 | GET | `/oauth2/TokenClientAssertions` | `JwksController` | `[HttpGet("TokenClientAssertions")]` en línea 30. |
| 6 | POST | `/oauth2/client-assertion` | `JwksController` | `[HttpPost("client-assertion")]` en línea 40. |
| 7 | POST | `/oauth2/Genearte-client-assertion` | `JwksController` | `[HttpPost("Genearte-client-assertion")]` en línea 54. |

## 3) Clasificación técnica de la discrepancia

### 3.1 Falsos positivos del inventario estático

**Sí, para propósito de publicación OpenAPI.**

Estas 7 operaciones existen en código y por eso el barrido estático las contó. Sin embargo, no aparecen en el OpenAPI publicado validado en Scalar-2B, por lo que deben tratarse como **falsos positivos del inventario estático respecto del universo realmente publicado**.

### 3.2 Rutas no publicadas por configuración

**Sí, en la práctica de publicación actual.**

Aunque no tienen `ApiExplorerSettings(IgnoreApi = true)` explícito en esos controladores, el resultado observable es que no son emitidas en el documento OpenAPI real.

### 3.3 Endpoints duplicados

**No.**

La discrepancia no proviene de duplicación de filas en OpenAPI; proviene de operaciones presentes en código que no entraron al documento generado.

### 3.4 Endpoints ocultos por ApiExplorer

**No hay evidencia explícita por atributo local** (`ApiExplorerSettings`) en estos controladores.

### 3.5 Endpoints en código que no salen en OpenAPI

**Sí, exactamente 7** (los de la tabla de la sección 2).

### 3.6 ¿Problema?

**Sí: problema de consistencia de inventario/publicación.**

- No es un problema de contratos rotos en rutas publicadas.
- Sí es un problema de gobernanza documental: el inventario estático y la publicación OpenAPI real no coinciden para estos 7 casos.

## 4) Evidencia y trazabilidad de comandos

Comandos aplicados para la conciliación:

1. Construcción estática de 220 operaciones desde atributos `[HttpGet|Post|Put|Patch|Delete]` en `Controllers`.
2. Lectura de la matriz completa 213/213 validada en Scalar-2B (`docs/api/scalar-validacion-openapi-real-2026-04-29.md`).
3. Normalización de rutas con y sin constraints (`{id:int}` → `{id}`) y diff de conjuntos.
4. Extracción de archivo/línea para cada operación faltante.

## 5) Veredicto de cierre documental Scalar-2B-A

1. La diferencia 220 vs 213 queda **conciliada y explicada**.
2. Las 7 operaciones faltantes están **identificadas de forma exacta**.
3. Para cobertura documental oficial de OpenAPI/Scalar, el universo válido de publicación actual es **213 operaciones**.
4. El inventario estático debe mantener una nota de excepción para estos 7 casos hasta resolver su política de publicación.
