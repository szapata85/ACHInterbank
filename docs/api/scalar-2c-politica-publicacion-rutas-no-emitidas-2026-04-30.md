# Política de publicación para rutas no emitidas en OpenAPI — Scalar-2C

**Fecha (UTC):** 2026-04-30  
**Alcance:** definición de gobierno documental para 7 rutas existentes en código y no emitidas en el OpenAPI real validado en Scalar-2B.

## 1) Contexto consolidado

- Inventario estático previo: **220** operaciones REST.
- OpenAPI real publicado por la API en ejecución: **213** operaciones REST.
- Brecha conciliada en Scalar-2B-A: **7** operaciones en código no emitidas.

Referencias base:
- `docs/api/scalar-2b-a-conciliacion-220-vs-213-2026-04-29.md`.
- `docs/api/scalar-validacion-openapi-real-2026-04-29.md`.

## 2) Criterios de decisión aplicados

Para cada ruta se aplicaron criterios de gobierno API y seguridad bancaria:

1. **Exposición externa real requerida:** si la ruta debe formar parte del contrato público de integración.
2. **Riesgo de seguridad:** presencia de generación/validación de artefactos OAuth2, material criptográfico o superficies de prueba.
3. **Madurez contractual:** consistencia de nombre, semántica y códigos HTTP para consumidores externos.
4. **Trazabilidad y auditoría:** si la operación exige controles previos antes de publicarse documentalmente.
5. **Uso operativo interno:** si la ruta es utilitaria de diagnóstico/prueba y no contractual.

## 3) Matriz de política recomendada por ruta (decisión sin implementación)

| # | Método | Ruta | Controlador | Política recomendada | Justificación técnica y de seguridad | Prioridad |
|---:|---|---|---|---|---|---|
| 1 | GET | `/Servers` | `ServersController` | **3. Mantener como ruta interna no documentada públicamente** | Es una ruta utilitaria de proxy/balanceo que consume destino dinámico (`servicesWCF`) y devuelve contenido crudo. Publicarla como contrato externo aumenta superficie de reconocimiento de infraestructura sin valor funcional para clientes de negocio. | Alta |
| 2 | GET | `/Tests` | `TestsController` | **7. Mantener fuera por tratarse de prueba/desarrollo** | Es una ruta de prueba técnica (incluye texto explícito de prueba y patrón de ejercicio de componentes). No representa capacidad de negocio ACH/CENIT/NACHA-M para terceros. | Alta |
| 3 | GET | `/Tests/Prueba` | `TestsController` | **7. Mantener fuera por tratarse de prueba/desarrollo** | Endpoint de verificación mínima (`Ok`) sin semántica de dominio ni contrato de integración. Debe permanecer fuera de catálogo público. | Alta |
| 4 | GET | `/oauth2/jwks` | `JwksController` | **5. Revisar seguridad antes de decidir** | Aunque `jwks` suele ser endpoint público de confianza, aquí convive con otros endpoints sensibles en el mismo controller con `AllowAnonymous`. Requiere revisión formal de controles de publicación, hardening y límites de exposición antes de incorporarlo a Scalar. | Crítica |
| 5 | GET | `/oauth2/TokenClientAssertions` | `JwksController` | **2. Ocultar explícitamente de OpenAPI** | Genera artefacto de client assertion/token y hoy está anónimo. Documentarlo públicamente sin rediseño de seguridad y segmentación de uso interno contradice principio de mínimo privilegio. | Crítica |
| 6 | POST | `/oauth2/client-assertion` | `JwksController` | **5. Revisar seguridad antes de decidir** | Valida assertion suministrada por cliente; por naturaleza es superficie de autenticación. Antes de decidir publicación se requiere definición de anti-abuso, límites de tasa, observabilidad y política de errores no reveladores. | Crítica |
| 7 | POST | `/oauth2/Genearte-client-assertion` | `JwksController` | **6. Corregir ruta/nombre antes de publicar** | La ruta contiene error ortográfico (`Genearte`). Antes de cualquier publicación debe corregirse nomenclatura y definir plan de compatibilidad/obsolescencia para evitar deuda contractual. Además requiere revisión de seguridad por generación de assertion. | Crítica |

## 4) Veredicto de gobierno Scalar-2C

1. **No publicar todavía** ninguna de las 7 rutas en Scalar/OpenAPI en esta fase.
2. Clasificar `/Servers` y `/Tests*` como **internas/no contractuales** para catálogo externo.
3. Tratar `/oauth2/*` como **superficie sensible** sujeta a revisión de seguridad previa a decisión documental.
4. Tratar `/oauth2/Genearte-client-assertion` como **no elegible para publicación** hasta corregir nombre de ruta y gobernar compatibilidad.
5. Mantener la discrepancia 220 vs 213 como **estado controlado y justificado** hasta ejecutar fase de decisión implementable.

## 5) Backlog recomendado (sin cambios funcionales en esta fase)

1. Emitir evaluación de seguridad específica para `JwksController` (autenticación, abuso, trazabilidad, política de errores).
2. Definir si `/oauth2/jwks` será contrato externo oficial o endpoint interno de soporte.
3. Diseñar plan de corrección nominal para `Genearte-client-assertion` con estrategia de transición.
4. Etiquetar formalmente rutas de prueba (`/Tests*`) y utilitarias (`/Servers`) como no contractuales en gobierno de APIs.
5. Solo después de ese cierre, ejecutar fase técnica de publicación/ocultamiento explícito en OpenAPI.

## 6) Evidencia de código revisada para sustentar decisiones

- `src/Cfa.ACHInterbank.Api/Controllers/ServersController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/TestsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs`

