# Auditoría integral frontend SPA Angular (ACH Interbank)

Fecha de auditoría: 2026-04-16.

> Actualización posterior: la arquitectura objetivo y el sistema visual enterprise propuesto se documentan en `docs/architecture/frontend-target-enterprise-spa-2026-04-16.md`.

## 1) Diagnóstico integral

El frontend tiene una **base funcional con intención de estandarización**, pero todavía presenta brechas relevantes para nivel enterprise bancario:

- Existe estructura `core / shared / features`, lazy loading y componentes standalone en múltiples módulos.
- Sin embargo, hay **inconsistencia transversal** en arquitectura de presentación, formularios, grids, idioma y manejo de acciones críticas.
- Se observa una convivencia parcial de buenas prácticas (tokens, `OnPush`, `app-page-header`, interceptor de loading) con patrones aún “legacy” (mezcla de `ngModel`/reactive, labels en inglés, estilos heterogéneos y lógicas de error duplicadas).

Resultado general: **nivel intermedio** (funcional en operación, no aún enterprise robusto).

---

## 2) Hallazgos por severidad

## CRÍTICO

### C1. Riesgo de doble submit en formularios críticos sin bloqueo explícito
- `UserFormComponent` ejecuta `save()` y dispara request, pero no implementa `isSubmitting`, `exhaustMap`, ni deshabilita botón de submit durante la llamada.
- En contexto bancario/backoffice esto habilita creación/actualización duplicada por doble click o latencia de red.

Evidencia:
- `user-form.component.ts`: `save()` sin guardas de concurrencia ni bandera de envío.
- `user-form.component.html`: botón Guardar sin `[disabled]` condicionado a estado de envío.

Impacto: duplicidad operativa y potencial inconsistencia de datos maestros.

---

## ALTO

### A1. Incumplimiento del requisito de idioma 100% español (mezcla ES/EN)
Se detectaron textos de UI y semántica visibles en inglés en múltiples pantallas.

Ejemplos:
- Breadcrumb `'Auth'`.
- Breadcrumb `'Dashboard'`.
- `aria-label="Breadcrumbs"`.
- Columna `'User agent'` (logs de navegación y autenticación).
- Label `'Email'`.
- Texto `(Active)` en ayuda de formulario transaccional.
- `'Chunk size'` en creación de lotes masivos.

Impacto: incumplimiento directo del lineamiento de idioma, deterioro UX y percepción de baja madurez.

### A2. Estrategia de formularios inconsistente (reactive + template-driven en el mismo ecosistema)
- `SharedModule` exporta simultáneamente `ReactiveFormsModule` y `FormsModule`.
- Módulos y componentes combinan `[(ngModel)]` con formularios reactivos sin una política única.

Impacto: mayor complejidad, más bugs de validación/estado, testing menos predecible.

### A3. Manejo de errores duplicado entre interceptor global y servicios feature
- Ya existe `ErrorInterceptor` con lógica 401/403/404/5xx.
- Servicios de dominio (ej. transacciones) vuelven a mapear errores por status con `catchError` repetitivo.

Impacto: acoplamiento, inconsistencia de mensajes y mantenimiento costoso.

### A4. Inconsistencia de rutas y semántica de navegación (ES/EN + naming mixto)
- Conviven rutas en inglés (`users`, `customers`, `reports`, `dashboard`) con otras en español (`integraciones`).
- La navegación y el routing no siguen una convención unificada.

Impacto: deuda semántica, menor trazabilidad, mayor costo de onboarding.

---

## MEDIO

### M1. Diseño de tablas heterogéneo y experiencia desigual por módulo
- Existe `app-table` reutilizable pero básico (sin buscador/ordenamiento por columna/filtros avanzados).
- Otros módulos usan `ag-grid` con capacidades superiores.
- Resultado: UX desigual entre pantallas, según módulo.

### M2. Selects nativos sin búsqueda en formularios de alta densidad
- En formularios transaccionales y administrativos aparecen múltiples `<select>` extensos sin autosuggest/search.
- Esto afecta velocidad operativa en backoffice.

### M3. Estilado no totalmente uniforme
- Conviven estilos globales/tokens con estilos inline en componentes standalone y mezcla de `.scss`/`.css`.
- No toda la UI parece pasar por un design system estricto y único.

### M4. Patrones de smart/dumb components no explícitos
- En varias pantallas, el componente concentra UI + orquestación + transformación de datos.
- Falta separación más estricta entre componentes contenedores y presentacionales.

### M5. Performance potencial en vistas de listas grandes
- Hay buenas prácticas (`OnPush` mayoritario), pero no universal.
- Persisten componentes sin `OnPush` en integración y otros puntos.
- Donde se usan tablas HTML simples no hay virtualización ni optimización avanzada.

---

## BAJO

### B1. Accesibilidad parcial (no sistemática)
- Existen aciertos puntuales de `aria-label` y estructura semántica.
- No se evidencia una estrategia integral A11y (teclado, contraste, patrones de foco, ARIA consistente por módulo).

### B2. Responsive con base sólida en layout principal, pero sin evidencia uniforme por feature
- `main-layout` tiene media queries y comportamiento móvil del sidebar.
- No se observó evidencia de criterios responsive homogéneos para todas las pantallas complejas (grids/formularios densos).

---

## 3) Qué refactorizar urgentemente

1. **Protección anti doble submit** en todas las acciones críticas (crear/actualizar/procesar lote/aprobar/rechazar):
   - bandera `isSubmitting` por caso,
   - deshabilitar CTA durante request,
   - estrategia `exhaustMap`/mutex UI para evitar reentradas.
2. **Plan de idioma único (español total)** con inventario de strings y corrección priorizada.
3. **Política única de formularios**: reactive forms como estándar enterprise; uso de `ngModel` solo por excepción documentada.
4. **Centralizar manejo de errores** (interceptor + catálogo de errores de negocio), eliminando duplicación por servicio.
5. **Normalizar routing/naming** (todo español o convención definida y establecida por arquitectura).

---

## 4) Qué se puede reutilizar

- Base de arquitectura modular (`core/shared/features`) y lazy loading.
- Interceptores globales (auth/loading/error) como punto de gobierno transversal.
- Tokens y utilidades de estilos (`_tokens.scss`, `_components.scss`, `_utilities.scss`).
- Componentes compartidos existentes: `app-page-header`, `app-table` (evolucionable), notificaciones, overlay de carga.
- Patrón `OnPush` ya adoptado en la mayoría de componentes (buena base para estandarizar al 100%).

---

## 5) Qué eliminar o rehacer completamente

- Rehacer la estrategia de textos hardcoded dispersos (migrar a capa central de i18n/recursos en español).
- Eliminar gradualmente tablas HTML básicas en pantallas de operación intensiva y unificar en un único patrón de grid enterprise.
- Rehacer formularios mixtos (ngModel + reactive) hacia plantillas reactivas homogéneas.

---

## 6) Patrones frontend faltantes

1. **Design System gobernado** (componentes, estados, variantes y tokens obligatorios).
2. **Arquitectura de presentación** explícita (container/presenter + view models).
3. **Anti-duplicate action pattern** transversal para operaciones críticas.
4. **Estrategia i18n enterprise** (aunque sea monolingüe español, centralizada y auditable).
5. **Policy-driven forms**: validación declarativa consistente + catálogo de mensajes.
6. **State management de dominio** para módulos complejos (transacciones/integraciones/reportes).
7. **Checklist A11y + QA visual** automatizable en CI.

---

## 7) Evaluación de calidad general

Clasificación: **INTERMEDIO**.

Justificación breve:
- Está por encima de “funcional básico” por modularidad, lazy loading, tokens y componentes compartidos.
- No alcanza “profesional/enterprise” por inconsistencias estructurales en UX/UI, idioma, formularios y controles críticos de operación.

---

## 8) Roadmap recomendado por fases

### Fase 0 (2 semanas) — Estabilización de riesgo operativo
- Anti doble submit en acciones críticas.
- Normalización inmediata de textos en inglés visibles al usuario (ALTO).
- Catálogo de mensajes UX de error/éxito estandarizado.

### Fase 1 (3-5 semanas) — Estandarización de experiencia base
- Definir y publicar Design System v1 (botones, inputs, selects, tablas, estados vacíos/error/loading).
- Unificar patrones de formulario en reactive forms.
- Estandarizar page shells y jerarquía visual.

### Fase 2 (4-6 semanas) — Arquitectura y mantenibilidad
- Refactor container/presenter en módulos de mayor complejidad.
- Consolidar estrategia de consumo API y manejo de errores.
- Homologar convenciones de rutas, naming y contratos UI.

### Fase 3 (4-8 semanas) — Escalabilidad y calidad enterprise
- Unificación de grids con capacidades enterprise (filtros, búsqueda, export, paginación consistente).
- Métricas de performance frontend (render, TTI, carga de módulos).
- Auditoría A11y completa + plan responsive por módulo.
- Automatización QA visual/regresión en pipeline.

---

## 9) Decisiones ejecutivas sugeridas

- Nombrar un **Frontend Tech Owner** con autoridad de arquitectura transversal.
- Establecer un **comité UX/arquitectura** para gobernar criterios de diseño enterprise.
- Crear Definition of Done obligatoria: idioma, accesibilidad mínima, estado loading/error/empty, anti doble submit, consistencia de componentes.
