# Scalar-2E — Validación OpenAPI real posterior a Scalar-2D

**Fecha (UTC):** 2026-04-30  
**Objetivo:** validar con evidencia técnica que la política implementada en Scalar-2D quedó aplicada en el OpenAPI real generado.

## 1) Resultado ejecutivo

- La solución compila correctamente en `Release`.
- La suite backend (`Cfa.ACHInterbank.Tests`) pasa completa: **408/408**.
- El OpenAPI real se generó y se descargó desde `http://127.0.0.1:5194/openapi/v1.json`.
- Las 7 rutas gobernadas por Scalar-2D **no aparecen** en el OpenAPI publicado.
- Total de operaciones publicadas en OpenAPI: **213** (sin regresión frente a Scalar-2B).
- Cobertura de campos:
  - `summary`: **213/213**.
  - `description`: **213/213**.
- Textos genéricos detectados por regla automática de control: **79 operaciones** (requiere hardening documental posterior).

## 2) Evidencia cuantitativa

| Validación | Resultado |
|---|---:|
| Operaciones publicadas OpenAPI | 213 |
| Rutas gobernadas ausentes (esperado 7) | 7 |
| Operaciones sin `summary` | 0 |
| Operaciones sin `description` | 0 |
| Operaciones con texto genérico (regla de control) | 79 |

## 3) Verificación puntual de las 7 rutas gobernadas

Las siguientes rutas quedaron fuera del OpenAPI real (estado esperado):

1. `GET /Servers`
2. `GET /Tests`
3. `GET /Tests/Prueba`
4. `GET /oauth2/jwks`
5. `GET /oauth2/TokenClientAssertions`
6. `POST /oauth2/client-assertion`
7. `POST /oauth2/Genearte-client-assertion`

## 4) Riesgos y observaciones

1. Se confirma cumplimiento de política de publicación para las 7 rutas sensibles/internas.
2. No se observan regresiones de conteo global de operaciones publicadas (se mantiene en 213).
3. Persisten descripciones/summaries genéricos en un subconjunto relevante (79), por lo que no corresponde declarar cierre de calidad documental total.
4. Durante la ejecución local de la API se observó indisponibilidad de PostgreSQL (`localhost:5432`) para procesos de fondo, sin impedir la descarga del OpenAPI.

## 5) Veredicto de cierre Scalar-2E

**Cierre técnico parcial exitoso** para objetivo de gobierno de publicación:
- Política Scalar-2D aplicada correctamente en OpenAPI real (7/7 rutas fuera).
- Compilación y pruebas backend completas correctas.

**Pendiente de calidad documental**:
- Ejecutar una fase de saneamiento para eliminar textos genéricos detectados por regla automática.
