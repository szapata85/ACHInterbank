# Evidencia Playwright contra Docker

Objetivo: `http://localhost:743`, con API real `http://localhost:843`, autenticación real y sin interceptar respuestas.

La suite cubre menú, navegación, filtros, código de respuesta, paginación, archivo exacto, aceptación y devolución, error técnico y resoluciones 1440×900, 1280×720, 768×1024 y 390×844. Las capturas sanitizadas se guardan en `evidencias/`.

## Resultados

| Proveedor | Suite Fase 4 | Suite de permisos | Fallidas | Omitidas |
|---|---:|---:|---:|---:|
| SQL Server | 5/5 | 4/4 | 0 | 0 |
| PostgreSQL | 5/5 | 4/4 | 0 | 0 |

Las corridas finales usaron explícitamente la URL Docker de cada ambiente, backend y base de datos reales, y un solo worker. No se utilizaron mocks de navegador. Finalizaron sin `pageerror`, `console.error` inesperado, respuestas 5xx/502, cargas permanentes ni desbordamiento horizontal global.

Las capturas finales confirman textos en español, cuenta enmascarada, menú localizado, escritorio, tableta y móvil. No contienen tokens, XML ni datos personales.
